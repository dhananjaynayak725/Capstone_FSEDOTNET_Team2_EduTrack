using EduTrack.API.DTOs;
using EduTrack.API.Models;
using EduTrack.API.Validators;

namespace EduTrack.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    private static RegisterRequest Valid() => new() { FullName = "Asha Rao", Email = "asha@example.com", Password = "Passw0rd" };

    [Fact]
    public void ValidRequest_Passes() => Assert.True(_validator.Validate(Valid()).IsValid);

    [Theory]
    [InlineData("")]
    [InlineData("short1A")]           // too short
    [InlineData("alllowercase1")]     // no uppercase
    [InlineData("ALLUPPERCASE1")]     // no lowercase
    [InlineData("NoDigitsHere")]      // no number
    public void WeakPassword_Fails(string password)
    {
        var request = Valid();
        request.Password = password;

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    public void InvalidEmail_Fails(string email)
    {
        var request = Valid();
        request.Email = email;

        Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void MissingName_Fails()
    {
        var request = Valid();
        request.FullName = " ";

        Assert.Contains(_validator.Validate(request).Errors, e => e.PropertyName == nameof(RegisterRequest.FullName));
    }
}

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void BothFieldsRequired()
    {
        var result = _validator.Validate(new LoginRequest());

        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public void ValidLogin_Passes() =>
        Assert.True(_validator.Validate(new LoginRequest { Email = "a@b.com", Password = "x" }).IsValid);
}

public class CourseUpsertRequestValidatorTests
{
    private readonly CourseUpsertRequestValidator _validator = new();

    private static CourseUpsertRequest Valid() => new() { Title = "Intro", Description = "", Category = "Programming", InstructorId = 2 };

    [Fact]
    public void ValidRequest_Passes_EvenWithEmptyDescription() => Assert.True(_validator.Validate(Valid()).IsValid);

    [Fact]
    public void MissingTitleCategoryAndInstructor_AllFail()
    {
        var result = _validator.Validate(new CourseUpsertRequest());

        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CourseUpsertRequest.Title));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CourseUpsertRequest.Category));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CourseUpsertRequest.InstructorId));
    }

    [Fact]
    public void TooLongTitle_Fails()
    {
        var request = Valid();
        request.Title = new string('x', 201);

        Assert.False(_validator.Validate(request).IsValid);
    }

    [Fact]
    public void TooLongDescription_Fails()
    {
        var request = Valid();
        request.Description = new string('x', 2001);

        Assert.False(_validator.Validate(request).IsValid);
    }
}

public class EnrollmentValidatorTests
{
    [Fact]
    public void CreateEnrollment_RequiresPositiveCourseId()
    {
        var validator = new CreateEnrollmentRequestValidator();

        Assert.False(validator.Validate(new CreateEnrollmentRequest { CourseId = 0 }).IsValid);
        Assert.True(validator.Validate(new CreateEnrollmentRequest { CourseId = 3 }).IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(55.5)]
    [InlineData(100)]
    public void UpdateStatus_GradeInRange_Passes(double grade)
    {
        var request = new UpdateEnrollmentStatusRequest { Status = EnrollmentStatus.Completed, Grade = (decimal)grade };

        Assert.True(new UpdateEnrollmentStatusRequestValidator().Validate(request).IsValid);
    }

    [Theory]
    [InlineData(-0.5)]
    [InlineData(100.5)]
    public void UpdateStatus_GradeOutOfRange_Fails(double grade)
    {
        var request = new UpdateEnrollmentStatusRequest { Status = EnrollmentStatus.Completed, Grade = (decimal)grade };

        Assert.False(new UpdateEnrollmentStatusRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void UpdateStatus_NullGrade_Passes()
    {
        var request = new UpdateEnrollmentStatusRequest { Status = EnrollmentStatus.Dropped, Grade = null };

        Assert.True(new UpdateEnrollmentStatusRequestValidator().Validate(request).IsValid);
    }

    [Fact]
    public void UpdateStatus_UnknownEnumValue_Fails()
    {
        var request = new UpdateEnrollmentStatusRequest { Status = (EnrollmentStatus)42 };

        Assert.False(new UpdateEnrollmentStatusRequestValidator().Validate(request).IsValid);
    }
}

public class UpdateUserRequestValidatorTests
{
    [Fact]
    public void NameIsRequired()
    {
        var validator = new UpdateUserRequestValidator();

        Assert.False(validator.Validate(new UpdateUserRequest { FullName = "" }).IsValid);
        Assert.True(validator.Validate(new UpdateUserRequest { FullName = "Asha" }).IsValid);
    }
}
