using System;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Enemies;

namespace Welcome_To_Ooblterra.Items;
internal class OoblCorpsePart : GrabbableObject {

#pragma warning disable 0649 // Assigned in Unity Editor
    public EnemyType OoblGhostTemplate;
#pragma warning restore 0649

    private OoblGhostAI MySpawnedGhost = null;

    private static readonly WTOBase.WTOLogger Log = new(typeof(OoblCorpsePart), LogSourceType.Item);

    public override void GrabItem() {
        base.GrabItem();
        OnGrabItemServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerHeldBy));
    }

    public override void DiscardItem() {
        base.DiscardItem();
        OnDiscardItemServerRpc();
    }

    [ServerRpc]
    private void OnGrabItemServerRpc(int clientId)
    {
        if(isInShipRoom)
        {
            return;
        }
        
        if (MySpawnedGhost)
        {
            // Change target to the player who most recently grabbed us
            MySpawnedGhost.IsMovingTowardPlayer = true;
            MySpawnedGhost.SetGhostTargetServerRpc(clientId);
            return;
        }

        Log.Info("Spawning Oobl Ghost");
        NetworkObjectReference NextGhost = RoundManager.Instance.SpawnEnemyGameObject(new Vector3(0, -700, 0), 0, 1, OoblGhostTemplate);
        if (NextGhost.TryGet(out NetworkObject GhostNetObject))
        {
            MySpawnedGhost = GhostNetObject.GetComponent<OoblGhostAI>();
            MySpawnedGhost.IsMovingTowardPlayer = true;
            MySpawnedGhost.LinkedCorpsePart = this;
            MySpawnedGhost.SetGhostTargetServerRpc(clientId);
        }
        else
        {
            Log.Error("Could not link this corpse part to the Oobl Ghost!");
        }
    }

    [ServerRpc]
    private void OnDiscardItemServerRpc()
    {
        if (MySpawnedGhost)
        {
            MySpawnedGhost.IsMovingTowardPlayer = false;
        }
    }

    public void DestroyCorpsePart()
    {
        if (IsHost)
        {
            DestroyCorpsePartClientRpc();
        }
        else
        {
            // probably unnecessary since this should not be called
            // from a client
            DestroyCorpsePartServerRpc();
        }
    }

    [ServerRpc]
    private void DestroyCorpsePartServerRpc() 
    {
        DestroyCorpsePartClientRpc();
    }

    [ClientRpc]
    private void DestroyCorpsePartClientRpc() {
        Log.Info($"Destroying: {this}");
        DestroyObjectInHand(playerHeldBy);
    }
}
