using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Items
{
    public class WTOBattery : GrabbableObject {

        public bool HasCharge = true;
        public Material ChargedMaterial;
        public Material DrainedMaterial;
        public ScanNodeProperties ScanNode;

        private static readonly WTOBase.WTOLogger Log = new(typeof(WTOBattery), LogSourceType.Item);

        private void Awake() 
        {
            ScanNode ??= gameObject.GetComponentInChildren<ScanNodeProperties>();
        }

        public override void OnNetworkSpawn() 
        {
            base.OnNetworkSpawn();

            if(IsServer)
            {
                SetCharge(HasCharge);

                // Server's initial state synced to clients on spawn.
                SetChargeClientRpc(HasCharge, new ClientRpcParams
                {
                    Send = WTOBase.AllClientsButHost()
                });
            }
        }

        [ClientRpc]
        private void SetChargeClientRpc(bool hasCharge, ClientRpcParams clientRpcParams = default)
        {
            SetCharge(hasCharge);
        }

        private void SetCharge(bool hasCharge)
        {
            HasCharge = hasCharge;

            if (HasCharge)
            {
                Log.Info("Setting Battery State to Charged");
                ScanNode.headerText = "Charged Battery";
                mainObjectRenderer.SetMaterials([ChargedMaterial]);
            }
            else
            {
                Log.Info("Setting Battery State to Drained");
                ScanNode.headerText = "Drained Battery";
                mainObjectRenderer.SetMaterials([DrainedMaterial]);
            }
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