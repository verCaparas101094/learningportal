using FluentValidation;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Courses.Commands.CreateCourse;
using LearningPortal.Application.Courses.Queries.GetCourses;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Results;
using Microsoft.Extensions.DependencyInjection;

namespace LearningPortal.Application;

/// <summary>Registers application use cases and validators.</summary>
public static class DependencyInjection
{
    /// <summary>Adds application-layer services.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateCourseCommandValidator>(ServiceLifetime.Scoped);
        services.AddScoped<ICommandHandler<CreateCourseCommand, Result<CourseDto>>, CreateCourseCommandHandler>();
        services.AddScoped<IQueryHandler<GetCoursesQuery, Result<IReadOnlyList<CourseDto>>>, GetCoursesQueryHandler>();

        return services;
    }
}
