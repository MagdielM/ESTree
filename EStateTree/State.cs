namespace EStateTree;

public class State
{
    public StateId Id { get; }

    public StateId ActiveChildId { get; private set; }
    public StateId DefaultChildId { get; private set; }

    public State? Parent { get; protected set; }

    public Dictionary<EventId, EventResponse> EventResponses { get; } = new();

    private Dictionary<StateId, State> Children { get; } = new();

    private Dictionary<TransitionId, Transition> Transitions { get; } = new();

    private Action? enterBehavior;
    private Action? updateBehavior;
    private Action? exitBehavior;

    public State(StateId id)
    {
        if (string.IsNullOrWhiteSpace(id.Id))
        {
            throw new ArgumentException(
                "State ID may not be null, empty, or comprised solely by white-space.",
                nameof(id));
        }

        Id = id;
    }

    public State this[StateId id] { get => GetChild(id); }

    public State GetChild(StateId id)
    {
        try
        {
            return Children[id];
        }
        catch (KeyNotFoundException ex)
        {
            throw new KeyNotFoundException($"State {id.Id} does not contain a child with ID {id.Id}.", ex);
        }
    }

    public IEnumerable<State> GetAllChildren() => Children.Values;

    public IReadOnlyDictionary<TransitionId, Transition> GetTransitions() => Transitions;

    public State AddChild(State child)
    {
        if (child == this)
            throw new ArgumentException("State may not add itself to its children.", nameof(child));

        Children.Add(child.Id, child);

        child.Parent = this;

        if (Children.Count == 1)
        {
            // First ever child. Set as default and active, and enter.
            DefaultChildId = child.Id;
            ActiveChildId = child.Id;
            child.EnterState();
        }

        return this;
    }

    public State AddChildren(IEnumerable<State> children)
    {
        foreach (var child in children) AddChild(child);

        return this;
    }

    public State RemoveChild(StateId id)
    {
        if (!Children.Remove(id, out State child))
            throw new ArgumentException($"State does not contain a child with the ID {id.Id}.", nameof(id));

        child.Parent = null;

        foreach (var pair in Transitions)
        {
            if (pair.Key.From == id || pair.Value.To == id) Transitions.Remove(pair.Key);
        }

        if (Children.Count == 0)
        {
            // All children removed, reset default and active IDs.
            DefaultChildId = default;
            ActiveChildId = default;
            return this;
        }

        if (id == DefaultChildId)
        {
            // Default child was not the last to be removed, throw exception.
            throw new InvalidOperationException("Default child must not be removed while other children remain.");
        }

        if (id == ActiveChildId)
        {
            // Active child was removed, fall back to default child.
            ActiveChildId = DefaultChildId;
            Children[ActiveChildId].EnterState();
        }

        return this;
    }

    public State ClearChildren()
    {
        foreach (var child in Children.Values) child.Parent = null;

        Children.Clear();
        Transitions.Clear();
        DefaultChildId = default;
        ActiveChildId = default;
        return this;
    }

    public State AddTransition(StateId from, StateId to, EventId on, Func<bool>? condition = null, Action? behavior = null)
    {
        if (!Children.ContainsKey(from))
        {
            throw new ArgumentException("State does not contain child with the given ID.", nameof(from));
        }
        if (!Children.ContainsKey(to))
        {
            throw new ArgumentException("State does not contain child with the given ID.", nameof(to));
        }

        Transitions.Add(new TransitionId(on, from), new Transition(to, condition, behavior));

        return this;
    }

    public State RemoveTransition(EventId on, StateId from)
    {
        Transitions.Remove(new TransitionId(on, from));

        return this;
    }

    public State AddEnterBehavior(Action behavior)
    {
        enterBehavior += behavior;
        return this;
    }

    public State RemoveEnterBehavior(Action behavior)
    {
        enterBehavior -= behavior;
        return this;
    }

    public State AddUpdateBehavior(Action behavior)
    {
        updateBehavior += behavior;
        return this;
    }

    public State RemoveUpdateBehavior(Action behavior)
    {
        updateBehavior -= behavior;
        return this;
    }

    public State AddExitBehavior(Action behavior)
    {
        exitBehavior += behavior;
        return this;
    }

    public State RemoveExitBehavior(Action behavior)
    {
        exitBehavior -= behavior;
        return this;
    }

    public void FireEvent(EventId eventId)
    {
        if (TryTransition(eventId)) return;

        if (TryHandleEvent(eventId)) return;

        Parent?.FireEvent(eventId);
    }

    public void DrillEvent(EventId eventId)
    {
        if (Children.Count == 0) return;

        State child = Children[ActiveChildId];

        if (child.TryTransition(eventId)) return;

        if (child.TryHandleEvent(eventId)) return;

        child.DrillEvent(eventId);
    }

    protected void EnterState()
    {
        enterBehavior?.Invoke();
        if (Children.TryGetValue(ActiveChildId, out State child))
        {
            child.EnterState();
        }
    }

    protected void UpdateState()
    {
        updateBehavior?.Invoke();
        if (Children.TryGetValue(ActiveChildId, out State child))
        {
            child.UpdateState();
        }
    }

    protected void ExitState()
    {
        exitBehavior?.Invoke();
        if (Children.TryGetValue(ActiveChildId, out State child))
        {
            child.ExitState();
        }
    }

    private bool TryHandleEvent(EventId eventId)
    {
        if (!EventResponses.TryGetValue(eventId, out EventResponse eventResponse)) return false;

        eventResponse.Response.Invoke();

        return eventResponse.ShouldConsumeEvent;
    }

    private bool TryTransition(EventId eventId)
    {
        if (!Transitions.TryGetValue(new TransitionId(eventId, ActiveChildId), out Transition transition)) return false;

        if (transition.Condition?.Invoke() == false) return false;

        Children[ActiveChildId].ExitState();
        transition.Behavior?.Invoke();
        ActiveChildId = transition.To;
        Children[ActiveChildId].EnterState();

        return true;
    }
}