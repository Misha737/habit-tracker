namespace Modules.Core.Domain;

public class Habit
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int FrequencyPerWeek { get; private set; }
    public HabitStatus Status { get; private set; }
    public Guid OwnerUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Habit() { }

    public Habit(string name, string description, int frequencyPerWeek, Guid ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Habit name cannot be empty.");

        if (frequencyPerWeek < 1)
            throw new DomainException("FrequencyPerWeek must be at least 1.");

        if (ownerUserId == Guid.Empty)
            throw new DomainException("OwnerUserId cannot be empty.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        FrequencyPerWeek = frequencyPerWeek;
        OwnerUserId = ownerUserId;
        Status = HabitStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeStatus(HabitStatus newStatus)
    {
        if (Status == HabitStatus.Archived)
            throw new DomainException("An archived habit cannot change its status.");
        if (Status == newStatus)
            throw new DomainException($"Habit is already in status '{newStatus}'.");
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string description, int frequencyPerWeek)
    {
        if (Status == HabitStatus.Archived)
            throw new DomainException("Cannot update an archived habit.");
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Habit name cannot be empty.");
        if (frequencyPerWeek < 1)
            throw new DomainException("FrequencyPerWeek must be at least 1.");
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        FrequencyPerWeek = frequencyPerWeek;
        UpdatedAt = DateTime.UtcNow;
    }
}
