using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Items;

namespace Welcome_To_Ooblterra.Things;

[ExecuteAlways]
public class LightComponent : MonoBehaviour {

    [InspectorName("Default")]
    public Chemical.ChemColor InitLightColor = Chemical.ChemColor.Red;
    public int LightBrightness = 100;
    public Light TargetLight;
    public MeshRenderer ObjectWithMatToChange;
    public Material StartMat;
    public int LightMatIndex = 0;

    private void Start() {
        SetLightColor(InitLightColor);
        SetLightBrightness(LightBrightness);
    }

    private void Update() {
        if (!Application.IsPlaying(this)) {
            SetLightColor(InitLightColor);
            SetLightBrightness(LightBrightness);
        }
    }

    private void SetLightColor(Chemical.ChemColor NextColor) {
        var tempMaterial = new Material(StartMat);
        tempMaterial.SetColor("_EmissiveColor", Chemical.GetColorFromEnum(NextColor));
        ObjectWithMatToChange.sharedMaterial = tempMaterial;

        TargetLight.color = Chemical.GetColorFromEnum(NextColor);
    }

    public void SetLightBrightness(int Brightness) {
        TargetLight.intensity = Brightness;
    }
}

