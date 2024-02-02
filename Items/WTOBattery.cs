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
public class WTOBattery : GrabbableObject {

    public bool HasCharge;

    public override void Start() {
        base.Start();
        System.Random ScrapValueRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        int StartingScrapValue = ScrapValueRandom.Next(itemProperties.minValue, itemProperties.maxValue);
        if (!HasCharge) {
            StartingScrapValue /= 5;
        }
        StartingScrapValue = (int)Mathf.Round(StartingScrapValue * 0.4f);
        SetScrapValue(StartingScrapValue);
    }

}