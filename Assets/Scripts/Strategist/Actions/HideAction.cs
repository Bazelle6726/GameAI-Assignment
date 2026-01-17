using UnityEngine;

public class HideAction : UtilityAction
{
    private Vector3 fleePosition;

    public HideAction(StrategistController strategist) : base(strategist) { }

    public override float CalculateUtility()
    {
        // No threat? No need to hide
        if (strategist.CurrentEnemy == null)
            return 0f;

        // Calculate utility based on health (lower health = more urgent to hide)
        float healthPercentage = strategist.currentHealth / strategist.maxHealth;
        float healthUrgency = 1f - healthPercentage;

        // Calculate distance to threat (closer threat = more urgent)
        float distanceToEnemy = Vector3.Distance(strategist.transform.position, strategist.CurrentEnemy.position);
        float enemyProximity = 1f - Mathf.Clamp01(distanceToEnemy / strategist.detectionRadius);

        // Combine factors (health is most important)
        float utility = (healthUrgency * 0.7f) + (enemyProximity * 0.3f);

        // Boost utility significantly if health is critical (below 30%)
        if (healthPercentage < 0.3f)
        {
            utility = Mathf.Max(utility, 0.8f); // Ensure high priority
        }

        return utility;
    }

    public override void Execute()
    {
        if (strategist.CurrentEnemy == null)
            return;

        // Calculate flee direction (away from threat)
        Vector3 directionAwayFromEnemy = (strategist.transform.position - strategist.CurrentEnemy.position).normalized;
        // Find flee position far away from threat
        fleePosition = strategist.transform.position + (directionAwayFromEnemy * 15f);

        // Move fast while fleeing
        strategist.agent.speed = strategist.fleeSpeed;
        strategist.agent.SetDestination(fleePosition);

        Debug.DrawLine(strategist.transform.position, fleePosition, Color.magenta);
    }
}
