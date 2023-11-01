namespace EStateTree;

public class Machine : State
{
    private bool entered;

    public Machine(StateId id) : base(id)
    {
    }

    public void Enter()
    {
        if (entered) return;
        EnterState();
        entered = true;
    }

    public void Update()
    {
        UpdateState();
    }

    public void Exit()
    {
        if (!entered) return;
        ExitState();
        entered = false;
    }
}
