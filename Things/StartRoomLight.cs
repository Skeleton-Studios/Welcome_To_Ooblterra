using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things;
public class StartRoomLight : MonoBehaviour {

    public Light[] CentralLights;
    public MeshRenderer CentralPillar;
    
    public void SetCentralRoomWhite() {
        Material[] PillarMatArray = CentralPillar.materials;
        PillarMatArray[6].SetColor("_EmissiveColor", Color.white);
        CentralPillar.materials = PillarMatArray;
        foreach(Light CentralLight in CentralLights) {
            CentralLight.color = Color.white;
        }
    }
}
