using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things {
    public class BabyLurkerEgg : NetworkBehaviour {
        private System.Random enemyRandom;
        private int TimeUntilBabySpawns;
        private int TimeInRange;
        public bool BabySpawned;

        private void Start() { 
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
            TimeUntilBabySpawns = enemyRandom.Next(180, 1200);
        }
        private void OnTriggerStay(Collider other) {
            if(BabySpawned) {
                return;
            }
            if (TimeInRange > TimeUntilBabySpawns) {
                SpawnBabyLurker();
            }
            TimeInRange++;
        }
        private void SpawnBabyLurker() { 
            BabySpawned = true;
            //animate the egg opening
            //INSTANCIATE BABY LURKER UnityEngine.Object.Instantiate()
        }
    }
}
