﻿using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies.EnemyThings;
public class BabyLurkerProjectile : NetworkBehaviour {

    public AudioSource Noisemaker;
    public ParticleSystem ExplodeParticle;
    public GameObject self;

    private void OnTriggerEnter(Collider other) {
        WTOBase.LogToConsole($"Collision registered! Collider: {other.gameObject}");
        PlayerControllerB victim = other.gameObject.GetComponent<PlayerControllerB>();
        if (other.gameObject.CompareTag("Player")) {
            victim.DamagePlayer(5, causeOfDeath: CauseOfDeath.Unknown);
        }
        if(other.GetComponent<BabyLurkerProjectile>() != null || other.GetComponent<BabyLurkerAI>() != null) {
            return;
        }
        DestroySelf();
    }

    private void DestroySelf() {
        
        Noisemaker?.Play();
        ExplodeParticle?.Play();
        Destroy(self); 
    }
}
