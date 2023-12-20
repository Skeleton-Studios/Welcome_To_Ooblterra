﻿using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;
using static Welcome_To_Ooblterra.Enemies.BabyLurkerAI;

namespace Welcome_To_Ooblterra.Enemies {
    public class LurkerAI : WTOEnemy {

        public const int GrabDistance = 3;
        //BEHAVIOR STATES
        private class Roam : BehaviorState {
            
            public int TotalInvestigationTime;
            

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerAI lurker = self as LurkerAI;
                self.StartSearch(self.transform.position, lurker.roamMap);
                self.creatureAnimator.SetBool("Moving", true);
                self.agent.speed = 5f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerAI lurker = self as LurkerAI;
                if (!lurker.roamMap.inProgress) {
                    self.StartSearch(self.transform.position, lurker.roamMap);
                }

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerAI lurker = self as LurkerAI;
                self.StopSearch(lurker.roamMap);
                self.creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FindPlayer()
            };
        }
        private class Stalk : BehaviorState {
            Vector3 StalkPos;
            LurkerAI Lurker;
            private int MoveCooldown = 0;
            private bool MovingToNextPos;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Lurker = self as LurkerAI;
                self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position).position);
                self.agent.speed = 5f;
                MovingToNextPos = true;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                StalkPos = self.targetPlayer.transform.position - (Vector3.Scale(new Vector3(-1, 0, -1), (self.targetPlayer.transform.forward * -1)));
                if (Vector3.Distance(self.transform.position, StalkPos) < GrabDistance) {
                    SwitchClingingToCeilingState(true);
                    self.agent.speed = 0;
                    MovingToNextPos = false;
                    if(MoveCooldown <= 0) {
                        MoveCooldown = 200;
                    }
                    return;
                }
                if(MoveCooldown > 0) {
                    MoveCooldown--;
                    return;
                }
                SwitchClingingToCeilingState(false);
                if (!MovingToNextPos) {
                    self.SetDestinationToPosition(StalkPos);
                    self.agent.speed = 5f;
                    MovingToNextPos = true;
                }

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (Lurker.clingingToCeiling) { 
                    SwitchClingingToCeilingState(false);
                }
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new PlayerIsntMoving()
            };
            private void SwitchClingingToCeilingState(bool shouldCling) {
                if(Lurker.clingingToCeiling = shouldCling) {
                    return;
                }
                Lurker.clingingToCeiling = shouldCling;
                if (shouldCling) {
                    //start clinging
                    return;
                }
                //stop clinging
                return;
            }
        }
        private class Drag : BehaviorState {

            LurkerAI lurker;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Grabbing", true);
                List<BabyLurkerEgg> LurkerEggs = new List<BabyLurkerEgg>();
                if(LurkerEggs.Count <= 0) {
                    self.SetDestinationToPosition(self.ChooseFarthestNodeFromPosition(self.transform.position, true).position);
                } else { 
                    foreach(BabyLurkerEgg egg in GameObject.FindObjectsOfType<BabyLurkerEgg>()) {
                        if (!egg.BabySpawned) {
                            LurkerEggs.Add(egg);
                        }
                    }
                    for (int i = 0; i < LurkerEggs.Count; i++) { 
                        if (self.SetDestinationToPosition(LurkerEggs[i].transform.position, true)) {
                            break;
                        }
                    }
                }
                lurker = self as LurkerAI;
                self.agent.speed = 2f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.targetPlayer.transform.position = self.transform.position;
                if(Vector3.Distance(self.transform.position, self.destination) < 5) {
                    lurker.finishPlayerDrag = true;
                } else {
                    if(self.agent.speed < 15f) {
                        self.agent.speed += Time.deltaTime;
                    }
                    
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Grabbing", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new PlayerDroppedOff()
            };
        }
        private class Flee : BehaviorState {


            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", true);
                self.SetDestinationToPosition(self.ChooseFarthestNodeFromPosition(self.targetPlayer.transform.position).position, true);
                self.agent.speed = 15f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new DoneFleeing()
            };
        }

        //STATE TRANSITIONS
        private class FindPlayer : StateTransition {

            public override bool CanTransitionBeTaken() {
                
                if (self.GetAllPlayersInLineOfSight(90f) == null) {
                    return false;
                }
                foreach (var possiblePlayer in self.GetAllPlayersInLineOfSight(90f)) { 
                    if(!possiblePlayer.HasLineOfSightToPosition(self.transform.position /*+ Vector3.up * 1.6f*/)){
                        self.targetPlayer = possiblePlayer;
                        return true;
                    }
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Stalk();
            }
        }
        private class PlayerIsntMoving : StateTransition {
            
            public override bool CanTransitionBeTaken() {
                LurkerAI lurker = self as LurkerAI;
                if (lurker.clingingToCeiling && self.targetPlayer.HasLineOfSightToPosition(lurker.transform.position + lurker.transform.up *2f)) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                if(Vector3.Distance(self.transform.position, self.targetPlayer.transform.position) < GrabDistance) {
                    return new Drag();
                }
                return new Flee();
            }
        }
        private class PlayerDroppedOff : StateTransition {

            public override bool CanTransitionBeTaken() {
                LurkerAI Lurker = self as LurkerAI;
                return Lurker.finishPlayerDrag;
            }
            public override BehaviorState NextState() {
                return new Flee();
            }
        }
        private class DoneFleeing : StateTransition {

            public override bool CanTransitionBeTaken() {
                if(Vector3.Distance(self.transform.position, self.destination) < 2) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }


        bool clingingToCeiling = false;
        bool finishPlayerDrag = false;
        public AISearchRoutine roamMap;
        protected override string __getTypeName() {
            return "LurkerAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }
        public override void Start() {
            PrintDebugs = true;
            InitialState = new Roam();
            base.Start();
        }
    }
}