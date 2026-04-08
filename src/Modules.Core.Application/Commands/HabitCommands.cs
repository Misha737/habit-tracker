using Modules.Core.Domain;

namespace Modules.Core.Application.Commands;

public record CreateHabitCommand(
    string Name,
    string Description,
    int FrequencyPerWeek,
    Guid OwnerUserId
);

public record UpdateHabitStatusCommand(Guid HabitId, HabitStatus NewStatus);

public record UpdateHabitDetailsCommand(
    Guid HabitId,
    string Name,
    string Description,
    int FrequencyPerWeek
);
