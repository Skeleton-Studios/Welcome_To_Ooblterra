using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;

namespace Welcome_To_Ooblterra.Enemies {

    public class WandererAI : WTOEnemy {
        //STATES
        private class Investigate : BehaviorState {

            private WandererAI Wanderer;
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
        
        //TRANSITIONS
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

        private List<PlayerControllerB> RegisteredThreats = new List<PlayerControllerB>();
        private int InvestigatingTime = 12;
        private int TotalInvestigationTime;
        private float previous_speed;
        public override void Start() {
            InitialState = new Investigate();
            base.Start();
            if (!agent.isOnNavMesh) {
                Physics.Raycast(new Ray(new Vector3(0, 0, 0), Vector3.down), out var hit, LayerMask.GetMask("Terrain"));
                agent.Warp(hit.point);
            }
        }

        /*public override void Update() {
            if(stunNormalizedTimer >= 0f) {
                previous_speed = agent.speed;
                creatureAnimator.SetBool("Stunned", value: true);
                agent.speed = 0f;
            } else {
                agent.speed = previous_speed;
                creatureAnimator.SetBool("Stunned", value: false);
                base.Update();
            }
        }*/
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
            creatureAnimator.SetTrigger("Hit");
            enemyHP -= force;
            if (base.IsOwner) {
                if (enemyHP <= 0) {
                    creatureAnimator.SetTrigger("Death");
                    ItemPatch.SpawnItem(transform.position + new Vector3(0, 5, 0));
                    Vector3 AdultSpawnPos = playerWhoHit.transform.position - Vector3.Scale(new Vector3(-5, 0, -5), playerWhoHit.transform.forward * -1);
                    Quaternion AdultSpawnRot = new Quaternion(0, Quaternion.LookRotation(playerWhoHit.transform.position - AdultSpawnPos).y, 0, 1);
                    GameObject obj = UnityEngine.Object.Instantiate(MonsterPatch.AdultWandererContainer[0].enemyType.enemyPrefab, AdultSpawnPos, AdultSpawnRot);
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

