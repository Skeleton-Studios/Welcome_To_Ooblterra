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

namespace Welcome_To_Ooblterra.Things;
public class BabyLurkerEgg : NetworkBehaviour {

    private System.Random enemyRandom;
    private GameObject HiveProjectile;
    public GameObject HiveMesh;
    public GameObject projectileTemplate;
    public Transform DropTransform;
    private float SecondsUntilNextSpawnAttempt = 25f;

    private void OnTriggerStay(Collider other) {
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (other.gameObject.CompareTag("Player")) {
            SpawnProjectileServerRpc((int)victim.actualClientId);
        }
    }
    private void Start() { 
        enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        //Raycast up to the ceiling. this will be where we put the base of the egg
    }
    private void Update() {
        if(SecondsUntilNextSpawnAttempt > 0) {
            SecondsUntilNextSpawnAttempt -= Time.deltaTime;
            return;
        }

        if(enemyRandom.Next(0, 100) < 1000) {
            SpawnEggServerRpc();
        } else {
            SecondsUntilNextSpawnAttempt = enemyRandom.Next(15, 40);
        }
    }



    [ServerRpc]
    public void SpawnEggServerRpc() {
        SpawnEggClientRpc();
    }
    [ClientRpc]
    public void SpawnEggClientRpc() {
        RaycastHit Linecast; 
        Physics.Linecast(transform.position, transform.up * 5000, out Linecast, StartOfRound.Instance.collidersRoomDefaultAndFoliage, QueryTriggerInteraction.Ignore);
        HiveMesh.transform.position = Linecast.transform.position;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnProjectileServerRpc(int targetID) {
        SpawnProjectileClientRpc(targetID);
    }
    [ClientRpc]
    public void SpawnProjectileClientRpc(int targetID){
        HiveProjectile = GameObject.Instantiate(projectileTemplate, DropTransform.position, DropTransform.rotation);
        HiveProjectile.GetComponent<BabyLurkerEggProjectile>().TargetID = targetID;
        //Destroy(this);
        
    }
}
