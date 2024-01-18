using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Items;
using Welcome_To_Ooblterra.Properties;
using static Welcome_To_Ooblterra.Items.Chemical;

namespace Welcome_To_Ooblterra.Things;
public class FrankensteinTerminal : NetworkBehaviour {

    private const int NumberOfPoints = 4;

    public FrankensteinChemPoint[] FrankensteinChemPoints = new FrankensteinChemPoint[NumberOfPoints];
    public FrankensteinBodyPoint BodyPoint;
    public MeshRenderer[] LightMeshes;
    public AudioClip FailSound;
    public AudioSource Noisemaker;
    public InteractTrigger GuessScript;
    public InteractTrigger CheckScript;


    private enum Correctness {
        Correct,
        Partial,
        Wrong
    }

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

    public void SetVariables(Vector3 Target, int ID) {
        SpawnTarget = Target;
        PlayerID = ID;
        PlayerToRevive = StartOfRound.Instance.allPlayerScripts[PlayerID];
    }

    public void GuessIfChemsAreCorrect() {
        if (IsUsedUp) {
            return;
        }
        if (!CheckAllChemPoints(true)) {
            Noisemaker.PlayOneShot(FailSound);
            return;
        }
        if (RemainingGuesses <= 0 || !AllowGuesses) {
            return;
        }
        RemainingGuesses--;
        AllowGuesses = false;
        StartCoroutine(SetLightsAndWait());
    }

    public void TryRevive() {
        if (IsUsedUp) {
            return;
        }
        if (!CheckAllChemPoints(true)) {
            Noisemaker.PlayOneShot(FailSound);
            return;
        }
        if(BodyPoint == null || BodyPoint.PlayerRagdoll == null) {
            WTOBase.LogToConsole("Problem with body point!!!");
            return;
        }
        SetVariables(BodyPoint.RespawnPos.position, BodyPoint.PlayerRagdoll.bodyID.Value);
        if (CalculateCorrectnessPercentage() > 50) {
            ReviveDeadPlayerServerRpc(PlayerID, SpawnTarget);
        } else {
            CreateMimicServerRpc(SpawnTarget);
        }
        IsUsedUp = true;
    }

    private Correctness[] CheckCorrectness() {
        Correctness[] TotalCorrect = { Correctness.Wrong, Correctness.Wrong, Correctness.Wrong, Correctness.Wrong };
        WTOBase.LogToConsole("BEGIN PRINT CORRECT COLORS");
        foreach(ChemColor color in CorrectColors) {
            Debug.Log(color);
        }
        WTOBase.LogToConsole("END PRINT CORRECT COLORS ; BEGIN PRINT GUESSED COLORS");
        foreach (ChemColor color in GuessedColorList) {
            Debug.Log(color);
        }
        WTOBase.LogToConsole("END PRINT GUESSED COLORS");
        for (int i = 0; i < NumberOfPoints; i++) {
            if (CorrectColors[i] == GuessedColorList[i]) {
                TotalCorrect[i] = Correctness.Correct;
            }
        }
        for(int i = 0; i < NumberOfPoints; i++) {
            if (TotalCorrect[i] == Correctness.Correct) {
                continue;
            }
            if (CorrectColors.Contains(GuessedColorList[i]) && TotalCorrect[Array.IndexOf(CorrectColors, GuessedColorList[i])] == Correctness.Wrong) {
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

    private void Start() {
        MyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        BodyPoint = FindObjectOfType<FrankensteinBodyPoint>();
        SetColorList();
    }

    private void Update() {
        if (BodyPoint == null) {
            BodyPoint = FindObjectOfType<FrankensteinBodyPoint>();
        }
        if (IsUsedUp) {
            GuessScript.interactable = false;
            CheckScript.interactable = false;
            GuessScript.disabledHoverTip = "";
            CheckScript.disabledHoverTip = "";
            return;
        }
        if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.localPlayerController != null) {
            GuessScript.hoverTip = $"Check Combination ({RemainingGuesses}) : [E]";
            GuessScript.interactable = CheckAllChemPoints(false);
            CheckScript.interactable = CheckAllChemPoints(false);
            string DisableString = $"[{PopulatedChemPoints}/{NumberOfPoints} Chemicals Placed!]";
            GuessScript.disabledHoverTip = DisableString;
            CheckScript.disabledHoverTip = DisableString;
        }
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
                return false;
            }
            PopulatedChemPoints++;
            if (AssignColors) { 
                GuessedColorList[i] = ChemPoint.GetCurrentChemicalColor();
            }
        }
        return true;
    }

    IEnumerator SetLightsAndWait() {
        Correctness[] LightSetter = CheckCorrectness();

        for (int i = 0; i < NumberOfPoints; i++) {
            if (LightSetter[i] == Correctness.Correct) {
                LightMeshes[i].materials[1].SetColor("_EmissiveColor", new Color(0, 1, 0, 1));
            } else {
                LightMeshes[i].materials[1].SetColor("_EmissiveColor", new Color(1, 0, 0, 1));
            }
        }
        yield return new WaitForSeconds(4);
        for (int i = 0; i < NumberOfPoints; i++) {
            LightMeshes[i].materials[1].SetColor("_EmissiveColor", new Color(0, 0, 0, 1));
        }
        AllowGuesses = true;
    }

    
    [ServerRpc]
    public void ReviveDeadPlayerServerRpc(int ID, Vector3 SpawnLoc) {
        Debug.Log($"Reviving dead player {ID} on server...");
        ReviveDeadPlayerClientRpc(ID, SpawnLoc);
    }
    [ClientRpc]
    public void ReviveDeadPlayerClientRpc(int ID, Vector3 SpawnLoc) {
        Debug.Log($"Reviving dead player {ID} on client...");
        ReviveDeadPlayer(ID, SpawnLoc);
    }
    public void ReviveDeadPlayer(int ID, Vector3 SpawnLoc) {
        PlayerToRevive = StartOfRound.Instance.allPlayerScripts[ID];
        WTOBase.LogToConsole("DEAD PLAYER INFO:");
        Debug.Log($"PLAYER ID: {ID}");
        Debug.Log($"PLAYER SCRIPT: {StartOfRound.Instance.allPlayerScripts[ID]}");
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
            PlayerToRevive.TeleportPlayer(SpawnLoc);
            PlayerToRevive.setPositionOfDeadPlayer = false;
            PlayerToRevive.DisablePlayerModel(StartOfRound.Instance.allPlayerObjects[ID], enable: true, disableLocalArms: true);
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
    [ServerRpc]
    public void CreateMimicServerRpc(Vector3 MimicCreationPoint) {

        Debug.Log("Server creating mimic from Frankenstein");
        Vector3 navMeshPosition = RoundManager.Instance.GetNavMeshPosition(MimicCreationPoint, default, 10f);
        if (!RoundManager.Instance.GotNavMeshPositionResult) {
            Debug.Log("No nav mesh found; no WTOMimic could be created");
            return;
        }
        const int MimicIndex = 12;
        EnemyType TheMimic = StartOfRound.Instance.levels[8].Enemies[MimicIndex].enemyType;
        Debug.Log($"Mimic Found: {TheMimic != null}");

        NetworkObjectReference netObjectRef = RoundManager.Instance.SpawnEnemyGameObject(navMeshPosition, PlayerToRevive.transform.eulerAngles.y, -1, TheMimic);

        if (netObjectRef.TryGet(out var networkObject)) {
            Debug.Log("Got network object for WTOMimic");
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
        CreateMimicClientRpc(netObjectRef);
    }
    [ClientRpc]
    public void CreateMimicClientRpc(NetworkObjectReference netObjectRef) {
        StartCoroutine(waitForMimicEnemySpawn(netObjectRef));
    }
    private IEnumerator waitForMimicEnemySpawn(NetworkObjectReference netObjectRef) {
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
        Debug.Log("Got network object for WTOMimic enemy client");
        MaskedPlayerEnemy component = netObject.GetComponent<MaskedPlayerEnemy>();
        component.mimickingPlayer = PlayerToRevive;
        component.SetSuit(PlayerToRevive.currentSuitID);
        component.SetEnemyOutside(false);
        component.SetVisibilityOfMaskedEnemy();

        //This makes it such that the mimic has no visible mask :)
        component.maskTypes[0].SetActive(value: false);
        component.maskTypes[1].SetActive(value: false);
        component.maskTypeIndex = 0;

        PlayerToRevive.redirectToEnemy = component;
    }
}
