using UnityEngine;

public class CollectAmmo : UtilityAction
{
    public CollectAmmoAction(StrategistController strategist) : base(strategist) { }

    public override float CalculateUtility()
    {
        // No ammo crate available? Utility = 0
        AmmoBox nearestAmmoBox = strategist.FindNearestAmmoBox();
        if (nearestAmmoBox == null)
            return 0f;

        // Calculate utility based on ammo percentage (lower ammo = higher utility)
        float ammoPercentage = (float)strategist.currentAmmo / strategist.maxAmmo;
        float ammoUrgency = 1f - ammoPercentage; // 0.0 (full ammo) to 1.0 (no ammo)

        // Factor in distance
        float distance = Vector3.Distance(strategist.transform.position, nearestAmmoBox.transform.position);
        float maxDistance = 50f;
        float distanceFactor = 1f - Mathf.Clamp01(distance / maxDistance);

        // Combine factors
        float utility = (ammoUrgency * 0.7f) + (distanceFactor * 0.3f);

        return utility;
    }

    public override void Execute()
    {
        AmmoBox nearestAmmoBox = strategist.FindNearestAmmoBox();

        if (nearestAmmoBox == null)
            return;

        // Move towards ammo box
        strategist.agent.speed = strategist.normalSpeed;
        strategist.agent.SetDestination(nearestAmmoCrate.transform.position);
        strategist.TargetAmmoCrate = nearestAmmoCrate;

        // Check if close enough to collect
        float distance = Vector3.Distance(strategist.transform.position, nearestAmmoCrate.transform.position);
        if (distance < 1.5f && nearestAmmoCrate.isAvailable)
        {
            // Collect ammo box
            strategist.AddAmmo(nearestAmmoBox.ammoAmount);
            nearestAmmoBox.Collect();
            strategist.TargetAmmoBox = null;
        }
    }
}
