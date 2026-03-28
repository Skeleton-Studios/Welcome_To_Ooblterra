using Dissonance;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things
{
    public class TestRoom : NetworkBehaviour
    {
        private static readonly WTOBase.WTOLogger Log = new(typeof(TestRoom), LogSourceType.Thing);

        [SerializeField]
        private GameObject returnTeleporterPosition;

        private GameObject? moonTeleporter;

        private List<GameObject> spawnedSyncedObjects = [];

        void Start()
        {
            if (!IsServer)
            {
                return;
            }

            var mapPropsContainer = GameObject.FindGameObjectWithTag("MapPropsContainer");

            foreach (var syncedObj in GetComponentsInChildren<SpawnSyncedObject>(includeInactive: true))
            {
                GameObject gameObject = Instantiate(syncedObj.spawnPrefab, syncedObj.transform.position, syncedObj.transform.rotation, mapPropsContainer.transform);
                if (gameObject != null)
                {
                    if (gameObject.TryGetComponent<NetworkObject>(out var netObj))
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

        public Vector3 AttachMoonTeleporter(TestRoomTeleport moonTeleporter) 
        {
            this.moonTeleporter = moonTeleporter.gameObject;
            return returnTeleporterPosition.transform.position + (Vector3.up * 3) + (Vector3.forward * 5);
        }

        public Vector3 GetReturnTeleporterTargetPosition()
        {
            if(moonTeleporter == null)
            {
                Log.Error("Attempted to get moon teleporter position before it was attached. This should never happen.");
                return Vector3.zero;
            }
            return moonTeleporter.transform.position + (Vector3.up * 3) + (Vector3.forward * 5);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if (!IsServer)
            {
                return;
            }

            foreach (var obj in spawnedSyncedObjects)
            {
                Destroy(obj);
            }
        }
    }
}
