using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningPortal.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInstructorEligibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInstructorAssessment",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SkillId",
                table: "Quizzes",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SkillId",
                table: "Courses",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InstructorEligibility",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QualifyingQuizId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BestPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    QualifiedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsEligible = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstructorEligibility", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InstructorEligibility_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstructorEligibility_Quizzes_QualifyingQuizId",
                        column: x => x.QualifyingQuizId,
                        principalTable: "Quizzes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InstructorEligibility_Skills_SkillId",
                        column: x => x.SkillId,
                        principalTable: "Skills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [Skills] (
                    [Id], [Name], [Slug], [Description], [IsActive],
                    [CreatedAtUtc], [CreatedBy], [UpdatedAtUtc], [UpdatedBy])
                SELECT
                    NEWID(),
                    categories.[Category],
                    LEFT(REPLACE(REPLACE(LOWER(categories.[Category]), N' ', N'-'), N'_', N'-'), 80)
                        + N'-' + LEFT(CONVERT(varchar(64), HASHBYTES('SHA2_256', categories.[Category]), 2), 16),
                    NULL,
                    CAST(1 AS bit),
                    SYSUTCDATETIME(),
                    NULL,
                    NULL,
                    NULL
                FROM (
                    SELECT DISTINCT LTRIM(RTRIM([Category])) AS [Category]
                    FROM [Courses]
                    WHERE NULLIF(LTRIM(RTRIM([Category])), N'') IS NOT NULL
                ) AS categories;

                UPDATE courses
                SET courses.[SkillId] = skills.[Id]
                FROM [Courses] AS courses
                INNER JOIN [Skills] AS skills
                    ON skills.[Name] = LTRIM(RTRIM(courses.[Category]));
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Quizzes_SkillId_IsInstructorAssessment_Status",
                table: "Quizzes",
                columns: new[] { "SkillId", "IsInstructorAssessment", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SkillId",
                table: "Courses",
                column: "SkillId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorEligibility_QualifyingQuizId",
                table: "InstructorEligibility",
                column: "QualifyingQuizId");

            migrationBuilder.CreateIndex(
                name: "IX_InstructorEligibility_SkillId_IsEligible",
                table: "InstructorEligibility",
                columns: new[] { "SkillId", "IsEligible" });

            migrationBuilder.CreateIndex(
                name: "IX_InstructorEligibility_UserId_SkillId",
                table: "InstructorEligibility",
                columns: new[] { "UserId", "SkillId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Skills_IsActive_Name",
                table: "Skills",
                columns: new[] { "IsActive", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Skills_Slug",
                table: "Skills",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Skills_SkillId",
                table: "Courses",
                column: "SkillId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Quizzes_Skills_SkillId",
                table: "Quizzes",
                column: "SkillId",
                principalTable: "Skills",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Skills_SkillId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Quizzes_Skills_SkillId",
                table: "Quizzes");

            migrationBuilder.DropTable(
                name: "InstructorEligibility");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropIndex(
                name: "IX_Quizzes_SkillId_IsInstructorAssessment_Status",
                table: "Quizzes");

            migrationBuilder.DropIndex(
                name: "IX_Courses_SkillId",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "IsInstructorAssessment",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "SkillId",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "SkillId",
                table: "Courses");
        }
    }
}
