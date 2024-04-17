using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Welcome_To_Ooblterra.Things;

internal class HazyWeather : MonoBehaviour {

    public LocalVolumetricFog HazyRedFog;
    public LocalVolumetricFog MainOoblFog;
    private bool IsFogActive = false;
    public BoxCollider MyCollider;

    private void Start() {    
        if(TimeOfDay.Instance.currentLevelWeather != LevelWeatherType.Foggy) {
            return;
        }
        //This probably doesn't work
        HazyRedFog.enabled = true;
        MainOoblFog.enabled = false;

        IsFogActive = true;
    }
    private void Update() {
        if (!IsFogActive) {
            return;
        }
        foreach(PlayerControllerB Player in StartOfRound.Instance.allPlayerScripts) { 
            if(Player.isPlayerDead || Player.isInsideFactory || Player.isInHangarShipRoom) {
                Player.isUnderwater = false;
                Player.underwaterCollider = null;
                continue;
            }
            Player.isUnderwater = true;
            Player.underwaterCollider = MyCollider;
        }
    }
}

