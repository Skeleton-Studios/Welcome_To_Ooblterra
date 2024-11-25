using System;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Enemies;

namespace Welcome_To_Ooblterra.Items;
internal class OoblCorpsePart : GrabbableObject {

    public EnemyType OoblGhostTemplate;
    private OoblGhostAI MySpawnedGhost = null;

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
            MySpawnedGhost.IsMovingTowardPlayer = true;
            return;
        }

        WTOBase.LogToConsole("Spawning Oobl Ghost");
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
            WTOBase.LogToConsole("Could not link this corpse part to the Oobl Ghost!");
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
        WTOBase.LogToConsole($"Destroying: {this}");
        DestroyObjectInHand(playerHeldBy);
    }
}
