using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Repositories;
using LearningPortal.Infrastructure.Identity;
using LearningPortal.Infrastructure.Persistence.Extensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence;

/// <summary>Provides the EF Core unit of work for domain data and ASP.NET Identity.</summary>
public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options), IUnitOfWork
{
    /// <summary>Gets the course data set.</summary>
    public DbSet<Course> Courses => Set<Course>();

    /// <summary>Gets the persisted hashed refresh tokens.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        builder.ConfigureDomainFoundation();
    }
}
