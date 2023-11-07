namespace EStateTree.Tests;

public class StateTests
{
    private readonly State _state;

    public StateTests()
    {
        _state = new(new StateId("state"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void State_Constructor_ThrowsArgumentExceptionWithNullEmptyOrWhiteSpaceIdString(string idString)
    {
        Assert.Throws<ArgumentException>(() => new State(new StateId(idString)));
    }

    [Fact]
    public void State_GetChild_ReturnsChild()
    {
        State child = new("child");

        _state.AddChild(child);

        Assert.Equal(child, _state.GetChild(child.Id));
    }

    [Fact]
    public void State_GetChild_ThrowsKeyNotFoundExceptionIfStateDoesNotContainChild()
    {
        Assert.Throws<KeyNotFoundException>(() => _state.GetChild("child"));
    }

    [Fact]
    public void State_GetAllChildren_ReturnsAllChildren()
    {
        State[] states = new State[]
        {
            new("child1"),
            new("child2"),
            new("child3"),
        };
        _state.AddChildren(states);

        IEnumerable<State> children = _state.GetAllChildren();

        Assert.Equal(states, children);
    }

    [Fact]
    public void State_GetTransitions_ReturnsAllTransitions()
    {
        State[] states = new State[]
        {
            new("child1"),
            new("child2"),
            new("child3"),
        };
        _state.AddChildren(states);
        _state.AddTransition(states[0].Id, states[1].Id, new EventId("event1"));
        _state.AddTransition(states[1].Id, states[2].Id, new EventId("event2"));

        Assert.Equal(2, _state.GetTransitions().Count);
    }

    [Fact]
    public void State_AddChild_ReturnsCaller()
    {
        State child = new("child");

        State caller = _state.AddChild(child);

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_AddChild_AddsChildToChildren()
    {
        State child = new("child");

        _state.AddChild(child);

        Assert.Single(_state.GetAllChildren());
        Assert.Contains(child, _state.GetAllChildren());
    }

    [Fact]
    public void State_AddChild_ThrowsArgumentExceptionIfArgumentIsItself()
    {
        Assert.Throws<ArgumentException>(() => _state.AddChild(_state));
    }

    [Fact]
    public void State_AddChild_ThrowsArgumentExceptionIfChildrenAlreadyHasChildWithTheSameId()
    {
        State child = new("child");
        State identicalChild = new("child");

        _state.AddChild(child);

        Assert.Throws<ArgumentException>(() => _state.AddChild(identicalChild));
    }

    [Fact]
    public void State_AddChild_ThrowsInvalidOperationExceptionIfChildAlreadyHasParent()
    {
        State child = new("child");
        State newParent = new("newparent");
        _state.AddChild(child);

        Assert.Throws<InvalidOperationException>(() => _state.AddChild(child));
    }

    [Fact]
    public void State_AddChild_ThrowsInvalidOperationExceptionIfAddingChildWouldResultInCycle()
    {
        State child = new("child");
        State grandChild = new("grandchild");
        _state.AddChild(child);
        child.AddChild(grandChild);

        Assert.Throws<InvalidOperationException>(() => grandChild.AddChild(_state));
    }

    [Fact]
    public void State_AddChild_SetsDefaultAndActiveChildIdsToFirstChild()
    {
        State child = new("child1");
        State child2 = new("child2");

        _state.AddChild(child);
        _state.AddChild(child2);

        Assert.Equal(_state.DefaultChildId, new StateId("child1"));
        Assert.Equal(_state.ActiveChildId, new StateId("child1"));
    }

    [Fact]
    public void State_AddChild_SetsSelfAsChildParent()
    {
        State child = new("child1");
        _state.AddChild(child);

        Assert.Equal(child.Parent, _state);
    }

    [Fact]
    public void State_AddChildren_ReturnsCaller()
    {
        State[] states = new State[]
        {
            new("child1"),
        };

        State caller = _state.AddChildren(states);

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_AddChildren_AddsAllToChildren()
    {
        State[] states = new State[]
        {
            new("child1"),
            new("child2"),
            new("child3"),
        };

        _state.AddChildren(states);

        IEnumerable<State> children = _state.GetAllChildren();

        Assert.Equal(states, children);
    }

    [Fact]
    public void State_RemoveChild_ReturnsCaller()
    {
        State[] states = new State[]
        {
            new("child1"),
            new("child2"),
        };

        _state.AddChildren(states);

        State caller = _state.RemoveChild(states[1].Id);

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_RemoveChild_RemovesStateWithGivenIdFromChildren()
    {
        State[] states = new State[]
        {
            new("child1"),
            new("child2"),
            new("child3"),
        };

        _state.AddChildren(states);

        _state.RemoveChild(states[1].Id);

        Assert.DoesNotContain(states[1], _state.GetAllChildren());
    }

    [Fact]
    public void State_RemoveChild_ResetsActiveAndDefaultChildIdsIfNoChildrenRemain()
    {
        State child = new(new StateId("child1"));

        _state.AddChild(child);

        _state.RemoveChild(child.Id);

        Assert.Equal(_state.DefaultChildId, default);
        Assert.Equal(_state.ActiveChildId, default);
    }

    [Fact]
    public void State_RemoveChild_ThrowsArgumentExceptionIfGivenIdNotFoundInChildren()
    {
        Assert.Throws<ArgumentException>(() => _state.RemoveChild(new("missingState")));
    }

    [Fact]
    public void State_RemoveChild_ThrowsInvalidOperationExceptionIfDefaultChildRemovedBeforeOthers()
    {
        State child = new("child1");
        State child2 = new("child2");

        _state.AddChild(child);
        _state.AddChild(child2);

        Assert.Throws<InvalidOperationException>(() => _state.RemoveChild(child.Id));
    }

    [Fact]
    public void State_RemoveChild_SetsChildParentToNull()
    {
        State child = new("child1");

        _state.AddChild(child);
        _state.RemoveChild(child.Id);

        Assert.Null(child.Parent);
    }

    [Fact]
    public void State_RemoveChild_RemovesAllTransitionsReferencingChild()
    {
        State first = new("first");
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        _state.AddChild(first);
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent);

        _state.RemoveChild(to.Id);

        Assert.Empty(_state.GetTransitions().Values);

        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent);

        _state.RemoveChild(from.Id);

        Assert.Empty(_state.GetTransitions().Values);
    }

    [Fact]
    public void State_ClearChildren_ReturnsCaller()
    {
        State caller = _state.ClearChildren();

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_ClearChildren_RemovesAllChildren()
    {
        State[] states = new State[]
        {
            new("child1"),
            new("child2"),
            new("child3"),
        };

        _state.AddChildren(states);
        _state.ClearChildren();

        Assert.Empty(_state.GetAllChildren());
    }

    [Fact]
    public void State_ClearChildren_RemovesAllTransitions()
    {
        State[] states = new State[]
        {
            new("child1"),
            new("child2"),
            new("child3"),
        };
        _state.AddChildren(states);
        _state.AddTransition(states[0].Id, states[1].Id, new EventId("event1"));
        _state.AddTransition(states[1].Id, states[2].Id, new EventId("event2"));

        _state.ClearChildren();

        Assert.Empty(_state.GetTransitions());
    }

    [Fact]
    public void State_ClearChildren_ResetsDefaultAndActiveChildIds()
    {
        State[] states = new State[]
        {
            new("child1"),
            new("child2"),
            new("child3"),
        };

        _state.AddChildren(states);
        _state.ClearChildren();

        Assert.Equal(_state.DefaultChildId, default);
        Assert.Equal(_state.ActiveChildId, default);
    }

    [Fact]
    public void State_ClearChildren_SetsAllChildrenParentToNull()
    {
        State[] states = new State[]
        {
            new("child1"),
            new("child2"),
            new("child3"),
        };

        _state.AddChildren(states);

        _state.ClearChildren();

        foreach (State child in states)
        {
            Assert.Null(child.Parent);
        }
    }

    [Fact]
    public void State_FireEvent_RespondsToEventIfEventResponseExists()
    {
        bool check = false;
        EventId eventId = new("MakeCheckTrue");
        _state.EventResponses.Add(
            eventId,
            new EventResponse(() => check = true, true));

        _state.FireEvent(eventId);

        Assert.True(check);
    }

    [Fact]
    public void State_FireEvent_ForwardsEventToParentIfNotConsumed()
    {
        bool check = false;
        EventId eventId = new("MakeCheckTrue");
        _state.EventResponses.Add(
            eventId,
            new EventResponse(() => check = true, true));
        State child = new("child");
        _state.AddChild(child);

        child.FireEvent(eventId);

        Assert.True(check);
    }

    [Fact]
    public void State_FireEvent_DoesNotForwardEventToParentIfConsumed()
    {
        bool check = false;
        EventId eventId = new("MakeCheckTrue");
        _state.EventResponses.Add(
            eventId,
            new EventResponse(() => check = true, false));
        State child = new("child");
        child.EventResponses.Add(
            eventId,
            new EventResponse(() => { }, true));

        child.FireEvent(eventId);

        Assert.False(check);
    }

    [Fact]
    public void State_FireEvent_TriggersTransitionIfOneExists()
    {
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent);

        _state.FireEvent(transitionEvent);

        Assert.Equal(_state.ActiveChildId, to.Id);
    }

    [Fact]
    public void State_FireEvent_DoesNotTriggerTransitionIfTransitionConditionReturnsFalse()
    {
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent, () => false);


        _state.FireEvent(transitionEvent);

        Assert.Equal(_state.ActiveChildId, from.Id);
    }

    [Fact]
    public void State_FireEvent_ExitsPreviousActiveChildIfTransitionTriggered()
    {
        bool check = false;
        State from = new("from");
        from.AddExitBehavior(() => check = true);
        State to = new("to");
        EventId transitionEvent = new("event");
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent);

        _state.FireEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_FireEvent_ExitsActiveBranchIfNonShallowTransitionTriggered()
    {
        bool check = false;
        State from = new("from");
        State childOfFrom = new("grandfrom");
        childOfFrom.AddExitBehavior(() => check = true);
        from.AddChild(childOfFrom);
        State to = new("to");
        State childOfTo = new("grandto");
        to.AddChild(childOfTo);
        EventId transitionEvent = new("event");
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent, isShallow: false);

        _state.FireEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_FireEvent_EntersNewActiveChildIfTransitionTriggered()
    {
        bool check = false;
        State from = new("from");
        State to = new("to");
        to.AddEnterBehavior(() => check = true);
        EventId transitionEvent = new("event");
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent);

        _state.FireEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_FireEvent_EntersNewActiveBranchIfNonShallowTransitionTriggered()
    {
        bool check = false;
        State from = new("from");
        State childOfFrom = new("grandfrom");
        from.AddChild(childOfFrom);
        State to = new("to");
        State childOfTo = new("grandto");
        childOfTo.AddEnterBehavior(() => check = true);
        to.AddChild(childOfTo);
        EventId transitionEvent = new("event");
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent, isShallow: false);

        _state.FireEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_FireEvent_PerformsTransitionBehaviorIfTransitionTriggered()
    {
        bool check = false;
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent, behavior: () => check = true);

        _state.FireEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_FireEvent_ConsumesEventIfTransitionTriggered()
    {
        bool check = false;
        _state.EventResponses.Add(
            new EventId("MakeCheckTrue"),
            new EventResponse(() => check = true, false));
        State child = new("child");
        _state.AddChild(child);
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        child.AddChild(from);
        child.AddChild(to);
        child.AddTransition(from.Id, to.Id, transitionEvent);

        child.FireEvent(transitionEvent);

        Assert.False(check);
    }

    [Fact]
    public void State_DrillEvent_HasActiveChildRespondToEventIfEventResponseExists()
    {
        bool check = false;
        EventId eventId = new("MakeCheckTrue");
        State child = new("child");
        child.EventResponses.Add(
            eventId,
            new EventResponse(() => check = true, true));
        _state.AddChild(child);

        _state.DrillEvent(eventId);

        Assert.True(check);
    }

    [Fact]
    public void State_DrillEvent_HasActiveChildTriggerTransitionIfOneExists()
    {
        State child = new("child");
        _state.AddChild(child);
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        child.AddChild(from);
        child.AddChild(to);
        child.AddTransition(from.Id, to.Id, transitionEvent);

        _state.DrillEvent(transitionEvent);

        Assert.Equal(child.ActiveChildId, to.Id);
    }

    [Fact]
    public void State_DrillEvent_DoesNotHaveActiveChildTriggerTransitionIfTransitionConditionReturnsFalse()
    {
        State child = new("child");
        _state.AddChild(child);
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        child.AddChild(from);
        child.AddChild(to);
        child.AddTransition(from.Id, to.Id, transitionEvent, () => false);

        _state.DrillEvent(transitionEvent);

        Assert.Equal(child.ActiveChildId, from.Id);
    }

    [Fact]
    public void State_DrillEvent_HasActiveChildExitPreviousActiveGrandChildIfTransitionTriggered()
    {
        bool check = false;
        State child = new("child");
        _state.AddChild(child);
        State from = new("from");
        from.AddExitBehavior(() => check = true);
        State to = new("to");
        EventId transitionEvent = new("event");
        child.AddChild(from);
        child.AddChild(to);
        child.AddTransition(from.Id, to.Id, transitionEvent);

        _state.DrillEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_DrillEvent_HasActiveChildExitActiveBranchIfNonShallowTransitionTriggered()
    {
        bool check = false;
        State child = new("child");
        _state.AddChild(child);
        State from = new("from");
        State childOfFrom = new("grandfrom");
        childOfFrom.AddExitBehavior(() => check = true);
        from.AddChild(childOfFrom);
        State to = new("to");
        State childOfTo = new("grandto");
        to.AddChild(childOfTo);
        EventId transitionEvent = new("event");
        child.AddChild(from);
        child.AddChild(to);
        child.AddTransition(from.Id, to.Id, transitionEvent, isShallow: false);

        _state.DrillEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_DrillEvent_HasActiveChildEnterNewActiveGrandChildIfTransitionTriggered()
    {
        bool check = false;
        State child = new("child");
        _state.AddChild(child);
        State from = new("from");
        State to = new("to");
        to.AddEnterBehavior(() => check = true);
        EventId transitionEvent = new("event");
        child.AddChild(from);
        child.AddChild(to);
        child.AddTransition(from.Id, to.Id, transitionEvent);

        _state.DrillEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_DrillEvent_HasActiveChildEnterNewActiveBranchIfNonShallowTransitionTriggered()
    {
        bool check = false;
        State child = new("child");
        _state.AddChild(child);
        State from = new("from");
        State childOfFrom = new("grandfrom");
        from.AddChild(childOfFrom);
        State to = new("to");
        State childOfTo = new("grandto");
        childOfTo.AddEnterBehavior(() => check = true);
        to.AddChild(childOfTo);
        EventId transitionEvent = new("event");
        child.AddChild(from);
        child.AddChild(to);
        child.AddTransition(from.Id, to.Id, transitionEvent, isShallow: false);

        _state.DrillEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_DrillEvent_HasActiveChildPerformTransitionBehaviorIfTransitionTriggered()
    {
        bool check = false;
        State child = new("child");
        _state.AddChild(child);
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        child.AddChild(from);
        child.AddChild(to);
        child.AddTransition(from.Id, to.Id, transitionEvent, behavior: () => check = true);

        _state.DrillEvent(transitionEvent);

        Assert.True(check);
    }

    [Fact]
    public void State_DrillEvent_ForwardsEventToActiveGrandChildIfNotConsumed()
    {
        bool check = false;
        EventId eventId = new("MakeCheckTrue");
        State child = new("child");
        child.EventResponses.Add(
            eventId,
            new EventResponse(() => { }, false));
        State grandchild = new("grandchild");
        grandchild.EventResponses.Add(
            eventId,
            new EventResponse(() => check = true, true));
        _state.AddChild(child);
        child.AddChild(grandchild);

        _state.DrillEvent(eventId);

        Assert.True(check);
    }

    [Fact]
    public void State_DrillEvent_DoesNotForwardEventToActiveGrandChildIfConsumed()
    {
        bool check = false;
        EventId eventId = new("MakeCheckTrue");
        State child = new("child");
        child.EventResponses.Add(
            eventId,
            new EventResponse(() => { }, true));
        State grandchild = new("grandchild");
        grandchild.EventResponses.Add(
            eventId,
            new EventResponse(() => check = true, false));
        _state.AddChild(child);
        child.AddChild(grandchild);

        _state.DrillEvent(eventId);

        Assert.False(check);
    }

    [Fact]
    public void State_DrillEvent_HasActiveChildConsumeEventIfTransitionTriggered()
    {
        bool check = false;
        State child = new("child");
        _state.AddChild(child);
        State from = new("from");
        State to = new("to");
        to.EventResponses.Add(
            new EventId("MakeCheckTrue"),
            new EventResponse(() => check = true, false));
        EventId transitionEvent = new("event");
        child.AddChild(from);
        child.AddChild(to);
        child.AddTransition(from.Id, to.Id, transitionEvent);

        _state.DrillEvent(transitionEvent);

        Assert.False(check);
    }

    [Fact]
    public void State_AddTransition_ReturnsCaller()
    {
        State from = new("from");
        State to = new("to");
        _state.AddChild(from);
        _state.AddChild(to);

        State caller = _state.AddTransition(from.Id, to.Id, new EventId("event"));

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_AddTransition_AddsTransitionToTransitions()
    {
        State from = new("from");
        State to = new("to");
        _state.AddChild(from);
        _state.AddChild(to);

        _state.AddTransition(from.Id, to.Id, new EventId("event"));

        Assert.Single(_state.GetTransitions().Values);
    }

    [Fact]
    public void State_AddTransition_ThrowsArgumentExceptionIfFromStateNotInChildren()
    {
        State from = new("from");
        State to = new("to");
        _state.AddChild(to);

        Assert.Throws<ArgumentException>(() => _state.AddTransition(from.Id, to.Id, new EventId("event")));
    }

    [Fact]
    public void State_AddTransition_ThrowsArgumentExceptionIfToStateNotInChildren()
    {
        State from = new("from");
        State to = new("to");
        _state.AddChild(from);

        Assert.Throws<ArgumentException>(() => _state.AddTransition(from.Id, to.Id, new EventId("event")));
    }

    [Fact]
    public void State_AddTransition_ThrowsArgumentExceptionIfFromStateIsTheSameAsToState()
    {
        StateId fromId = new("from");
        State from = new(new StateId("from"));
        _state.AddChild(from);

        Assert.Throws<ArgumentException>(() => _state.AddTransition(from.Id, fromId, new EventId("event")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void State_AddTransition_ThrowsArgumentExceptionIfOnEventIdIsNullEmptyOrWhiteSpace(string eventId)
    {
        State from = new("from");
        State to = new("to");
        _state.AddChild(from);
        _state.AddChild(to);

        Assert.Throws<ArgumentException>(() => _state.AddTransition(from.Id, to.Id, new EventId(eventId)));
    }

    [Fact]
    public void State_RemoveTransition_ReturnsCaller()
    {
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent);

        State caller = _state.RemoveTransition(transitionEvent, from.Id);

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_RemoveTransition_RemovesTransition()
    {
        State from = new("from");
        State to = new("to");
        EventId transitionEvent = new("event");
        _state.AddChild(from);
        _state.AddChild(to);
        _state.AddTransition(from.Id, to.Id, transitionEvent);

        _state.RemoveTransition(transitionEvent, from.Id);

        Assert.Empty(_state.GetTransitions().Values);
    }

    [Fact]
    public void State_AddEnterBehavior_ReturnsCaller()
    {
        State caller = _state.AddEnterBehavior(() => { });

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_AddEnterBehavior_AddsActionToEnterBehavior()
    {
        bool check = false;
        Machine machine = new("machine");

        machine.AddEnterBehavior(() => check = true);
        machine.Enter();

        Assert.True(check);
    }

    [Fact]
    public void State_RemoveEnterBehavior_ReturnsCaller()
    {
        State caller = _state.RemoveEnterBehavior(() => { });

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_RemoveEnterBehavior_RemovesActionFromEnterBehavior()
    {
        bool check = false;
        void behavior() => check = true;
        Machine machine = new("machine");
        machine.AddEnterBehavior(behavior);

        machine.RemoveEnterBehavior(behavior);
        machine.Enter();

        Assert.False(check);
    }

    [Fact]
    public void State_AddUpdateBehavior_ReturnsCaller()
    {
        State caller = _state.AddUpdateBehavior(() => { });

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_AddUpdateBehavior_AddsActionToUpdateBehavior()
    {
        bool check = false;
        Machine machine = new("machine");

        machine.AddUpdateBehavior(() => check = true);
        machine.Enter();
        machine.Update();

        Assert.True(check);
    }

    [Fact]
    public void State_RemoveUpdateBehavior_ReturnsCaller()
    {
        State caller = _state.RemoveUpdateBehavior(() => { });

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_RemoveUpdateBehavior_RemovesActionFromUpdateBehavior()
    {
        bool check = false;
        void behavior() => check = true;
        Machine machine = new("machine");
        machine.AddUpdateBehavior(behavior);

        machine.RemoveUpdateBehavior(behavior);
        machine.Enter();
        machine.Update();

        Assert.False(check);
    }

    [Fact]
    public void State_AddExitBehavior_ReturnsCaller()
    {
        State caller = _state.AddExitBehavior(() => { });

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_AddExitBehavior_AddsActionToExitBehavior()
    {
        bool check = false;
        Machine machine = new("machine");
        machine.AddExitBehavior(() => check = true);

        machine.Enter();
        machine.Exit();

        Assert.True(check);
    }

    [Fact]
    public void State_RemoveExitBehavior_ReturnsCaller()
    {
        State caller = _state.RemoveExitBehavior(() => { });

        Assert.Same(_state, caller);
    }

    [Fact]
    public void State_RemoveExitBehavior_RemovesActionFromExitBehavior()
    {
        bool check = false;
        void behavior() => check = true;
        Machine machine = new("machine");
        machine.AddExitBehavior(behavior);

        machine.RemoveExitBehavior(behavior);
        machine.Enter();
        machine.Exit();

        Assert.False(check);
    }
}
