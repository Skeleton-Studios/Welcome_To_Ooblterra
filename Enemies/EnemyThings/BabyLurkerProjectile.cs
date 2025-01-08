using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;

namespace Welcome_To_Ooblterra.Enemies.EnemyThings;
public class BabyLurkerProjectile : NetworkBehaviour {

    public AudioSource Noisemaker;
    public AudioClip[] ExplodeSounds;
    public ParticleSystem ExplodeParticle;
    public GameObject self;
    public SkinnedMeshRenderer LurkerMesh;
    public MeshRenderer ArachnophobiaMesh;
    public BabyLurkerAI OwningLurker;
    private float AutoDestroyTime = 2f;
    private bool StartAutoDestroy = false;
    private System.Random ProjectileRandom;
    private bool IsDead = false;
    private bool IsArachnophobiaMode = false;

    private static readonly WTOBase.WTOLogger Log = new(typeof(BabyLurkerProjectile), LogSourceType.Thing);

    private void OnTriggerEnter(Collider other) {
        Log.Debug($"Collision registered! Collider: {other.gameObject}");
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (other.gameObject.CompareTag("Player") && !IsDead) {
            victim.DamagePlayer(15, causeOfDeath: CauseOfDeath.Unknown);
            
        }
        DestroySelf(); 
        IsDead = true;
    }
    
    private void Start() {
        ProjectileRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        if (IsArachnophobiaMode != IngamePlayerSettings.Instance.unsavedSettings.spiderSafeMode) {
            IsArachnophobiaMode = IngamePlayerSettings.Instance.unsavedSettings.spiderSafeMode;
            ArachnophobiaMesh.enabled = IsArachnophobiaMode;
            LurkerMesh.enabled = !IsArachnophobiaMode;
        }
    }
    private void Update() { 
        if (!StartAutoDestroy){
            return;
        }
        AutoDestroyTime -= Time.deltaTime;
        if(AutoDestroyTime <= 0f) {
            Destroy(this);
        }
    }

    private void DestroySelf() {

        Noisemaker?.PlayOneShot(ExplodeSounds[ProjectileRandom.Next(0, ExplodeSounds.Length)]);
        ExplodeParticle?.Play();
        GetComponent<Rigidbody>().isKinematic = true;
        
        LurkerMesh.enabled = false;
        ArachnophobiaMesh.enabled = false;
        if (OwningLurker != null) { 
            OwningLurker.ThrowingSelfAtPlayer = false;
            OwningLurker.KillEnemyOnOwnerClient(true);
        }
        StartAutoDestroy = true;
    }
}
