using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;
using static UnityEngine.GraphicsBuffer;

namespace Welcome_To_Ooblterra.Enemies.EnemyThings;
public class BabyLurkerEggProjectile : NetworkBehaviour {

    public int TargetID = 0;
    public int BabiesToSpawn = 35;
     
    private void OnTriggerEnter(Collider other) {
        WTOBase.LogToConsole($"Collision registered! Collider: {other.gameObject}");
        if (other.GetComponent<BoxCollider>() != null || other.GetComponent<BabyLurkerAI>() != null || other.GetComponent<BabyLurkerProjectile>() != null) {
            return;
        } 
        EnemyType BabyLurker = MonsterPatch.InsideEnemies.First(x => x.enemyType.enemyName == "Baby Lurker").enemyType;
        Vector3 SpawnPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(transform.position, radius: 1);
        if(SpawnPosition == transform.position) {
            return;
        }
        for (int i = 0; i < BabiesToSpawn; i++) { 
            RoundManager.Instance.SpawnEnemyGameObject(SpawnPosition, 0, 1, BabyLurker);
        }
        DestroyEgg();
    }

    private void DestroyEgg() {
        Destroy(this.gameObject);
    }
}
