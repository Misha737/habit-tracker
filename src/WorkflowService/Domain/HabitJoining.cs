namespace WorkflowService.Domain;

public class HabitJoining
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid HabitId { get; private set; }
    public JoiningStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private HabitJoining() { }

    public HabitJoining(Guid userId, Guid habitId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        HabitId = habitId;
        Status = JoiningStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = JoiningStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }
}

public enum JoiningStatus
{
    Active,
    Cancelled
}
