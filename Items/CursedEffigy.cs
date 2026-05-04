using GameNetcodeStuff;
using System;
using Unity.Netcode;
using UnityEngine.AI;

namespace Welcome_To_Ooblterra.Items
{
    internal class CursedEffigy : GrabbableObject {

#pragma warning disable 0649 // Assigned in Unity Editor
        public EnemyType TheMimic;
#pragma warning restore 0649

        private bool MimicSpawned;
        private PlayerControllerB? previousPlayerHeldBy;

        private static readonly WTOBase.WTOLogger Log = new(typeof(CursedEffigy), LogSourceType.Item);

        public override void Update() 
        {
            base.Update();

            if (IsServer && !MimicSpawned && previousPlayerHeldBy?.isPlayerDead == true) 
            {
                Log.Info($"Effigy knows that {previousPlayerHeldBy.playerUsername} is dead at position {previousPlayerHeldBy.deadBody.transform.position}");
                MimicSpawned = true;
                CreateMimic();
            }
        }

        public override void GrabItem() 
        {
            base.GrabItem();
            SetOwningPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerHeldBy));
        }

        public override void DiscardItem() 
        {
            base.DiscardItem();
            SetOwningPlayerServerRpc(-1);
        }

        [ServerRpc]
        public void SetOwningPlayerServerRpc(int OwnerID) 
        {
            if (OwnerID == -1)
            {
                // maintain reference to previous player held by if the player dies, but drop it if they're still alive.
                if (previousPlayerHeldBy != null && !previousPlayerHeldBy.isPlayerDead)
                {
                    previousPlayerHeldBy = null;
                }
                return;
            }
            previousPlayerHeldBy = StartOfRound.Instance.allPlayerScripts[OwnerID];
        }

        public void CreateMimic() 
        {
            if (previousPlayerHeldBy == null) 
            {
                Log.Error("Previousplayerheldby is null so the Ghost Player could not be spawned");
                return;
            }
            
            Log.Info($"Server creating Ghost Player from Effigy. Previous Player: {previousPlayerHeldBy.playerUsername}");
            NavMeshHit? MimicSpawnPos = Utils.GetRandomNavMeshPositionInRadiusExtended(previousPlayerHeldBy.deadBody.transform.position, 10f);
            if (!MimicSpawnPos.HasValue) 
            {
                Log.Error("No nav mesh found; no Ghost Player could be created");
                return;
            }

            NetworkObjectReference MimicNetObject = RoundManager.Instance.SpawnEnemyGameObject(MimicSpawnPos.Value.position, 0, -1, TheMimic);
            CreateMimicClientRpc(MimicNetObject, previousPlayerHeldBy.isInsideFactory, Array.IndexOf(StartOfRound.Instance.allPlayerScripts, previousPlayerHeldBy));
        }

        [ClientRpc]
        public void CreateMimicClientRpc(NetworkObjectReference netObjectRef, bool inFactory, int playerIndex) 
        {
            if (netObjectRef.TryGet(out var networkObject))
            {
                Log.Debug("Got network object for Ghost Player");
                MaskedPlayerEnemy MimicScript = networkObject.GetComponent<MaskedPlayerEnemy>();
                MimicScript.mimickingPlayer = previousPlayerHeldBy;
                MimicScript.rendererLOD0.material = WTOBase.ghostPlayerSuit;
                MimicScript.rendererLOD1.material = WTOBase.ghostPlayerSuit;
                MimicScript.rendererLOD2.material = WTOBase.ghostPlayerSuit;
                MimicScript.SetEnemyOutside(!previousPlayerHeldBy.isInsideFactory);
                MimicScript.SetVisibilityOfMaskedEnemy();

                //This makes it such that the mimic has no visible mask :)
                MimicScript.maskTypes[0].SetActive(value: false);
                MimicScript.maskTypes[1].SetActive(value: false);
                MimicScript.maskTypeIndex = 0;

                previousPlayerHeldBy.redirectToEnemy = MimicScript;
                previousPlayerHeldBy.deadBody.DeactivateBody(setActive: false);
            }

            DestroyObjectInHand(playerHeldBy);
        }
    }
}
