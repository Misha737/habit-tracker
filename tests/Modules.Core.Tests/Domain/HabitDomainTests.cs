using Modules.Core.Domain;
using Xunit;

namespace Modules.Core.Tests.Domain;

public class HabitDomainTests
{
    private static readonly Guid ValidOwner = Guid.NewGuid();

    [Fact]
    public void CreateHabit_WithEmptyName_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new Habit("", "desc", 3, ValidOwner));
        Assert.Contains("name cannot be empty", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("  ")]
    [InlineData("\t")]
    public void CreateHabit_WithWhitespaceName_ThrowsDomainException(string name)
        => Assert.Throws<DomainException>(() => new Habit(name, "desc", 3, ValidOwner));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateHabit_WithNonPositiveFrequency_ThrowsDomainException(int freq)
    {
        var ex = Assert.Throws<DomainException>(() => new Habit("Run", "desc", freq, ValidOwner));
        Assert.Contains("FrequencyPerWeek", ex.Message);
    }

    [Fact]
    public void CreateHabit_WithEmptyOwnerGuid_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new Habit("Run", "desc", 3, Guid.Empty));
        Assert.Contains("OwnerUserId", ex.Message);
    }

    [Fact]
    public void CreateHabit_WithValidData_SetsActiveStatus()
    {
        var habit = new Habit("Morning Run", "Run 5km", 5, ValidOwner);
        Assert.Equal(HabitStatus.Active, habit.Status);
        Assert.Equal("Morning Run", habit.Name);
        Assert.Equal(ValidOwner, habit.OwnerUserId);
    }

    [Fact]
    public void ChangeStatus_ActiveToPaused_Succeeds()
    {
        var habit = new Habit("Read", "", 7, ValidOwner);
        habit.ChangeStatus(HabitStatus.Paused);
        Assert.Equal(HabitStatus.Paused, habit.Status);
    }

    [Fact]
    public void ChangeStatus_FromArchived_ThrowsDomainException()
    {
        var habit = new Habit("Read", "", 7, ValidOwner);
        habit.ChangeStatus(HabitStatus.Archived);
        var ex = Assert.Throws<DomainException>(() => habit.ChangeStatus(HabitStatus.Active));
        Assert.Contains("archived", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ChangeStatus_ToSameStatus_ThrowsDomainException()
    {
        var habit = new Habit("Read", "", 7, ValidOwner);
        var ex = Assert.Throws<DomainException>(() => habit.ChangeStatus(HabitStatus.Active));
        Assert.Contains("already in status", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UpdateDetails_OnArchivedHabit_ThrowsDomainException()
    {
        var habit = new Habit("Read", "", 7, ValidOwner);
        habit.ChangeStatus(HabitStatus.Archived);
        Assert.Throws<DomainException>(() => habit.UpdateDetails("New", "", 3));
    }
}
