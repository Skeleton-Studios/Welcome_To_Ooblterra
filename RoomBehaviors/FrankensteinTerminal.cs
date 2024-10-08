﻿using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Items;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.RoomBehaviors;
using static Welcome_To_Ooblterra.Items.Chemical;

namespace Welcome_To_Ooblterra.Things;
public class FrankensteinTerminal : NetworkBehaviour {

    private const int NumberOfPoints = 4;

    public FrankensteinChemPoint[] FrankensteinChemPoints = new FrankensteinChemPoint[NumberOfPoints];
    public FrankensteinBodyPoint BodyPoint;
    public MeshRenderer[] LightMeshes;
    public MeshRenderer SwitchMesh;
    public Material SwitchRedMat;
    public Material SwitchGreenMat;
    public AudioClip FailSound;
    public AudioSource Noisemaker;
    public InteractTrigger GuessScript;
    public InteractTrigger ReviveScript; 

    public FrankensteinVisuals Visuals;

    private bool ReviveStarted = false;
    private enum Correctness {
        Correct,
        Partial,
        Wrong
    }

    private bool AnimStarted;
    private ChemColor[] GuessedColorList = new ChemColor[NumberOfPoints];
    private ChemColor[] CorrectColors = new ChemColor[NumberOfPoints];
    private PlayerControllerB PlayerToRevive;
    private Vector3 SpawnTarget;
    private int PlayerID;
    private int RemainingGuesses = 3;
    private bool AllowGuesses = true;
    private System.Random MyRandom;
    private bool IsUsedUp = false;
    private int PopulatedChemPoints;
    private static bool MimicSpawned = false;

    private void Start() {
        MyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        BodyPoint = FindObjectOfType<FrankensteinBodyPoint>();
        Visuals = FindObjectOfType<FrankensteinVisuals>();
        SetColorList();
    }
    private void Update() {
        if (BodyPoint == null) {
            BodyPoint = FindObjectOfType<FrankensteinBodyPoint>();
        }
        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null) {
            return;
        }
        ManageInteractor(GuessScript);
        ManageInteractor(ReviveScript);
    }

    private void ManageInteractor(InteractTrigger Target) {
        if (IsUsedUp) {
            Target.interactable = false;
            Target.disabledHoverTip = "";
            return;
        }
        if (Target == ReviveScript && !BodyPoint.HasBody) {
            Target.disabledHoverTip = "No Body on Bed!";
            Target.interactable = false;
            return;
        }
        if (Target == GuessScript) {
            if(RemainingGuesses <= 0) {
                Target.interactable = false;
                Target.hoverTip = $"No Guesses Remaining!";
                return;
            }
            Target.hoverTip = $"Check Combination ({RemainingGuesses}) : [E]";
        }
        Target.interactable = CheckAllChemPoints(false);
        Target.disabledHoverTip = $"[{PopulatedChemPoints}/{NumberOfPoints} Chemicals Placed!]";
    }

    public void GuessIfChemsAreCorrect(PlayerControllerB TriggeringPlayer) {
        if (IsUsedUp) {
            return;
        }
        
        if (!CheckAllChemPoints(true)) {
            return;
        }
        if (RemainingGuesses <= 0 || !AllowGuesses) {
            return;
        }
        AllowGuesses = false;
        Correctness[] LightSetter = CheckCorrectness();
        SetLightsServerRpc(LightSetter);
    }
    public void TryRevive() {
        if (IsUsedUp) {
            return;
        }
        if (!CheckAllChemPoints(true)) {
            return;
        }
        if(BodyPoint == null || BodyPoint.PlayerRagdoll == null) {
            WTOBase.LogToConsole("Problem with body point!!!");
            return;
        }
        SetVariablesServerRpc(BodyPoint.RespawnPos.position, BodyPoint.PlayerRagdoll.bodyID.Value);
        StartSceneServerRpc(CalculateCorrectnessPercentage());
        foreach(FrankensteinChemPoint ChemPoint in FrankensteinChemPoints) {
            ChemPoint.ClearChemical();
        }
        IsUsedUp = true;
    }
    private Correctness[] CheckCorrectness() {
        Correctness[] TotalCorrect = { Correctness.Wrong, Correctness.Wrong, Correctness.Wrong, Correctness.Wrong };
        WTOBase.LogToConsole("BEGIN PRINT CORRECT COLORS");
        foreach(ChemColor color in CorrectColors) {
            WTOBase.LogToConsole(color.ToString());
        }
        WTOBase.LogToConsole("END PRINT CORRECT COLORS ; BEGIN PRINT GUESSED COLORS");
        foreach (ChemColor color in GuessedColorList) {
            WTOBase.LogToConsole(color.ToString());
        }
        WTOBase.LogToConsole("END PRINT GUESSED COLORS");

        for (int i = 0; i < NumberOfPoints; i++) {
            if (CorrectColors[i] == GuessedColorList[i]) {
                TotalCorrect[i] = Correctness.Correct;
            }
        }
        for(int i = 0; i < NumberOfPoints; i++) {
            ChemColor ColorBeingChecked = GuessedColorList[i];
            if (TotalCorrect[i] == Correctness.Correct) {
                continue;
            }
            if (CorrectColors.Contains(ColorBeingChecked) && TotalCorrect[Array.IndexOf(GuessedColorList, ColorBeingChecked)] == Correctness.Wrong) {
                TotalCorrect[i] = Correctness.Partial;
            }
        }
        return TotalCorrect;
    }
    private int CalculateCorrectnessPercentage() {
        int TotalScore = 0;
        foreach (Correctness CorrectnessValue in CheckCorrectness()) { 
            switch(CorrectnessValue) {
                case Correctness.Correct:
                    TotalScore += 25;
                    break;
                case Correctness.Partial:
                    TotalScore += 12;
                    break;
            }
        }
        WTOBase.LogToConsole($"CORRECTNESS SCORE IS {TotalScore}");
        return TotalScore;
    }
    private void SetColorList() {
        List<ChemColor> AllColors = new List<ChemColor>((ChemColor[])Enum.GetValues(typeof(ChemColor)));
        ChemColor NextColor;
        for (int i = 0; i < NumberOfPoints; i++) {
            NextColor = AllColors[MyRandom.Next(0, AllColors.Count - 1)];
            CorrectColors[i] = NextColor;
            AllColors.Remove(NextColor);
            WTOBase.LogToConsole($"COLOR NUMBER {i} IS {CorrectColors[i]}");
        }
    }
    private bool CheckAllChemPoints(bool AssignColors) {
        PopulatedChemPoints = 0;
        for (int i = 0; i < NumberOfPoints; i++) {
            FrankensteinChemPoint ChemPoint = FrankensteinChemPoints[i];
            if (!ChemPoint.hasChemical) {
                Array.Clear(GuessedColorList, 0, 4);
                if (AssignColors) { 
                    return false;
                }
                continue;
            }
            PopulatedChemPoints++;
            if (AssignColors) { 
                GuessedColorList[i] = ChemPoint.GetCurrentChemicalColor();
            }
        }
        return PopulatedChemPoints == NumberOfPoints;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartSceneServerRpc(int Correctness) {
        WTOBase.LogToConsole("Starting visuals on server...");
        StartSceneClientRpc(Correctness);
    }
    [ClientRpc]
    public void StartSceneClientRpc(int Correctness) {
        WTOBase.LogToConsole("Starting visuals on client...");
        StartCoroutine(StartScene(Correctness));
    }
    IEnumerator StartScene(int Correctness) {
        if (!AnimStarted) {
            WTOBase.LogToConsole("Telling Visuals script to start visuals ...");
            Visuals.StartVisuals();
        }
        yield return new WaitForSeconds(7);
        if (Correctness > 50 ) {
            ReviveDeadPlayerServerRpc(PlayerID, SpawnTarget);
        } else {
            CreateMimicServerRpc(PlayerID, SpawnTarget);
        }

        StopCoroutine(StartScene(Correctness));
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetLightsServerRpc(Correctness[] Colorset) {
        SetLightsClientRpc(Colorset);
    }
    [ClientRpc]
    private void SetLightsClientRpc(Correctness[] Colorset) {
        RemainingGuesses--;
        StartCoroutine(SetLightsAndWait(Colorset));
    }
    IEnumerator SetLightsAndWait(Correctness[] Colorset) {
        SwitchMesh.materials[1] = SwitchGreenMat;
        for (int i = 0; i < NumberOfPoints; i++) {
            WTOBase.LogToConsole($"SLOT {i} is {Colorset[i]}");
            switch (Colorset[i]) {
                case Correctness.Correct:
                    LightMeshes[i].materials[1].SetColor("_EmissiveColor", new Color(0, 1, 0, 1));
                    break;
                case Correctness.Partial:
                    LightMeshes[i].materials[1].SetColor("_EmissiveColor", new Color(1, 1, 0, 1));
                    break;
                case Correctness.Wrong:
                    LightMeshes[i].materials[1].SetColor("_EmissiveColor", new Color(1, 0, 0, 1));
                    break;
            }
        }
        yield return new WaitForSeconds(4);
        for (int i = 0; i < NumberOfPoints; i++) {
            LightMeshes[i].materials[1].SetColor("_EmissiveColor", new Color(0, 0, 0, 1));
        }
        SwitchMesh.materials[1] = SwitchRedMat;
        AllowGuesses = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetVariablesServerRpc(Vector3 TargetLoc, int iD) {
        SetVariablesClientRpc(TargetLoc, iD);
    }

    [ClientRpc]
    public void SetVariablesClientRpc(Vector3 Target, int ID) {
        SpawnTarget = Target;
        PlayerID = ID;
        PlayerToRevive = StartOfRound.Instance.allPlayerScripts[PlayerID];
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReviveDeadPlayerServerRpc(int ID, Vector3 SpawnLoc) {
        WTOBase.LogToConsole($"Reviving dead player {StartOfRound.Instance.allPlayerScripts[ID].playerUsername} on server...");
        ReviveDeadPlayerClientRpc(ID, SpawnLoc);
    }
    [ClientRpc]
    public void ReviveDeadPlayerClientRpc(int ID, Vector3 SpawnLoc) {
        if (ReviveStarted) {
            return;
        }
        WTOBase.LogToConsole($"Reviving dead player {StartOfRound.Instance.allPlayerScripts[ID].playerUsername} on client...");
        ReviveDeadPlayer(ID, SpawnLoc);
        ReviveStarted = true;
    }
    public void ReviveDeadPlayer(int ID, Vector3 SpawnLoc) {
        PlayerToRevive = StartOfRound.Instance.allPlayerScripts[ID];
        if(PlayerToRevive.isPlayerDead == false) {
            return;
        }
        WTOBase.LogToConsole("DEAD PLAYER INFO:");
        WTOBase.LogToConsole($"PLAYER Name: {StartOfRound.Instance.allPlayerScripts[ID].playerUsername}");
        WTOBase.LogToConsole($"PLAYER SCRIPT: {StartOfRound.Instance.allPlayerScripts[ID]}");
        WTOBase.LogToConsole("Reviving players A");
        PlayerToRevive.ResetPlayerBloodObjects(PlayerToRevive.isPlayerDead);
        PlayerToRevive.isClimbingLadder = false;
        PlayerToRevive.ResetZAndXRotation();
        PlayerToRevive.thisController.enabled = true;
        PlayerToRevive.health = 100;
        PlayerToRevive.disableLookInput = false;
        WTOBase.LogToConsole("Reviving players B");
        if (PlayerToRevive.isPlayerDead) {
            PlayerToRevive.isPlayerDead = false;
            PlayerToRevive.isPlayerControlled = true;
            PlayerToRevive.isInElevator = false;
            PlayerToRevive.isInHangarShipRoom = false;
            PlayerToRevive.isInsideFactory = true;
            //PlayerToRevive.wasInElevatorLastFrame = false;
            StartOfRound.Instance.SetPlayerObjectExtrapolate(enable: false);
            PlayerToRevive.TeleportPlayer(BodyPoint.RespawnPos.position);
            PlayerToRevive.setPositionOfDeadPlayer = false;
            PlayerToRevive.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[ID], enable: true, disableLocalArms: true);
            PlayerToRevive.helmetLight.enabled = false;
            PlayerToRevive.SetNightVisionEnabled(PlayerToRevive == StartOfRound.Instance.localPlayerController);
            WTOBase.LogToConsole("Reviving players C");
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
            WTOBase.LogToConsole("Reviving players D");
            PlayerToRevive.isSinking = false;
            PlayerToRevive.isUnderwater = false;
            PlayerToRevive.sinkingValue = 0f;
            PlayerToRevive.statusEffectAudio.Stop();
            PlayerToRevive.DisableJetpackControlsLocally();
            PlayerToRevive.health = 100;
            WTOBase.LogToConsole("Reviving players E");
            PlayerToRevive.mapRadarDotAnimator.SetBool("dead", value: false);
            if (PlayerToRevive.IsOwner) {
                HUDManager.Instance.gasHelmetAnimator.SetBool("gasEmitting", value: false);
                PlayerToRevive.hasBegunSpectating = false;
                HUDManager.Instance.RemoveSpectateUI();
                HUDManager.Instance.gameOverAnimator.SetTrigger("revive");
                PlayerToRevive.hinderedMultiplier = 1f;
                PlayerToRevive.isMovementHindered = 0;
                PlayerToRevive.sourcesCausingSinking = 0;
                WTOBase.LogToConsole("Reviving players E2");
            }
        }
        WTOBase.LogToConsole("Reviving players F");
        SoundManager.Instance.earsRingingTimer = 0f;
        PlayerToRevive.voiceMuffledByEnemy = false;
        SoundManager.Instance.playerVoicePitchTargets[PlayerID] = 1f;
        SoundManager.Instance.SetPlayerPitch(1f, PlayerID);
        PlayerToRevive.currentVoiceChatAudioSource = null;
        /*
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
        */
        WTOBase.LogToConsole("Reviving players G");
        if(GameNetworkManager.Instance.localPlayerController == PlayerToRevive) {
            HUDManager.Instance.HideHUD(hide: false);
            StartOfRound.Instance.SetSpectateCameraToGameOverMode(enableGameOver: false, PlayerToRevive);
            HUDManager.Instance.audioListenerLowPass.enabled = false;
            HUDManager.Instance.UpdateHealthUI(100, hurtPlayer: false);
        }
        PlayerToRevive.bleedingHeavily = false;
        PlayerToRevive.criticallyInjured = false;
        PlayerToRevive.playerBodyAnimator.SetBool("Limp", value: false);
        PlayerToRevive.health = 100;
        PlayerToRevive.spectatedPlayerScript = null;
        WTOBase.LogToConsole("Reviving players H");
        if (base.IsServer) {
            BodyPoint.BodyGO.NetworkObject.Despawn();
        }
        Destroy(BodyPoint.BodyGO);
        StartOfRound.Instance.livingPlayers += 1;
        WTOBase.LogToConsole($"New living player count: {StartOfRound.Instance.livingPlayers}");
        StartOfRound.Instance.allPlayersDead = false;
    }

    [ServerRpc]
    public void SyncMimicPropertiesServerRpc(NetworkObjectReference MimicNetObject, int SuitID) {
        SyncMimicPropertiesClientRpc(MimicNetObject, SuitID);
    }

    [ClientRpc]
    public void SyncMimicPropertiesClientRpc(NetworkObjectReference MimicNetObject, int SuitID) {
        NetworkObject netObject = null;
        MimicNetObject.TryGet(out netObject);
        MaskedPlayerEnemy Mask = netObject.GetComponent<MaskedPlayerEnemy>();
        Mask.maskTypes[0].SetActive(value: false);
        Mask.maskTypes[1].SetActive(value: false);
        Mask.SetSuit(PlayerToRevive.currentSuitID);
        Mask.maskTypeIndex = 0;
        PlayerToRevive.redirectToEnemy = Mask;
        Mask.mimickingPlayer = PlayerToRevive;
        PlayerToRevive.deadBody.DeactivateBody(setActive: false);
    }

    //if fail, spawn a mimic from the player's body
    [ServerRpc(RequireOwnership = false)]
    public void CreateMimicServerRpc(int ID, Vector3 MimicCreationPoint) {
        if(MimicSpawned == true) {
            return;
        }
        MimicSpawned = true;
        PlayerToRevive = StartOfRound.Instance.allPlayerScripts[ID];
        WTOBase.LogToConsole("Server creating mimic from Frankenstein");
        Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(MimicCreationPoint, default, 10f);
        if (!RoundManager.Instance.GotNavMeshPositionResult) {
            WTOBase.LogToConsole("No nav mesh found; no WTOMimic could be created");
            return;
        } 
        //const int MimicIndex = 12;
        EnemyType TheMimic = StartOfRound.Instance.levels.First(x => x.PlanetName == "8 Titan").Enemies.First(x => x.enemyType.enemyName == "Masked").enemyType;
        WTOBase.LogToConsole($"Mimic Found: {TheMimic != null}");

        WTOBase.LogToConsole($"NAVMESHPOS: {navMeshPosition}");
        WTOBase.LogToConsole($"PLAYER: {PlayerToRevive.playerUsername}");
        WTOBase.LogToConsole($"MIMIC: {TheMimic}");

        NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(navMeshPosition, PlayerToRevive.transform.eulerAngles.y, -1, TheMimic);

        if (netObjectRef.TryGet(out var networkObject)) {
            WTOBase.LogToConsole("Got network object for WTOMimic");
            MaskedPlayerEnemy component = networkObject.GetComponent<MaskedPlayerEnemy>();
            component.SetSuit(PlayerToRevive.currentSuitID);
            component.mimickingPlayer = PlayerToRevive;
            component.SetEnemyOutside(false);
            component.SetVisibilityOfMaskedEnemy();

            //This makes it such that the mimic has no visible mask :)
            component.maskTypes[0].SetActive(value: false);
            component.maskTypes[1].SetActive(value: false);
            component.maskTypeIndex = 0;

            PlayerToRevive.redirectToEnemy = component;
            PlayerToRevive.deadBody?.DeactivateBody(setActive: false);
        }
        CreateMimicClientRpc(ID, netObjectRef);
    }
    [ClientRpc]
    public void CreateMimicClientRpc(int ID, NetworkObjectReference netObjectRef) {
        if (!base.IsServer) {
            return;
        }
        StartCoroutine(waitForMimicEnemySpawn(ID, netObjectRef));
    }
    private IEnumerator waitForMimicEnemySpawn(int ID, NetworkObjectReference netObjectRef) {
        PlayerToRevive = StartOfRound.Instance.allPlayerScripts[ID];
        NetworkObject netObject = null;
        float startTime = Time.realtimeSinceStartup;
        yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || netObjectRef.TryGet(out netObject));
        if (PlayerToRevive == null || PlayerToRevive.deadBody == null) {
            startTime = Time.realtimeSinceStartup;
            yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime > 20f || PlayerToRevive.deadBody != null);
        }
        PlayerToRevive.deadBody.DeactivateBody(setActive: false);
        if (netObject == null) {
            yield break;
        }
        WTOBase.LogToConsole("Got network object for WTOMimic enemy client");
        MaskedPlayerEnemy component = netObject.GetComponent<MaskedPlayerEnemy>();
        component.mimickingPlayer = PlayerToRevive;
        component.SetEnemyOutside(false);
        component.SetVisibilityOfMaskedEnemy();
        //This makes it such that the mimic has no visible mask :)
        if (base.IsServer) {
            SyncMimicPropertiesServerRpc(netObjectRef, PlayerToRevive.currentSuitID);
        }        
    }
}
