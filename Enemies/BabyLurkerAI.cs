using System.Collections.Generic;
using UnityEngine;
using GameNetcodeStuff;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using System.Runtime.CompilerServices;
using Welcome_To_Ooblterra.Enemies.EnemyThings;

namespace Welcome_To_Ooblterra.Enemies;
public class BabyLurkerAI : WTOEnemy {

    [Header("Defaults")]
    public bool ThrowProjectile;
    public int ProjectilesThrown;
    public GameObject projectileTemplate;
    private float launchVelocity = 700f;
    public Transform LaunchTransform;
    public static int AttackRange = 5;
    private static float SecondsUntilCanJump = 3f;

    //STATES
    private class Spawn : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BabyLurkerList[enemyIndex].targetPlayer = BabyLurkerList[enemyIndex].FindNearestPlayer();
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            if (BabyLurkerList[enemyIndex].targetPlayer == null) {
                BabyLurkerList[enemyIndex].LogMessage($"Attempting to find nearest player...");
                BabyLurkerList[enemyIndex].targetPlayer = BabyLurkerList[enemyIndex].FindNearestPlayer();
            }
        }
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new FoundNearestPlayer()
        };

    }
    private class ChasePlayer : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BabyLurkerList[enemyIndex].SetDestinationToPosition(BabyLurkerList[enemyIndex].targetPlayer.transform.position);
        }
        public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BabyLurkerList[enemyIndex].SetDestinationToPosition(BabyLurkerList[enemyIndex].targetPlayer.transform.position);
        } 
        public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
             
        }
        public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
            new TargetPlayerInLOS()
        };

    }
    private class ThrowSelfAtPlayer : BehaviorState {

        public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
            BabyLurkerList[enemyIndex].transform.LookAt(BabyLurkerList[enemyIndex].targetPlayer.transform);
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
            return BabyLurkerList[enemyIndex].IsTargetPlayerWithinLOS(range: AttackRange, width: 90) && SecondsUntilCanJump <= 0f;
        }
        public override BehaviorState NextState() {
            return new ThrowSelfAtPlayer();
        }
    }
    private class FoundNearestPlayer : StateTransition {
        public override bool CanTransitionBeTaken() {
            return BabyLurkerList[enemyIndex].targetPlayer != null; 
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
        PrintDebugs = false;
        BabyLurkerID++;
        WTOEnemyID = BabyLurkerID;
        agent.speed = 7f;
        //SetTargetServerRpc((int)StartOfRound.Instance.allPlayerScripts[0].playerClientId);
        LogMessage($"Adding BabyLurker {this} #{BabyLurkerID}");
        BabyLurkerList.Add(BabyLurkerID, this);
        base.Start();
        LaunchTransform.rotation = Quaternion.Euler(new Vector3(45, 0, enemyRandom.Next(-10, 10)));
    }

    public override void Update() {
        base.Update();
        SecondsUntilCanJump -= Time.deltaTime;
    }

    public void LaunchProjectile(Transform LaunchTransform) {
        if(ProjectilesThrown > 0) {
            return;
        }
        ProjectilesThrown++;
        LiveProjectile = Instantiate(projectileTemplate, LaunchTransform.position, LaunchTransform.rotation);
        LiveProjectile.GetComponent<Rigidbody>().AddRelativeForce(new Vector3(0, launchVelocity, 0));
        KillEnemyOnOwnerClient(true);
    }
}
