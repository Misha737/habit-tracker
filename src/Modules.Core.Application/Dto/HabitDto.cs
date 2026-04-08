using Modules.Core.Domain;

namespace Modules.Core.Application.Dto;

public record HabitDto(
    Guid Id,
    string Name,
    string Description,
    int FrequencyPerWeek,
    string Status,
    Guid OwnerUserId,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public static class HabitMapper
{
    public static HabitDto ToDto(Habit habit) => new(
        habit.Id,
        habit.Name,
        habit.Description,
        habit.FrequencyPerWeek,
        habit.Status.ToString(),
        habit.OwnerUserId,
        habit.CreatedAt,
        habit.UpdatedAt
    );
}
