using UnityEngine;

public abstract class UtilityAction
{
    protected StrategistController strategist;

    public UtilityAction(StrategistController strategist)
    {
        this.strategist = strategist;
    }

    public abstract float CalculateUtility();
    public abstract void Execute();
    public virtual string GetActionName()
    {
        return this.GetType().Name;
    }
}
