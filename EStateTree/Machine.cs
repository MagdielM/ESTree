namespace EStateTree;

public class Machine : State
{
    private bool entered;

    /// <summary>
    /// The constructor for <see cref="Machine"/>s.
    /// </summary>
    ///
    /// <param name="id">The <see cref="StateId"/> that the new machine will have.</param>
    ///
    /// <exception cref="ArgumentException">
    /// <paramref name="id"/> is <c>null</c>, empty, or comprised solely of whitespace.
    /// </exception>
    public Machine(StateId id) : base(id) { }
    public Machine(string id) : base(id) { }

    /// <summary>
    /// Executes the <see cref="State.EnterState"/> method of this machine. Does nothing if
    /// called more than once without calling <see cref="Exit"/> first.
    /// </summary>
    public void Enter()
    {
        if (entered) return;
        EnterState();
        entered = true;
    }

    /// <summary>
    /// Executes the <see cref="State.UpdateState"/> method of this machine.
    /// </summary>
    public void Update()
    {
        UpdateState();
    }

    /// <summary>
    /// Executes the <see cref="State.ExitState"/> method of this machine. Does nothing if
    /// called before calling <see cref="Enter"/>.
    /// </summary>
    public void Exit()
    {
        if (!entered) return;
        ExitState();
        entered = false;
    }
}
