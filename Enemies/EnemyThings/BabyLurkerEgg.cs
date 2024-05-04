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
using static LethalLib.Modules.Enemies;

namespace Welcome_To_Ooblterra.Things;
public class BabyLurkerEgg : NetworkBehaviour {

    private System.Random enemyRandom;
    private GameObject HiveProjectile;
    public GameObject HiveMesh; 
    public GameObject projectileTemplate;
    public GameObject MapDot;
    public Transform DropTransform;
    public Transform TraceTransform; 
    public AudioClip[] BreakoffSound;
    private bool EggSpawned = false;
    private bool EggDropped = false;
    private float SecondsUntilNextSpawnAttempt = 15f;
    public ScanNodeProperties ScanNode;

    private void OnTriggerStay(Collider other) { 
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (other.gameObject.CompareTag("Player") && EggSpawned && !EggDropped) {
            SpawnProjectileServerRpc((int)victim.actualClientId); 
        }
    }
    private void Start() {
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        SecondsUntilNextSpawnAttempt = enemyRandom.Next(15, 40);
        ScanNode.creatureScanID = spawnableEnemies.FirstOrDefault((SpawnableEnemy x) => x.enemy.enemyName == "Baby Lurker").terminalNode.creatureFileID;
        SpawnEgg();
    }
    private void Update() {
        if (EggSpawned) {
            return;
        }
        if(SecondsUntilNextSpawnAttempt > 0) {
            SecondsUntilNextSpawnAttempt -= Time.deltaTime;
            return;
        }
        if(enemyRandom.Next(0, 100) < 60){
            SpawnEggServerRpc();
        } else {
            SecondsUntilNextSpawnAttempt = enemyRandom.Next(15, 40);
        }
    }

    public void SpawnEgg() {
        if (EggSpawned) {
            return;
        }
        HiveMesh.SetActive(true);
        MapDot.SetActive(true);
        if (Physics.Linecast(TraceTransform.position, TraceTransform.position + (Vector3.up * 5000), out RaycastHit HitResult, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore)) {
            HiveMesh.transform.position = HitResult.point;
            HiveMesh.transform.rotation = new Quaternion(180, 0, 0, 0);
            WTOBase.LogToConsole($"Lurker Egg active!");
            EggSpawned = true;
        } else { 
            WTOBase.LogToConsole($"Lurker Egg Line trace failed!");
        }

    }
    [ServerRpc]
    public void SpawnEggServerRpc() {
        SpawnEggClientRpc();
    }

    [ClientRpc]
    public void SpawnEggClientRpc() {
        SpawnEgg();
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
        GetComponent<AudioSource>()?.PlayOneShot(BreakoffSound[enemyRandom.Next(0, BreakoffSound.Length)]);
        WTOBase.LogToConsole($"Lurker egg projectile being spawned!");
        HiveProjectile = GameObject.Instantiate(projectileTemplate, DropTransform.position, DropTransform.rotation);
        HiveProjectile.GetComponent<BabyLurkerEggProjectile>().TargetID = targetID;
        EggDropped = true;
        Destroy(HiveMesh); 
         
    }
}
