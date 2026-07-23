using FluentValidation;

namespace LearningPortal.Application.Enrollments.Commands.WithdrawEnrollment;

/// <summary>Validates withdrawal input.</summary>
public sealed class WithdrawEnrollmentCommandValidator : AbstractValidator<WithdrawEnrollmentCommand>
{
    /// <summary>Creates validation rules.</summary>
    public WithdrawEnrollmentCommandValidator()
    {
        RuleFor(x => x.EnrollmentId).NotEmpty();
        RuleFor(x => x.RowVersion).NotEmpty().Must(IsBase64).WithMessage("RowVersion must be valid Base64.");
    }

    private static bool IsBase64(string value)
    {
        Span<byte> buffer = stackalloc byte[value.Length];
        return Convert.TryFromBase64String(value, buffer, out _);
    }
}
