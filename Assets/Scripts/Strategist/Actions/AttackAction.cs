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

        // Calculate utility based on health, ammo, and distance to enemy
        float healthPercentage = strategist.currentHealth / strategist.maxHealth;
        float ammoPercentage = (float)strategist.currentAmmo / strategist.maxAmmo;

        // Only attack with health and ammo
        float combatReadiness = (healthPercentage * 0.6f) + (ammoPercentage * 0.4f);

        // Calculate distance
        float distanceToEnemy = Vector3.Distance(strategist.transform.position, strategist.CurrentEnemy.position);
        float optimalRange = strategist.attackRange * 0.7f;
        float distanceFactor = 1f - Mathf.Abs(distanceToEnemy - optimalRange) / strategist.attackRange;
        distanceFactor = Mathf.Clamp01(distanceFactor);

        float utility = (combatReadiness * 0.7f) + (distanceFactor * 0.3f);

        // Don't attack if health is too low
        if (healthPercentage < 0.6f)
        {
            utility *= 0.2f;
        }

        //  survive
        if (healthPercentage < 0.3f)
        {
            return 0f;
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
            strategist.agent.SetDestination(strategist.transform.position); // Stop moving

            // Face enemy
            Vector3 directionToEnemy = (strategist.CurrentEnemy.position - strategist.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToEnemy.x, 0, directionToEnemy.z));
            strategist.transform.rotation = Quaternion.Slerp(strategist.transform.rotation, lookRotation, Time.deltaTime * 5f);

            // Attack with cooldown
            if (Time.time - strategist.lastAttackTime > strategist.attackCooldown)
            {
                strategist.currentAmmo--;
                strategist.lastAttackTime = Time.time;

                GuardController guard = strategist.CurrentEnemy.GetComponent<GuardController>();
                if (guard != null)
                {
                    guard.TakeDamage(strategist.attackDamage, strategist.transform);
                    Debug.Log($"<color=cyan>Strategist attacked Guard for {strategist.attackDamage} damage!</color>");
                }
            }
        }
    }
}