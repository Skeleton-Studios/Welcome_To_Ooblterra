using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Items
{
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
                mainObjectRenderer.SetMaterials(new() { EmptyMaterial });
                /*
                StartingScrapValue = ScrapValueRandom.Next(itemProperties.minValue, itemProperties.maxValue);
                StartingScrapValue /= 5;
                StartingScrapValue = (int)Mathf.Round(StartingScrapValue * 0.4f);
                */ 
                insertedBattery.charge = 100;
            } else {
                Log.Info("Setting Battery State to Charged");
                ScanNode.headerText = "Charged Battery";
                mainObjectRenderer.SetMaterials(new() { ChargedMaterial });
                HasCharge = true;
                scrapValue = 200;
                /*StartingScrapValue = ScrapValueRandom.Next(450, 550);
                */
            }
            SetScrapValue(scrapValue); 
        }

        public override void OnPlaceObject()
        {
            base.OnPlaceObject();

            if(transform.parent == null || !transform.parent.TryGetComponent(out NetworkObject possibleRecepticleTransform)) {
                Log.Info("Battery placed, but not in a recepticle transform. Ignoring.");
                return;
            }

            if(possibleRecepticleTransform.name.StartsWith("BatteryRecepticleTransform"))
            {
                // To avoid the rotation changing due to dropped item logic in base LC, we set the
                // parentObject here to be the recepticle.
                // This will get cleared by the base LC code when the Battery is picked up.
                Log.Info("Battery placed in recepticle, setting parent object to recepticle transform.");
                parentObject = possibleRecepticleTransform.transform;
                PlayDropSFX();
            }
        }
    }
}