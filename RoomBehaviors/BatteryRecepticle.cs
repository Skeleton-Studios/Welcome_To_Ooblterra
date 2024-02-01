using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Welcome_To_Ooblterra.Things;
public class BatteryRecepticle : NetworkBehaviour {

    public Animator MachineAnimator;
    public AudioSource Noisemaker;
    public AudioClip FacilityPowerUp;

    private ScrapShelf scrapShelf;


    public void Start() {
        scrapShelf = FindFirstObjectByType<ScrapShelf>();

    }

    [ServerRpc]
    public void TurnOnPowerServerRpc() {
        TurnOnPowerClientRpc();
    }

    [ClientRpc]
    public void TurnOnPowerClientRpc() {
        TurnOnPower();
    }
    
    public void TurnOnPower() {
        Noisemaker.PlayOneShot(FacilityPowerUp);
        scrapShelf.OpenShelf();
        LightComponent[] LightsInLevel = FindObjectsOfType<LightComponent>();
        foreach (LightComponent light in LightsInLevel) {
            light.SetLightColor();
            light.SetLightBrightness(300);
        }
    }
}
