#pragma warning disable CS1591
using LearningPortal.Domain.Common;
namespace LearningPortal.Domain.Quizzes;
public sealed class QuizQuestion : AuditableEntity
{
    private QuizQuestion() { } private QuizQuestion(Guid quizId,string text,QuestionType type,decimal points,int order,string? explanation){QuizId=quizId;Text=text;QuestionType=type;Points=points;Order=order;Explanation=explanation;}
    public Guid QuizId{get;private set;} public string Text{get;private set;}=string.Empty; public QuestionType QuestionType{get;private set;} public decimal Points{get;private set;} public int Order{get;private set;} public string? Explanation{get;private set;} public bool IsActive{get;private set;}=true;
    public static QuizQuestion Create(Guid quizId,string text,QuestionType type,decimal points,int order,string? explanation=null){if(quizId==Guid.Empty||string.IsNullOrWhiteSpace(text)||points<=0||order<1)throw new ArgumentException("Question values are invalid.");return new(quizId,text.Trim(),type,points,order,explanation?.Trim());}
}
