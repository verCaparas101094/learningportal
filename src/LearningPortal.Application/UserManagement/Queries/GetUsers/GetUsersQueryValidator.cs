using FluentValidation;

namespace LearningPortal.Application.UserManagement.Queries.GetUsers;

/// <summary>Validates administrator user-list pagination.</summary>
public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    /// <summary>Initializes user-list validation rules.</summary>
    public GetUsersQueryValidator()
    {
        RuleFor(query => query.PageNumber)
            .GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);
        RuleFor(query => query.Search)
            .MaximumLength(256);
    }
}
