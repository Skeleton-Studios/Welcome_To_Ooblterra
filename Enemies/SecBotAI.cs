using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;

namespace Welcome_To_Ooblterra.Enemies;

public class SecBotAI : WTOEnemy {

    //STATES
    private class Charge : BehaviorState {
        public Charge() {

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
            return;
        }
        public override BehaviorState NextState() {
            return new;
        }
    }
    private class FoundNothingDuringRandomExplore : StateTransition {
        public override bool CanTransitionBeTaken() {
            return;
        }
        public override BehaviorState NextState() {
            return new;
        }
    }
    private class ItemHasBeenStolen : StateTransition {
        public override bool CanTransitionBeTaken() {
            return;
        }
        public override BehaviorState NextState() {
            return new;
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

    public override void Start() {
        MyValidState = PlayerState.Inside;
        InitialState = new Charge();
        PrintDebugs = true;
        BotID++;
        WTOEnemyID = BotID;
        LogMessage($"Adding Sec Bot {this} #{BotID}");
        BotList.Add(BotID, this);
        base.Start();
    }
    public override void Update() {
        base.Update();
    }
}
