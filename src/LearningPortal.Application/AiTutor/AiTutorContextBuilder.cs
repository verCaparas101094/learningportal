#pragma warning disable CS1591
using System.Text;
using LearningPortal.Domain.Courses;
using LearningPortal.Domain.Enrollments;
using LearningPortal.Domain.Lessons;
using LearningPortal.Domain.Repositories;
using LearningPortal.Shared.Results;
namespace LearningPortal.Application.AiTutor;
public sealed class AiTutorContextBuilder(ICourseRepository courses,ILessonRepository lessons,IEnrollmentRepository enrollments,OllamaOptions options):IAiTutorContextBuilder
{
 public async Task<AiTutorContextResult> BuildAsync(Guid student,Guid courseId,Guid? lessonId,CancellationToken ct)
 {
  var enrollment=await enrollments.GetByCourseAndStudentAsync(courseId,student,ct);
  if(enrollment is null||enrollment.Status is EnrollmentStatus.Withdrawn)return Fail(Errors.Authorization.Forbidden());
  var course=await courses.GetByIdReadOnlyAsync(courseId,ct);
  if(course is null||course.Status!=CourseStatus.Published)return Fail(Errors.Authorization.Forbidden());
  var published=await lessons.GetPublishedByCourseAsync(courseId,ct);
  Lesson? current=null;if(lessonId is Guid id){current=published.SingleOrDefault(x=>x.Id==id);if(current is null)return Fail(Errors.Authorization.Forbidden());}
  var b=new StringBuilder();Append(b,"COURSE",course.Title,$"{course.Description}\nCategory / skill: {course.Category}");
  if(current is not null)Append(b,"CURRENT LESSON",current.Title,Visible(current));
  foreach(var lesson in published.Where(x=>x.Id!=current?.Id))Append(b,"PUBLISHED LESSON",lesson.Title,Visible(lesson));
  var context=Truncate(Clean(b.ToString()),options.MaxContextCharacters);
  return new(true,context,course.Title,current?.Title,null);
 }
 private static string Visible(Lesson x)=>x.LessonType==LessonType.Article?$"{x.Description}\n{x.MarkdownContent}":x.Description;
 private static void Append(StringBuilder b,string section,string title,string content)=>b.AppendLine($"--- BEGIN UNTRUSTED {section}: {title} ---").AppendLine(content).AppendLine($"--- END UNTRUSTED {section} ---");
 private static string Clean(string value)=>new string(value.Where(ch=>ch=='\n'||ch=='\r'||ch=='\t'||!char.IsControl(ch)).ToArray());
 private static string Truncate(string value,int max){if(value.Length<=max)return value;var end=max;if(end>0&&char.IsHighSurrogate(value[end-1]))end--;return value[..end];}
 private static AiTutorContextResult Fail(Error e)=>new(false,null,null,null,e);
}
