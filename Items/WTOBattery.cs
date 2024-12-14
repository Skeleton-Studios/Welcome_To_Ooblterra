using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Items;
public class WTOBattery : GrabbableObject {

    public bool HasCharge;
    public Material ChargedMaterial;
    public Material EmptyMaterial;
    public ScanNodeProperties ScanNode;

    private static readonly WTOBase.WTOLogger Log = new(typeof(WTOBattery), LogSourceType.Item);

    public override void Start() {
        base.Start();
        //int StartingScrapValue;
        // System.Random ScrapValueRandom = new(StartOfRound.Instance.randomMapSeed);
        if(ScanNode == null) {
            ScanNode = base.gameObject.GetComponentInChildren<ScanNodeProperties>();
        }
        Log.Info($"Battery value: {scrapValue}");
        if (scrapValue < 100) {
            Log.Info("Setting Battery State to Drained");
            ScanNode.headerText = "Drained Battery";
            HasCharge = false;
            mainObjectRenderer.SetMaterials([EmptyMaterial]);
            /*
            StartingScrapValue = ScrapValueRandom.Next(itemProperties.minValue, itemProperties.maxValue);
            StartingScrapValue /= 5;
            StartingScrapValue = (int)Mathf.Round(StartingScrapValue * 0.4f);
            */ 
            insertedBattery.charge = 100;
        } else {
            Log.Info("Setting Battery State to Charged");
            ScanNode.headerText = "Charged Battery";
            mainObjectRenderer.SetMaterials([ChargedMaterial]);
            HasCharge = true;
            scrapValue = 200;
            /*StartingScrapValue = ScrapValueRandom.Next(450, 550);
            */
        }
        SetScrapValue(scrapValue); 
    }
}