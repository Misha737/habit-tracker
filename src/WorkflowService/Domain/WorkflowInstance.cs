namespace WorkflowService.Domain;

public enum WorkflowState
{
    Started,
    UserValidated,
    HabitValidated,
    JoiningCreated,
    NotificationSent,
    Completed,
    Compensating,
    Compensated,
    Failed
}

public enum WorkflowType
{
    JoinHabit
}

public class WorkflowInstance
{
    public Guid WorkflowId { get; private set; }
    public WorkflowType Type { get; private set; }
    public WorkflowState State { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? LastError { get; private set; }
    public Guid UserId { get; private set; }
    public Guid HabitId { get; private set; }
    public Guid? JoiningId { get; private set; }

    private WorkflowInstance() { }

    public WorkflowInstance(WorkflowType type, Guid userId, Guid habitId)
    {
        WorkflowId = Guid.NewGuid();
        Type = type;
        State = WorkflowState.Started;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UserId = userId;
        HabitId = habitId;
    }

    public void TransitionTo(WorkflowState newState)
    {
        State = newState;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetJoiningId(Guid joiningId)
    {
        JoiningId = joiningId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetError(string error)
    {
        LastError = error;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearError()
    {
        LastError = null;
    }
}
