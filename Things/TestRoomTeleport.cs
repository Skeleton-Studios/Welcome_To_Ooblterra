using Dissonance;
using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things
{
    public class TestRoomTeleport : MonoBehaviour
    {
        // Set to True for the teleporter that's in the actual level.
        // The other one exists inside of the Prefab for the test room.
        public bool IsLevelTeleporter = false;

        private GameObject? spawnedTestRoom = null;
        private Vector3 target;
        private List<GameObject> spawnedSyncedObjects = [];

        private static readonly WTOBase.WTOLogger Log = new(typeof(TestRoomTeleport), LogSourceType.Thing);

        public void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent<PlayerControllerB>(out var playerControllerB))
            {
                playerControllerB.TeleportPlayer(target);
            }
        }

        void Start()
        {
            if (WTOBase.WTOTestRoom.Value)
            {
                if (IsLevelTeleporter)
                {
                    GameObject TestRoomPrefab = WTOBase.ContextualLoadAsset<GameObject>("CustomDungeon/TestRoom.prefab");
                    spawnedTestRoom = Instantiate(TestRoomPrefab, new Vector3(500, 500, 500), Quaternion.identity);

                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                    {
                        foreach (var netObj in spawnedTestRoom.GetComponentsInChildren<NetworkObject>(includeInactive: true))
                        {
                            netObj.Spawn(destroyWithScene: true);
                        }

                        var mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");
                        foreach (var syncedObj in spawnedTestRoom.GetComponentsInChildren<SpawnSyncedObject>(includeInactive: true))
                        {
                            GameObject gameObject = Instantiate(syncedObj.spawnPrefab, syncedObj.transform.position, syncedObj.transform.rotation, mapPropsContainer.transform);
                            if (gameObject != null)
                            {
                                if(gameObject.TryGetComponent<NetworkObject>(out var netObj)) 
                                {
                                    netObj.Spawn(destroyWithScene: true);
                                }
                                else
                                {
                                    Log.Warning($"Spawned object {gameObject.name} does not have a NetworkObject component and cannot be synced across the network.");
                                    Log.Warning($"Spawned from sync object prefab: {syncedObj.spawnPrefab.name}");
                                }
                                spawnedSyncedObjects.Add(gameObject);
                            }
                        }
                    }

                    for (int i = 0; i < spawnedTestRoom.transform.childCount; i++)
                    {
                        if(spawnedTestRoom.transform.GetChild(i).TryGetComponent<TestRoomTeleport>(out var returnTeleport))
                        {
                            target = returnTeleport.transform.position + (Vector3.up * 3) + (Vector3.forward * 3);
                            returnTeleport.target = transform.position + (Vector3.up * 3) + (Vector3.forward * 3);
                            break;
                        }
                    }
                }
            }
            else
            {
                // Disappear if not in test room mode
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            if (spawnedTestRoom != null)
            {
                Destroy(spawnedTestRoom);

                foreach (var obj in spawnedSyncedObjects)
                {
                    if (obj != null)
                    {
                        obj.GetComponent<NetworkObject>().Despawn();
                    }
                }
                spawnedSyncedObjects.Clear();
            }
        }
    }
}
