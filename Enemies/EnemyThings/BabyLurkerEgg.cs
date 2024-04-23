using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Patches;

namespace Welcome_To_Ooblterra.Things;
public class BabyLurkerEgg : NetworkBehaviour {

    private System.Random enemyRandom;
    public bool BabiesSpawned;
    public int BabiesToSpawn = 35;

    private void Start() { 
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
    }
    private void OnTriggerStay(Collider other) {
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (other.gameObject.CompareTag("Player")) {
            SpawnBabyLurkersServerRpc((int)victim.actualClientId);
        }
        if (BabiesSpawned) {
            return;
        }   
    }
    [ServerRpc]
    public void SpawnBabyLurkersServerRpc(int targetID) {
        SpawnBabyLurkersClientRpc(targetID);
    }
    [ClientRpc]
    public void SpawnBabyLurkersClientRpc(int targetID){ 
        BabiesSpawned = true;
        GameObject BabyLurkerPrefab = MonsterPatch.InsideEnemies.First(x => x.enemyType.enemyName == "Baby Lurker").enemyType.enemyPrefab;
        for (int i = 0; i < BabiesToSpawn; i++) { 
            GameObject BabyLurker = Instantiate(BabyLurkerPrefab);
            if (base.IsServer) {
                BabyLurker.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
                BabyLurker.gameObject.GetComponentInChildren<BabyLurkerAI>().SetTargetServerRpc(targetID);
            }
        }
    }
}
