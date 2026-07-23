using System.Globalization;
using System.Text;
using LearningPortal.Application.Certificates;
using LearningPortal.Domain.Certificates;

namespace LearningPortal.Infrastructure.Certificates;

/// <summary>Generates dependency-free, landscape PDF certificates.</summary>
public sealed class CertificatePdfGenerator : ICertificatePdfGenerator
{
    /// <inheritdoc />
    public byte[] Generate(Certificate value, string verificationUrl)
    {
        var status = value.Status == CertificateStatus.Revoked ? "REVOKED" : "ACTIVE";
        var lines = new List<(int Size, int X, int Y, string Text)>
        {
            (18, 340, 530, "ERNI LEARNING PORTAL"), (32, 245, 455, "CERTIFICATE OF COMPLETION"),
            (14, 350, 405, "This certifies that"), (28, 280, 360, value.StudentDisplayName),
            (14, 270, 315, "has successfully completed"), (24, 250, 275, value.CourseTitle),
            (13, 310, 240, $"Skill / Category: {value.CourseCategory}"),
            (11, 100, 175, $"Completed: {value.CompletedAtUtc:yyyy-MM-dd}"),
            (11, 330, 175, $"Issued: {value.IssuedAtUtc:yyyy-MM-dd}"),
            (11, 560, 175, $"Instructor: {value.InstructorDisplayName ?? "Not specified"}"),
            (10, 100, 115, $"Certificate: {value.CertificateNumber}"),
            (10, 100, 92, $"Verification code: {value.VerificationCode}"),
            (9, 100, 69, verificationUrl), (14, 680, 115, status)
        };
        if (value.Status == CertificateStatus.Revoked)
            lines.Add((48, 300, 215, "REVOKED"));
        var stream = new StringBuilder("0.94 0.96 0.99 rg 20 20 802 555 re f\n0.08 0.23 0.39 RG 4 w 35 35 772 525 re S\n");
        foreach (var line in lines)
            stream.AppendFormat(CultureInfo.InvariantCulture, "BT /F1 {0} Tf {1} {2} Td ({3}) Tj ET\n",
                line.Size, line.X, line.Y, Escape(line.Text));
        return BuildPdf(stream.ToString());
    }

    private static string Escape(string value) => new string(value.Select(ch => ch is >= ' ' and <= '~' ? ch : '?').ToArray())
        .Replace("\\", "\\\\", StringComparison.Ordinal).Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);
    private static byte[] BuildPdf(string content)
    {
        var objects = new[] {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 842 595] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };
        using var output = new MemoryStream(); Write(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i=0;i<objects.Length;i++){ offsets.Add(output.Position); Write(output,$"{i+1} 0 obj\n{objects[i]}\nendobj\n"); }
        var xref=output.Position; Write(output,$"xref\n0 {objects.Length+1}\n0000000000 65535 f \n");
        foreach(var offset in offsets.Skip(1)) Write(output,$"{offset:0000000000} 00000 n \n");
        Write(output,$"trailer << /Size {objects.Length+1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }
    private static void Write(Stream stream,string value){var bytes=Encoding.ASCII.GetBytes(value);stream.Write(bytes);}
}
