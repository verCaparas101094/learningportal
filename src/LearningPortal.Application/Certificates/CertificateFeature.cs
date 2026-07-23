#pragma warning disable CS1591
using System.Security.Cryptography;
using FluentValidation;
using LearningPortal.Application.Abstractions.Identity;
using LearningPortal.Application.Abstractions.Messaging;
using LearningPortal.Application.Abstractions.Time;
using LearningPortal.Application.Authorization;
using LearningPortal.Domain.Certificates;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Certificates;
using LearningPortal.Shared.Results;

namespace LearningPortal.Application.Certificates;

public sealed record IssueCertificate(Guid EnrollmentId) : ICommand<Result<IssueCertificateResponse>>;
public sealed record GetMyCertificates : IQuery<Result<IReadOnlyList<CertificateListItemResponse>>>;
public sealed record GetCertificateById(Guid CertificateId) : IQuery<Result<CertificateResponse>>;
public sealed record GetCertificateByEnrollment(Guid EnrollmentId) : IQuery<Result<CertificateResponse>>;
public sealed record DownloadCertificate(Guid CertificateId) : IQuery<Result<CertificateFile>>;
public sealed record VerifyCertificate(string VerificationCode) : IQuery<Result<CertificateVerificationResponse>>;
public sealed record RevokeCertificate(Guid CertificateId, string Reason) : ICommand<Result<CertificateResponse>>;
public sealed record GetCourseCertificates(Guid CourseId) : IQuery<Result<IReadOnlyList<CertificateResponse>>>;
public sealed record GetUserCertificates(Guid UserId) : IQuery<Result<IReadOnlyList<CertificateResponse>>>;
public sealed record CertificateFile(byte[] Content, string FileName);

public sealed class RevokeCertificateValidator : AbstractValidator<RevokeCertificate>
{
    public RevokeCertificateValidator()
    {
        RuleFor(x=>x.CertificateId).NotEmpty(); RuleFor(x=>x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CertificateHandler(
    ICertificateRepository certificates, IEnrollmentRepository enrollments, ICourseRepository courses,
    IUnitOfWork unit, ICurrentUserService currentUser, IUserManagementService users, ISystemClock clock,
    ICertificatePdfGenerator pdf, CertificateOptions options)
    : ICommandHandler<IssueCertificate, Result<IssueCertificateResponse>>,
      ICommandHandler<RevokeCertificate, Result<CertificateResponse>>,
      IQueryHandler<GetMyCertificates, Result<IReadOnlyList<CertificateListItemResponse>>>,
      IQueryHandler<GetCertificateById, Result<CertificateResponse>>,
      IQueryHandler<GetCertificateByEnrollment, Result<CertificateResponse>>,
      IQueryHandler<DownloadCertificate, Result<CertificateFile>>,
      IQueryHandler<VerifyCertificate, Result<CertificateVerificationResponse>>,
      IQueryHandler<GetCourseCertificates, Result<IReadOnlyList<CertificateResponse>>>,
      IQueryHandler<GetUserCertificates, Result<IReadOnlyList<CertificateResponse>>>
{
    public async Task<Result<IssueCertificateResponse>> HandleAsync(IssueCertificate command,CancellationToken ct=default)
    {
        var enrollment=await enrollments.GetByIdReadOnlyAsync(command.EnrollmentId,ct);
        var access=ValidateEnrollmentAccess(enrollment);
        if(access is not null)return Result<IssueCertificateResponse>.Failure(access);
        var existing=await certificates.GetByEnrollmentAsync(command.EnrollmentId,true,ct);
        if(existing is not null)return Result<IssueCertificateResponse>.Success(new(ToResponse(existing),true));
        if(enrollment!.Status!=EnrollmentStatus.Completed || enrollment.CompletedAtUtc is null)
            return Result<IssueCertificateResponse>.Failure(Errors.Common.Failure("Certificate.EnrollmentIncomplete","Only completed enrollments can receive certificates."));
        var course=await courses.GetByIdReadOnlyAsync(enrollment.CourseId,ct);
        if(course is null || course.IsDeleted)return Result<IssueCertificateResponse>.Failure(Errors.Common.NotFound("Course",enrollment.CourseId));
        var people=await users.GetUsersByIdsAsync([enrollment.StudentId,course.InstructorId],ct);
        if(!people.TryGetValue(enrollment.StudentId,out var student))
            return Result<IssueCertificateResponse>.Failure(Errors.Common.NotFound("User",enrollment.StudentId));
        people.TryGetValue(course.InstructorId,out var instructor);
        var sequence=await certificates.GetNextSequenceAsync(ct);
        var number=$"ERNI-{clock.UtcNow.Year}-{sequence:D8}";
        var code=CreateVerificationCode();
        var certificate=Certificate.Issue(number,enrollment.Id,course.Id,enrollment.StudentId,student.DisplayName,
            course.Title,course.Category,instructor?.DisplayName,enrollment.CompletedAtUtc.Value,clock.UtcNow,code);
        await certificates.AddAsync(certificate,ct);
        try { await unit.SaveChangesAsync(ct); }
        catch(DuplicateCertificateEnrollmentException)
        {
            var concurrent=await certificates.GetByEnrollmentAsync(enrollment.Id,true,ct);
            if(concurrent is not null)return Result<IssueCertificateResponse>.Success(new(ToResponse(concurrent),true));
            throw;
        }
        return Result<IssueCertificateResponse>.Success(new(ToResponse(certificate),false));
    }

    public async Task<Result<CertificateResponse>> HandleAsync(RevokeCertificate command,CancellationToken ct=default)
    {
        var admin=RequireAdmin(); if(admin is not null)return Result<CertificateResponse>.Failure(admin);
        var value=await certificates.GetByIdAsync(command.CertificateId,false,ct);
        if(value is null)return Result<CertificateResponse>.Failure(Errors.Common.NotFound("Certificate",command.CertificateId));
        if(!value.TryRevoke(command.Reason,clock.UtcNow))
            return Result<CertificateResponse>.Failure(Errors.Validation.Failed("A revocation reason is required."));
        await unit.SaveChangesAsync(ct); return Result<CertificateResponse>.Success(ToResponse(value));
    }

    public async Task<Result<IReadOnlyList<CertificateListItemResponse>>> HandleAsync(GetMyCertificates query,CancellationToken ct=default)
    {
        if(!currentUser.IsAuthenticated||currentUser.UserId is not Guid id)return Result<IReadOnlyList<CertificateListItemResponse>>.Failure(Errors.Authentication.Unauthorized());
        var values=await certificates.GetByStudentAsync(id,ct);
        return Result<IReadOnlyList<CertificateListItemResponse>>.Success(values.Select(ToListItem).ToArray());
    }
    public async Task<Result<CertificateResponse>> HandleAsync(GetCertificateById query,CancellationToken ct=default)
    {
        var value=await certificates.GetByIdAsync(query.CertificateId,true,ct); var error=ValidateCertificateAccess(value);
        return error is null?Result<CertificateResponse>.Success(ToResponse(value!)):Result<CertificateResponse>.Failure(error);
    }
    public async Task<Result<CertificateResponse>> HandleAsync(GetCertificateByEnrollment query,CancellationToken ct=default)
    {
        var enrollment=await enrollments.GetByIdReadOnlyAsync(query.EnrollmentId,ct); var error=ValidateEnrollmentAccess(enrollment);
        if(error is not null)return Result<CertificateResponse>.Failure(error);
        var value=await certificates.GetByEnrollmentAsync(query.EnrollmentId,true,ct);
        return value is null?Result<CertificateResponse>.Failure(Errors.Common.NotFound("Certificate",query.EnrollmentId)):Result<CertificateResponse>.Success(ToResponse(value));
    }
    public async Task<Result<CertificateFile>> HandleAsync(DownloadCertificate query,CancellationToken ct=default)
    {
        var value=await certificates.GetByIdAsync(query.CertificateId,true,ct); var error=ValidateCertificateAccess(value);
        if(error is not null)return Result<CertificateFile>.Failure(error);
        var url=$"{options.VerificationBaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(value!.VerificationCode)}";
        return Result<CertificateFile>.Success(new(pdf.Generate(value,url),$"ERNI-Certificate-{value.CertificateNumber}.pdf"));
    }
    public async Task<Result<CertificateVerificationResponse>> HandleAsync(VerifyCertificate query,CancellationToken ct=default)
    {
        if(string.IsNullOrWhiteSpace(query.VerificationCode))return Result<CertificateVerificationResponse>.Failure(Errors.Validation.Required("verificationCode"));
        var value=await certificates.GetByVerificationCodeAsync(query.VerificationCode.Trim(),ct);
        return value is null?Result<CertificateVerificationResponse>.Failure(Errors.Common.NotFound("Certificate",query.VerificationCode))
            :Result<CertificateVerificationResponse>.Success(new(value.CertificateNumber,value.StudentDisplayName,value.CourseTitle,value.CourseCategory,
                value.InstructorDisplayName,value.CompletedAtUtc,value.IssuedAtUtc,value.Status.ToString(),value.RevokedAtUtc));
    }
    public async Task<Result<IReadOnlyList<CertificateResponse>>> HandleAsync(GetCourseCertificates query,CancellationToken ct=default)=>
        await AdminListAsync(()=>certificates.GetByCourseAsync(query.CourseId,ct));
    public async Task<Result<IReadOnlyList<CertificateResponse>>> HandleAsync(GetUserCertificates query,CancellationToken ct=default)=>
        await AdminListAsync(()=>certificates.GetByStudentAsync(query.UserId,ct));

    private async Task<Result<IReadOnlyList<CertificateResponse>>> AdminListAsync(Func<Task<IReadOnlyList<Certificate>>> load)
    {var error=RequireAdmin();return error is null?Result<IReadOnlyList<CertificateResponse>>.Success((await load()).Select(ToResponse).ToArray()):Result<IReadOnlyList<CertificateResponse>>.Failure(error);}
    private Error? ValidateEnrollmentAccess(Enrollment? value)=>!currentUser.IsAuthenticated?Errors.Authentication.Unauthorized()
        :value is null?Errors.Common.NotFound("Enrollment",Guid.Empty)
        :currentUser.UserId==value.StudentId||currentUser.HasRole(ApplicationRoles.Administrator)?null:Errors.Authorization.Forbidden();
    private Error? ValidateCertificateAccess(Certificate? value)=>!currentUser.IsAuthenticated?Errors.Authentication.Unauthorized()
        :value is null?Errors.Common.NotFound("Certificate",Guid.Empty)
        :currentUser.UserId==value.StudentId||currentUser.HasRole(ApplicationRoles.Administrator)?null:Errors.Authorization.Forbidden();
    private Error? RequireAdmin()=>!currentUser.IsAuthenticated?Errors.Authentication.Unauthorized():!currentUser.HasRole(ApplicationRoles.Administrator)?Errors.Authorization.Forbidden():null;
    private static string CreateVerificationCode()=>Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+','-').Replace('/','_');
    private static CertificateResponse ToResponse(Certificate x)=>new(x.Id,x.EnrollmentId,x.CertificateNumber,x.VerificationCode,x.StudentDisplayName,x.CourseTitle,x.CourseCategory,x.InstructorDisplayName,x.CompletedAtUtc,x.IssuedAtUtc,x.Status.ToString(),x.RevokedAtUtc,x.RevocationReason);
    private static CertificateListItemResponse ToListItem(Certificate x)=>new(x.Id,x.EnrollmentId,x.CertificateNumber,x.VerificationCode,x.CourseTitle,x.IssuedAtUtc,x.Status.ToString());
}
