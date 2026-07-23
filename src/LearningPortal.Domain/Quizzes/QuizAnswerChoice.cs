#pragma warning disable CS1591
using LearningPortal.Domain.Common;
namespace LearningPortal.Domain.Quizzes;
public sealed class QuizAnswerChoice:Entity { private QuizAnswerChoice(){} private QuizAnswerChoice(Guid questionId,string text,bool correct,int order){QuestionId=questionId;Text=text;IsCorrect=correct;Order=order;} public Guid QuestionId{get;private set;} public string Text{get;private set;}=string.Empty; public bool IsCorrect{get;private set;} public int Order{get;private set;} public static QuizAnswerChoice Create(Guid questionId,string text,bool correct,int order){if(questionId==Guid.Empty||string.IsNullOrWhiteSpace(text)||order<1)throw new ArgumentException("Answer values are invalid.");return new(questionId,text.Trim(),correct,order);} }
