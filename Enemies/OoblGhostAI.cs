using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;
using static UnityEngine.Rendering.DebugUI;

namespace Welcome_To_Ooblterra.Enemies;
internal class OoblGhostAI : WTOEnemy {

    [InspectorName("BalanceConstants")]
    public static float GhostInterferenceRange = 5f;
    public static float OoblGhostSpeed = 6f;
    private float GhostDamagePerTick = 10f;
    private float GhostInterferenceTime = 3.5f;

    //STATES
    private class WaitForNextAttack : BehaviorState {
        public WaitForNextAttack() {
            
            RandomRange = new Vector2(90, 135);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GhostList[enemyIndex].SecondsUntilGhostWillAttack = MyRandomInt;
            GhostList[enemyIndex].creatureVoice.Stop();
            GhostList[enemyIndex].transform.position = new Vector3(0, -1000, 0);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new AttackTimerDone()
        };
    }
    private class ChooseTarget : BehaviorState {

        List<PlayerControllerB> AllPlayersList;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            AllPlayersList = StartOfRound.Instance.allPlayerScripts.ToList();
            for (int i = 0; i < StartOfRound.Instance.allPlayerScripts.Length; i++) {
                PlayerControllerB player = StartOfRound.Instance.allPlayerScripts[i];
                if (!player.isPlayerControlled || player.isPlayerDead){
                    AllPlayersList.Remove(player);
                    continue;
                }
                WTOBase.LogToConsole($"GHOST #{GhostList[enemyIndex].WTOEnemyID}: Found Player {StartOfRound.Instance.allPlayerScripts[i].playerUsername}");
            }
            if(AllPlayersList != null && AllPlayersList.Count >= 0 && GhostList[enemyIndex].IsOwner) {
                PlayerControllerB CachedTargetPlayer = GhostList[enemyIndex].FindMostHauntedPlayer(enemyRandom, AllPlayersList);
                GhostList[enemyIndex].SetGhostTargetServerRpc(Array.IndexOf(StartOfRound.Instance.allPlayerScripts, CachedTargetPlayer));

            }
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new TargetChosen()
        };
    }
    private class GoTowardPlayer : BehaviorState {
        public GoTowardPlayer() {
            RandomRange = new Vector2(-300, 300);
        }
        Vector3 DirectionVector;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GhostList[enemyIndex].PlayerToAttack.statusEffectAudio.PlayOneShot(GhostList[enemyIndex].StartupSound);
            GhostList[enemyIndex].creatureVoice.Play();   
            GhostList[enemyIndex].transform.position = new Vector3(MyRandomInt, GhostList[enemyIndex].PlayerToAttack.transform.position.y, MyRandomInt);
            GhostList[enemyIndex].GhostPickedUpInterference = false;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GhostList[enemyIndex].MoveGhostTowardPlayer();
            if (Vector3.Distance(GhostList[enemyIndex].transform.position, GhostList[enemyIndex].PlayerToAttack.transform.position) < GhostInterferenceRange) {
                GhostList[enemyIndex].ShouldListenForWalkie = true;
            } else {
                GhostList[enemyIndex].ShouldListenForWalkie = false;
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GhostList[enemyIndex].PlayerToAttack = null;
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new KilledPlayer(),
            new PlayerHasFoughtBack()
        };
    }

    //TRANSITIONS
    private class AttackTimerDone : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].SecondsUntilGhostWillAttack <= 0f;
        }
        public override BehaviorState NextState() {
            return new ChooseTarget();
        }
    }
    private class TargetChosen : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].PlayerToAttack != null;
        }
        public override BehaviorState NextState() {
            return new GoTowardPlayer();
        }
    }
    private class KilledPlayer : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].PlayerToAttack.isPlayerDead;
        }
        public override BehaviorState NextState() {
            return new WaitForNextAttack();
        }
    }
    private class PlayerHasFoughtBack : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].GhostPickedUpInterference;
        }
        public override BehaviorState NextState() {
            GhostList[enemyIndex].ShouldFlickerLights = false;
            return new WaitForNextAttack();
        }
    }

    public static Dictionary<int, OoblGhostAI> GhostList = new();
    private static int GhostID;
    private PlayerControllerB PlayerToAttack;
    private float SecondsUntilGhostWillAttack;
    public AudioClip StartupSound;
    public bool GhostPickedUpInterference = false;
    private bool ShouldFlickerLights;
    private bool ShouldFlickerShipLights;
    private bool ShouldListenForWalkie;

    private static Dictionary<PlayerControllerB, int> PlayersTimesHaunted = new();

    public override void Start() {
        MyValidState = PlayerState.Inside;
        InitialState = new WaitForNextAttack();
        PrintDebugs = true;
        GhostID++;
        WTOEnemyID = GhostID;
        //transform.position = new Vector3(0, -300, 0);
        LogMessage($"Adding Oobl Ghost {this} #{GhostID}");
        GhostList.Add(GhostID, this);

        //GetComponent<AcidWater>().DamageAmount = (int)GhostDamagePerTick;
        base.Start();
        transform.position = new Vector3(0, -1000, 0);
    }
    public override void Update() {
        MoveTimerValue(ref SecondsUntilGhostWillAttack);
        base.Update();
        if (PlayerToAttack == null || ActiveState is not GoTowardPlayer) {
            return;
        }
        if (ShouldFlickerShipLights) {
            StartCoroutine(FlickerShipLights());
        } else {
            StartCoroutine(FlickerNearbyLights());
        }
        if (ShouldListenForWalkie) {
            if (PlayerToAttack.PlayerIsHearingOthersThroughWalkieTalkie()) {
                StartCoroutine(ListenForWalkie());
            }
            StopCoroutine(ListenForWalkie());
        }
    }

    public PlayerControllerB FindMostHauntedPlayer(System.Random EnemyRandom, List<PlayerControllerB> PlayersToCheck) {
        float HighestInsanityLevel = 0f;
        float PlayerWithHighestInsanityLevel = 0f;
        int HighestTurnAmount = 0;
        int PlayerWithHighestTurnAmount = 0;
        for (int PlayerIndex = 0; PlayerIndex < PlayersToCheck.Count; PlayerIndex++) {
            if (StartOfRound.Instance.gameStats.allPlayerStats[PlayerIndex].turnAmount > HighestTurnAmount) {
                HighestTurnAmount = StartOfRound.Instance.gameStats.allPlayerStats[PlayerIndex].turnAmount;
                PlayerWithHighestTurnAmount = PlayerIndex;
            }
            if (StartOfRound.Instance.allPlayerScripts[PlayerIndex].insanityLevel > HighestInsanityLevel) {
                HighestInsanityLevel = StartOfRound.Instance.allPlayerScripts[PlayerIndex].insanityLevel;
                PlayerWithHighestInsanityLevel = PlayerIndex;
            }
        }
        int[] PlayerInsanityLevelList = new int[PlayersToCheck.Count];
        for (int NextPlayerIndex = 0; NextPlayerIndex < PlayersToCheck.Count; NextPlayerIndex++) {
            if (!StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].isPlayerControlled) {
                PlayerInsanityLevelList[NextPlayerIndex] = 0;
                continue;
            }
            PlayerInsanityLevelList[NextPlayerIndex] += 80;
            if (PlayerWithHighestInsanityLevel == (float)NextPlayerIndex && HighestInsanityLevel > 1f) {
                PlayerInsanityLevelList[NextPlayerIndex] += 50;
            }
            if (PlayerWithHighestTurnAmount == NextPlayerIndex) {
                PlayerInsanityLevelList[NextPlayerIndex] += 30;
            }
            if (!StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].hasBeenCriticallyInjured) {
                PlayerInsanityLevelList[NextPlayerIndex] += 10;
            }
            if (!StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].isPlayerAlone) {
                PlayerInsanityLevelList[NextPlayerIndex] += 60;
            }
            if (StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].currentlyHeldObjectServer != null && StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].currentlyHeldObjectServer.scrapValue > 150) {
                PlayerInsanityLevelList[NextPlayerIndex] += 30;
            }
            if (!StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].isInsideFactory) {
                PlayerInsanityLevelList[NextPlayerIndex] += 70;
            }
            if (PlayersTimesHaunted.ContainsKey(StartOfRound.Instance.allPlayerScripts[NextPlayerIndex])) {
                PlayerInsanityLevelList[NextPlayerIndex] -= (50 * PlayersTimesHaunted[StartOfRound.Instance.allPlayerScripts[NextPlayerIndex]]);
            }
            Mathf.Clamp(PlayerInsanityLevelList[NextPlayerIndex], 0, 10000);
        }
        PlayerControllerB ResultingPlayer = PlayersToCheck[RoundManager.Instance.GetRandomWeightedIndex(PlayerInsanityLevelList, EnemyRandom)];

        ChangeOwnershipOfEnemy(ResultingPlayer.actualClientId);
        return ResultingPlayer;
    }

    private void MoveGhostTowardPlayer() {
        Vector3 DirectionVector = (PlayerToAttack.transform.position - transform.position).normalized;
        if (transform.position != PlayerToAttack.transform.position) {
            transform.position += DirectionVector * OoblGhostSpeed * Time.deltaTime;
            CalculateGhostRotation();
        }
    }

    private void CalculateGhostRotation() {
        Vector3 DirectionVector = (PlayerToAttack.transform.position - transform.position).normalized;
        Quaternion CurrentRot = transform.rotation;
        Quaternion TargetRot = Quaternion.LookRotation(DirectionVector, Vector3.up);
        transform.rotation = Quaternion.LookRotation(DirectionVector, Vector3.up); //Quaternion.RotateTowards(CurrentRot, TargetRot, 30 * Time.deltaTime);
    }

    //Called every frame
    private void EvaluateRadioInterference() {
        if (PlayerToAttack.PlayerIsHearingOthersThroughWalkieTalkie()) {
            GhostPickedUpInterference = true;
            return;
        }
    }

    //called when translator used
    public void EvalulateSignalTranslatorUse() {
        WTOBase.LogToConsole("Ghost Recieved Signal Translator use!");
        if (PlayerToAttack == null || ActiveState is not GoTowardPlayer) {
            return;
        }
        if (Vector3.Distance(transform.position, PlayerToAttack.transform.position) > GhostInterferenceRange) {
            return;
        }
        GhostPickedUpInterference = true;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetGhostTargetServerRpc(int TargetClientID) {
        SetGhostTargetClientRpc(TargetClientID);
    }

    [ClientRpc]
    public void SetGhostTargetClientRpc(int TargetClientID) {
        PlayerToAttack = StartOfRound.Instance.allPlayerScripts[TargetClientID];
        WTOBase.LogToConsole($"GHOST #{WTOEnemyID}: HAUNTING {PlayerToAttack.playerUsername}");
        if (PlayersTimesHaunted.ContainsKey(PlayerToAttack)) {
            PlayersTimesHaunted[PlayerToAttack]++;
        } else {
            PlayersTimesHaunted.Add(PlayerToAttack, 1);
        }
    }

    [ServerRpc]
    public void FlickerNearbyLightsServerRpc(bool ShipLights) {
        FlickerNearbyLightsClientRpc(ShipLights);
    }

    [ClientRpc]
    public void FlickerNearbyLightsClientRpc(bool ShipLights) {
        ShouldFlickerLights = true;
        this.ShouldFlickerShipLights = ShipLights;
    }

    //TODO: Impliment
    IEnumerator FlickerShipLights() {
        yield return null;
    }

    IEnumerator FlickerNearbyLights() {
        yield return null;
    }

    IEnumerator ListenForWalkie() {
        yield return new WaitForSeconds(GhostInterferenceTime);
        GhostPickedUpInterference = true;
    }
}
