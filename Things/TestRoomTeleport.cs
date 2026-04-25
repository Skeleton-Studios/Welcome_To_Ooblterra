using Dissonance;
using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things
{
    /// <summary>
    /// Responsible for managing the teleport that allows players to enter the test room.
    /// The teleporter is managed by the WTOTestRoom config option. If true, a prot statue exists
    /// near the ship that the player can jump on to teleport to the test room.
    /// </summary>
    public class TestRoomTeleport : NetworkBehaviour
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
            if(!IsClient || !other.gameObject.TryGetComponent<PlayerControllerB>(out var playerControllerB))
            {
                return;
            }

            // Tell server when a client touches us
            OnTriggerEnterServerRpc(playerControllerB);
        }

        void Start()
        {
            if (!WTOBase.WTOTestRoom.Value)
            {
                // Disappear if not in test room mode
                Destroy(gameObject);
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(!IsServer)
            {
                return;
            }

            // On being spawned, if this is the teleport that's inside the test room prefab,
            // we want to link it back out to the teleport on the main moon.
            if(!IsLevelTeleporter)
            {
                // Find the main teleporter and link to it
                TestRoom testRoom = FindObjectOfType<TestRoom>();
                if(testRoom == null)
                {
                    Log.Error("Failed to find TestRoom in scene on spawn of TestRoomTeleport. This should never happen.");
                    return;
                }
                target = testRoom.GetReturnTeleporterTargetPosition();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void OnTriggerEnterServerRpc(NetworkBehaviourReference playerControllerReference)
        { 
            // Do all test room spawning logic on server.
            // Only spawn and teleport the client after they touch.
            if(!playerControllerReference.TryGet(out var playerControllerB) || playerControllerB == null)
            {
                Log.Warning("Failed to get player controller reference in OnTriggerEnterServerRpc");
                return;
            }

            if (IsLevelTeleporter)
            {
                MaybeSpawnTestRoom();
            }

            // Tell the specific client that touched us to teleport to the test room
            OnTeleportPlayerClientRpc(target, playerControllerB, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = [ playerControllerB.OwnerClientId ]
                }
            });
        }

        [ClientRpc]
        void OnTeleportPlayerClientRpc(Vector3 targetPosition, NetworkBehaviourReference playerControllerReference, ClientRpcParams rpcParams = default)
        {
            if (!playerControllerReference.TryGet(out var playerControllerB) || playerControllerB == null)
            {
                Log.Warning("Failed to get player controller reference in OnTeleportPlayerClientRpc");
                return;
            }

            (playerControllerB as PlayerControllerB).TeleportPlayer(targetPosition);
        }

        void MaybeSpawnTestRoom()
        {
            if(spawnedTestRoom != null)
            {
                return;
            }

            GameObject TestRoomPrefab = WTOBase.ContextualLoadAsset<GameObject>("CustomDungeon/TestRoom.prefab");
            spawnedTestRoom = Instantiate(TestRoomPrefab, new Vector3(500, 500, 500), Quaternion.identity);

            TestRoom testRoom = spawnedTestRoom.GetComponent<TestRoom>();
            target = testRoom.AttachMoonTeleporter(this);
            Log.Info("Spawned test room at " + spawnedTestRoom.transform.position + " and set moon teleport target to " + target);

            spawnedTestRoom.GetComponent<NetworkObject>().Spawn(destroyWithScene: true);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

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
