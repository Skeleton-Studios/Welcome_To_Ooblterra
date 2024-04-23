using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using System.Runtime.CompilerServices;

namespace Welcome_To_Ooblterra.Enemies;
public class BabyLurkerAI : WTOEnemy {

    [Header("Defaults")]
    public bool ThrowProjectile;
    public int ProjectilesThrown;
    public GameObject projectileTemplate;
    public float launchVelocity = 100f;
    public Transform LaunchTransform;


    //STATES
    private class Spawn : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new TargetPlayerInLOS(),
            new TargetPlayerNotInLOS()
        };

    }
    private class ChasePlayer : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BabyLurkerList[enemyIndex].SetDestinationToPosition(BabyLurkerList[enemyIndex].targetPlayer.transform.position);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new AnyPlayerInLOS()
        };

    }
    private class ThrowSelfAtPlayer : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BabyLurkerList[enemyIndex].LaunchProjectile(BabyLurkerList[enemyIndex].LaunchTransform);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

        };

    }

    //TRANSITIONS
    private class TargetPlayerInLOS : StateTransition {
        public override bool CanTransitionBeTaken() {
            return BabyLurkerList[enemyIndex].IsTargetPlayerWithinLOS(range: 10, width: 90);
        }
        public override BehaviorState NextState() {
            return new ThrowSelfAtPlayer();
        }
    }
    private class AnyPlayerInLOS : StateTransition {
        public override bool CanTransitionBeTaken() {
            return false;
            //return BabyLurkerList[enemyIndex].IsAnyPlayerWithinLOS(range: 10, width: 90);
        }
        public override BehaviorState NextState() {
            return new ThrowSelfAtPlayer();
        }
    }
    private class TargetPlayerNotInLOS : StateTransition {
        public override bool CanTransitionBeTaken() {
            return !BabyLurkerList[enemyIndex].IsTargetPlayerWithinLOS(range: 10, width: 90); 
        }
        public override BehaviorState NextState() {
            return new ChasePlayer();
        }
    }

    private GameObject LiveProjectile;

    public static Dictionary<int, BabyLurkerAI> BabyLurkerList = new();
    private static int BabyLurkerID;

    public override void Start() {
        MyValidState = PlayerState.Inside;
        InitialState = new Spawn();
        PrintDebugs = true;
        BabyLurkerID++;
        WTOEnemyID = BabyLurkerID;
        LogMessage($"Adding BabyLurker {this} #{BabyLurkerID}");
        BabyLurkerList.Add(BabyLurkerID, this);
        base.Start();
    }

    public override void Update() {
        if (ThrowProjectile) {
            LaunchProjectile(LaunchTransform);
        } 
        //base.Update();
    }
    public void LaunchProjectile(Transform LaunchTransform) {
        if(ProjectilesThrown > 0) {
            return;
        }
        ProjectilesThrown++;
        LiveProjectile = Instantiate(projectileTemplate, LaunchTransform.position, LaunchTransform.rotation);
        LiveProjectile.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, launchVelocity, 0));
        GameObject.Destroy(this);
    }
}
