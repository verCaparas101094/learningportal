using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LearningPortal.Infrastructure.Migrations;

/// <inheritdoc />
public partial class LessonContentEnhancement : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF OBJECT_ID(N'[dbo].[Lessons]', N'U') IS NULL
            BEGIN
                CREATE TABLE [dbo].[Lessons] (
                    [Id] uniqueidentifier NOT NULL,
                    [CourseId] uniqueidentifier NOT NULL,
                    [Title] nvarchar(200) NOT NULL,
                    [Description] nvarchar(2000) NOT NULL,
                    [Order] int NOT NULL,
                    [EstimatedMinutes] int NOT NULL,
                    [LessonType] nvarchar(30) NOT NULL,
                    [MarkdownContent] nvarchar(max) NULL,
                    [ExternalUrl] nvarchar(2048) NULL,
                    [VideoProvider] nvarchar(30) NOT NULL CONSTRAINT [DF_Lessons_VideoProvider] DEFAULT N'None',
                    [Status] nvarchar(20) NOT NULL,
                    [IsDeleted] bit NOT NULL CONSTRAINT [DF_Lessons_IsDeleted] DEFAULT CAST(0 AS bit),
                    [DeletedAtUtc] datetimeoffset(0) NULL,
                    [DeletedBy] uniqueidentifier NULL,
                    [CreatedAtUtc] datetimeoffset(0) NOT NULL,
                    [CreatedBy] uniqueidentifier NULL,
                    [UpdatedAtUtc] datetimeoffset(0) NULL,
                    [UpdatedBy] uniqueidentifier NULL,
                    [RowVersion] rowversion NOT NULL,
                    CONSTRAINT [PK_Lessons] PRIMARY KEY ([Id]),
                    CONSTRAINT [FK_Lessons_Courses_CourseId] FOREIGN KEY ([CourseId])
                        REFERENCES [dbo].[Courses] ([Id]) ON DELETE NO ACTION
                );
                CREATE UNIQUE INDEX [IX_Lessons_CourseId_Order] ON [dbo].[Lessons] ([CourseId], [Order])
                    WHERE [IsDeleted] = 0;
                CREATE INDEX [IX_Lessons_CourseId_Status] ON [dbo].[Lessons] ([CourseId], [Status]);
                CREATE INDEX [IX_Lessons_IsDeleted] ON [dbo].[Lessons] ([IsDeleted]);
                CREATE INDEX [IX_Lessons_Title] ON [dbo].[Lessons] ([Title]);
                EXEC sys.sp_addextendedproperty
                    @name = N'LearningPortal.LessonContentEnhancement.CreatedTable',
                    @value = 1,
                    @level0type = N'SCHEMA', @level0name = N'dbo',
                    @level1type = N'TABLE', @level1name = N'Lessons';
            END
            ELSE
            BEGIN
                IF COL_LENGTH(N'[dbo].[Lessons]', N'MarkdownContent') IS NULL
                   AND COL_LENGTH(N'[dbo].[Lessons]', N'Content') IS NOT NULL
                    EXEC sp_rename N'[dbo].[Lessons].[Content]', N'MarkdownContent', N'COLUMN';

                IF COL_LENGTH(N'[dbo].[Lessons]', N'ExternalUrl') IS NULL
                    ALTER TABLE [dbo].[Lessons] ADD [ExternalUrl] nvarchar(2048) NULL;

                IF COL_LENGTH(N'[dbo].[Lessons]', N'VideoProvider') IS NULL
                    ALTER TABLE [dbo].[Lessons] ADD [VideoProvider] nvarchar(30) NOT NULL
                        CONSTRAINT [DF_Lessons_VideoProvider] DEFAULT N'None';

                ALTER TABLE [dbo].[Lessons] ALTER COLUMN [MarkdownContent] nvarchar(max) NULL;

                UPDATE [dbo].[Lessons]
                SET [ExternalUrl] = [MarkdownContent],
                    [MarkdownContent] = NULL,
                    [VideoProvider] =
                        CASE
                            WHEN LOWER([MarkdownContent]) LIKE N'https://youtu.be/%'
                              OR LOWER([MarkdownContent]) LIKE N'https://youtube.com/%'
                              OR LOWER([MarkdownContent]) LIKE N'https://www.youtube.com/%' THEN N'YouTube'
                            WHEN LOWER([MarkdownContent]) LIKE N'https://vimeo.com/%'
                              OR LOWER([MarkdownContent]) LIKE N'https://player.vimeo.com/%' THEN N'Vimeo'
                            WHEN LOWER([MarkdownContent]) LIKE N'https://%.sharepoint.com/%'
                              OR LOWER([MarkdownContent]) LIKE N'https://stream.microsoft.com/%'
                              OR LOWER([MarkdownContent]) LIKE N'https://%.microsoftstream.com/%' THEN N'MicrosoftStream'
                            WHEN LOWER(LEFT([MarkdownContent], CHARINDEX(N'?', [MarkdownContent] + N'?') - 1))
                              LIKE N'https://%.mp4' THEN N'DirectMp4'
                            ELSE N'None'
                        END
                WHERE [LessonType] = N'Video';

                UPDATE [dbo].[Lessons]
                SET [ExternalUrl] = [MarkdownContent], [MarkdownContent] = NULL,
                    [LessonType] = N'Pdf', [VideoProvider] = N'None'
                WHERE [LessonType] = N'PDF';

                UPDATE [dbo].[Lessons]
                SET [LessonType] = N'Article', [ExternalUrl] = NULL, [VideoProvider] = N'None'
                WHERE [LessonType] = N'QuizPlaceholder';
            END
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM sys.extended_properties
                WHERE [major_id] = OBJECT_ID(N'[dbo].[Lessons]')
                  AND [name] = N'LearningPortal.LessonContentEnhancement.CreatedTable')
            BEGIN
                DROP TABLE [dbo].[Lessons];
            END
            ELSE IF OBJECT_ID(N'[dbo].[Lessons]', N'U') IS NOT NULL
                AND COL_LENGTH(N'[dbo].[Lessons]', N'ExternalUrl') IS NOT NULL
            BEGIN
                UPDATE [dbo].[Lessons]
                SET [MarkdownContent] = COALESCE([MarkdownContent], [ExternalUrl]),
                    [LessonType] = CASE WHEN [LessonType] = N'Pdf' THEN N'PDF'
                                        WHEN [LessonType] = N'ExternalLink' THEN N'Article'
                                        ELSE [LessonType] END;
                ALTER TABLE [dbo].[Lessons] DROP CONSTRAINT [DF_Lessons_VideoProvider];
                ALTER TABLE [dbo].[Lessons] DROP COLUMN [VideoProvider];
                ALTER TABLE [dbo].[Lessons] DROP COLUMN [ExternalUrl];
                ALTER TABLE [dbo].[Lessons] ALTER COLUMN [MarkdownContent] nvarchar(max) NOT NULL;
                EXEC sp_rename N'[dbo].[Lessons].[MarkdownContent]', N'Content', N'COLUMN';
            END
            """);
    }
}
