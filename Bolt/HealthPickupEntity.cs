using UnityEngine;

public class HealthPickupEntity : CollectOnWalkOverPickupEntity<IHealthPickupState>
{
    public static void SpawnPickup(PickupSpawnPoint spawnPoint, PickupType pickupType)
    {
        SpawnPickup(BoltPrefabs.HealthPickupEntity, spawnPoint, pickupType);
    }
}
