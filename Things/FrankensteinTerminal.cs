using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Things;
public class FrankensteinTerminal : NetworkBehaviour {

    private PlayerControllerB PlayerToRevive;
    private Vector3 TargetPosition;
    private int PlayerID = 1;
    //Create a new terminal, change what it displays and what you can type into it

    //for now we're just gonna have two commands: revive and fail.
    //Later this will be tied to a minigame and your performance in it will determine which happens
    public void SetVariables(Vector3 Target, int ID) {
        TargetPosition = Target;
        PlayerID = ID;
        PlayerToRevive = StartOfRound.Instance.allPlayerScripts[PlayerID];
    }

    //Both commands should fail unless the associated BodyPoint has a body
    public void Wrapper() {
        Debug.Log("Wrapper calling Server RPC...");
        ReviveDeadPlayerServerRpc();
    }

    //if revive, bring the player back from the dead, steal this from the roundmanager
    //also make sure to find some way to gag the player
    
    [ServerRpc]
    public void ReviveDeadPlayerServerRpc() {
        Debug.Log("Reviving dead player on server...");
        ReviveDeadPlayerClientRpc();
    }
    [ClientRpc]
    public void ReviveDeadPlayerClientRpc() {
        Debug.Log("Reviving dead player on client...");
        ReviveDeadPlayer();
    }
    public void ReviveDeadPlayer() {
        WTOBase.LogToConsole("DEAD PLAYER INFO:");
        Debug.Log($"PLAYER ID: {PlayerID}");
        Debug.Log($"PLAYER SCRIPT: {StartOfRound.Instance.allPlayerScripts[PlayerID]}");
        Debug.Log($"PLAYER SCRIPT VALID: {PlayerToRevive = StartOfRound.Instance.allPlayerScripts[PlayerID]}");
        Debug.Log("Reviving players A");
        PlayerToRevive.ResetPlayerBloodObjects(PlayerToRevive.isPlayerDead);
        PlayerToRevive.isClimbingLadder = false;
        PlayerToRevive.ResetZAndXRotation();
        PlayerToRevive.thisController.enabled = true;
        PlayerToRevive.health = 100;
        PlayerToRevive.disableLookInput = false;
        Debug.Log("Reviving players B");
        if (PlayerToRevive.isPlayerDead) {
            PlayerToRevive.isPlayerDead = false;
            PlayerToRevive.isPlayerControlled = true;
            PlayerToRevive.isInElevator = true;
            PlayerToRevive.isInHangarShipRoom = true;
            PlayerToRevive.isInsideFactory = false;
            PlayerToRevive.wasInElevatorLastFrame = false;
            StartOfRound.Instance.SetPlayerObjectExtrapolate(enable: false);
            PlayerToRevive.TeleportPlayer(TargetPosition);
            PlayerToRevive.setPositionOfDeadPlayer = false;
            PlayerToRevive.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[PlayerID], enable: true, disableLocalArms: true);
            PlayerToRevive.helmetLight.enabled = false;
            Debug.Log("Reviving players C");
            PlayerToRevive.Crouch(crouch: false);
            PlayerToRevive.criticallyInjured = false;
            if (PlayerToRevive.playerBodyAnimator != null) {
                PlayerToRevive.playerBodyAnimator.SetBool("Limp", value: false);
            }
            PlayerToRevive.bleedingHeavily = false;
            PlayerToRevive.activatingItem = false;
            PlayerToRevive.twoHanded = false;
            PlayerToRevive.inSpecialInteractAnimation = false;
            PlayerToRevive.disableSyncInAnimation = false;
            PlayerToRevive.inAnimationWithEnemy = null;
            PlayerToRevive.holdingWalkieTalkie = false;
            PlayerToRevive.speakingToWalkieTalkie = false;
            Debug.Log("Reviving players D");
            PlayerToRevive.isSinking = false;
            PlayerToRevive.isUnderwater = false;
            PlayerToRevive.sinkingValue = 0f;
            PlayerToRevive.statusEffectAudio.Stop();
            PlayerToRevive.DisableJetpackControlsLocally();
            PlayerToRevive.health = 100;
            Debug.Log("Reviving players E");
            PlayerToRevive.mapRadarDotAnimator.SetBool("dead", value: false);
            if (PlayerToRevive.IsOwner) {
                HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: false);
                PlayerToRevive.hasBegunSpectating = false;
                HUDManager.Instance.RemoveSpectateUI();
                HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                PlayerToRevive.hinderedMultiplier = 1f;
                PlayerToRevive.isMovementHindered = 0;
                PlayerToRevive.sourcesCausingSinking = 0;
                Debug.Log("Reviving players E2");
            }
        }
        Debug.Log("Reviving players F");
        SoundManager.Instance.earsRingingTimer = 0f;
        PlayerToRevive.voiceMuffledByEnemy = false;
        SoundManager.Instance.playerVoicePitchTargets[PlayerID] = 1f;
        SoundManager.Instance.SetPlayerPitch(1f, PlayerID);
        
        if (PlayerToRevive.currentVoiceChatIngameSettings == null) {
            StartOfRound.Instance.RefreshPlayerVoicePlaybackObjects();
        }
        if (PlayerToRevive.currentVoiceChatIngameSettings != null) {
            if (PlayerToRevive.currentVoiceChatIngameSettings.voiceAudio == null) {
               PlayerToRevive.currentVoiceChatIngameSettings.InitializeComponents();
            }
            if (PlayerToRevive.currentVoiceChatIngameSettings.voiceAudio == null) {
                return;
            }
            PlayerToRevive.currentVoiceChatIngameSettings.voiceAudio.GetComponent<OccludeAudio>().overridingLowPass = false;
        }
        
        Debug.Log("Reviving players G");
        PlayerControllerB playerControllerB = GameNetworkManager.Instance.localPlayerController;
        playerControllerB.bleedingHeavily = false;
        playerControllerB.criticallyInjured = false;
        playerControllerB.playerBodyAnimator.SetBool("Limp", value: false);
        playerControllerB.health = 100;
        HUDManager.Instance.UpdateHealthUI(100, hurtPlayer: false);
        playerControllerB.spectatedPlayerScript = null;
        HUDManager.Instance.audioListenerLowPass.enabled = false;
        Debug.Log("Reviving players H");
        StartOfRound.Instance.SetSpectateCameraToGameOverMode(enableGameOver: false, playerControllerB);
        RagdollGrabbableObject[] array = UnityEngine.Object.FindObjectsOfType<RagdollGrabbableObject>();
        for (int j = 0; j < array.Length; j++) {
            if (!array[j].isHeld) {
                if (StartOfRound.Instance.IsServer) {
                    if (array[j].NetworkObject.IsSpawned) {
                        array[j].NetworkObject.Despawn();
                    } else {
                        UnityEngine.Object.Destroy(array[j].gameObject);
                    }
                }
            } else if (array[j].isHeld && array[j].playerHeldBy != null) {
                array[j].playerHeldBy.DropAllHeldItems();
            }
        }
        DeadBodyInfo[] array2 = UnityEngine.Object.FindObjectsOfType<DeadBodyInfo>();
        for (int k = 0; k < array2.Length; k++) {
            UnityEngine.Object.Destroy(array2[k].gameObject);
        }
        StartOfRound.Instance.livingPlayers++;
        StartOfRound.Instance.allPlayersDead = false;
    }

    //if fail, spawn a mimic from the player's body

}
