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
    public float SecondsUntilShock = 10f;
    public int ChanceObjectWillBeShocked = 100;
    public float ShockCooldown = 5f;
    public int Damage = 150;

    [InspectorName("Defaults")]
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
    Coroutine Shock;
    public Vector3 RotationOffset = new Vector3(0, 90f, 0);

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
            if (ObjectToShock.playerHeldBy == PlayerInRange) {
                SetTargetZapObjectServerRpc(ObjectToShock.gameObject, true);
                StopCoroutine(Shock);
                isShocking = false;
            }

        } catch { }
    }

    private void Start() {
        RecieveToggleTeslaCoil(false);
        RecieveToggleTeslaCoil(true);
        GrabbableObjectList = FindObjectsOfType<GrabbableObject>();
        TeslaRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        ParticleSystem.MainModule main = StaticParticle.main;
        main.duration = SecondsUntilShock;
    }
    private void Update() {
        if (!TeslaCoilOn) {
            return;
        }
        SpinRings();
        DisableAllNearbyElectronics();
        CalculateShock();
    }

    private void SpinRings() {
        SmallRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
        MediumRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
        LargeRing.transform.Rotate(0, 0, -160 * Time.deltaTime);
    }
    private void DisableAllNearbyElectronics() {
        //Wow this code sucks cock
        if (PlayerInRangeList.Count <= 0) {
            return;
        }
        foreach (PlayerControllerB Player in PlayerInRangeList) {
            if (Player.ItemSlots.Count() <= 0) {
                continue;
            }
            foreach (GrabbableObject HeldObject in Player.ItemSlots) {
                if (HeldObject is WalkieTalkie) {
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
                if (HeldObject is FlashlightItem) {
                    HeldObject.GetComponent<FlashlightItem>().SwitchFlashlight(false);
                    continue;
                }
                if (HeldObject is BoomboxItem) {
                    HeldObject.GetComponent<BoomboxItem>().StartMusic(false);
                    continue;
                }
                if (HeldObject is PatcherTool) {
                    HeldObject.GetComponent<PatcherTool>().DisablePatcherGun();
                    continue;
                }
                if (HeldObject is RadarBoosterItem) {
                    HeldObject.GetComponent<RadarBoosterItem>().EnableRadarBooster(false);
                    continue;
                }
                if (HeldObject is ShotgunItem && AttemptedFireShotgun == false) {
                    HeldObject.GetComponent<ShotgunItem>().ItemActivate(true);
                    AttemptedFireShotgun = true;
                    continue;
                }
            }
        }
    }
    private void CalculateShock() {
        if (isShocking) {
            StaticParticle.transform.position = ObjectToShock.transform.position;
            LightningBolt.transform.rotation = Quaternion.LookRotation(ObjectToShock.transform.position - LightningBolt.transform.position, Vector3.up) * Quaternion.Euler(RotationOffset.x, RotationOffset.y, RotationOffset.z);
        } else {
            StaticParticle.transform.position = new Vector3(0, -1000, 0);
            if (ShockCooldown > 0f) {
                ShockCooldown -= Time.deltaTime;
                return;
            }
            if (ObjectToShock != null) {
                return;
            }
            //get all grabbableobjects in range. I LOVE ITERATING OVER LOOPS IN THE UPDATE!
            foreach (GrabbableObject Object in GrabbableObjectList.Where(x => Vector3.Distance(x.transform.position, this.transform.position) < 16)) {
                if (!Object.itemProperties.isConductiveMetal) {
                    return;
                }
                //if the item is conductive, we want a 1 in x chance to shock it
                if ((TeslaRandom.Next(0, 100) > ChanceObjectWillBeShocked)) {
                    ShockCooldown = 3f;
                    continue;
                }
                SetTargetZapObjectServerRpc(Object.gameObject.GetComponent<NetworkObject>());
                SetShockServerRpc(true);
                break;
            }
        }
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
            if (ObjectToShock != null) {
                SetTargetZapObjectServerRpc(ObjectToShock.gameObject, true);
                StopCoroutine(Shock);
                isShocking = false;
            }
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

    [ServerRpc(RequireOwnership = false)]
    public void SetTargetZapObjectServerRpc(NetworkObjectReference grabbableObjectNetObject, bool SetNull = false) {
        SetTargetZapObjectClientRpc(grabbableObjectNetObject, SetNull);
    }
    [ClientRpc]
    public void SetTargetZapObjectClientRpc(NetworkObjectReference grabbableObjectNetObject, bool SetNull = false) {
        if(SetNull) {
            ObjectToShock = null;
            return;
        }
        if (grabbableObjectNetObject.TryGet(out NetworkObject ItemNetObj)) {
            ObjectToShock = ItemNetObj.gameObject.GetComponentInChildren<GrabbableObject>();
        } else {
            WTOBase.LogToConsole("No networkobject found; assuming null for ObjectToShock");
            ObjectToShock = null;
        }
        
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetShockServerRpc(bool StartShock) {
        SetShockClientRpc(StartShock);
    }
    [ClientRpc]
    public void SetShockClientRpc(bool StartShock) {
        ManageStaticParticle();
        HasShocked = false;
        isShocking = true;
        Shock = StartCoroutine(ShockCoroutine());
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
            RingNoiseMaker.PlayOneShot(ShockClip);
            ObjectToShock.playerHeldBy?.DamagePlayer(Damage, false, true, CauseOfDeath.Blast);            
            ManageStaticParticle(true);
            SetTargetZapObjectServerRpc(ObjectToShock.gameObject, true);
        }
        yield return new WaitForSeconds(0.3f);
        WTOBase.LogToConsole("Stopping lightning bolt!");
        isShocking = false;
        ShockCooldown = 5f;
        LightningBolt.Stop();
        StopCoroutine(Shock);
    }
}
