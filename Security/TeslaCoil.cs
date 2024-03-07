using DigitalRuby.ThunderAndLightning;
using GameNetcodeStuff;
using LethalLib.Modules;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
internal class TeslaCoil : NetworkBehaviour {

    [InspectorName("Balance Constants")]
    public float SecondsUntilShock = 4f;
    public int ChanceObjectWillBeShocked = 25;
    public float ShockCooldown = 5f;

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

    public bool isShocking;
    public bool HasShocked = true;
    public GrabbableObject ObjectToShock;
    public ParticleSystem StaticParticle;
    public ParticleSystem LightningBolt;
    public AudioClip ShockClip;
    private GrabbableObject[] GrabbableObjectList;
    private System.Random TeslaRandom;

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
                //WTOBase.LogToConsole($"Adding Player {PlayerInRange} to player in range list...");
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
            if(ObjectToShock.playerHeldBy == PlayerInRange) {
                ObjectToShock = null;
                StopCoroutine(ShockCoroutine());
                isShocking = false;
            }
            if (PlayerInRangeList.Contains(PlayerInRange) && PlayerInRange != null) {
                //WTOBase.LogToConsole($"Removing Player {PlayerInRange} from player in range list...");
                PlayerInRangeList.Remove(PlayerInRange);
            }
        } catch { }
    }
    private void Start() {
        RecieveToggleTeslaCoil(false);
        RecieveToggleTeslaCoil(true);
        GrabbableObjectList = FindObjectsOfType<GrabbableObject>();
        TeslaRandom = new System.Random();
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
                    if (NowYoureOnWalkies.clientIsHoldingAndSpeakingIntoThis) {                    
                        NowYoureOnWalkies.SwitchWalkieTalkieOn(false);
                        SendDeathSFXServerRpc((int)Player.actualClientId);
                        continue;
                    }
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
                    HeldObject.GetComponent<ShotgunItem>().ItemActivate(true);
                    AttemptedFireShotgun = true;
                    continue;
                }
            }
        }
        if (isShocking) {
            StaticParticle.transform.position = ObjectToShock.transform.position;
            StartCoroutine(ShockCoroutine());
        } else {
            StopCoroutine(ShockCoroutine());
            if (ShockCooldown > 0f) {
                ShockCooldown -= Time.deltaTime;
                return;
            }
            if(ObjectToShock != null) {
                return;
            }
            //get all grabbableobjects in range. I LOVE ITERATING OVER LOOPS IN THE UPDATE
            foreach(GrabbableObject Object in GrabbableObjectList.Where(x => Vector3.Distance(x.transform.position, this.transform.position) < 16)){
                if (!Object.itemProperties.isConductiveMetal) {
                    return;
                }
                //if the item is conductive, we want a 1 in x chance to shock it
                if (!(TeslaRandom.Next(0, 100) < ChanceObjectWillBeShocked)){
                    continue;
                }
                ObjectToShock = Object;
                ManageStaticParticle();
                HasShocked = false;
                isShocking = true;
                break;
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

    private void ManageStaticParticle(bool ShouldDestroy = false) {
        if (ShouldDestroy) {
            StaticParticle.Stop();
            StaticParticle.GetComponent<AudioSource>().Stop();
        }
        StaticParticle.GetComponent<AudioSource>().Play();
        StaticParticle.Play();
    }

    IEnumerator ShockCoroutine() {

        yield return new WaitForSeconds(SecondsUntilShock);
        if (!HasShocked) {
            HasShocked = true;
            LightningBolt.Play();
            WTOBase.LogToConsole("Starting lightning bolt!");
            LightningBolt.transform.rotation = Quaternion.LookRotation(ObjectToShock.transform.position, Vector3.up);
            RingNoiseMaker.PlayOneShot(ShockClip);
            if (ObjectToShock.playerHeldBy != null) {
                ObjectToShock.playerHeldBy.DamagePlayer(10, false, true, CauseOfDeath.Blast);
                ManageStaticParticle(true);
            }
            ObjectToShock = null;
        }
        yield return new WaitForSeconds(0.3f);
        WTOBase.LogToConsole("Stopping lightning bolt!");
        isShocking = false;
        ShockCooldown = 5f;
        LightningBolt.Stop();
    }
}
