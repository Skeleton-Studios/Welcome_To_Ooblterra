using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using System.Collections;
using UnityEngine.AI;
using Welcome_To_Ooblterra.Properties;
using LethalLib.Modules;

namespace Welcome_To_Ooblterra.Items;
internal class CursedEffigy : GrabbableObject {
    
    public List<AudioClip> AmbientSounds;
    public AudioSource AudioPlayer;
    public EnemyType TheMimic;

    private bool MimicSpawned;
    private PlayerControllerB MyOwner;
    private int OwnerID;
    public override void GrabItem() {
        base.GrabItem();
        MyOwner = playerHeldBy;
        OwnerID = Array.IndexOf(StartOfRound.Instance.allPlayerScripts, MyOwner);
        ChangeOwnershipOfProp(playerHeldBy.actualClientId);
    }
    public override void DiscardItem() {
        base.DiscardItem();
        if (!MyOwner.isPlayerDead) { 
            MyOwner = null;
            OwnerID = -1;
        }
    }
    public override void Update() {
        base.Update();
        if (MyOwner == null) {
            return;
        }
        if (MyOwner.isPlayerDead) {
            if (!MimicSpawned) {
                MyOwner = StartOfRound.Instance.allPlayerScripts[OwnerID];
                CreateMimicServerRpc(MyOwner.isInsideFactory, MyOwner.transform.position);
                MimicSpawned = true;
                WTOBase.LogToConsole($"Effigy knows that owning player {MyOwner} is dead!");
            }
            //Destroy(this);
        }
    }

    [ServerRpc]
    public void CreateMimicServerRpc(bool inFactory, Vector3 playerPositionAtDeath) {
        if (OwnerID == -1) {
            Debug.LogError("Effigy does not have owner!");
            return;
        }
        MyOwner = StartOfRound.Instance.allPlayerScripts[OwnerID];
        Debug.Log("Server creating mimic from Effigy");
        Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(playerPositionAtDeath, default, 10f);
        if (!RoundManager.Instance.GotNavMeshPositionResult) {
            Debug.Log("No nav mesh found; no WTOMimic could be created");
            return;
        }
        const int MimicIndex = 12;
        TheMimic = StartOfRound.Instance.levels[8].Enemies[MimicIndex].enemyType;
        Debug.Log($"Mimic Found: {TheMimic != null}");
        

        NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(navMeshPosition, 0, -1, TheMimic);

        if (netObjectRef.TryGet(out var networkObject)) {
            Debug.Log("Got network object for WTOMimic");
            MaskedPlayerEnemy component = networkObject.GetComponent<MaskedPlayerEnemy>();
            component.SetSuit(MyOwner.currentSuitID);
            component.mimickingPlayer = MyOwner;
            component.SetEnemyOutside(!inFactory);
            component.SetVisibilityOfMaskedEnemy();

            //This makes it such that the mimic has no visible mask :)
            component.maskTypes[0].SetActive(value: false);
            component.maskTypes[1].SetActive(value: false);
            component.maskTypeIndex = 0;

            MyOwner.redirectToEnemy = component;
            MyOwner.deadBody?.DeactivateBody(setActive: false);
        }
        CreateMimicClientRpc(netObjectRef, inFactory);
    }

    [ClientRpc]
    public void CreateMimicClientRpc(NetworkObjectReference netObjectRef, bool inFactory) {
        StartCoroutine(waitForMimicEnemySpawn(netObjectRef, inFactory));
    }

    private IEnumerator waitForMimicEnemySpawn(NetworkObjectReference netObjectRef, bool inFactory) {
        NetworkObject netObject = null;
        float startTime = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || netObjectRef.TryGet(out netObject));
        if (MyOwner.deadBody == null) {
            startTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || MyOwner.deadBody != null);
        }
        MyOwner.deadBody.DeactivateBody(setActive: false);
        if (netObject == null) {
            yield break;
        }
        Debug.Log("Got network object for WTOMimic enemy client");
        MaskedPlayerEnemy component = netObject.GetComponent<MaskedPlayerEnemy>();
        component.mimickingPlayer = MyOwner;
        component.SetSuit(MyOwner.currentSuitID);
        component.SetEnemyOutside(!inFactory);
        component.SetVisibilityOfMaskedEnemy();

        //This makes it such that the mimic has no visible mask :)
        component.maskTypes[0].SetActive(value: false);
        component.maskTypes[1].SetActive(value: false);
        component.maskTypeIndex = 0;

        MyOwner.redirectToEnemy = component;
    }
}
