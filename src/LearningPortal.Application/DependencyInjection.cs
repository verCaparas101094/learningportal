using FluentValidation;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Behaviors;
using LearningPortal.Application.Authentication.Commands.Login;
using LearningPortal.Application.Authentication.Commands.Refresh;
using LearningPortal.Application.Authentication.Commands.Revoke;
using LearningPortal.Application.Courses.Commands.CreateCourse;
using LearningPortal.Application.Courses.Queries.GetCourses;
using LearningPortal.Application.Messaging;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Identity;
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
        services.AddScoped<ICommandDispatcher, CommandDispatcher>();
        services.AddScoped(typeof(ICommandPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<ICommandHandler<LoginCommand, Result<AuthenticationResponse>>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, Result<AuthenticationResponse>>, RefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<RevokeRefreshTokenCommand, Result<bool>>, RevokeRefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<CreateCourseCommand, Result<CourseDto>>, CreateCourseCommandHandler>();
        services.AddScoped<IQueryHandler<GetCoursesQuery, Result<IReadOnlyList<CourseDto>>>, GetCoursesQueryHandler>();

        return services;
    }
}
