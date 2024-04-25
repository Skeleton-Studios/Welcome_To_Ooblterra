using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Enemies.EnemyThings;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class BabyLurkerEgg : NetworkBehaviour {

    private System.Random enemyRandom;
    private GameObject HiveProjectile;
    public GameObject HiveMesh;
    public GameObject projectileTemplate;
    public Transform DropTransform;
    private bool EggSpawned = false;
    private bool EggDropped = false;
    private float SecondsUntilNextSpawnAttempt = 25f;

    private void OnTriggerStay(Collider other) {
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (other.gameObject.CompareTag("Player") && EggSpawned && !EggDropped) {
            SpawnProjectileServerRpc((int)victim.actualClientId); 
        }
    }
    private void Start() {
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        
    }
    private void Update() {
        if(SecondsUntilNextSpawnAttempt > 0) {
            SecondsUntilNextSpawnAttempt -= Time.deltaTime;
            return;
        }
        SpawnEggClientRpc();
        /*
        if(enemyRandom.Next(0, 100) < 1000) {
            
        } else {
            SecondsUntilNextSpawnAttempt = enemyRandom.Next(15, 40);
        }*/
    }



    [ServerRpc]
    public void SpawnEggServerRpc() {
        SpawnEggClientRpc();
    }
    [ClientRpc]
    public void SpawnEggClientRpc() {
        if (EggSpawned) {
            return;
        }
        HiveMesh.SetActive(true);
        WTOBase.LogToConsole($"Lurker Egg active!");
        EggSpawned = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnProjectileServerRpc(int targetID) {
        SpawnProjectileClientRpc(targetID);
    }
    [ClientRpc] 
    public void SpawnProjectileClientRpc(int targetID){
        if (EggDropped) {
            return;
        }
        WTOBase.LogToConsole($"Lurker egg projectile being spawned!");
        HiveProjectile = GameObject.Instantiate(projectileTemplate, DropTransform.position, DropTransform.rotation);
        HiveProjectile.GetComponent<BabyLurkerEggProjectile>().TargetID = targetID;
        EggDropped = true;
        Destroy(HiveMesh); 
         
    }
}
