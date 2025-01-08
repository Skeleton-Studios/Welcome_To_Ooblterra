using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;

namespace Welcome_To_Ooblterra.Things;
internal class EyeSecScanner : MonoBehaviour {
    #pragma warning disable 0649 // Assigned in Unity Editor
    public EyeSecAI myAI;
    #pragma warning restore 0649

    private void OnTriggerStay(Collider other) {
        myAI.ScanOurEnemy(other);
    }
}
