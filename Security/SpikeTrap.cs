using DunGen;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;
using static System.Net.Mime.MediaTypeNames;

namespace Welcome_To_Ooblterra.Security;
internal class SpikeTrap : NetworkBehaviour {

    public GameObject SpikeMesh;
    public AudioClip SpikesExtend;
    public AudioClip SpikesRetract;
    public AudioClip SpikesDisable;
    public AudioSource SpikeSoundPlayer;
    public Transform RootRotation;

    private bool SpikesEnabled = true;
    private bool SpikesActivated;
    private bool AllowDamagePlayer;
    private float SecondsSinceSpikesActivated;
    private float SecondsUntilCanDamagePlayer;
    private const int PlayerDamageAmount = 80;
    
    //anim controls
    private float TimeElapsed;
    private const float MoveTime = 0.2f;
    private Vector3 SpikeRaisePos = new Vector3(0, 1.163f, 0);
    private Vector3 SpikeFallPos;

    public void OnTriggerEnter(Collider other) {
        if (!SpikesEnabled) {
            return;
        }
        if (SpikesActivated) {
            return;
        }
        if (other.gameObject.CompareTag("Player")) {
            WTOBase.LogToConsole("Player entered spike trap, calling Raise RPC!");
            RaiseSpikesServerRpc();
        }
    }
    public void OnTriggerStay(Collider other) {
        if (AllowDamagePlayer && other.gameObject.CompareTag("Player") && SecondsUntilCanDamagePlayer <= 0) { 
            other.GetComponent<PlayerControllerB>().DamagePlayer(PlayerDamageAmount, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 0);
            SecondsUntilCanDamagePlayer = 0.75f;
        }
    }

    public void Update() {
        RootRotation.eulerAngles = new Vector3(0, 0, 0);
        if(SecondsUntilCanDamagePlayer > 0) {
            SecondsUntilCanDamagePlayer -= Time.deltaTime;
        }
        if (SecondsSinceSpikesActivated >= 3f) {
            if(SpikesActivated == true) {
                SpikeSoundPlayer.clip = SpikesRetract;
                SpikeSoundPlayer.Play();
                TimeElapsed = 0f;
            }
            SpikesActivated = false;
            WTOBase.LogToConsole("Lowering Spikes!");
            StartCoroutine(LowerSpikes());
        }
        if (!SpikesEnabled) {
            return;
        }
        if (SpikesActivated) {
            WTOBase.LogToConsole("Raising Spikes!");
            StartCoroutine(RaiseSpikes());
            SecondsSinceSpikesActivated += Time.deltaTime;
        }

    }
    IEnumerator RaiseSpikes() {
        TimeElapsed += Time.deltaTime;
        SpikeMesh.transform.localPosition = Vector3.Lerp(SpikeFallPos, SpikeRaisePos, TimeElapsed / MoveTime);
        if (TimeElapsed / MoveTime >= 1) {
            AllowDamagePlayer = true;
            WTOBase.LogToConsole("Finished Raising Spikes");
            StopCoroutine(RaiseSpikes());
        }
        yield return null;
    }
    IEnumerator LowerSpikes() {
        TimeElapsed += Time.deltaTime;
        WTOBase.LogToConsole($"Current Lerp Position: {TimeElapsed / MoveTime}");
        SpikeMesh.transform.localPosition = Vector3.Lerp(SpikeRaisePos, SpikeFallPos, TimeElapsed / MoveTime);
        if (TimeElapsed / MoveTime >= 1) {
            AllowDamagePlayer = false;
            WTOBase.LogToConsole("Finished Lowering Spikes");
            StopCoroutine(LowerSpikes());
            SecondsSinceSpikesActivated = 0f;
            SpikeMesh.GetComponent<MeshRenderer>().enabled = false;
        }
        yield return null;
    }

    public void RecieveToggleSpikes(bool enabled) {
        WTOBase.LogToConsole($"Called toggle spikes with state: {enabled}");
        ToggleSpikesServerRpc(enabled);
        ToggleSpikes(enabled);
    }
    [ServerRpc(RequireOwnership = false)]
    public void ToggleSpikesServerRpc(bool enabled) {
        WTOBase.LogToConsole($"Toggling spikes to {enabled} serverRpc");
        ToggleSpikesClientRpc(enabled);
    }
    [ClientRpc]
    public void ToggleSpikesClientRpc(bool enabled) {
        WTOBase.LogToConsole($"Toggling spikes to {enabled} clientRpc");
        if (SpikesEnabled != enabled) {
            ToggleSpikes(enabled);
        }
    }
    private void ToggleSpikes(bool enabled) {
        if(enabled == false) { 
            SpikeSoundPlayer.clip = SpikesDisable;
            SpikeSoundPlayer.Play();
        }
        SpikesEnabled = enabled;
        WTOBase.LogToConsole($"SPIKES STATE: {SpikesEnabled}");
        if (SpikesActivated) {
            SecondsSinceSpikesActivated = 5f;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RaiseSpikesServerRpc() {
        RaiseSpikesClientRpc();
    }
    [ClientRpc]
    public void RaiseSpikesClientRpc() {
        TimeElapsed = 0f;
        SpikeSoundPlayer.clip = SpikesExtend;
        SpikeSoundPlayer.Play();
        SpikeMesh.GetComponent<MeshRenderer>().enabled = true;
        SpikesActivated = true;
    }
}
