using UnityEngine;

public class AttackAction : UtilityAction
{
    public AttackAction(StrategistController strategist) : base(strategist) { }

    public override float CalculateUtility()
    {
        // No threat? Can't attack
        if (strategist.CurrentEnemy == null)
            return 0f;

        // Need ammo to attack
        if (strategist.currentAmmo <= 0)
            return 0f;

        // Calculate utility based on combat readiness
        float healthPercentage = strategist.currentHealth / strategist.maxHealth;
        float ammoPercentage = (float)strategist.currentAmmo / strategist.maxAmmo;

        // Only attack if we have decent health and ammo
        float combatReadiness = (healthPercentage * 0.6f) + (ammoPercentage * 0.4f);

        // Calculate distance factor (prefer medium range)
        float distanceToEnemy = Vector3.Distance(strategist.transform.position, strategist.CurrentEnemy.position);
        float optimalRange = strategist.attackRange * 0.7f;
        float distanceFactor = 1f - Mathf.Abs(distanceToEnemy - optimalRange) / strategist.attackRange;
        distanceFactor = Mathf.Clamp01(distanceFactor);

        // Combine factors
        float utility = (combatReadiness * 0.7f) + (distanceFactor * 0.3f);

        // Don't attack if health is too low (below 40%)
        if (healthPercentage < 0.4f)
        {
            utility *= 0.3f; // Heavily reduce attack utility
        }

        return utility;
    }

    public override void Execute()

    {
        if (strategist.CurrentEnemy == null || strategist.currentAmmo <= 0)
            return;

        float distanceToEnemy = Vector3.Distance(strategist.transform.position, strategist.CurrentEnemy.position);

        if (distanceToEnemy > strategist.attackRange)
        {
            // Move closer to attack range
            strategist.agent.speed = strategist.normalSpeed;
            strategist.agent.SetDestination(strategist.CurrentEnemy.position);
        }
        else
        {
            // In range - stop and attack
            strategist.agent.SetDestination(strategist.transform.position); // Stop moving

            // Face the threat
            Vector3 directionToEnemy = (strategist.CurrentEnemy.position - strategist.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToEnemy.x, 0, directionToEnemy.z));
            strategist.transform.rotation = Quaternion.Slerp(strategist.transform.rotation, lookRotation, Time.deltaTime * 5f);

            // Attack with cooldown
            if (Time.time - strategist.lastAttackTime > strategist.attackCooldown)
            {
                strategist.currentAmmo--;
                strategist.lastAttackTime = Time.time;  // Update last attack time
                GuardController guard = strategist.CurrentEnemy.GetComponent<GuardController>();
                if (guard != null)
                {
                    guard.TakeDamage(strategist.attackDamage);
                    Debug.Log($"Strategist attacked Guard for {strategist.attackDamage} damage!");
                }
                Debug.Log($"Strategist attacking! Ammo remaining: {strategist.currentAmmo}");
                // Here you would add actual attack logic (e.g., shooting, applying damage)
            }
        }
    }
}
