using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
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
    public bool CannotBeWhite;
    public bool SetColorByDistance;
    public Color CloseColor = Color.green;
    public Color FarColor = Color.red;
    public float MaxDistance = 300;
    private static System.Random LightRandom;
    private static int LightOnChance = -1;

    private void Start() {
        LightRandom ??= new System.Random(StartOfRound.Instance.randomMapSeed);
        if(LightOnChance == -1) {
            int NextChance = LightRandom.Next(4, 8);
            LightOnChance = NextChance * 10;
        }
        if (LightRandom.Next(0, 100) > LightOnChance) {
            SetLightBrightness(0);
            SetLightColor(Chemical.ChemColor.Clear);
            return;
        }
        SetLightColor(InitLightColor);
        SetLightBrightness(LightBrightness); 
        
    }
    private void Update() {
        if (!Application.IsPlaying(this)) {
            SetLightColor(InitLightColor);
            SetLightBrightness(LightBrightness);
        }
    }

    public void SetLightColor() {
        if (CannotBeWhite) {
            return;
        }
        var tempMaterial = new Material(StartMat);
        tempMaterial.SetColor("_EmissiveColor", new Color(1, 1, 1));
        ObjectWithMatToChange.sharedMaterial = tempMaterial;
        TargetLight.color = new Color(1, 1, 1);
    }
    private void SetLightColor(Chemical.ChemColor NextColor) {
        var tempMaterial = new Material(StartMat);
        tempMaterial.SetColor("_EmissiveColor", Chemical.GetColorFromEnum(NextColor));
        ObjectWithMatToChange.sharedMaterial = tempMaterial;

        TargetLight.color = Chemical.GetColorFromEnum(NextColor);
    }
    private void SetLightColor(Color NextColor) {
        var tempMaterial = new Material(StartMat);
        tempMaterial.SetColor("_EmissiveColor", NextColor);
        ObjectWithMatToChange.sharedMaterial = tempMaterial;

        TargetLight.color = NextColor;
    }
    public void SetLightBrightness(int Brightness) {
        TargetLight.intensity = Brightness;
    }

    public void SetColorRelative(Vector3 MachineLocation) {
        //if (!SetColorByDistance) {
            return;
        //}
        // float DistanceToMachine = Vector3.Distance(MachineLocation, this.transform.position);
        // float NormalizedDistance = DistanceToMachine/ MaxDistance;
        // NormalizedDistance = Mathf.Clamp(NormalizedDistance, 0, 1);
        // SetLightColor(Color.Lerp(CloseColor, FarColor, NormalizedDistance)); 
    }
}

