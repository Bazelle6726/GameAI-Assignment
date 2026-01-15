using UnityEngine;

public abstract class GuardState
{
    protected GuardController guard;
    protected GuardState(GuardController guard)
    {
        this.guard = guard;
    }

    public virtual void Enter()
    {
        Debug.Log("Entering " + this.GetType().Name);
    }

    public virtual void Update()
    {

    }
    public virtual void Exit()
    {
        Debug.Log("Exiting " + this.GetType().Name);
    }
}
