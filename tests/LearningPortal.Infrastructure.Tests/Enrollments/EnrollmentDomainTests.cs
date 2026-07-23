using LearningPortal.Domain.Enrollments;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Enrollments;

/// <summary>Verifies enrollment lifecycle invariants.</summary>
public sealed class EnrollmentDomainTests
{
    private static readonly DateTimeOffset EnrolledAt = new(2026, 7, 23, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Verifies initial state.</summary>
    [Fact]
    public void Create_InitializesEnrolledState()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), EnrolledAt);

        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);
        Assert.Equal(EnrolledAt, enrollment.EnrolledAtUtc);
        Assert.Null(enrollment.StartedAtUtc);
    }

    /// <summary>Verifies starting.</summary>
    [Fact]
    public void Start_FromEnrolled_SucceedsAndSetsTimestamp()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), EnrolledAt);
        var startedAt = EnrolledAt.AddMinutes(1);

        Assert.True(enrollment.TryStart(startedAt));
        Assert.Equal(EnrollmentStatus.InProgress, enrollment.Status);
        Assert.Equal(startedAt, enrollment.StartedAtUtc);
    }

    /// <summary>Verifies completion from active states.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Complete_FromActiveState_Succeeds(bool startFirst)
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), EnrolledAt);
        if (startFirst) Assert.True(enrollment.TryStart(EnrolledAt.AddMinutes(1)));

        Assert.True(enrollment.TryComplete(EnrolledAt.AddMinutes(2)));
        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
        Assert.NotNull(enrollment.CompletedAtUtc);
    }

    /// <summary>Verifies withdrawal from active states.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Withdraw_FromActiveState_Succeeds(bool startFirst)
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), EnrolledAt);
        if (startFirst) Assert.True(enrollment.TryStart(EnrolledAt.AddMinutes(1)));

        Assert.True(enrollment.TryWithdraw(EnrolledAt.AddMinutes(2)));
        Assert.Equal(EnrollmentStatus.Withdrawn, enrollment.Status);
    }

    /// <summary>Verifies completed enrollment immutability.</summary>
    [Fact]
    public void Completed_CannotBeWithdrawn()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), EnrolledAt);
        Assert.True(enrollment.TryComplete(EnrolledAt.AddMinutes(1)));

        Assert.False(enrollment.TryWithdraw(EnrolledAt.AddMinutes(2)));
        Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
    }

    /// <summary>Verifies withdrawn enrollments cannot restart.</summary>
    [Fact]
    public void Withdrawn_CannotBeRestarted()
    {
        var enrollment = Enrollment.Create(Guid.NewGuid(), Guid.NewGuid(), EnrolledAt);
        Assert.True(enrollment.TryWithdraw(EnrolledAt.AddMinutes(1)));

        Assert.False(enrollment.TryStart(EnrolledAt.AddMinutes(2)));
        Assert.Equal(EnrollmentStatus.Withdrawn, enrollment.Status);
    }
}
