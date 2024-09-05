using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;
using System.Linq;
using Welcome_To_Ooblterra.Enemies;

namespace Welcome_To_Ooblterra.Items;
internal class OoblCorpsePart : GrabbableObject {

    PlayerControllerB previousPlayerHeldBy;
    private EnemyType OoblGhostTemplate;
    private OoblGhostAI MySpawnedGhost;

    public override void GrabItem() {
        base.GrabItem();
        SetOwningPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerHeldBy));
        //TODO: spawn in oobl ghost and make them attack the owning player
        if(MySpawnedGhost != null) {
            MySpawnedGhost.SetGhostTargetServerRpc((int)previousPlayerHeldBy.actualClientId);
            MySpawnedGhost.IsMovingTowardPlayer = true;
            return;
        }
        NetworkObjectReference NextGhost = RoundManager.Instance.SpawnEnemyGameObject(new Vector3(0, -700, 0), 0, 1, OoblGhostTemplate);
        if (NextGhost.TryGet(out NetworkObject GhostNetObject)){
            MySpawnedGhost = GhostNetObject.GetComponent<OoblGhostAI>();
            MySpawnedGhost.LinkedCorpsePart = this;
            MySpawnedGhost.SetGhostTargetServerRpc((int)previousPlayerHeldBy.actualClientId);
        } else {
            WTOBase.LogToConsole("Could not link this corpse part to the Oobl Ghost!");
        }
    }
    public override void DiscardItem() {
        base.DiscardItem();
        MySpawnedGhost.IsMovingTowardPlayer = false;
    }

    [ServerRpc]
    public void SetOwningPlayerServerRpc(int OwnerID) {
        SetOwningPlayerClientRpc(OwnerID);
    }
    [ClientRpc]
    public void SetOwningPlayerClientRpc(int OwnerID) {
        if (OwnerID == -1) {
            previousPlayerHeldBy = null;
            return;
        }
        previousPlayerHeldBy = StartOfRound.Instance.allPlayerScripts[OwnerID];
    }

    [ServerRpc]
    public void DestroyCorpsePartServerRpc() {
        DestroyCorpsePartClientRpc();
    }
    [ClientRpc]
    public void DestroyCorpsePartClientRpc() {
        if (isHeld) { 
            DestroyObjectInHand(playerHeldBy);
        } else {
            Destroy(this);
        }
    }
}
