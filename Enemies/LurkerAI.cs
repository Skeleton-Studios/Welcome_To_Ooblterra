using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Things;
using static Welcome_To_Ooblterra.Enemies.BabyLurkerAI;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;

namespace Welcome_To_Ooblterra.Enemies {
    public class LurkerAI : EnemyAI {
        private class Roam : BehaviorState {
            
            public int TotalInvestigationTime;
            public AISearchRoutine roamMap;

            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.StartSearch(self.transform.position, roamMap);
                self.creatureAnimator.SetBool("Moving", true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                if (!roamMap.inProgress) {
                    self.StartSearch(self.transform.position, roamMap);
                }

            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.StopSearch(roamMap);
                self.creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FindPlayer()
            };
        }
        private class Stalk : BehaviorState {
            LurkerAI Lurker;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                Lurker = self as LurkerAI;
                self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position).position);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                
                if (Lurker.clingingToCeiling) {
                    //start breathing noise
                    return;
                }
                if (Vector3.Distance(self.transform.position, self.targetPlayer.transform.position) < 10 && !Lurker.clingingToCeiling) {
                    SwitchClingingToCeilingState(true);
                    return;
                }
                SwitchClingingToCeilingState(false);
                self.SetDestinationToPosition(self.ChooseClosestNodeToPosition(self.targetPlayer.transform.position).position);
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
                lurker = self as LurkerAI;
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.targetPlayer.transform.position = self.transform.position;
                if(Vector3.Distance(self.transform.position, self.destination) < 5) {
                    lurker.finishPlayerDrag = true;
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
        private class FindPlayer : StateTransition {

            public override bool CanTransitionBeTaken() {
                PlayerControllerB[] possiblePlayers = self.GetAllPlayersInLineOfSight();
                if (possiblePlayers.Length > 0) {
                    foreach(var possiblePlayer in possiblePlayers) { 
                        if(!possiblePlayer.HasLineOfSightToPosition(self.transform.position + Vector3.up * 1.6f)){
                            self.targetPlayer = possiblePlayer;
                            return true;
                        }
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
                if (lurker.clingingToCeiling && self.targetPlayer.HasLineOfSightToPosition(lurker.transform.position)){
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                if(Vector3.Distance(self.transform.position, self.targetPlayer.transform.position) < 7) {
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
                if(Vector3.Distance(self.transform.position, self.destination) > 5) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }

        private BehaviorState InitialState = new Roam();
        private BehaviorState ActiveState = null;
        private System.Random enemyRandom;
        private RoundManager roundManager;
        bool clingingToCeiling = false;
        bool finishPlayerDrag = false;
        private float AITimer;
        protected override string __getTypeName() {
            return "LurkerAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }
        public override void Start() {
            base.Start();
        }
        public override void Update() {
            base.Update();
            AITimer++;
            //don't run enemy ai if they're dead

            if (isEnemyDead || !ventAnimationFinished) {
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

            //Custom Monster Code
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
    }
}
