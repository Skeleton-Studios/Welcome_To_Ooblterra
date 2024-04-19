using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;

namespace Welcome_To_Ooblterra.Enemies;
public class EnforcerAI : WTOEnemy {

    [Header("Balance Constants")]
    public float SecondsUntilBeginStalking = 5f;
    public float StalkSpeed = 6f;
    public float ChaseSpeed = 6.5f;

    [Header("Defaults")]
    public Material[] EnforcerMatList;

    //STATES
    private class WaitForPlayerToPass : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };

    }
    private class StalkPlayer : BehaviorState {
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
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };

    }
    private class AttackPlayer : BehaviorState {
        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };

    }
    private class SearchForPlayer : BehaviorState {
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
    private class PlayerPassedBy : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].SecondsUntilGhostWillAttack <= 0f;
        }
        public override BehaviorState NextState() {
            return new ChooseTarget();
        }
    }
    private class SeenByPlayer : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].SecondsUntilGhostWillAttack <= 0f;
        }
        public override BehaviorState NextState() {
            return new ChooseTarget();
        }
    }
    private class PlayerInRange : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].SecondsUntilGhostWillAttack <= 0f;
        }
        public override BehaviorState NextState() {
            return new ChooseTarget();
        }
    }
    private class PlayerKilled : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].SecondsUntilGhostWillAttack <= 0f;
        }
        public override BehaviorState NextState() {
            return new ChooseTarget();
        }
    }
    private class PlayerNotFoundDuringSearch : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].SecondsUntilGhostWillAttack <= 0f;
        }
        public override BehaviorState NextState() {
            return new ChooseTarget();
        }
    }
    private class LostPlayerDuringChase : StateTransition {
        public override bool CanTransitionBeTaken() {
            return GhostList[enemyIndex].SecondsUntilGhostWillAttack <= 0f;
        }
        public override BehaviorState NextState() {
            return new ChooseTarget();
        }
    }
    private class PlayerFoundDuringSearch : StateTransition {
        public override bool CanTransitionBeTaken() {

        }
        public override BehaviorState NextState() {

        }
    }

    public static Dictionary<int, EnforcerAI> EnforcerList = new();
    private static int EnforcerID;

    public override void Start() {

    }
    public override void Update() {

    }

    IEnumerator StartActiveCamo() {
        yield return null;
    }
    IEnumerator StopActiveCamo() {
        yield return null;
    }
}
