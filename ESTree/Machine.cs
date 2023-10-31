using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ESTree;

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
