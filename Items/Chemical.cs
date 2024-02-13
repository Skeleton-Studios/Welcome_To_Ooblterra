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
    public List<AudioClip> ShakeSounds;

    [HideInInspector]
    public enum ChemColor {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Indigo,
        Purple,
        Clear
    }
    private ChemColor CurrentColor;
    private System.Random MyRandom;
    private int RandomEffectIndex;
    private float ShakeCooldownSeconds = 0;
    private bool IsFull = true;

    public override void Start() {
        base.Start();
        MyRandom = new System.Random();
        System.Random ScrapValueRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        int StartingScrapValue = ScrapValueRandom.Next(itemProperties.minValue, itemProperties.maxValue);
        StartingScrapValue = (int)Mathf.Round(StartingScrapValue * 0.4f);
        SetScrapValue(StartingScrapValue);
        ChangeChemColorAndEffect(false);

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
        if (!right && ShakeCooldownSeconds <= 0 && IsFull) {
            ShakeCooldownSeconds = 1.2f;
            playerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
            ChangeChemColorAndEffect();
        }
    }
    private void ChangeChemColorAndEffect(bool PlayShakeSound = true) {
        int NextColor = GetNextRandomInt();
        int NextRandomEffect = MyRandom.Next(0, 7);
        WTOBase.LogToConsole($"Next Color Value: {(ChemColor)NextColor}");
        SetColorAndEffectServerRpc(NextColor, NextRandomEffect, PlayShakeSound);
    }
    private int GetNextRandomInt() {
        int NextInt = MyRandom.Next(0, 7);
        if(NextInt != (int)CurrentColor) {
            return NextInt;
        }
        return GetNextRandomInt();
    }
    public void EmptyBeaker() {
        SetColorAndEffectServerRpc(7, -1);
    }
    public ChemColor GetCurrentColor() {
        return CurrentColor;
    }
    private void DrinkChemicals() {
        if (IsFull) { 
            RandomChemEffectServerRpc(RandomEffectIndex);
            EmptyBeaker();
        }
    }
    public static Color GetColorFromEnum(ChemColor inColor) {
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
            case ChemColor.Clear:
                return new Color(0, 0, 0, 0);
            default:
                return new Color(1, 1, 1);
        }
    }
    private void RandomChemEffect(int RandomIndex) {
        IsFull = false;
        switch (RandomIndex) {
            case 0:
                //Explode
                break;
            case 1:
                //Temporary deafness
                break;
            case 2:
                //Inverted controls (drunk)
                break;
            case 3:
                //Case 3 is a dud and does nothing
                break;
            case 4:
                //Teleport back to start room (w/ items)
                break;
            case 5:
                //needs good idea
                break;
            case 6:
                //Carry weight set to 0
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

    [ServerRpc]
    private void SetColorAndEffectServerRpc(int color, int effect, bool PlayShakeSound = true) {
        SetColorAndEffectClientRpc(color, effect, PlayShakeSound);
    }

    [ClientRpc]
    private void SetColorAndEffectClientRpc(int color, int effect, bool PlayShakeSound = true) {
        SetColorAndEffect(color, effect, PlayShakeSound);
    }
    private void SetColorAndEffect(int color, int effect, bool PlayShakeSound = true) {
        if (PlayShakeSound) { 
            GetComponent<AudioSource>().PlayOneShot(ShakeSounds[MyRandom.Next(0, ShakeSounds.Count - 1)]);
        }
        CurrentColor = (ChemColor)Enum.GetValues(typeof(ChemColor)).GetValue(color);
        BeakerMesh.materials[1].SetColor("_BaseColor", GetColorFromEnum(CurrentColor));
        RandomEffectIndex = effect;
        if(color == 7) {
            IsFull = false;
            SetScrapValue(scrapValue / 3);
        }
    }
}
