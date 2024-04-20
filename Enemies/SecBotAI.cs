using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Things;
using Welcome_To_Ooblterra.Properties;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;
using System.Collections;

namespace Welcome_To_Ooblterra.Enemies;

public class SecBotAI : WTOEnemy {

    //STATES
    private class ChargeUp : BehaviorState {
        public ChargeUp() {
            RandomRange = new Vector2(0, 100);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BotList[enemyIndex].transform.position = BotList[enemyIndex].NextDoorway.SecBotLocationBehindDoor.position;
            BotList[enemyIndex].transform.rotation = BotList[enemyIndex].NextDoorway.SecBotLocationBehindDoor.rotation;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new ShouldDoRandomExplore(),
            new ItemHasBeenStolen()
        };
    }
    private class ExploreNearby : BehaviorState {
        public ExploreNearby() {

            RandomRange = new Vector2(0, 100);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new FoundNothingDuringRandomExplore()
        };
    }
    private class ChasePlayer : BehaviorState {
        public ChasePlayer() {

            RandomRange = new Vector2(0, 100);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class EnterVentSystem : BehaviorState {
        public EnterVentSystem() {

            RandomRange = new Vector2(0, 100);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class WaitInNextVent : BehaviorState {
        public WaitInNextVent() {

            RandomRange = new Vector2(0, 100);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class LeaveVent : BehaviorState {
        public LeaveVent() {

            RandomRange = new Vector2(0, 100);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class ShutDown : BehaviorState {
        public ShutDown() {

            RandomRange = new Vector2(0, 100);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class KillTarget : BehaviorState {
        public KillTarget() {

            RandomRange = new Vector2(0, 100);
        }
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }

    //TRANSITIONS
    private class ShouldDoRandomExplore : StateTransition {
        public override bool CanTransitionBeTaken() {
            return BotList[enemyIndex].enemyRandom.Next(0, 1000) < 1;
        }
        public override BehaviorState NextState() {
            return new ExploreNearby();
        }
    }
    private class ItemHasBeenStolen : StateTransition {
        public override bool CanTransitionBeTaken() {
            return BotList[enemyIndex].FavoriteItem.playerHeldBy != null;
        }
        public override BehaviorState NextState() {
            BotList[enemyIndex].targetPlayer = BotList[enemyIndex].FavoriteItem.playerHeldBy;
            return new ChasePlayer();
        }
    }
    private class FoundNothingDuringRandomExplore : StateTransition {
        public override bool CanTransitionBeTaken() {
            return BotList[enemyIndex].ExploreCoroutineFinished;
        }
        public override BehaviorState NextState() {
            return new EnterVentSystem();
        }
    }

    private class CheckForgivenessAfterItemDropped : StateTransition {
        public override bool CanTransitionBeTaken() {
            return;
        }
        public override BehaviorState NextState() {
            return new;
        }
    }
    private class DecideIfRecharging : StateTransition {
        public override bool CanTransitionBeTaken() {
            return;
        }
        public override BehaviorState NextState() {
            return new;
        }
    }
    private class OutOfBattery : StateTransition {
        public override bool CanTransitionBeTaken() {
            return;
        }
        public override BehaviorState NextState() {
            return new;
        }
    }
    private class CaughtEnemy : StateTransition {
        public override bool CanTransitionBeTaken() {
            return;
        }
        public override BehaviorState NextState() {
            return new;
        }
    }
    private class SeesPlayerWhoDidNotAnger : StateTransition {
        public override bool CanTransitionBeTaken() {
            return;
        }
        public override BehaviorState NextState() {
            return new;
        }
    }
    private class SeesPlayerWhoAngered : StateTransition {
        public override bool CanTransitionBeTaken() {
            return;
        }
        public override BehaviorState NextState() {
            return new;
        }
    }

    public static Dictionary<int, SecBotAI> BotList = new();
    private static int BotID;
    private static float SecondsUntilRandomExploreChance;
    private bool NextVentIsPlayerPrediction;
    private GrabbableObject FavoriteItem;
    private int BatteryAmount = 100;
    private System.Random BotRandom;
    private int MyWillingnessToForgive;
    private int MyWillingnessToGiveUp;
    private bool IsInsideVent;
    private SecBotDoor NextDoorway;
    private bool ExploreCoroutineFinished;

    public override void Start() {
        MyValidState = PlayerState.Inside;
        InitialState = new ChargeUp();
        PrintDebugs = true;
        BotID++;
        WTOEnemyID = BotID;
        LogMessage($"Adding Sec Bot {this} #{BotID}");
        BotList.Add(BotID, this);
        BotRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        SecBotDoor[] DoorList = GameObject.FindObjectsOfType<SecBotDoor>();
        NextDoorway = DoorList[BotRandom.Next(0, DoorList.Length)];
        MyWillingnessToForgive = BotRandom.Next(0, 100);
        MyWillingnessToGiveUp = BotRandom.Next(0, 100);
        base.Start();
    }
    public override void Update() {
        base.Update();
    }

    IEnumerator SearchNearby() {
        yield return null;
    }
}
