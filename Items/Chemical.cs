using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Welcome_To_Ooblterra.Properties;
using System.Linq;

namespace Welcome_To_Ooblterra.Items
{
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

        private readonly System.Random MyRandom = new();
        private float ShakeCooldownSeconds = 0;

        private ChemColor Color = ChemColor.Clear;
        private int ScrapValue = 0;

        private static readonly WTOBase.WTOLogger Log = new(typeof(Chemical), LogSourceType.Item);

        public override void Start() {
            base.Start();

            if(IsServer)
            {
                // Determine initial values on server only and replicate to clients so that it's
                // all consistent.
                System.Random ScrapValueRandom = new(StartOfRound.Instance.randomMapSeed);
                int StartingScrapValue = ScrapValueRandom.Next(itemProperties.minValue, itemProperties.maxValue);
                StartingScrapValue = (int)Mathf.Round(StartingScrapValue * 0.4f);
                SetChemState(RandomColor(), StartingScrapValue, playShakeSound: false);
            }
        }

        public override void Update() 
        {
            base.Update();
            if(ShakeCooldownSeconds > 0) { 
                ShakeCooldownSeconds -= Time.deltaTime;
            }
        }

        public override void EquipItem() 
        {
            base.EquipItem();
            playerHeldBy.equippedUsableItemQE = true;
        }

        public override void ItemInteractLeftRight(bool right) 
        {
            base.ItemInteractLeftRight(right);
            if (!right && ShakeCooldownSeconds <= 0 && GetCurrentColor() != ChemColor.Clear) {
                ShakeCooldownSeconds = 1.2f;
                playerHeldBy.playerBodyAnimator.SetTrigger("shakeItem");

                SetChemState(NextColor(Color), ScrapValue, playShakeSound: true);
            }
        }

        private ChemColor RandomColor()
        {
            return (ChemColor)MyRandom.Next(0, 7);
        }

        private static ChemColor NextColor(ChemColor color)
        {
            // Skip over the clear color when cycling through colors
            int next = (int)color + 1;
            int size = (int)ChemColor.Purple;
            return (ChemColor)(next % size);
        }

        public void EmptyBeaker() 
        {
            SetChemState(ChemColor.Clear, ScrapValue / 3, playShakeSound: false);
        }

        public ChemColor GetCurrentColor() 
        {
            return Color;
        }

        public static Color GetColorFromEnum(ChemColor inColor) {
            return inColor switch
            {
                ChemColor.Red => new Color(1, 0, 0),
                ChemColor.Orange => new Color(1, 0.4f, 0),
                ChemColor.Yellow => new Color(1, 1, 0),
                ChemColor.Green => new Color(0, 1, 0),
                ChemColor.Blue => new Color(0, 1, 1),
                ChemColor.Indigo => new Color(0, 0, 1),
                ChemColor.Purple => new Color(1, 0, 1),
                ChemColor.Clear => new Color(0, 0, 0, 0),
                _ => new Color(1, 1, 1),
            };
        }

        [ServerRpc]
        private void SetChemStateServerRpc(ChemColor color, int scrapValue, bool playShakeSound, ServerRpcParams rpcParams = default)
        {
            SetChemStateClientRpc(color, scrapValue, playShakeSound, new ClientRpcParams
            {
                Send = WTOBase.AllClientsButSender(rpcParams)
            });
        }

        [ClientRpc]
        private void SetChemStateClientRpc(ChemColor color, int scrapValue, bool playShakeSound, ClientRpcParams rpcParams = default) 
        {
            UpdateChemStateOnClient(color, scrapValue, playShakeSound);
        }

        private void UpdateChemStateOnClient(ChemColor color, int scrapValue, bool playShakeSound)
        {
            if (playShakeSound)
            {
                GetComponent<AudioSource>().PlayOneShot(ShakeSounds[MyRandom.Next(0, ShakeSounds.Count - 1)]);
            }

            Color = color;
            ScrapValue = scrapValue;
            SetScrapValue(ScrapValue);
            BeakerMesh.materials[1].SetColor("_BaseColor", GetColorFromEnum(Color));
        }

        /// <summary>
        /// Can be called from the client or server and will do the right thing -
        /// passes a new chem state to the clients.
        /// </summary>
        /// <param name="color">The new chem colour</param>
        /// <param name="scrapValue">The new chem scrap value</param>
        /// <param name="playShakeSound">Whether to play the shake sound on clients when they received this update</param>
        private void SetChemState(ChemColor color, int scrapValue, bool playShakeSound)
        {
            Log.Info($"Next Color Value: {color}");

            if (IsClient)
            {
                // If client is calling, then set immediately and use server to broadcast to all
                UpdateChemStateOnClient(color, scrapValue, playShakeSound);
                SetChemStateServerRpc(color, scrapValue, playShakeSound);
            }
            else
            {
                // If server is calling, then broadcast to clients
                SetChemStateClientRpc(color, scrapValue, playShakeSound);
            }
        }
    }
}
