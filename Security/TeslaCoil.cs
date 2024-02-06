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

    [HideInInspector]
    private bool TeslaCoilOn = true;
    private bool AttemptedFireShotgun = false;

    private List<PlayerControllerB> PlayerInRangeList = new();

    WalkieTalkie NowYoureOnWalkies;

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
            if(Player.ItemSlots.Count() <= 0) {
                continue;
            }
            foreach (GrabbableObject HeldObject in Player.ItemSlots) {
                if(HeldObject is WalkieTalkie) {
                    NowYoureOnWalkies = HeldObject.GetComponent<WalkieTalkie>();
                    if (HeldObject.isBeingUsed == false) {
                        continue;
                    }
                    WTOBase.LogToConsole($"Client holding and talking into walkie: {NowYoureOnWalkies.clientIsHoldingAndSpeakingIntoThis}");
                    if (NowYoureOnWalkies.clientIsHoldingAndSpeakingIntoThis) {
                        WTOBase.LogToConsole("Turning walkie off before broadcasting SFX");
                        NowYoureOnWalkies.SwitchWalkieTalkieOn(false);
                        WTOBase.LogToConsole($"Broadcasting death SFX! Player Client ID: {Player.playerClientId} ACTUAL CLIENT ID: {Player.actualClientId}");
                        SendDeathSFXServerRpc((int)Player.actualClientId);
                        continue;
                    }
                    WTOBase.LogToConsole("Turning walkie off...");
                    NowYoureOnWalkies.SwitchWalkieTalkieOn(false);
                    continue;
                }
                if(HeldObject is FlashlightItem) {
                    HeldObject.GetComponent<FlashlightItem>().SwitchFlashlight(false);
                    continue;
                }
                if(HeldObject is BoomboxItem) {
                    HeldObject.GetComponent<BoomboxItem>().StartMusic(false);
                    continue;
                }
                if(HeldObject is PatcherTool) {
                    HeldObject.GetComponent<PatcherTool>().DisablePatcherGun();
                    continue;
                }
                if(HeldObject is RadarBoosterItem) {
                    HeldObject.GetComponent<RadarBoosterItem>().EnableRadarBooster(false);
                    continue;
                }
                if(HeldObject is ShotgunItem && AttemptedFireShotgun == false) {
                    AttemptedFireShotgun = true;
                    HeldObject.GetComponent<ShotgunItem>().ItemActivate(true);
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
        WTOBase.LogToConsole($"Called toggle tesla coil with state: {enabled}");
        ToggleTeslaCoilServerRpc(enabled);
        ToggleTeslaCoil(enabled);
    }
    [ServerRpc(RequireOwnership = false)]
    public void ToggleTeslaCoilServerRpc(bool enabled) {
        WTOBase.LogToConsole($"Toggling tesla coil to {enabled} serverRpc");
        ToggleTeslaCoilClientRpc(enabled);
    }
    [ClientRpc]
    public void ToggleTeslaCoilClientRpc(bool enabled) {
        WTOBase.LogToConsole($"Toggling tesla coil to {enabled} clientRpc");
        if(TeslaCoilOn != enabled) { 
            ToggleTeslaCoil(enabled);
        }
    }
    private void ToggleTeslaCoil(bool enabled) {
        TeslaCoilOn = enabled;
        WTOBase.LogToConsole($"TESLA COIL STATE: {TeslaCoilOn}");
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
