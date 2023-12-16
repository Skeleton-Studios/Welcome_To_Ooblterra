using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using Welcome_To_Ooblterra;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;

namespace Welcome_To_Ooblterra.Enemies {

    public class WandererAI : EnemyAI {
        private class Investigate : BehaviorState {

            private WandererAI Wanderer;
            public AISearchRoutine roamMap;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer = self as WandererAI;
                Wanderer.TotalInvestigationTime = enemyRandom.Next(200, 1500);
                Wanderer.InvestigatingTime = 0;
                self.creatureAnimator.speed = 1f;
                self.agent.speed = 0f;
                self.creatureAnimator.SetBool("Investigating", true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer.InvestigatingTime++;
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Investigating", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new DoneInvestigating(),
                new InDanger()
            };
        }
        private class Roam : BehaviorState {
            WandererAI Wanderer;

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer = self as WandererAI;
                self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                self.agent.speed = 7f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FoundNextPoint(),
                new InDanger()
            };
        }
        private class Flee : BehaviorState {

            private WandererAI Wanderer;

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Wanderer = self as WandererAI;
                self.agent.speed = 2f;
                self.creatureAnimator.speed = 2f;
                self.creatureAnimator.SetBool("Moving", true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.SetDestinationToPosition(self.ChooseFarthestNodeFromPosition(Wanderer.NearestPlayer(Wanderer.RegisteredThreats).transform.position).position);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new NoLongerInDanger()
            };
        }
        private class DoneInvestigating : StateTransition {
            public override bool CanTransitionBeTaken() {
                WandererAI MyWanderer = self as WandererAI;
                return (MyWanderer.InvestigatingTime > MyWanderer.TotalInvestigationTime);
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }
        private class FoundNextPoint : StateTransition {
            public override bool CanTransitionBeTaken() {
                return (Vector3.Distance(self.transform.position, self.destination) < 5);
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }
        private class InDanger : StateTransition {
            public override bool CanTransitionBeTaken() {
                WandererAI MyWanderer = self as WandererAI;
                if (MyWanderer.RegisteredThreats.Count <= 0) {
                    return false;
                }
                return (Vector3.Distance(self.transform.position, MyWanderer.NearestPlayer(MyWanderer.RegisteredThreats).transform.position) < 5);
            }
            public override BehaviorState NextState() {
                return new Flee();
            }
        }
        private class NoLongerInDanger : StateTransition {

            public override bool CanTransitionBeTaken() {
                WandererAI MyWanderer = self as WandererAI;
                return !(Vector3.Distance(self.transform.position, MyWanderer.NearestPlayer(MyWanderer.RegisteredThreats).transform.position) < 5);
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }

        private BehaviorState InitialState = new Investigate();
        private BehaviorState ActiveState = null;

        private RoundManager roundManager;
        private float AITimer;

        private List<PlayerControllerB> RegisteredThreats = new List<PlayerControllerB>();
        private List<PlayerControllerB> PlayersCuriousAbout = new List<PlayerControllerB>();

        private bool MovingToNextPoint = false;

        private int InvestigatingTime = 12;
        //private bool Investigating = false;
        private int TotalInvestigationTime;
        
        //private Ray ray;
        //private RaycastHit rayHit;
        //private AISearchRoutine roamPlanet;
        private System.Random enemyRandom;
        private bool HasReachedDestination = false;
        private bool HasFoundNextSearchPoint = false;
        //private bool stateInterrupted = false;


        protected override string __getTypeName() {
            return "WandererAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }

        public override void Start() {
            base.Start();
            ActiveState = InitialState;
            
            //Mouthdog Start() code
            roundManager = FindObjectOfType<RoundManager>();
            
            useSecondaryAudiosOnAnimatedObjects = true;
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            //roamPlanet = new AISearchRoutine();
            if (!agent.isOnNavMesh) { 
                Physics.Raycast(new Ray(new Vector3(0, 0, 0), Vector3.down), out var hit, LayerMask.GetMask("Terrain"));
                agent.Warp(hit.point);
            }
            WTOBase.LogToConsole("Wanderer on NavMesh: " + (agent.isOnNavMesh).ToString());
            
            //Debug for the animations not fucking working
            creatureAnimator.Rebind();
            AnimatorClipInfo[] MyInfo = creatureAnimator.GetCurrentAnimatorClipInfo(0);
            WTOBase.LogToConsole("Printing clip info...");
            foreach (AnimatorClipInfo info in MyInfo) {
                WTOBase.LogToConsole(info.clip.name);
            }
            ActiveState.OnStateEntered(this, enemyRandom, creatureAnimator);

        }
        public override void Update() {
            base.Update();
            AITimer++;
            //don't run enemy ai if they're dead
            
            if (isEnemyDead || !ventAnimationFinished || MovingToNextPoint) {
                return;
            }

            //play the stun animation if they're stunned 
            //TODO: SetLayerWeight switches between the basic animation layer (0) and the stun animation layer (1). 
            //The wanderer will need a similar setup if we want to be able to stun him, plus a stun animation 
            /*
            if (stunNormalizedTimer > 0f && !isEnemyDead) {
                if (stunnedByPlayer != null && currentBehaviourStateIndex != 2 && base.IsOwner) {
                    creatureAnimator.SetLayerWeight(1, 1f);
                }
            } else {
                creatureAnimator.SetLayerWeight(1, 0f);
            }
            */

            //Custom Wanderer Code
            bool RunUpdate = true;
            //don't run enemy ai if they're dead
            foreach (StateTransition transition in ActiveState.transitions) {
                transition.self = this;
                if (transition.CanTransitionBeTaken()) {
                    RunUpdate = false;
                    ActiveState.OnStateExit(this, enemyRandom, creatureAnimator);
                    ActiveState = transition.NextState();
                    ActiveState.OnStateEntered(this, enemyRandom, creatureAnimator);
                    break;
                }
            }
            if (RunUpdate) {
                ActiveState.UpdateBehavior(this, enemyRandom, creatureAnimator);
            }
        }
        private PlayerControllerB NearestPlayer(List<PlayerControllerB> List) {
            float distance = 100000;
            PlayerControllerB nearestPlayer = null;
            if (!List.Any()){
                return nearestPlayer;
            }
            foreach (PlayerControllerB threat in List) {
                float enemydistance = Vector3.Distance(threat.transform.position, transform.position);
                if (enemydistance < distance) {
                    distance = enemydistance;
                    nearestPlayer = threat;
                }
            }
            return nearestPlayer;
        }
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            enemyHP -= force;
            if (base.IsOwner) {
                if (enemyHP <= 0) {
                    ItemPatch.SpawnItem(transform.position + new Vector3(0, 5, 0));
                    Vector3 AdultSpawnPos = playerWhoHit.transform.position - (Vector3.Scale(new Vector3(-5, 0, -5), (playerWhoHit.transform.forward * -1)) );
                    GameObject obj = UnityEngine.Object.Instantiate(MonsterPatch.AdultWandererContainer[0].enemyType.enemyPrefab, AdultSpawnPos, Quaternion.Euler(Vector3.zero));
                    obj.gameObject.GetComponentInChildren<NetworkObject>().Spawn(destroyWithScene: true);
                    obj.gameObject.GetComponentInChildren<AdultWandererAI>().SetMyTarget(playerWhoHit);
                    KillEnemyOnOwnerClient();
                    return;
                }
            }
            if(!RegisteredThreats.Contains(playerWhoHit)) RegisteredThreats.Add(playerWhoHit);
        }
    }
}

