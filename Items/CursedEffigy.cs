using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;
using System.Linq;

namespace Welcome_To_Ooblterra.Items;
internal class CursedEffigy : GrabbableObject {

#pragma warning disable 0649 // Assigned in Unity Editor
    public List<AudioClip> AmbientSounds;
    public AudioSource AudioPlayer;
    public EnemyType TheMimic;
#pragma warning restore 0649

    private bool MimicSpawned;
    private PlayerControllerB previousPlayerHeldBy;

    public override void Update() {
        base.Update();
        if (previousPlayerHeldBy == null) {
            return;
        }
        if (previousPlayerHeldBy.isPlayerDead) {
            if (!MimicSpawned && IsOwner) {
                WTOBase.LogToConsole($"Effigy knows that {previousPlayerHeldBy.playerUsername} is dead at position {previousPlayerHeldBy.deadBody.transform.position}");
                CreateMimicServerRpc(previousPlayerHeldBy.isInsideFactory, previousPlayerHeldBy.deadBody.transform.position);
                MimicSpawned = true;
                DestroyEffigyServerRpc();
            }
        }
    }

    public override void GrabItem() {
        base.GrabItem();
        SetOwningPlayerServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, playerHeldBy));
    }
    public override void DiscardItem() {
        base.DiscardItem();
        if (!previousPlayerHeldBy.isPlayerDead) {
            SetOwningPlayerServerRpc(-1);
        }
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

    [ServerRpc(RequireOwnership = false)]
    public void CreateMimicServerRpc(bool inFactory, Vector3 playerPositionAtDeath) {
        if (previousPlayerHeldBy == null) {
            WTOBase.LogToConsole("Previousplayerheldby is null so the Ghost Player could not be spawned");
            return;
        }
        WTOBase.LogToConsole($"Server creating Ghost Player from Effigy. Previous Player: {previousPlayerHeldBy.playerUsername}");
        Vector3 MimicSpawnPos = RoundManager.Instance.GetNavMeshPosition(playerPositionAtDeath, default, 10f);
        if (!RoundManager.Instance.GotNavMeshPositionResult) {
            WTOBase.LogToConsole("No nav mesh found; no Ghost Player could be created");
            return;
        }
        TheMimic = StartOfRound.Instance.levels.First(x => x.PlanetName == "8 Titan").Enemies.First(x => x.enemyType.enemyName == "Masked").enemyType;
        WTOBase.LogToConsole($"Masked Enemy Type Found: {TheMimic != null}");

        NetworkObjectReference MimicNetObject = RoundManager.Instance.SpawnEnemyGameObject(MimicSpawnPos, 0, -1, TheMimic);

        if (MimicNetObject.TryGet(out var networkObject)) {
            WTOBase.LogToConsole("Got network object for Ghost Player");
            MaskedPlayerEnemy MimicScript = networkObject.GetComponent<MaskedPlayerEnemy>();
            MimicScript.mimickingPlayer = previousPlayerHeldBy;
            Material suitMaterial = SuitPatch.GhostPlayerSuit;
            MimicScript.rendererLOD0.material = suitMaterial;
            MimicScript.rendererLOD1.material = suitMaterial;
            MimicScript.rendererLOD2.material = suitMaterial;
            MimicScript.SetEnemyOutside(!inFactory);
            MimicScript.SetVisibilityOfMaskedEnemy();

            //This makes it such that the mimic has no visible mask :)
            MimicScript.maskTypes[0].SetActive(value: false);
            MimicScript.maskTypes[1].SetActive(value: false);
            MimicScript.maskTypeIndex = 0;

            previousPlayerHeldBy.redirectToEnemy = MimicScript;
            previousPlayerHeldBy.deadBody.DeactivateBody(setActive: false);
        }
        CreateMimicClientRpc(MimicNetObject, inFactory);
    }
    [ClientRpc]
    public void CreateMimicClientRpc(NetworkObjectReference netObjectRef, bool inFactory) {
        StartCoroutine(WaitForMimicEnemySpawn(netObjectRef, inFactory));
    }
    private IEnumerator WaitForMimicEnemySpawn(NetworkObjectReference netObjectRef, bool inFactory) {
        NetworkObject netObject = null;
        float startTime = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || netObjectRef.TryGet(out netObject));
        if (previousPlayerHeldBy.deadBody == null) {
            startTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || previousPlayerHeldBy.deadBody != null);
        }
        previousPlayerHeldBy.deadBody.DeactivateBody(setActive: false);
        if (netObject == null) {
            yield break;
        }
        WTOBase.LogToConsole("Got network object for Ghost Player enemy client");
        MaskedPlayerEnemy MimicReference = netObject.GetComponent<MaskedPlayerEnemy>();
        MimicReference.mimickingPlayer = previousPlayerHeldBy;
        Material suitMaterial = SuitPatch.GhostPlayerSuit;
        MimicReference.rendererLOD0.material = suitMaterial;
        MimicReference.rendererLOD1.material = suitMaterial;
        MimicReference.rendererLOD2.material = suitMaterial;
        MimicReference.SetEnemyOutside(!inFactory);
        MimicReference.SetVisibilityOfMaskedEnemy();

        //This makes it such that the mimic has no visible mask :)
        MimicReference.maskTypes[0].SetActive(value: false);
        MimicReference.maskTypes[1].SetActive(value: false);
        MimicReference.maskTypeIndex = 0;

        previousPlayerHeldBy.redirectToEnemy = MimicReference;
    }

    [ServerRpc]
    private void DestroyEffigyServerRpc() {
        DestroyEffigyClientRpc();
    }
    [ClientRpc]
    private void DestroyEffigyClientRpc() {
        DestroyObjectInHand(playerHeldBy);
    }

}
