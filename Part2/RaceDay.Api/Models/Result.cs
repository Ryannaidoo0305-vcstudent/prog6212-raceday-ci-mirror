namespace RaceDay.Api.Models;

// Finish time and position for one enrolment. Participants only see it once IsPublished is true.
public class Result
{
    public int ResultId { get; set; }
    public int EnrolmentId { get; set; }
    public TimeSpan FinishTime { get; set; }
    public int FinishPosition { get; set; }
    public bool IsPublished { get; set; }
    public DateTime RecordedAt { get; set; }

    public Enrolment? Enrolment { get; set; }
}
