#pragma warning disable CS1591
using System.Net;
using System.Net.Http.Json;
using System.Text;
using LearningPortal.Blazor.Services;
using LearningPortal.Domain.Certificates;
using LearningPortal.Infrastructure.Certificates;
using LearningPortal.Infrastructure.Persistence;
using LearningPortal.Infrastructure.Persistence.Repositories;
using LearningPortal.Shared.Certificates;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LearningPortal.Infrastructure.Tests.Certificates;

public sealed class CertificateTests
{
    [Fact]
    public void Issue_SnapshotsTrustedValues()
    {
        var value=Create();
        Assert.Equal("Employee Name",value.StudentDisplayName); Assert.Equal("Secure Coding",value.CourseTitle);
        Assert.Equal(CertificateStatus.Active,value.Status); Assert.Null(value.RevokedAtUtc);
    }
    [Fact]
    public void Revoke_IsIdempotentAndCannotReactivate()
    {
        var value=Create(); Assert.True(value.TryRevoke("Incorrect completion record",UtcNow));
        var revoked=value.RevokedAtUtc; Assert.True(value.TryRevoke("Repeated request",UtcNow.AddMinutes(1)));
        Assert.Equal(CertificateStatus.Revoked,value.Status); Assert.Equal(revoked,value.RevokedAtUtc);
    }
    [Fact]
    public void Revoke_RequiresReason()=>Assert.False(Create().TryRevoke(" ",UtcNow));
    [Fact]
    public void Pdf_IsValidAndContainsActiveCertificateText()
    {
        var bytes=new CertificatePdfGenerator().Generate(Create(),"https://portal/verify/code");
        Assert.StartsWith("%PDF-",Encoding.ASCII.GetString(bytes)); Assert.Contains("CERTIFICATE OF COMPLETION",Encoding.ASCII.GetString(bytes));
    }
    [Fact]
    public void RevokedPdf_ContainsWarning()
    {
        var value=Create();value.TryRevoke("Reason",UtcNow);
        Assert.Contains("REVOKED",Encoding.ASCII.GetString(new CertificatePdfGenerator().Generate(value,"https://portal/verify/code")));
    }
    [Fact]
    public void PublicContract_DoesNotExposePrivateIdsOrEmail()
    {
        var names=typeof(CertificateVerificationResponse).GetProperties().Select(x=>x.Name).ToArray();
        Assert.DoesNotContain("StudentId",names);Assert.DoesNotContain("EnrollmentId",names);Assert.DoesNotContain("Email",names);
    }
    [Fact]
    public void Model_HasRequiredUniqueIndexesAndSequence()
    {
        using var context=Context();var entity=context.Model.FindEntityType(typeof(Certificate))!;
        foreach(var property in new[]{nameof(Certificate.CertificateNumber),nameof(Certificate.VerificationCode),nameof(Certificate.EnrollmentId)})
            Assert.Contains(entity.GetIndexes(),index=>index.IsUnique&&index.Properties.Single().Name==property);
        Assert.NotNull(context.Model.FindSequence("CertificateNumberSequence"));
    }
    [Fact]
    public async Task ReadOnlyRepositoryQueries_DoNotTrack()
    {
        await using var context=Context();var repository=new CertificateRepository(context);
        await repository.GetByStudentAsync(Guid.NewGuid());Assert.Empty(context.ChangeTracker.Entries());
    }
    [Fact]
    public async Task Client_UsesIssueDownloadAndPublicVerificationRoutes()
    {
        var enrollment=Guid.NewGuid();var certificate=Guid.NewGuid();
        var handler=new Handler(request=>request.RequestUri!.AbsolutePath.EndsWith("/download",StringComparison.Ordinal)
            ?new(HttpStatusCode.OK){Content=new ByteArrayContent("%PDF"u8.ToArray())}
            :request.RequestUri.AbsolutePath.Contains("/verify/",StringComparison.Ordinal)
                ?Json(new CertificateVerificationResponse("ERNI-2026-1","Name","Course","Skill",null,UtcNow,UtcNow,"Active",null))
                :Json(new IssueCertificateResponse(ToResponse(Create()),false)));
        var client=new LearningPortalApiClient(new HttpClient(handler){BaseAddress=new("https://localhost/")});
        await client.IssueCertificateAsync(enrollment);Assert.Equal($"/api/enrollments/{enrollment:D}/certificate",handler.Uri!.AbsolutePath);
        Assert.StartsWith("%PDF",Encoding.ASCII.GetString(await client.DownloadCertificateAsync(certificate)));
        await client.VerifyCertificateAsync("safe-code");Assert.Equal("/api/public/certificates/verify/safe-code",handler.Uri!.AbsolutePath);
    }

    private static readonly DateTimeOffset UtcNow=new(2026,7,23,12,0,0,TimeSpan.Zero);
    private static Certificate Create()=>Certificate.Issue("ERNI-2026-00000001",Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid(),
        "Employee Name","Secure Coding","Security","Instructor",UtcNow.AddDays(-1),UtcNow,"random-verification-code");
    private static CertificateResponse ToResponse(Certificate x)=>new(x.Id,x.EnrollmentId,x.CertificateNumber,x.VerificationCode,x.StudentDisplayName,x.CourseTitle,x.CourseCategory,x.InstructorDisplayName,x.CompletedAtUtc,x.IssuedAtUtc,x.Status.ToString(),x.RevokedAtUtc,x.RevocationReason);
    private static ApplicationDbContext Context()=>new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static HttpResponseMessage Json<T>(T value)=>new(HttpStatusCode.OK){Content=JsonContent.Create(value)};
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> response):HttpMessageHandler
    {public Uri? Uri{get;private set;}protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct){Uri=request.RequestUri;return Task.FromResult(response(request));}}
}
