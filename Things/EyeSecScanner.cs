using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;

namespace Welcome_To_Ooblterra.Things {
    internal class EyeSecScanner : MonoBehaviour {
        public EyeSecAI myAI;

        private void OnTriggerStay(Collider other) {
            myAI.ScanOurEnemy(other);
        }
    }
}
