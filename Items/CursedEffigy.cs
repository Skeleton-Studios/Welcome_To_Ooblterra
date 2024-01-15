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

namespace Welcome_To_Ooblterra.Items;
internal class CursedEffigy : GrabbableObject {
    
    public List<AudioClip> AmbientSounds;
    public AudioSource AudioPlayer;
    public EnemyType TheMimic;

    private PlayerControllerB MyOwner;
    public override void GrabItem() {
        base.GrabItem();
        MyOwner = playerHeldBy;
    }
    public override void DiscardItem() {
        base.DiscardItem();
        if (!MyOwner.isPlayerDead) { 
            MyOwner = null;
        }
    }
    public override void Update() {
        base.Update();
        if (MyOwner == null) {
            return;
        }
        if (MyOwner.isPlayerDead) {
            WTOBase.LogToConsole($"Effigy knows that owning player {MyOwner} is dead!");
            //TurnPlayerToWTOMimic(playerHeldBy);
            Destroy(this);
        }
    }

    public void TurnPlayerToWTOMimic(PlayerControllerB PlayerToTurn) {
        if (PlayerToTurn == null || !PlayerToTurn.isPlayerDead) {
            return;
        }
        bool isInsideFactory = PlayerToTurn.isInsideFactory;
        CreateMimicServerRpc(isInsideFactory, PlayerToTurn.transform.position);
    }

    [ServerRpc]
    public void CreateMimicServerRpc(bool inFactory, Vector3 playerPositionAtDeath) {
        if (MyOwner == null) {
            Debug.LogError("Effigy does not have owner!");
            return;
        }
        Debug.Log("Server creating mimic from Effigy");
        Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(playerPositionAtDeath, default, 10f);
        if (!RoundManager.Instance.GotNavMeshPositionResult) {
            Debug.Log("No nav mesh found; no WTOMimic could be created");
            return;
        }
        if (TheMimic == null) {
            const int MimicIndex = 0;
            TheMimic = StartOfRound.Instance.levels[5].Enemies[MimicIndex].enemyType;
            Debug.Log($"Mimic Found: {TheMimic != null}");
        }

        NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(navMeshPosition, MyOwner.transform.eulerAngles.y, -1, TheMimic);

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
