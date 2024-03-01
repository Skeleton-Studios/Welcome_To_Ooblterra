using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using Welcome_To_Ooblterra.Properties;
using static UnityEngine.Rendering.DebugUI;

namespace Welcome_To_Ooblterra.Enemies;
internal class OoblGhostAI : WTOEnemy {
    //STATES
    private class WaitForNextAttack : BehaviorState {
        public WaitForNextAttack() {
            
            RandomRange = new Vector2(/*80, 125*/20, 45);
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
            RandomRange = new Vector2(-800, 800);
        }
        Vector3 DirectionVector;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GhostList[enemyIndex].PlayerToAttack.statusEffectAudio.PlayOneShot(GhostList[enemyIndex].StartupSound);
            GhostList[enemyIndex].creatureVoice.Play();   
            GhostList[enemyIndex].transform.position = new Vector3(MyRandomInt, GhostList[enemyIndex].YAxisLockedTo, MyRandomInt);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            DirectionVector = (GhostList[enemyIndex].PlayerToAttack.transform.position - GhostList[enemyIndex].transform.position).normalized;
            if (Vector3.Distance(GhostList[enemyIndex].transform.position, GhostList[enemyIndex].PlayerToAttack.transform.position) > 50) {
                GhostList[enemyIndex].YAxisLockedTo = GhostList[enemyIndex].PlayerToAttack.transform.position.y;
                GhostList[enemyIndex].transform.position = new Vector3(GhostList[enemyIndex].transform.position.x, GhostList[enemyIndex].YAxisLockedTo, GhostList[enemyIndex].transform.position.z);
            }
            //WTOBase.LogToConsole($"DIRECTION: {DirectionVector}");
            DirectionVector.y = 0;
            if (GhostList[enemyIndex].transform.position != GhostList[enemyIndex].PlayerToAttack.transform.position) {
                GhostList[enemyIndex].transform.position += DirectionVector * GhostList[enemyIndex].OoblGhostSpeed * Time.deltaTime;
                GhostList[enemyIndex].transform.rotation = Quaternion.LookRotation(DirectionVector, Vector3.up);
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GhostList[enemyIndex].PlayerToAttack = null;
            GhostList[enemyIndex].SelfPosition = new Vector2(-1, -1);
            GhostList[enemyIndex].TargetPlayerPosition = new Vector2(-1, -1);
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new KilledPlayer(),
            new PlayerIsOutOfAttackRange()
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
    private class PlayerIsOutOfAttackRange : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].SecondsSincePlayerBrokeYAxis > 5f;
        }
        public override BehaviorState NextState() {
            return new WaitForNextAttack();
        }
    }
    public float OoblGhostSpeed = 25f;
    public static Dictionary<int, OoblGhostAI> GhostList = new();
    private static int GhostID;
    private PlayerControllerB PlayerToAttack;
    private Vector2 TargetPlayerPosition;
    private Vector2 SelfPosition;
    private float SecondsUntilGhostWillAttack;
    private float TrackPlayerMovementSeconds = 3f;
    private float YAxisLockedTo;
    private float SecondsSincePlayerBrokeYAxis;
    public AudioClip StartupSound;


    public override void Start() {
        MyValidState = PlayerState.Inside;
        InitialState = new WaitForNextAttack();
        PrintDebugs = true;
        GhostID++;
        WTOEnemyID = GhostID;
        //transform.position = new Vector3(0, -300, 0);
        LogMessage($"Adding Oobl Ghost {this} #{GhostID}");
        GhostList.Add(GhostID, this);
        base.Start();
    }
    public override void Update() {
        //WTOBase.LogToConsole($"{SecondsUntilGhostWillAttack}");
        MoveTimerValue(ref SecondsUntilGhostWillAttack);
        SelfPosition = new Vector2(transform.position.x, transform.position.z);
        if(PlayerToAttack != null) {
            TargetPlayerPosition = new Vector2(PlayerToAttack.transform.position.x, PlayerToAttack.transform.position.z);
        }
        if (ActiveState is GoTowardPlayer && GhostInDissipateRange()) {
            MoveTimerValue(ref SecondsSincePlayerBrokeYAxis, true);
        } else {
            SecondsSincePlayerBrokeYAxis = 0f;
        }
        //WTOBase.LogToConsole($"{SecondsSincePlayerBrokeYAxis}");
        base.Update();
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
        }
        PlayerControllerB ResultingPlayer = PlayersToCheck[RoundManager.Instance.GetRandomWeightedIndex(PlayerInsanityLevelList, EnemyRandom)];

        ChangeOwnershipOfEnemy(ResultingPlayer.actualClientId);
        return ResultingPlayer;
    }

    [ServerRpc]
    public void SetGhostTargetServerRpc(int TargetClientID) {
        SetGhostTargetClientRpc(TargetClientID);
    }

    [ClientRpc]
    public void SetGhostTargetClientRpc(int TargetClientID) {
        PlayerToAttack = StartOfRound.Instance.allPlayerScripts[TargetClientID];
        YAxisLockedTo = PlayerToAttack.transform.position.y;
        WTOBase.LogToConsole($"GHOST #{WTOEnemyID}: HAUNTING {PlayerToAttack.playerUsername}");
    }


    private bool GhostInDissipateRange() {
        return (Mathf.Abs(PlayerToAttack.transform.position.x - transform.position.x) <= 5 && Mathf.Abs(PlayerToAttack.transform.position.z - transform.position.z) <= 5 && PlayerToAttack.transform.position.y != YAxisLockedTo);
    }
}
