#pragma warning disable CS1591
using LearningPortal.Domain.Certificates;
using LearningPortal.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LearningPortal.Infrastructure.Persistence.Repositories;

public sealed class CertificateRepository(ApplicationDbContext context) : ICertificateRepository
{
    public Task<Certificate?> GetByIdAsync(Guid id, bool readOnly, CancellationToken ct = default)
    { var query=context.Certificates.AsQueryable(); return (readOnly?query.AsNoTracking():query).SingleOrDefaultAsync(x=>x.Id==id,ct); }
    public Task<Certificate?> GetByEnrollmentAsync(Guid id, bool readOnly, CancellationToken ct = default)
    { var query=context.Certificates.AsQueryable(); return (readOnly?query.AsNoTracking():query).SingleOrDefaultAsync(x=>x.EnrollmentId==id,ct); }
    public Task<Certificate?> GetByVerificationCodeAsync(string code, CancellationToken ct = default) =>
        context.Certificates.AsNoTracking().SingleOrDefaultAsync(x=>x.VerificationCode==code,ct);
    public async Task<IReadOnlyList<Certificate>> GetByStudentAsync(Guid id,CancellationToken ct=default)=>
        await context.Certificates.AsNoTracking().Where(x=>x.StudentId==id).OrderByDescending(x=>x.IssuedAtUtc).ToListAsync(ct);
    public async Task<IReadOnlyList<Certificate>> GetByCourseAsync(Guid id,CancellationToken ct=default)=>
        await context.Certificates.AsNoTracking().Where(x=>x.CourseId==id).OrderByDescending(x=>x.IssuedAtUtc).ToListAsync(ct);
    public async Task<long> GetNextSequenceAsync(CancellationToken ct=default)
    {
        var connection=context.Database.GetDbConnection(); var close=connection.State!=System.Data.ConnectionState.Open;
        if(close) await connection.OpenAsync(ct);
        try { await using var command=connection.CreateCommand(); command.CommandText="SELECT NEXT VALUE FOR CertificateNumberSequence"; return Convert.ToInt64(await command.ExecuteScalarAsync(ct),System.Globalization.CultureInfo.InvariantCulture); }
        finally { if(close) await connection.CloseAsync(); }
    }
    public Task AddAsync(Certificate value,CancellationToken ct=default)=>context.Certificates.AddAsync(value,ct).AsTask();
}
