namespace EStateTree;

/// <summary>
/// A single unit of logic in a hierarchical finite state machine.
/// </summary>
///
/// <remarks>
/// <para>
/// States may contain any number of child states, and consider only one of these children to
/// be active at any given time. Any individual lineage of children in a state machine is
/// known as a branch. The lineage of all children in the containing machine that are
/// considered active at one time is known as the active branch.
/// </para>
///
/// <para>
/// A state that contains children will define a default child. Typically this is the first
/// child added to the state.
/// </para>
///
/// <para>
/// States may define behavior to be executed upon the state initially becoming active,
/// behavior to be executed on demand, and behavior to be executed upon becoming inactive.
/// These are known as entry, update, and exit behaviors respectively. Entry and update
/// behaviors are executed top-to-bottom: the outermost states execute first, while
/// the innermost states execute last. Exit behaviors, however, are executed bottom-to-top.
/// </para>
///
/// <para>
/// States may also contain transitions between children, which change the active child of
/// the state. Transitions may define conditions to be met in order for the transition to
/// occurr. When a transition is performed, any exit behavior defined by the current active
/// child will be performed. Then, any behavior defind by the transition itself will be
/// performed. Finally, any entry behavior defined by the new active child will be performed.
/// </para>
///
/// <para>
/// Transitions are marked as "shallow" by default. A shallow transition will only perform the
/// entry and exit behaviors of the states that are involved in the transition. Non-shallow
/// transitions will also execute the entry and exit behaviors of all states in the active
/// branches of the involved states.
/// </para>
///
/// <para>
/// States may propagate events up and down the active branch. Events are defined as
/// <see cref="EventId"/>s. Upon receiving an event, A state may choose to respond by
/// executing custom behavior or performing a transition. Said state may also consume the
/// received event. An event is consumed when the receiving state performs a transition in
/// response, or when the state responds with custom behavior that explicitly declares the
/// event consumed. A state that does not consume a received event via custom behavior may
/// still consume the event by performing a transition. Events that are consumed do not
/// propagate any further.
/// </para>
/// </remarks>
public class State
{
    /// <summary>
    /// The <see cref="StateId"/> that identifies this state.
    /// </summary>
    public StateId Id { get; }

    /// <summary>
    /// The <see cref="StateId"/> of this state's active child.
    /// </summary>
    public StateId ActiveChildId { get; private set; }
    /// <summary>
    /// The <see cref="StateId"/> of this state's default child.
    /// </summary>
    public StateId DefaultChildId { get; private set; }

    /// <summary>
    /// The state that contains this state within its children.
    /// </summary>
    public State? Parent { get; protected set; }

    /// <summary>
    /// A collection of responses to events that this state may perform.
    /// </summary>
    public Dictionary<EventId, EventResponse> EventResponses { get; } = new();

    private Dictionary<StateId, State> Children { get; } = new();

    private Dictionary<TransitionId, Transition> Transitions { get; } = new();

    private Action? enterBehavior;
    private Action? updateBehavior;
    private Action? exitBehavior;

    /// <summary>
    /// The constructor for <see cref="State"/>s.
    /// </summary>
    ///
    /// <param name="id">The <see cref="StateId"/> that the new state will have.</param>
    ///
    /// <exception cref="ArgumentException">
    /// <paramref name="id"/> is <c>null</c>, empty, or comprised solely of whitespace.
    /// </exception>
    public State(StateId id)
    {
        if (string.IsNullOrWhiteSpace(id.Id))
        {
            throw new ArgumentException(
                "State ID may not be null, empty, or comprised solely of white-space.",
                nameof(id));
        }

        Id = id;
    }
    public State(string id) : this(new StateId(id)) {}

    /// <summary>
    /// Indexer equivalent to <see cref="GetChild(StateId)"/>.
    /// </summary>
    public State this[StateId id] { get => GetChild(id); }
    public State this[string id] { get => GetChild(new StateId(id)); }

    /// <summary>
    /// Gets the child state with the specified <see cref="StateId"/>.
    /// </summary>
    ///
    /// <param name="id">The <see cref="StateId"/> of the target child.</param>
    ///
    /// <exception cref="KeyNotFoundException">
    /// State does not contain a child with the specified <paramref name="id"/>.
    /// </exception>"
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

    /// <summary>
    /// Returns a collection of all of this state's children.
    /// </summary>
    public IEnumerable<State> GetAllChildren() => Children.Values;

    /// <summary>
    /// Returns a read-only collection of all of this state's transitions.
    /// </summary>
    public IReadOnlyDictionary<TransitionId, Transition> GetTransitions() => Transitions;

    /// <summary>
    /// Adds the given child to this state's children.
    /// </summary>
    ///
    /// <param name="child">The child state to be added.</param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// The specified child is this state itself.
    /// </exception>
    ///
    /// <exception cref="InvalidOperationException">
    /// <para>The specified child already has a parent.</para>
    ///
    /// <para>-or-</para>
    ///
    /// <para>A cycle would be created in the hierarchy if the child was added.</para>
    /// </exception>
    public State AddChild(State child)
    {
        if (child == this)
            throw new ArgumentException("State may not add itself to its children.", nameof(child));

        if (child.Parent != null)
            throw new InvalidOperationException("Child must be removed from current parent before being added to new parent.");

        for (State? check = Parent; check != null; check = check.Parent)
        {
            if (check == child)
            {
                throw new InvalidOperationException($"Parenting {child.Id} to {Id} would create a cycle in the hierarchy."
                    + " This is not supported.");
            }
        }

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

    /// <summary>
    /// Adds all states in the given collection to this state's children.
    /// </summary>
    ///
    /// <param name="children">The collection of states to be added.</param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    public State AddChildren(IEnumerable<State> children)
    {
        foreach (var child in children) AddChild(child);

        return this;
    }

    /// <summary>
    /// Removes the child with the specified <see cref="StateId"/> from this state's children.
    /// </summary>
    ///
    /// <remarks>
    /// This method also ensures that this state's active and default children remain valid
    /// after a child is removed, reassigning them if necessary. It also removes any transitions
    /// contained within this state that reference the removed child.
    /// </remarks>
    ///
    /// <param name="id">The <see cref="StateId"/> of the child to be removed.</param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// This state contains no child with the specified <paramref name="id"/>.
    /// </exception>
    ///
    /// <exception cref="InvalidOperationException">
    /// An attempt was made to remove the default child while this state still contains other
    /// children.
    /// </exception>
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

    /// <summary>
    /// Removes all children from this state.
    /// </summary>
    /// 
    /// <remarks>
    /// This method also removes all transitions from this state, as well as setting this
    /// state's default and active child IDs to their default value.
    /// </remarks>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    public State ClearChildren()
    {
        foreach (var child in Children.Values) child.Parent = null;

        Children.Clear();
        Transitions.Clear();
        DefaultChildId = default;
        ActiveChildId = default;
        return this;
    }

    /// <summary>
    /// Adds a new transition to this state as defined by the specified parameters.
    /// </summary>
    ///
    /// <param name="from">
    /// The <see cref="StateId"/> of the child that must be active for the transition to
    /// begin.
    /// </param>
    ///
    /// <param name="to">
    /// The <see cref="StateId"/> of the child that will become the new active child once the
    /// transition concludes.
    /// </param>
    ///
    /// <param name="on">
    /// The <see cref="EventId"/> that should trigger the transition.
    /// </param>
    ///
    /// <param name="condition">
    /// A function that returns whether or not the transition may be performed.
    /// </param>
    ///
    /// <param name="behavior">
    /// An action to be invoked when the transition is performed.
    /// </param>
    ///
    /// <param name="isShallow">
    /// Determines whether or not the transition exits and enters the entire active branch
    /// when performed.
    /// </param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    ///
    /// <exception cref="ArgumentException">
    /// <para>
    /// No child among this state's children have either the <paramref name="from"/> or <paramref name="to"/> IDs.
    /// </para>
    ///
    /// <para>-or-</para>
    ///
    /// <para><paramref name="from"/> is the same as <paramref name="to"/>.</para>
    ///
    /// <para>-or-</para>
    ///
    /// <para>
    /// The <see cref="EventId.Id"/> of <paramref name="on"/> is either <c>null</c>, empty, or
    /// comprised solely of whitespace.
    /// </para>
    /// </exception>
    public State AddTransition(StateId from, StateId to, EventId on, Func<bool>? condition = null, Action? behavior = null, bool isShallow = true)
    {
        if (!Children.ContainsKey(from))
            throw new ArgumentException("State does not contain child with the given ID.", nameof(from));

        if (!Children.ContainsKey(to))
            throw new ArgumentException("State does not contain child with the given ID.", nameof(to));

        if (from == to)
            throw new ArgumentException($"{nameof(from)} may not be equal to {nameof(to)}.", nameof(to));

        if (string.IsNullOrWhiteSpace(on.Id))
            throw new ArgumentException("Event ID must not be null, empty, or comprised solely of whitespace.", nameof(on));

        Transitions.Add(new TransitionId(on, from), new Transition(to, condition, behavior, isShallow));

        return this;
    }

    /// <summary>
    /// Removes the transition with the specified origin child and event ID from this state's
    /// transitions.
    /// </summary>
    ///
    /// <param name="on">The event that triggers the transition to be removed.</param>
    ///
    /// <param name="from">
    /// The state that the transition to be removed would start from.
    /// </param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    public State RemoveTransition(EventId on, StateId from)
    {
        Transitions.Remove(new TransitionId(on, from));

        return this;
    }

    /// <summary>
    /// Adds the specified action to this states entry behavior.
    /// </summary>
    ///
    /// <param name="behavior">The action to be added.</param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    public State AddEnterBehavior(Action behavior)
    {
        enterBehavior += behavior;
        return this;
    }

    /// <summary>
    /// Removes the specified action from this state's entry behavior. Does nothing if the
    /// specified action is not contained within this state's entry behavior.
    /// </summary>
    ///
    /// <param name="behavior">The action to be removed.</param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    public State RemoveEnterBehavior(Action behavior)
    {
        enterBehavior -= behavior;
        return this;
    }

    /// <summary>
    /// Adds the specified action to this state's update behavior.
    /// </summary>
    ///
    /// <param name="behavior">The action to be added.</param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    public State AddUpdateBehavior(Action behavior)
    {
        updateBehavior += behavior;
        return this;
    }

    /// <summary>
    /// Removes the specified action from this state's update behavior. Does nothing if the
    /// specified action is not contained within this state's update behavior.
    /// </summary>
    ///
    /// <param name="behavior">The action to be removed.</param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    public State RemoveUpdateBehavior(Action behavior)
    {
        updateBehavior -= behavior;
        return this;
    }

    /// <summary>
    /// Adds the specified action to this state's exit behavior.
    /// </summary>
    ///
    /// <param name="behavior">The action to be added.</param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    public State AddExitBehavior(Action behavior)
    {
        exitBehavior += behavior;
        return this;
    }

    /// <summary>
    /// Removes the specified action from this state's exit behavior. Does nothing if the
    /// specified action is not contained within this state's exit behavior.
    /// </summary>
    ///
    /// <param name="behavior">The action to be removed.</param>
    ///
    /// <returns>
    /// The calling state, so that calls to this method may be chained with other calls.
    /// </returns>
    public State RemoveExitBehavior(Action behavior)
    {
        exitBehavior -= behavior;
        return this;
    }

    /// <summary>
    /// Attempts to consume the specified event and propagates it up the tree if it is not
    /// consumed, with each subsequent parent attempting to consume it. Does nothing if the
    /// state is part of a hierarchy and is not the active child of its parent.
    /// </summary>
    ///
    /// <remarks>
    /// An event is consumed when a state responds to the event with an
    /// <see cref="EventResponse"/> that has its
    /// <see cref="EventResponse.ShouldConsumeEvent"/> property set to true, or when it can
    /// successfully perform a transition that is triggered by the specified event. Events
    /// that have been consumed do not propagate further.
    /// </remarks>
    ///
    /// <param name="eventId">The event to be propagated upwards.</param>
    public void FireEvent(EventId eventId)
    {
        if (Parent != null && Parent.ActiveChildId != Id) return;

        if (TryTransition(eventId)) return;

        if (TryHandleEvent(eventId)) return;

        Parent?.FireEvent(eventId);
    }

    /// <summary>
    /// Propagates the specified event down the tree, with each subsequent active child
    /// attempting to consume it. Does nothing if the state is part of a hierarchy and is not
    /// the active child of its parent.
    /// </summary>
    ///
    /// <remarks>
    /// An event is consumed when a state responds to the event with an
    /// <see cref="EventResponse"/> that has its
    /// <see cref="EventResponse.ShouldConsumeEvent"/> property set to true, or when it can
    /// successfully perform a transition that is triggered by the specified event. Events
    /// that have been consumed do not propagate further.
    /// </remarks>
    ///
    /// <param name="eventId">The event to be propagated downwards.</param>
    public void DrillEvent(EventId eventId)
    {
        if (Parent != null && Parent.ActiveChildId != Id) return;
        if (Children.Count == 0) return;

        State child = Children[ActiveChildId];

        if (child.TryTransition(eventId)) return;

        if (child.TryHandleEvent(eventId)) return;

        child.DrillEvent(eventId);
    }

    /// <summary>
    /// Invokes any entry behavior that is assigned to this state, then calls
    /// <see cref="EnterState"/> on this state's active child, if one exists.
    /// </summary>
    ///
    /// <remarks>
    /// The order of execution results in the entry behaviors of the active branch being
    /// called top-to-bottom: the behavior of the outermost states will be invoked before that
    /// of the innermost states.
    /// </remarks>
    internal void EnterState()
    {
        enterBehavior?.Invoke();
        if (Children.TryGetValue(ActiveChildId, out State child))
        {
            child.EnterState();
        }
    }

    /// <summary>
    /// Invokes any update behavior that is assigned to this state, then calls
    /// <see cref="UpdateState"/> on this state's active child, if one exists.
    /// </summary>
    ///
    /// <remarks>
    /// Like with <see cref="EnterState"/>, the order of execution results in the update
    /// behaviors of the active branch being called top-to-bottom: the behavior of the
    /// outermost states will be invoked before that of the innermost states.
    /// </remarks>
    internal void UpdateState()
    {
        updateBehavior?.Invoke();
        if (Children.TryGetValue(ActiveChildId, out State child))
        {
            child.UpdateState();
        }
    }

    /// <summary>
    /// Invokes any entry behavior that is assigned to this state, then calls
    /// <see cref="EnterState"/> on this state's active child, if one exists.
    /// </summary>
    ///
    /// <remarks>
    /// Unlike with <see cref="EnterState"/> and <see cref="UpdateState"/>, the order of
    /// execution results in the exit behaviors of the active branch being called
    /// bottom-to-top: the behavior of the innermost states will be invoked before that of the
    /// outermost states.
    /// </remarks>
    internal void ExitState()
    {
        if (Children.TryGetValue(ActiveChildId, out State child))
        {
            child.ExitState();
        }
        exitBehavior?.Invoke();
    }

    internal bool TryHandleEvent(EventId eventId)
    {
        if (!EventResponses.TryGetValue(eventId, out EventResponse eventResponse)) return false;

        eventResponse.Response.Invoke();

        return eventResponse.ShouldConsumeEvent;
    }

    internal bool TryTransition(EventId eventId)
    {
        if (!Transitions.TryGetValue(new TransitionId(eventId, ActiveChildId), out Transition transition)) return false;

        if (transition.Condition?.Invoke() == false) return false;

        if (transition.IsShallow)
        {
            Children[ActiveChildId].exitBehavior?.Invoke();
        }
        else
        {
        Children[ActiveChildId].ExitState();
        }

        transition.Behavior?.Invoke();
        ActiveChildId = transition.To;

        if (transition.IsShallow)
        {
            Children[ActiveChildId].enterBehavior?.Invoke();
        }
        else
        {
        Children[ActiveChildId].EnterState();
        }

        return true;
    }
}