using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things;
internal class BlinkingMonitors : MonoBehaviour {

    System.Random BlinkRandom;
    bool ShouldBlink;
    int BlinkTimer;
    public Material EyeMaterial;
    public MeshRenderer MonitorMesh;

    public void Awake() {
        MonitorMesh.materials[1] = EyeMaterial;
        BlinkRandom = new System.Random();
    }
    public void Update() {
        if(ShouldBlink) {
            StartCoroutine(BlinkEye());
        }
        if(BlinkRandom.Next(0, 100) < 99) {
            return;
        }

    }
    IEnumerator BlinkEye() {
        if(BlinkTimer < 5) {
            EyeMaterial.SetInt("_ShouldBlink", 1);
            BlinkTimer++;
        } else {
            EyeMaterial.SetInt("_ShouldBlink", 0);
            BlinkTimer = 0;
            StopCoroutine(BlinkEye());
        }
        yield return null;
    }
}
