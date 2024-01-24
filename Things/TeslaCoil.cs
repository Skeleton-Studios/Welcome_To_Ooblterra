using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;

namespace Welcome_To_Ooblterra.Things;
internal class TeslaCoil : NetworkBehaviour {

    public BoxCollider RangeBox;

    [HideInInspector]
    public bool TeslaCoilOn;

    private List<PlayerControllerB> PlayerInRangeList;

    public void OnTriggerEnter(Collider other) {
        try {
            EyeSecAI EyeSecInRange = other.gameObject.GetComponent<EyeSecAI>();
            other.gameObject.GetComponent<EyeSecAI>().BuffedByTeslaCoil = true;
        } catch {}       
        try {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            if (!PlayerInRangeList.Contains(PlayerInRange)){
                PlayerInRangeList.Add(PlayerInRange);
            }
        } catch {}
    }
    public void OnTriggerExit(Collider other) {
        try {
            EyeSecAI EyeSecInRange = other.gameObject.GetComponent<EyeSecAI>();
            other.gameObject.GetComponent<EyeSecAI>().BuffedByTeslaCoil = false;
        } catch { }
        try {
            PlayerControllerB PlayerInRange = other.gameObject.GetComponent<PlayerControllerB>();
            if (PlayerInRangeList.Contains(PlayerInRange)) {
                PlayerInRangeList.Remove(PlayerInRange);
            }
        } catch { }
    }

    private void Update() {
        if (!TeslaCoilOn) {
            return;
        }
        //Wow this code sucks cock
        foreach (PlayerControllerB Player in PlayerInRangeList) {
            foreach (GrabbableObject HeldObject in Player.ItemSlots) {
                if(HeldObject is WalkieTalkie) {
                    WalkieTalkie NowYoureOnWalkies = HeldObject.GetComponent<WalkieTalkie>();
                    NowYoureOnWalkies.SwitchWalkieTalkieOn(false);
                    if (NowYoureOnWalkies.clientIsHoldingAndSpeakingIntoThis) { 
                        NowYoureOnWalkies.BroadcastSFXFromWalkieTalkie(NowYoureOnWalkies.playerDieOnWalkieTalkieSFX, (int)NowYoureOnWalkies.playerHeldBy.playerClientId);
                    }
                }
                if(HeldObject is FlashlightItem) {
                    HeldObject.GetComponent<FlashlightItem>().SwitchFlashlight(false);
                }
                if(HeldObject is BoomboxItem) {
                    HeldObject.GetComponent<BoomboxItem>().StartMusic(false);
                }
                if(HeldObject is PatcherTool) {
                    HeldObject.GetComponent<PatcherTool>().DisablePatcherGun();
                }
            }
        }
    }

    private void RecieveToggleTeslaCoil(bool enabled) {
        ToggleTeslaCoilServerRpc(enabled);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ToggleTeslaCoilServerRpc(bool enabled) {
        ToggleTeslaCoilClientRpc(enabled);
    }

    [ClientRpc]
    public void ToggleTeslaCoilClientRpc(bool enabled) {
        ToggleTeslaCoil(enabled);
    }

    private void ToggleTeslaCoil(bool enabled) {
        TeslaCoilOn = enabled;
    }
}
