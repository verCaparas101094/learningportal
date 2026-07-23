#pragma warning disable CS1591

using LearningPortal.Application.Authorization;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Authentication;

public sealed class DevelopmentDataSeederTests
{
    [Fact]
    public void DevelopmentInitialization_DefaultsToDisabled()
    {
        Assert.False(new DevelopmentSeedOptions().Enabled);
        Assert.False(new DatabaseInitializationOptions().ApplyMigrations);
    }

    [Fact]
    public async Task SeedAsync_WhenRepeated_CreatesOneValidLearningScenario()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in ApplicationRoles.All)
        {
            Assert.True((await roleManager.CreateAsync(
                new IdentityRole<Guid>(role))).Succeeded);
        }

        var seeder = scope.ServiceProvider.GetRequiredService<IDevelopmentDataSeeder>();
        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();
        await seeder.SeedAsync();
        var administrator = await userManager.FindByEmailAsync(
            "admin@learningportal.local");
        Assert.NotNull(administrator);
        var changeResult = await userManager.ChangePasswordAsync(
            administrator,
            "Admin123!",
            "LocallyChangedPassword!123");
        Assert.True(changeResult.Succeeded);

        await seeder.SeedAsync();

        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(3, await userManager.Users.CountAsync());
        Assert.Single(await context.Courses.ToListAsync());
        Assert.Equal(4, await context.Lessons.CountAsync());
        Assert.Single(await context.Quizzes.ToListAsync());
        Assert.Equal(3, await context.QuizQuestions.CountAsync());
        Assert.Single(await context.Enrollments.ToListAsync());
        Assert.Single(await context.InstructorEligibility.ToListAsync());
        var instructor = await userManager.FindByEmailAsync(
            "instructor@learningportal.local");
        var student = await userManager.FindByEmailAsync(
            "student@learningportal.local");
        Assert.NotNull(instructor);
        Assert.NotNull(student);
        Assert.True(await userManager.IsInRoleAsync(
            instructor, ApplicationRoles.Instructor));
        Assert.True(await userManager.IsInRoleAsync(
            student, ApplicationRoles.Student));
        Assert.True(await userManager.CheckPasswordAsync(
            administrator,
            "LocallyChangedPassword!123"));
        Assert.False(await userManager.CheckPasswordAsync(
            administrator,
            "Admin123!"));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        var databaseName = $"DevelopmentSeedTests-{Guid.CreateVersion7()}";
        services.AddLogging();
        services.AddDataProtection();
        services.AddDbContext<ApplicationDbContext>(
            options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        services.AddScoped<IDevelopmentDataSeeder, DevelopmentDataSeeder>();
        return services.BuildServiceProvider(validateScopes: true);
    }
}
