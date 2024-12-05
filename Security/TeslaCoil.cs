using GameNetcodeStuff;
using LethalLib.Modules;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
internal class TeslaCoil : NetworkBehaviour {

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
    public AudioClip WalkieTalkieDie;

    public MeshRenderer[] Emissives;
    public Animator TeslaCoilAnim;

    readonly WalkieTalkie NowYoureOnWalkies;
#pragma warning restore 0649

    [HideInInspector]
    private bool TeslaCoilOn = true;
    private bool AttemptedFireShotgun = false;

    private readonly List<PlayerControllerB> PlayerInRangeList = [];
    private readonly List<WalkieTalkie> WalkiesToReEnable = [];
    private readonly List<FlashlightItem> FlashLightsToReEnable = [];


    public void OnTriggerEnter(Collider other) {
        try {
            EyeSecAI EyeSecInRange = other.gameObject.GetComponent<EyeSecAI>();
            EyeSecInRange.BuffedByTeslaCoil = true;
        } catch {}       
        try {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            
            if (!PlayerInRangeList.Contains(PlayerInRange) && PlayerInRange != null){
                WTOBase.LogToConsole($"Adding Player {PlayerInRange} to player in range list...");
                PlayerInRangeList.Add(PlayerInRange);
            }
        } catch {}
        try {
            RadarBoosterItem RadarBoosterInRange = other.gameObject.GetComponent<RadarBoosterItem>();
            RadarBoosterInRange.EnableRadarBooster(false);
        } catch { }
    }
    public void OnTriggerExit(Collider other) {
        try {
            EyeSecAI EyeSecInRange = other.gameObject.GetComponent<EyeSecAI>();
            other.gameObject.GetComponent<EyeSecAI>().BuffedByTeslaCoil = false;
        } catch { }
        try {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            if (PlayerInRangeList.Contains(PlayerInRange) && PlayerInRange != null) {
                WTOBase.LogToConsole($"Removing Player {PlayerInRange} from player in range list...");
                ReEnableEquipment(PlayerInRange);
                PlayerInRangeList.Remove(PlayerInRange);
            }
        } catch { }
    }
    private void Start() {
        RecieveToggleTeslaCoil(false);
        RecieveToggleTeslaCoil(true);
    }
    private void Update() {
        if (!TeslaCoilOn) {
            return;
        }
        SpinRings();
        //Wow this code sucks cock
        if(PlayerInRangeList.Count <= 0) {
            return;
        }
        foreach (PlayerControllerB Player in PlayerInRangeList) {
            if(Vector3.Distance(Player.transform.position, this.transform.position) > 30) {
                PlayerInRangeList.Remove(Player);
                continue;
            }
            if(Player.ItemSlots.Count() <= 0) {
                continue;
            }
            foreach (GrabbableObject HeldObject in Player.ItemSlots) {
                if(HeldObject is WalkieTalkie NextWalkie) {
                    if (HeldObject.isBeingUsed == false) {
                        continue;
                    }
                    if (!WalkiesToReEnable.Contains(NextWalkie)) {
                        WalkiesToReEnable.Add(NextWalkie);
                    }
                    if (NextWalkie.clientIsHoldingAndSpeakingIntoThis) {
                        NextWalkie.SwitchWalkieTalkieOn(false);
                        SendDeathSFXServerRpc((int)Player.actualClientId);
                        continue;
                    }
                    NextWalkie.SwitchWalkieTalkieOn(false);
                    continue;
                }
                if(HeldObject is FlashlightItem NextFlashlight) {
                    if(NextFlashlight.isBeingUsed && !FlashLightsToReEnable.Contains(NextFlashlight)) {
                        FlashLightsToReEnable.Add(NextFlashlight);
                    }
                    HeldObject.GetComponent<FlashlightItem>().SwitchFlashlight(false);
                    continue;
                }
                if(HeldObject is BoomboxItem NextBoombox) {
                    NextBoombox.StartMusic(false);
                    continue;
                }
                if(HeldObject is PatcherTool NextZapGun) {
                    NextZapGun.DisablePatcherGun();
                    continue;
                }
                if(HeldObject is RadarBoosterItem NextBooster) {
                    NextBooster.EnableRadarBooster(false);
                    continue;
                }
                if(HeldObject is ShotgunItem NextShotgun && AttemptedFireShotgun == false) {
                    NextShotgun.ItemActivate(true);
                    AttemptedFireShotgun = true;
                    continue;
                }
            }
        }
    }
    private void SpinRings() {
        SmallRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
        MediumRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
        LargeRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
    }
    private void ToggleRings(bool State) {
        TeslaCoilAnim.SetBool("Powered", State);
        foreach (MeshRenderer Mesh in Emissives) {
            Color CachedColor = Mesh.materials[0].GetColor("_EmissionColor");
            Mesh.materials[0].SetColor("_EmissiveColor", CachedColor * (State ? 1 : 0));
        }
        AttemptedFireShotgun = false;
        if(State == false) {
            StaticNoiseMaker.Stop();
            RingNoiseMaker.clip = RingsOff;
            RingNoiseMaker.Play();
        } else {
            StaticNoiseMaker.Play();
            RingNoiseMaker.clip = RingsOn;
            RingNoiseMaker.Play();
        }
    }
    public void RecieveToggleTeslaCoil(bool enabled) {
        ToggleTeslaCoilServerRpc(enabled);
        ToggleTeslaCoil(enabled);
    }

    private void ReEnableEquipment(PlayerControllerB PlayerToCheck) {
        foreach (GrabbableObject NextItem in PlayerToCheck.ItemSlots) {
            if (WalkiesToReEnable.Contains(NextItem)) {
                WalkiesToReEnable.Remove(NextItem as WalkieTalkie);
                NextItem.GetComponent<WalkieTalkie>().SwitchWalkieTalkieOn(true);
            }
            if (FlashLightsToReEnable.Contains(NextItem)) {
                FlashLightsToReEnable.Remove(NextItem as FlashlightItem);
                NextItem.GetComponent<FlashlightItem>().SwitchFlashlight(true);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleTeslaCoilServerRpc(bool enabled) {
        ToggleTeslaCoilClientRpc(enabled);
    }
    [ClientRpc]
    public void ToggleTeslaCoilClientRpc(bool enabled) {
        if(TeslaCoilOn != enabled) { 
            ToggleTeslaCoil(enabled);
        }
    }
    private void ToggleTeslaCoil(bool enabled) {
        TeslaCoilOn = enabled;
        ToggleRings(TeslaCoilOn);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendDeathSFXServerRpc(int PlayerID) {
        SendDeathSFXClientRpc(PlayerID);
    }
    [ClientRpc]
    public void SendDeathSFXClientRpc(int PlayerID) {
        SendDeathSFX(PlayerID);
    }
    private void SendDeathSFX(int PlayerID) {
        NowYoureOnWalkies.BroadcastSFXFromWalkieTalkie(WalkieTalkieDie, PlayerID);
    }
}
