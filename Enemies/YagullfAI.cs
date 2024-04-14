using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies.EnemyThings;

namespace Welcome_To_Ooblterra.Enemies;
internal class YagullfAI : WTOEnemy {

    //STATES
    private class Perched : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //send the yagullf to a random perch point
            BirdList[enemyIndex].CurrentPerchPoint = PerchPoints[enemyRandom.Next(0, PerchPoints.Length)];
            //this should be an animation of the bird flying over
            BirdList[enemyIndex].transform.position = BirdList[enemyIndex].CurrentPerchPoint.transform.position;
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //Yagullf should move around left and right on the perch point
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class TrackPlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //Yagullf should track target player with his head
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class PrepareSwoop : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //Yagullf should go up into the air and wait until the path to the player is clear
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class SwoopDown : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //Yagullf needs to move in a downward arc between its current location and the player's location
            //this will 100% be the hardest part to get right
            //it also needs to bump into walls and shit
            //maybe I can use the navmesh system in order to fake this...? Make the mesh fall in an arc but otherwise make it move along the floor
            //pretty sure sandworms do a similar thing but idk
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class GrabTarget : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //Yagullf will grab player, and crush him between its talons as it's about 80% of the way to its nest
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class StayInNest : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //set the yagullf to its eating anim
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }
    private class ReturnToPerchPoint : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            //set the yagullf to its eating anim
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };
    }

    //TRANSITIONS
    private class SpottedPlayer : StateTransition {
        public override bool CanTransitionBeTaken() {

        }
        public override BehaviorState NextState() {

        }
    }
    private class TrackTimerFinished : StateTransition {
        public override bool CanTransitionBeTaken() {

        }
        public override BehaviorState NextState() {

        }
    }
    private class SwoopWaitTimerFinished : StateTransition {
        public override bool CanTransitionBeTaken() {

        }
        public override BehaviorState NextState() {

        }
    }
    private class CollidedWithPlayer : StateTransition {
        public override bool CanTransitionBeTaken() {

        }
        public override BehaviorState NextState() {

        }
    }
    private class CollidedWithWall : StateTransition {
        public override bool CanTransitionBeTaken() {

        }
        public override BehaviorState NextState() {

        }
    }
    private class CollidedWithNothing : StateTransition {
        public override bool CanTransitionBeTaken() {

        }
        public override BehaviorState NextState() {
            //random chance it'll either try swooping again or return to a perch point
        }
    }
    private class ReachedNest : StateTransition {
        public override bool CanTransitionBeTaken() {

        }
        public override BehaviorState NextState() {

        }
    }
    private class FinishedEating : StateTransition {
        public override bool CanTransitionBeTaken() {

        }
        public override BehaviorState NextState() {

        }
    }

    public static Dictionary<int, YagullfAI> BirdList = new Dictionary<int, YagullfAI>();
    public static int BirdID;
    public static YagullfPerchPoint[] PerchPoints;
    private YagullfPerchPoint CurrentPerchPoint;
    private YagullfPerchPoint NestLocation;
    private bool IsEatingPlayer;
    private float EatingPlayerSeconds;

    public override void Start() {
        InitialState = new Perched();
        PrintDebugs = true;
        BirdID++;
        WTOEnemyID = BirdID;
        LogMessage($"Adding Yagullf {this} at {BirdID}");
        BirdList.Add(BirdID, this);
        base.Start();
        PerchPoints = FindObjectsOfType<YagullfPerchPoint>();
        int MyNestLoc = enemyRandom.Next(0, PerchPoints.Length);
        NestLocation = PerchPoints[MyNestLoc];
        PerchPoints[MyNestLoc].SetNestVisible();

    }

}

