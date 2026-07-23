using FluentValidation;

namespace LearningPortal.Application.Enrollments.Queries.GetPublishedCourseCatalog;

/// <summary>Validates catalog paging and search.</summary>
public sealed class GetPublishedCourseCatalogQueryValidator : AbstractValidator<GetPublishedCourseCatalogQuery>
{
    /// <summary>Creates validation rules.</summary>
    public GetPublishedCourseCatalogQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Search).MaximumLength(200);
    }
}
