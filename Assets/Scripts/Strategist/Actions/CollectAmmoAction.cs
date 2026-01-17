using UnityEngine;

public class CollectAmmoAction : UtilityAction
{
    public CollectAmmoAction(StrategistController strategist) : base(strategist) { }

    public override float CalculateUtility()
    {
        // Calculate ammo percentage
        float ammoPercentage = (float)strategist.currentAmmo / strategist.maxAmmo;

        // If ammo is above 70%, don't bother collecting more
        if (ammoPercentage > 0.7f)
        {
            return 0f;
        }

        // No ammo box available? Utility = 0
        AmmoBox nearestAmmoBox = strategist.FindNearestAmmoBox();
        if (nearestAmmoBox == null)
        {
            Debug.Log("[CollectAmmo] No ammo box available, returning 0 utility");
            return 0f;
        }

        // Calculate utility based on ammo percentage (lower ammo = higher utility)
        float ammoUrgency = 1f - ammoPercentage;

        // Factor in distance
        float distance = Vector3.Distance(strategist.transform.position, nearestAmmoBox.transform.position);
        float maxDistance = 50f;
        float distanceFactor = 1f - Mathf.Clamp01(distance / maxDistance);

        // Combine factors (ammo need is more important than distance)
        float utility = (ammoUrgency * 0.8f) + (distanceFactor * 0.2f);

        Debug.Log($"[CollectAmmo] Urgency: {ammoUrgency:F2}, Distance Factor: {distanceFactor:F2}, Final Utility: {utility:F2}");

        return utility;
    }

    public override void Execute()
    {
        AmmoBox nearestAmmoBox = strategist.FindNearestAmmoBox();

        if (nearestAmmoBox == null)
        {
            strategist.TargetAmmoBox = null;
            return;
        }

        // Move towards ammo box
        strategist.agent.speed = strategist.normalSpeed;
        strategist.agent.SetDestination(nearestAmmoBox.transform.position);
        strategist.TargetAmmoBox = nearestAmmoBox;

        // Check if close enough to collect
        float distance = Vector3.Distance(strategist.transform.position, nearestAmmoBox.transform.position);
        if (distance < 1.5f && nearestAmmoBox.isAvailable)
        {
            // Collect ammo box
            strategist.AddAmmo(nearestAmmoBox.ammoAmount);
            nearestAmmoBox.Collect();
            strategist.TargetAmmoBox = null;
        }
    }
}