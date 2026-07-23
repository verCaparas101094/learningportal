using LearningPortal.Api.Extensions;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Authorization;
using LearningPortal.Application.Certificates;
using LearningPortal.Shared.Certificates;
using LearningPortal.Shared.Results;

namespace LearningPortal.Api.Endpoints;

/// <summary>Maps certificate issuance, download, administration, and verification.</summary>
public static class CertificateEndpoints
{
    /// <summary>Maps certificate endpoints.</summary>
    public static IEndpointRouteBuilder MapCertificateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/enrollments/{enrollmentId:guid}/certificate",IssueAsync).RequireAuthorization();
        endpoints.MapGet("/api/certificates/me",GetMineAsync).RequireAuthorization();
        endpoints.MapGet("/api/certificates/{certificateId:guid}",GetAsync).RequireAuthorization();
        endpoints.MapGet("/api/certificates/{certificateId:guid}/download",DownloadAsync).RequireAuthorization();
        endpoints.MapGet("/api/enrollments/{enrollmentId:guid}/certificate",GetByEnrollmentAsync).RequireAuthorization();
        endpoints.MapGet("/api/users/{userId:guid}/certificates",GetUserAsync).RequireAuthorization(Policies.AdminOnly);
        endpoints.MapGet("/api/courses/{courseId:guid}/certificates",GetCourseAsync).RequireAuthorization(Policies.AdminOnly);
        endpoints.MapPost("/api/certificates/{certificateId:guid}/revoke",RevokeAsync).RequireAuthorization(Policies.AdminOnly);
        endpoints.MapGet("/api/public/certificates/verify/{verificationCode}",VerifyAsync).AllowAnonymous();
        return endpoints;
    }
    private static async Task<IResult> IssueAsync(Guid enrollmentId,ICommandDispatcher d,CancellationToken ct)=>
        (await d.SendAsync<IssueCertificate,IssueCertificateResponse>(new(enrollmentId),ct)).ToHttpResult();
    private static async Task<IResult> GetMineAsync(IQueryHandler<GetMyCertificates,Result<IReadOnlyList<CertificateListItemResponse>>> h,CancellationToken ct)=>(await h.HandleAsync(new(),ct)).ToHttpResult();
    private static async Task<IResult> GetAsync(Guid certificateId,IQueryHandler<GetCertificateById,Result<CertificateResponse>> h,CancellationToken ct)=>(await h.HandleAsync(new(certificateId),ct)).ToHttpResult();
    private static async Task<IResult> GetByEnrollmentAsync(Guid enrollmentId,IQueryHandler<GetCertificateByEnrollment,Result<CertificateResponse>> h,CancellationToken ct)=>(await h.HandleAsync(new(enrollmentId),ct)).ToHttpResult();
    private static async Task<IResult> GetUserAsync(Guid userId,IQueryHandler<GetUserCertificates,Result<IReadOnlyList<CertificateResponse>>> h,CancellationToken ct)=>(await h.HandleAsync(new(userId),ct)).ToHttpResult();
    private static async Task<IResult> GetCourseAsync(Guid courseId,IQueryHandler<GetCourseCertificates,Result<IReadOnlyList<CertificateResponse>>> h,CancellationToken ct)=>(await h.HandleAsync(new(courseId),ct)).ToHttpResult();
    private static async Task<IResult> RevokeAsync(Guid certificateId,RevokeCertificateRequest r,ICommandDispatcher d,CancellationToken ct)=>(await d.SendAsync<RevokeCertificate,CertificateResponse>(new(certificateId,r.Reason),ct)).ToHttpResult();
    private static async Task<IResult> VerifyAsync(string verificationCode,IQueryHandler<VerifyCertificate,Result<CertificateVerificationResponse>> h,CancellationToken ct)=>(await h.HandleAsync(new(verificationCode),ct)).ToHttpResult();
    private static async Task<IResult> DownloadAsync(Guid certificateId,HttpContext context,IQueryHandler<DownloadCertificate,Result<CertificateFile>> h,CancellationToken ct)
    {
        var result=await h.HandleAsync(new(certificateId),ct); if(result.IsFailure)return result.Error!.ToProblem();
        context.Response.Headers.CacheControl="private, no-store"; context.Response.Headers.Pragma="no-cache";
        return Results.File(result.Value.Content,"application/pdf",result.Value.FileName);
    }
}
