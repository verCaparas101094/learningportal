using FluentValidation;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Behaviors;
using LearningPortal.Application.Authentication.Commands.Login;
using LearningPortal.Application.Authentication.Commands.Refresh;
using LearningPortal.Application.Authentication.Commands.Revoke;
using LearningPortal.Application.Courses.Commands.ArchiveCourse;
using LearningPortal.Application.Courses.Commands.CreateCourse;
using LearningPortal.Application.Courses.Commands.DeleteCourse;
using LearningPortal.Application.Courses.Commands.PublishCourse;
using LearningPortal.Application.Courses.Commands.UpdateCourse;
using LearningPortal.Application.Courses.Queries.GetCourseById;
using LearningPortal.Application.Courses.Queries.GetCourses;
using LearningPortal.Application.Messaging;
using LearningPortal.Application.UserManagement.Commands.AssignUserRole;
using LearningPortal.Application.UserManagement.Commands.SetUserEnabled;
using LearningPortal.Application.UserManagement.Queries.GetUserById;
using LearningPortal.Application.UserManagement.Queries.GetUsers;
using LearningPortal.Shared.Courses;
using LearningPortal.Shared.Identity;
using LearningPortal.Shared.Results;
using LearningPortal.Shared.UserManagement;
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
        services.AddScoped<ICommandHandler<CreateCourseCommand, Result<CourseResponse>>, CreateCourseCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateCourseCommand, Result<CourseResponse>>, UpdateCourseCommandHandler>();
        services.AddScoped<ICommandHandler<PublishCourseCommand, Result<CourseResponse>>, PublishCourseCommandHandler>();
        services.AddScoped<ICommandHandler<ArchiveCourseCommand, Result<CourseResponse>>, ArchiveCourseCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteCourseCommand, Result<bool>>, DeleteCourseCommandHandler>();
        services.AddScoped<IQueryHandler<GetCoursesQuery, Result<PagedCoursesResponse>>, GetCoursesQueryHandler>();
        services.AddScoped<IQueryHandler<GetCourseByIdQuery, Result<CourseResponse>>, GetCourseByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetUsersQuery, Result<PagedUsersResponse>>, GetUsersQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserByIdQuery, Result<UserResponse>>, GetUserByIdQueryHandler>();
        services.AddScoped<ICommandHandler<SetUserEnabledCommand, Result<UserResponse>>, SetUserEnabledCommandHandler>();
        services.AddScoped<ICommandHandler<AssignUserRoleCommand, Result<UserResponse>>, AssignUserRoleCommandHandler>();

        return services;
    }
}
