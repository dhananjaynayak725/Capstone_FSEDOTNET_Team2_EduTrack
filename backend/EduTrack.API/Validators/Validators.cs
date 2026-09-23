using EduTrack.API.DTOs;
using FluentValidation;

namespace EduTrack.API.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(150).WithMessage("Full name must be 150 characters or fewer.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Enter a valid email address.")
            .MaximumLength(256).WithMessage("Email must be 256 characters or fewer.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .MaximumLength(72).WithMessage("Password must be 72 characters or fewer.")
            .Matches("[A-Z]").WithMessage("Password must contain an uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain a lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain a number.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class CourseUpsertRequestValidator : AbstractValidator<CourseUpsertRequest>
{
    public CourseUpsertRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must be 200 characters or fewer.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must be 2000 characters or fewer.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .MaximumLength(100).WithMessage("Category must be 100 characters or fewer.");

        RuleFor(x => x.InstructorId)
            .GreaterThan(0).WithMessage("Choose an instructor.");
    }
}

public class CreateEnrollmentRequestValidator : AbstractValidator<CreateEnrollmentRequest>
{
    public CreateEnrollmentRequestValidator()
    {
        RuleFor(x => x.CourseId).GreaterThan(0).WithMessage("Course id is required.");
    }
}

public class UpdateEnrollmentStatusRequestValidator : AbstractValidator<UpdateEnrollmentStatusRequest>
{
    public UpdateEnrollmentStatusRequestValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status must be Active, Completed or Dropped.");

        RuleFor(x => x.Grade)
            .Must(grade => grade is null || (grade >= 0m && grade <= 100m))
            .WithMessage("Grade must be between 0 and 100.");
    }
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(150).WithMessage("Full name must be 150 characters or fewer.");
    }
}
