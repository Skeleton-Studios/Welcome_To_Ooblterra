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
internal class Chemical : GrabbableObject {

    public enum ChemColor {
        Red,
        Orange,
        Yellow,
        Green,
        Blue,
        Indigo,
        Purple
    }

    ChemColor CurrentColor;
    private System.Random MyRandom;
    private int RandomEffectIndex;
    private float ShakeCooldownSeconds;

    public override void Start() {
        base.Start();
        MyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        RandomEffectIndex = MyRandom.Next(0, 7);
    }
    public override void Update() {
        if(ShakeCooldownSeconds > 0) { 
            ShakeCooldownSeconds -= Time.deltaTime;
        }
    }

    public override void ItemInteractLeftRight(bool right) {
        if (right && ShakeCooldownSeconds <= 0) {
            ShakeCooldownSeconds = 1.2f;
            playerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");
            RandomEffectIndex = MyRandom.Next(0, 7);

            CurrentColor = (ChemColor)Enum.GetValues(typeof(ChemColor)).GetValue(MyRandom.Next(0, 7));
        }
    }

    private void DrinkChemicals() {
        RandomChemEffectServerRpc(RandomEffectIndex);
    }

    [ServerRpc]
    private void RandomChemEffectServerRpc(int RandomIndex) {
        RandomChemEffectClientRpc(RandomIndex);
    }

    [ClientRpc]
    private void RandomChemEffectClientRpc(int RandomIndex) {
        RandomChemEffect(RandomIndex);
    }

    private void RandomChemEffect(int RandomIndex) {
        switch (RandomIndex) {
            case 0:

                return;
            case 1:
                
                return;
            case 2:
                
                return;
            case 3:
                
                return;
            case 4:
                
                return;
            case 5:
                
                return;
            case 6:
                
                return;
        }
    }

}
