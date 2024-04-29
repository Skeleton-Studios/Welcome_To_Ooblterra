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
    public ParticleSystem ExplodeParticle;
    public GameObject self;
    public BabyLurkerAI OwningLurker;
    private float AutoDestroyTime = 2f;
     
    private void OnTriggerEnter(Collider other) {
        WTOBase.LogToConsole($"Collision registered! Collider: {other.gameObject}");
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (other.gameObject.CompareTag("Player")) {
            victim.DamagePlayer(15, causeOfDeath: CauseOfDeath.Unknown); 
        }
        DestroySelf();
    }

    private void Update() {
        AutoDestroyTime -= Time.deltaTime;
        if(AutoDestroyTime <= 0f) {
            DestroySelf();
        }
    }

    private void DestroySelf() {
        
        Noisemaker?.Play();
        ExplodeParticle?.Play();
        if(OwningLurker != null) { 
            OwningLurker.ThrowingSelfAtPlayer = false;
            OwningLurker.KillEnemyOnOwnerClient(true);
        }
        Destroy(self); 
    }
}
