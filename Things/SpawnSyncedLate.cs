using UnityEngine;
using Unity.Netcode;

namespace Welcome_To_Ooblterra.Things
{
    /// <summary>
    /// Spawns a networked prefab that is spawned as an outside object.
    /// LC does not spawn the network object for outside objects, so instead we simply
    /// create this object, then use this object to spawn the network object.
    /// - SpawnOutsideHazards()
    /// - SpawnSyncedLate::Start()
    /// - Spawns networked prefab as though it was an outside object.
    public class SpawnSyncedLate : MonoBehaviour
    {
        public GameObject prefab;
        public Vector3 offset;

        void Start()
        {
            if(RoundManager.Instance.IsServer)
            {
                GameObject instance = UnityEngine.Object.Instantiate(prefab, transform.position + offset, transform.rotation, RoundManager.Instance.mapPropsContainer.transform);
                if (instance != null)
                {
                    instance.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
                    RoundManager.Instance.spawnedSyncedObjects.Add(instance);
                }
            }
        }
    }
}
