using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;

namespace Welcome_To_Ooblterra.Things
{
    internal class TeslaCoil : NetworkBehaviour 
    {
#pragma warning disable 0649 // Assigned in Unity Editor
        public BoxCollider RangeBox;
        public GameObject SmallRing; 
        public GameObject MediumRing;
        public GameObject LargeRing;
        public AudioSource StaticNoiseMaker;
        public AudioSource RingNoiseMaker;

        public AudioClip RingsOn;
        public AudioClip RingsOff;
        public AudioClip RingsActive;

        public MeshRenderer[] Emissives;
        public Animator TeslaCoilAnim;
#pragma warning restore 0649

        /// <summary>
        /// State that marks what is in range of a tesla coil.
        /// These are merged together by the <see cref="TeslaCoilManager"/> to determine what items
        /// should be affected by the tesla coil.
        /// </summary>
        public class CoilInRangeState
        {
            public bool isEnabled = true;
            public bool localPlayerInRange = false;
            public HashSet<GrabbableObject> grabbableObjectsInRange = new();
            public HashSet<EyeSecAI> eyeSecInRange = new();

            public void Clear()
            {
                isEnabled = true;
                localPlayerInRange = false;
                grabbableObjectsInRange.Clear();
                eyeSecInRange.Clear();
            }
        }

        private CoilInRangeState coilState = new();

        private Color[] emissiveColors = [];
        private Material[] emissiveMaterials = [];

        private static readonly WTOBase.WTOLogger Log = new(typeof(TeslaCoil), LogSourceType.Room);

        private void Start()
        {
            emissiveMaterials = new Material[Emissives.Length];
            emissiveColors = new Color[Emissives.Length];

            for (int i = 0; i < Emissives.Length; i++)
            {
                var mats = Emissives[i].sharedMaterials;
                var instance = new Material(mats[0]);
                emissiveMaterials[i] = instance;
                emissiveColors[i] = instance.GetColor("_EmissiveColor");

                mats = [.. mats];
                mats[0] = instance;
                Emissives[i].materials = mats;
            }

            ToggleTeslaCoil(true);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            // Unity does not readily automatically destroy the material clones.
            for (int i = 0; i < emissiveMaterials.Length; i++)
            {
                if (emissiveMaterials[i] != null)
                {
                    Destroy(emissiveMaterials[i]);
                }
            }
        }

        private void OnEnable()
        {
            TeslaCoilManager.Instance.AddTeslaCoil(this, coilState);
        }

        private void OnDisable()
        {
            TeslaCoilManager.Instance.RemoveTeslaCoil(this);
            coilState.Clear();
        }

        public void OnTriggerEnter(Collider other) 
        {
            bool updated = false;
            if (other.gameObject.TryGetComponent(out EnemyAICollisionDetect enemy))
            {
                if (enemy.mainScript != null && enemy.mainScript is EyeSecAI EyeSecInRange && !EyeSecInRange.isEnemyDead)
                {
                    coilState.eyeSecInRange.Add(EyeSecInRange);
                    updated = true;
                }
            }

            if (IsServer && other.gameObject.TryGetComponent(out GrabbableObject grabbableObject))
            {
                coilState.grabbableObjectsInRange.Add(grabbableObject);
                updated = true;
            }

            if (IsClient && other.gameObject.TryGetComponent(out PlayerControllerB PlayerInRange) && StartOfRound.Instance.localPlayerController == PlayerInRange)
            {
                // Allow each player to apply tesla coil effects on themselves.
                coilState.localPlayerInRange = true;
                updated = true;
            }

            if (updated)
            {
                PrintInRangeObjects();
            }
        }

        public void OnTriggerExit(Collider other) 
        {
            bool updated = false;
            if (other.gameObject.TryGetComponent(out EyeSecAI EyeSecInRange) && !EyeSecInRange.isEnemyDead)
            {
                // Note: Once eye sec dies, the TeslaCoilManager will remove it from the list, so we don't need to worry about that here.
                coilState.eyeSecInRange.Remove(EyeSecInRange);
                updated = true;
            }

            if (IsServer && other.gameObject.TryGetComponent(out GrabbableObject grabbableObject))
            {
                coilState.grabbableObjectsInRange.Remove(grabbableObject);
                updated = true;
            }

            if (IsClient && other.gameObject.TryGetComponent(out PlayerControllerB PlayerInRange) && StartOfRound.Instance.localPlayerController == PlayerInRange)
            {
                // Allow each player to apply tesla coil effects on themselves.
                coilState.localPlayerInRange = false;
                updated = true;
            }

            if (updated)
            {
                PrintInRangeObjects();
            }
        }

        private void PrintInRangeObjects()
        {
            Log.Debug($"EyeSecs in range: {coilState.eyeSecInRange.Count}");
            Log.Debug($"Grabbable objects in range: {coilState.grabbableObjectsInRange.Count}");
            Log.Debug($"Local player in range: {coilState.localPlayerInRange}");
        }

        private void Update() 
        {
            if (coilState.isEnabled)
            {
                SpinRings();
            }

        }

        private void SpinRings() 
        {
            SmallRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
            MediumRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
            LargeRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
        }

        /// <summary>
        /// Called when the tesla coil is triggered from the map (like opening/closing doors)
        /// </summary>
        /// <param name="enabled">True if the tesla coil should be enabled, false if it should be disabled.</param>
        public void RecieveToggleTeslaCoil(bool enabled) 
        {
            ToggleTeslaCoil(enabled);
            ToggleTeslaCoilServerRpc(enabled);
        }

        [ServerRpc(RequireOwnership = false)]
        public void ToggleTeslaCoilServerRpc(bool enabled, ServerRpcParams serverParams = default) 
        {
            ToggleTeslaCoilClientRpc(enabled, new ClientRpcParams
            {
                Send = WTOBase.AllClientsButSender(serverParams)
            });
        }

        [ClientRpc]
        public void ToggleTeslaCoilClientRpc(bool enabled, ClientRpcParams clientRpcParams = default) 
        {
            ToggleTeslaCoil(enabled);
        }

        private void ToggleTeslaCoil(bool enabled) 
        {
            coilState.isEnabled = enabled;
            TeslaCoilAnim.SetBool("Powered", enabled);

            for (int i = 0; i < emissiveMaterials.Length; i++)
            {
                emissiveMaterials[i].SetColor("_EmissiveColor", emissiveColors[i] * (enabled ? 1 : 0));
            }

            if (enabled)
            {
                StaticNoiseMaker.Play();
                RingNoiseMaker.clip = RingsOn;
                RingNoiseMaker.Play();
            }
            else
            {
                StaticNoiseMaker.Stop();
                RingNoiseMaker.clip = RingsOff;
                RingNoiseMaker.Play();
            }
        }
    }
}
