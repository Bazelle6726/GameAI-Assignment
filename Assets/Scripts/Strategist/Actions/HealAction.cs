using UnityEngine;

public class HealAction : UtilityAction
{
    public HealAction(StrategistController strategist) : base(strategist) { }

    public override float CalculateUtility()
    {
        float healthPercentage = (float)strategist.currentHealth / strategist.maxHealth;

        // If health is above 70%, don't bother healing
        if (healthPercentage > 0.7f)
            return 0f;


        // No firstaid available? Utility = 0
        Firstaid nearestFirstaid = strategist.FindNearestFirstaid();
        if (nearestFirstaid == null)
            return 0f;

        // Calculate utility based on health percentage (lower health = higher utility)
        float healthUrgency = 1f - healthPercentage; // 0.0 (full health) to 1.0 (no health)

        // Factor in distance (closer = better)
        float distance = Vector3.Distance(strategist.transform.position, nearestFirstaid.transform.position);
        float maxDistance = 50f;
        float distanceFactor = 1f - Mathf.Clamp01(distance / maxDistance);

        // Combine factors (health is more important than distance)
        float utility = (healthUrgency * 0.8f) + (distanceFactor * 0.2f);

        return utility;
    }

    public override void Execute()
    {
        Firstaid nearestFirstaid = strategist.FindNearestFirstaid();

        if (nearestFirstaid == null)
        {
            strategist.TargetFirstaid = null;
            return;
        }
        // Move towards firstaid
        strategist.agent.speed = strategist.normalSpeed;
        strategist.agent.SetDestination(nearestFirstaid.transform.position);
        strategist.TargetFirstaid = nearestFirstaid;

        // Check if close enough to collect
        float distance = Vector3.Distance(strategist.transform.position, nearestFirstaid.transform.position);
        if (distance < 1.5f && nearestFirstaid.isAvailable)
        {
            // Collect firstaid
            strategist.Heal(nearestFirstaid.healAmount);
            nearestFirstaid.Collect();
            strategist.TargetFirstaid = null;

            Debug.Log("Firstaid collected");
        }
    }
}