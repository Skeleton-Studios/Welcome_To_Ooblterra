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

    public int TargetID;
    public int BabiesToSpawn = 35;

    private void OnTriggerEnter(Collider other) {
        WTOBase.LogToConsole($"Collision registered! Collider: {other.gameObject}");
        if(other.GetComponent<BabyLurkerEgg>() != null) {
            return;
        }
        GameObject BabyLurkerPrefab = MonsterPatch.InsideEnemies.First(x => x.enemyType.enemyName == "Baby Lurker").enemyType.enemyPrefab;
        for (int i = 0; i < BabiesToSpawn; i++) {
            GameObject BabyLurker = Instantiate(BabyLurkerPrefab);
            if (base.IsServer) {
                BabyLurker.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
                BabyLurker.gameObject.GetComponentInChildren<BabyLurkerAI>().SetTargetServerRpc(TargetID);
            }
        }
    }


}
