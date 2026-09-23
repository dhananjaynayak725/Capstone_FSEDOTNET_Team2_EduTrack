using System.Text;
using System.Text.Json.Serialization;
using EduTrack.API.Common;
using EduTrack.API.Data;
using EduTrack.API.Filters;
using EduTrack.API.Mappings;
using EduTrack.API.Middleware;
using EduTrack.API.Repositories;
using EduTrack.API.Services;
using EduTrack.API.Settings;
using EduTrack.API.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuration ----------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
builder.Services.Configure<JwtOptions>(jwtSection);
var jwt = jwtSection.Get<JwtOptions>() ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
{
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");
}

// ---------- Database ----------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Layers: repositories -> services ----------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();

builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<ITokenDenylist, InMemoryTokenDenylist>();

builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

// ---------- Web API ----------
builder.Services
    .AddControllers(options =>
    {
        // FluentValidation owns validation messages, so don't add implicit [Required] for non-nullable strings.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        options.Filters.Add<ValidationFilter>();
    })
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Malformed JSON / unknown enum values also use the standard error format.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "The value is invalid." : e.ErrorMessage)
                    .ToArray());

        var body = ErrorResponse.Create(context.HttpContext, "Validation Failed", "The request body or parameters are invalid.", errors);
        return new BadRequestObjectResult(body);
    };
});

// ---------- Authentication ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false; // keep claim names exactly as issued ("sub", "role", ...)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var denylist = context.HttpContext.RequestServices.GetRequiredService<ITokenDenylist>();
                var tokenId = context.Principal?.FindFirst("jti")?.Value;
                if (tokenId is not null && denylist.IsRevoked(tokenId))
                {
                    context.Fail("This token has been revoked.");
                }

                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// ---------- CORS (needed only when the UI calls the API directly instead of via the Vite proxy) ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));

// ---------- Swagger ----------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "EduTrack API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the JWT returned by /api/auth/login (without the 'Bearer ' prefix)."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// ---------- Database bootstrap ----------
if (app.Configuration.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    if (db.Database.GetMigrations().Any())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        // No migrations generated yet: create the schema straight from the model so the app still runs.
        // Once you run `dotnet ef migrations add InitialCreate`, migrations take over (see README).
        logger.LogWarning("No EF Core migrations found. Creating the schema with EnsureCreated().");
        await db.Database.EnsureCreatedAsync();
    }

    await DbSeeder.SeedAsync(db, app.Configuration, logger);
}

// ---------- HTTP pipeline ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Gives 401/403/404/405 (which have no body by default) the same JSON error shape.
app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    var (error, message) = response.StatusCode switch
    {
        StatusCodes.Status401Unauthorized => ("Unauthorized", "Sign in to continue. The token is missing, invalid or expired."),
        StatusCodes.Status403Forbidden => ("Forbidden", "You do not have permission to perform this action."),
        StatusCodes.Status404NotFound => ("Not Found", "The requested resource was not found."),
        _ => (ReasonPhrases.GetReasonPhrase(response.StatusCode), "The request could not be completed.")
    };

    await response.WriteAsJsonAsync(ErrorResponse.Create(context.HttpContext, error, message));
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();
app.MapControllers();

app.Run();
