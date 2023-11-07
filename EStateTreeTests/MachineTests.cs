namespace EStateTree.Tests;

public class MachineTests
{
    private readonly Machine _machine;

    public MachineTests()
    {
        _machine = new(new StateId("machine"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Machine_Constructor_ThrowsArgumentExceptionWithNullEmptyOrWhiteSpaceIdString(string idString)
    {
        Assert.Throws<ArgumentException>(() => new Machine(new StateId(idString)));
    }

    [Fact]
    public void Machine_Start_EntersState()
    {
        bool check = false;
        _machine.AddEnterBehavior(()  => check = true);

        _machine.Enter();

        Assert.True(check);
    }

    [Fact]
    public void Machine_Enter_DoesNothingWhenCalledMoreThanOnceInARow()
    {
        int count = 0;
        _machine.AddEnterBehavior(() => count++);

        _machine.Enter();
        _machine.Enter();

        Assert.Equal(1, count);
    }

    [Fact]
    public void Machine_Update_UpdatesState()
    {
        bool check = false;
        _machine.AddUpdateBehavior(() => check = true);

        _machine.Enter();
        _machine.Update();

        Assert.True(check);
    }

    [Fact]
    public void Machine_Exit_ExitsState()
    {
        bool check = false;
        _machine.AddExitBehavior(() => check = true);

        _machine.Enter();
        _machine.Exit();

        Assert.True(check);
    }

    [Fact]
    public void Machine_Exit_DoesNothingWhenCalledBeforeEnter()
    {
        bool check = false;
        _machine.AddExitBehavior(() => check = true);

        _machine.Exit();
        _machine.Enter();

        Assert.False(check);
    }

    [Fact]
    public void Machine_Exit_DoesNothingWhenCalledMoreThanOnceInARow()
    {
        int count = 0;
        _machine.AddExitBehavior(() => count++);

        _machine.Enter();
        _machine.Exit();
        _machine.Exit();

        Assert.Equal(1, count);
    }

    [Fact]
    public void Machine_SendEvent_DoesNothingIfCalledBeforeCallingEnter()
    {
        EventId eventId = new("event");
        int count = 0;
        _machine.EventResponses.Add(
            eventId,
            new EventResponse(() => count++, true));

        _machine.SendEvent(eventId);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Machine_BubbleEvent_DoesNothingIfCalledBeforeCallingEnter()
    {
        int count = 0;
        State child = new("child");
        State grandChild = new("grandchild");
        child.AddChild(grandChild);
        _machine.AddChild(child);
        EventId eventId = new("event");
        EventResponse response = new(() => count++, true);
        grandChild.EventResponses.Add(eventId, response);

        _machine.BubbleEvent(eventId);

        Assert.Equal(0, count);
    }

    [Fact]
    public void Machine_BubbleEvent_FiresEventFromInnermostActiveStateInBranch()
    {
        int count = 0;
        State child = new("child");
        State grandChild = new("grandchild");
        child.AddChild(grandChild);
        _machine.AddChild(child);
        EventId eventId = new("event");
        EventResponse response = new(() => count++, true);
        grandChild.EventResponses.Add(eventId, response);
        _machine.EventResponses.Add(eventId, response);

        Console.WriteLine($"{_machine.ActiveChildId.Id}");

        _machine.Enter();
        _machine.BubbleEvent(eventId);

        Assert.Equal(1, count);
    }
}
