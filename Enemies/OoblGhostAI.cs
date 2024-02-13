using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

namespace Welcome_To_Ooblterra.Enemies;
internal class OoblGhostAI : WTOEnemy {

    //STATES
    private class WaitForNextAttack : BehaviorState {
        public WaitForNextAttack() {
            RandomRange = new Vector2(27, 66);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GhostList[enemyIndex].SecondsUntilGhostWillAttack = MyRandomInt;
            GhostList[enemyIndex].creatureVoice.Stop();
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

        private float TargetPlayerYAxisValue;
        List<PlayerControllerB> AllPlayersArray;
        private float SecondsSincePlayerWasOffPosition;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //get the player with the highest insanity
            AllPlayersArray = StartOfRound.Instance.allPlayerScripts.ToList();
            GhostList[enemyIndex].PlayerToAttack = GhostList[enemyIndex].FindMostHauntedPlayer(enemyRandom, AllPlayersArray);
            TargetPlayerYAxisValue = GhostList[enemyIndex].PlayerToAttack.transform.position.y;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //Check and make sure the chosen player's height stays the same for about 3 seconds, with 1 second of acceptable variation time
            if (TargetPlayerYAxisValue == GhostList[enemyIndex].PlayerToAttack.transform.position.y) {
                return;
            }
            if(TargetPlayerYAxisValue != GhostList[enemyIndex].PlayerToAttack.transform.position.y && SecondsSincePlayerWasOffPosition < 1f) {
                SecondsSincePlayerWasOffPosition += Time.deltaTime;
                return;
            } else {
                GhostList[enemyIndex].TrackPlayerMovementSeconds = 3f;
                AllPlayersArray.Remove(GhostList[enemyIndex].PlayerToAttack);
                GhostList[enemyIndex].PlayerToAttack = GhostList[enemyIndex].FindMostHauntedPlayer(enemyRandom, AllPlayersArray);
                TargetPlayerYAxisValue = GhostList[enemyIndex].PlayerToAttack.transform.position.y;
                SecondsSincePlayerWasOffPosition = 0f;
                return;
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GhostList[enemyIndex].YAxisLockedTo = TargetPlayerYAxisValue;
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new TargetChosen()
        };
    }
    private class GoTowardPlayer : BehaviorState {
        public GoTowardPlayer() {
            RandomRange = new Vector2(-3000, 3000);
        }
        Vector2 DirectionVector;
        Vector3 TargetGhostPos;
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            GhostList[enemyIndex].creatureVoice.Play();
            GhostList[enemyIndex].transform.position = new Vector3(MyRandomInt, GhostList[enemyIndex].YAxisLockedTo, MyRandomInt);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            DirectionVector = (GhostList[enemyIndex].TargetPlayerPosition - GhostList[enemyIndex].SelfPosition).normalized;
            if (GhostList[enemyIndex].SelfPosition != GhostList[enemyIndex].TargetPlayerPosition) {
                GhostList[enemyIndex].transform.position += new Vector3(GhostList[enemyIndex].SelfPosition.x += DirectionVector.x * 7 * Time.deltaTime, GhostList[enemyIndex].YAxisLockedTo, GhostList[enemyIndex].SelfPosition.y += DirectionVector.y * 7 * Time.deltaTime);
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
            return GhostList[enemyIndex].TrackPlayerMovementSeconds <= 0f;
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
            return GhostList[enemyIndex].SelfPosition != GhostList[enemyIndex].TargetPlayerPosition && GhostList[enemyIndex].PlayerToAttack.transform.position.y != GhostList[enemyIndex].YAxisLockedTo;
        }
        public override BehaviorState NextState() {
            return new WaitForNextAttack();
        }
    }


    public static Dictionary<int, OoblGhostAI> GhostList = new();
    private static int GhostID;

    private PlayerControllerB PlayerToAttack;
    private Vector2 TargetPlayerPosition;
    private Vector2 SelfPosition;
    private float SecondsUntilGhostWillAttack;
    private float TrackPlayerMovementSeconds = 3f;
    private float YAxisLockedTo;

    public override void Start() {
        MyValidState = PlayerState.Outside;
        InitialState = new WaitForNextAttack();
        PrintDebugs = false;
        GhostID++;
        WTOEnemyID = GhostID;
        transform.position = new Vector3(0, -300, 0);

        LogMessage($"Adding Oobl Ghost {this} #{GhostID}");
        GhostList.Add(GhostID, this);
        base.Start();
    }

    public override void Update() {
        LowerTimerValue(ref SecondsUntilGhostWillAttack);
        base.Update();
        SelfPosition = new Vector2(transform.position.x, transform.position.z);
        if (PlayerToAttack != null) {
            TargetPlayerPosition = new Vector2(PlayerToAttack.transform.position.x, PlayerToAttack.transform.position.z);
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
        int[] PlayerInsanityLevelArray = new int[PlayersToCheck.Count];
        for (int NextPlayerIndex = 0; NextPlayerIndex < PlayersToCheck.Count; NextPlayerIndex++) {
            if (!StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].isPlayerControlled) {
                PlayerInsanityLevelArray[NextPlayerIndex] = 0;
                continue;
            }
            PlayerInsanityLevelArray[NextPlayerIndex] += 80;
            if (PlayerWithHighestInsanityLevel == (float)NextPlayerIndex && HighestInsanityLevel > 1f) {
                PlayerInsanityLevelArray[NextPlayerIndex] += 50;
            }
            if (PlayerWithHighestTurnAmount == NextPlayerIndex) {
                PlayerInsanityLevelArray[NextPlayerIndex] += 30;
            }
            if (!StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].hasBeenCriticallyInjured) {
                PlayerInsanityLevelArray[NextPlayerIndex] += 10;
            }
            if (!StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].isPlayerAlone) {
                PlayerInsanityLevelArray[NextPlayerIndex] += 60;
            }
            if (StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].currentlyHeldObjectServer != null && StartOfRound.Instance.allPlayerScripts[NextPlayerIndex].currentlyHeldObjectServer.scrapValue > 150) {
                PlayerInsanityLevelArray[NextPlayerIndex] += 30;
            }
        }
        PlayerControllerB ResultingPlayer = PlayersToCheck[RoundManager.Instance.GetRandomWeightedIndex(PlayerInsanityLevelArray, EnemyRandom)];
        if (ResultingPlayer.isPlayerDead) {
            for (int k = 0; k < StartOfRound.Instance.allPlayerScripts.Length; k++) {
                if (!StartOfRound.Instance.allPlayerScripts[k].isPlayerDead) {
                    ResultingPlayer = StartOfRound.Instance.allPlayerScripts[k];
                    break;
                }
            }
        }
        return ResultingPlayer;
    }
}
