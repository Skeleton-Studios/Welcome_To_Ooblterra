using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;
using System.Collections;
using UnityEngine.AI;
using Welcome_To_Ooblterra.Properties;
using DunGen;

namespace Welcome_To_Ooblterra.Items;
public class Chemical : GrabbableObject {

    [InspectorName("Defaults")]
    public MeshRenderer BeakerMesh;

    [HideInInspector]
    public enum ChemColor {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Indigo,
        Purple
    }
    private ChemColor CurrentColor;
    private System.Random MyRandom;
    private int RandomEffectIndex;
    private float ShakeCooldownSeconds = 0;
    private bool HasChemicals = true;

    public override void Start() {
        base.Start();
        MyRandom = new System.Random();
        int NextInt = MyRandom.Next(0, 7);
        RandomEffectIndex = NextInt;
        CurrentColor = (ChemColor)Enum.GetValues(typeof(ChemColor)).GetValue(NextInt);
        BeakerMesh.materials[1].SetColor("_BaseColor", GetColorFromEnum(CurrentColor));
    }
    public override void Update() {
        base.Update();
        if(ShakeCooldownSeconds > 0) { 
            ShakeCooldownSeconds -= Time.deltaTime;
        }
    }
    public override void EquipItem() {
        base.EquipItem();
        playerHeldBy.equippedUsableItemQE = true;
    }

    public override void ItemInteractLeftRight(bool right) {
        base.ItemInteractLeftRight(right);
        if (!right && ShakeCooldownSeconds <= 0 && HasChemicals) {
            ShakeCooldownSeconds = 1.2f;
            playerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
            int NextInt = GetNextRandomInt();
            WTOBase.LogToConsole($"Next Int Value: {NextInt}");
            RandomEffectIndex = NextInt;
            CurrentColor = (ChemColor)Enum.GetValues(typeof(ChemColor)).GetValue(NextInt);
            WTOBase.LogToConsole($"Next Color Value: {CurrentColor}");
            BeakerMesh.materials[1].SetColor("_BaseColor", GetColorFromEnum(CurrentColor));
        }
    }
    private int GetNextRandomInt() {
        int NextInt = MyRandom.Next(0, 7);
        if(NextInt != RandomEffectIndex) {
            return NextInt;
        }
        return GetNextRandomInt();
    }

    public ChemColor GetCurrentColor() {
        return CurrentColor;
    }
    private void DrinkChemicals() {
        if (HasChemicals) { 
            RandomChemEffectServerRpc(RandomEffectIndex);
        }
    }
    private Color GetColorFromEnum(ChemColor inColor) {
        switch(inColor) {
            case ChemColor.Red:
                return new Color(1, 0, 0);
            case ChemColor.Orange:
                return new Color(1, 0.4f, 0);
            case ChemColor.Yellow:
                return new Color(1, 1, 0);
            case ChemColor.Green:
                return new Color(0, 1, 0);
            case ChemColor.Blue:
                return new Color(0, 1, 1);
            case ChemColor.Indigo:
                return new Color(0, 0, 1);
            case ChemColor.Purple:
                return new Color(1, 0, 1);
            default:
                return new Color(1, 1, 1);
        }
    }
    private void RandomChemEffect(int RandomIndex) {
        HasChemicals = false;
        switch (RandomIndex) {
            case 0:
                break;
            case 1:
                break;
            case 2:
                break;
            case 3:
                //Case 3 is a dud and does nothing
                break;
            case 4:
                break;
            case 5:
                break;
            case 6:
                break;
            default:
                break;
        }
        BeakerMesh.materials[1].SetColor("_BaseColor", new Color(0, 0, 0, 0));
    }

    [ServerRpc]
    private void RandomChemEffectServerRpc(int RandomIndex) {
        RandomChemEffectClientRpc(RandomIndex);
    }

    [ClientRpc]
    private void RandomChemEffectClientRpc(int RandomIndex) {
        RandomChemEffect(RandomIndex);
    }
}
