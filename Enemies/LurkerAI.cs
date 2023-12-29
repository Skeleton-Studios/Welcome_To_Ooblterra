using GameNetcodeStuff;
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

        
        //BEHAVIOR STATES
        private class Roam : BehaviorState {
            
            private bool MovingToPosition;

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerAI lurker = self as LurkerAI;
                self.creatureAnimator.SetBool("Moving", true);
                MovingToPosition = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                self.agent.speed = 5f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerAI lurker = self as LurkerAI;
                if (!MovingToPosition) {
                    MovingToPosition = self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                }
                if(Vector3.Distance(self.transform.position, self.destination) < 2) {
                    MovingToPosition = false;
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
            //Find a spot behind the player and go to it
            Vector3 StalkPos;
            LurkerAI Lurker;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Lurker = self as LurkerAI;
                self.creatureAnimator.SetBool("Moving", true);
                if (self.PlayerIsTargetable(self.targetPlayer)) { 
                    StalkPos = self.targetPlayer.transform.position - (Vector3.Scale(new Vector3(-5, 0, -5), (self.targetPlayer.transform.forward * -1)));
                    self.SetDestinationToPosition(StalkPos);
                    self.agent.speed = 5f;
                }
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new WaitForPlayerEyes(),
                new HoldPosition()
            };
        }
        private class Wait : BehaviorState {
            //Cling to the ceiling and wait to be looked at
            //If the player moves out of range, go back to stalking
            LurkerAI Lurker;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Lurker = self as LurkerAI;
                SwitchClingingToCeilingState(true);
                self.agent.speed = 0;
                Lurker.MoveCooldown = enemyRandom.Next(150, 300);
                Lurker.creatureVoice.Play();
                Lurker.finishPlayerDrag = false;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (!Lurker.clingingToCeiling) {
                    Lurker.clingingToCeiling = true;
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Lurker = self as LurkerAI;
                Lurker.creatureVoice.Stop();
                /*
                if (Lurker.clingingToCeiling) {
                    SwitchClingingToCeilingState(false);
                }
                */
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new WaitForPlayerEyes(),
                new PlayerOutOfRange()
            };
            private void SwitchClingingToCeilingState(bool shouldCling) {
                
                if (Lurker.clingingToCeiling = shouldCling) {
                    return;
                }
                Lurker.clingingToCeiling = shouldCling;
                if (shouldCling) {
                    Lurker.creatureAnimator.SetBool("Flipping", true);
                    return;
                }
                Lurker.creatureAnimator.SetBool("Flipping", false);
                return;
            }
        }
        private class Drag : BehaviorState {

            LurkerAI lurker;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.creatureAnimator.SetBool("Holding", true);
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
                self.agent.speed = 4f;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.targetPlayer.transform.position = self.transform.position;
                if(Vector3.Distance(self.transform.position, self.destination) < 5) {
                    self.creatureAnimator.SetBool("Holding", false);

                    lurker.finishPlayerDrag = true;
                } else {
                    if(self.agent.speed < 15f) {
                        self.agent.speed += Time.deltaTime * 2;
                    }
                    
                }
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new PlayerDroppedOff()
            };
        }
        private class Flee : BehaviorState {
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerAI lurkerAI = self as LurkerAI;
                self.creatureAnimator.SetBool("Moving", true);
                
                self.SetDestinationToPosition(self.ChooseFarthestNodeFromPosition(self.transform.position).position);
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
        private class WaitForPlayerEyes : StateTransition {    
            public override bool CanTransitionBeTaken() {
                LurkerAI lurker = self as LurkerAI;
                if (self.targetPlayer.HasLineOfSightToPosition(lurker.transform.position + lurker.transform.up *2f)) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                LurkerAI lurker = self as LurkerAI;
                WTOBase.LogToConsole("Clinging to ceiling: " + lurker.clingingToCeiling);
                WTOBase.LogToConsole("Distance: " + Vector3.Distance(self.transform.position, self.targetPlayer.transform.position));
                if (lurker.clingingToCeiling && Vector3.Distance(self.transform.position, self.targetPlayer.transform.position) < lurker.GrabDistance) {
                    return new Drag();
                }
                return new Flee();
            }
        }
        private class HoldPosition : StateTransition {
            public override bool CanTransitionBeTaken() {
                LurkerAI lurker = self as LurkerAI;
                if (Vector3.Distance(self.transform.position, self.destination) < 2) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Wait();
            }
        }
        private class PlayerOutOfRange : StateTransition {

            public override bool CanTransitionBeTaken() {
                LurkerAI lurker = self as LurkerAI;
                if (Vector3.Distance(self.transform.position, self.targetPlayer.transform.position) > lurker.GrabDistance && lurker.MoveCooldown > 0) {
                    WTOBase.LogToConsole("Lurker lost player...");
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Stalk();
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
        private class TargetPlayerUnreachable : StateTransition {

            public override bool CanTransitionBeTaken() {
                WTOEnemy selfenemy = self as WTOEnemy;
                if (self.targetPlayer == null || self.PlayerIsTargetable(self.targetPlayer, true, false)) {
                    return false;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }

        bool clingingToCeiling = false;
        bool finishPlayerDrag = false;
        public int GrabDistance = 6;
        public AISearchRoutine roamMap;
        public int MoveCooldown;
        public override string __getTypeName() {
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
        public override void Update() {
            base.Update();
            if (MoveCooldown > 0) {
                MoveCooldown--;
            }
        }
    }
}
