using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Welcome_To_Ooblterra.Enemies.WTOEnemy;

namespace Welcome_To_Ooblterra.Enemies {
    //Trimming this for now, will add it back later if I have enough time

    /*
    public class SecBotAI : EnemyAI {

        private class Move : BehaviorState {
            public bool MovingToNextPoint;
            private Ray newRaycast;
            private RaycastHit raycastHit;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (!MovingToNextPoint) {
                    newRaycast.origin = self.transform.position;
                    newRaycast.direction = self.transform.forward;
                    Physics.Raycast(newRaycast, out raycastHit);
                    self.SetDestinationToPosition(raycastHit.point);
                    MovingToNextPoint = true;
                }
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

            };
        }
        private class Turn : BehaviorState {
            public bool SearchInProgress;
            public bool investigate;
            public int investigateTimer;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

            };
        }
        private class Chase : BehaviorState {
            public bool SearchInProgress;
            public bool investigate;
            public int investigateTimer;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

            };
        }
        private class ShutDown : BehaviorState {
            public bool SearchInProgress;
            public bool investigate;
            public int investigateTimer;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: true);
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Moving", value: false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {

            };
        }

        private class HitWall : StateTransition {
            EyeSecAI SelfEyeSec;
            bool ShouldDoScan;
            public override bool CanTransitionBeTaken() {
                //Grab a list of every player in range
                PlayerControllerB[] players = self.GetAllPlayersInLineOfSight();
                return (Vector3.Distance(self.transform.position, self.destination) > 5);
            }
            public override BehaviorState NextState() {
                return new Turn();
            }

        }
        private class FoundDir : StateTransition {
            EyeSecAI SelfEyeSec;
            bool ShouldDoScan;
            public override bool CanTransitionBeTaken() {
                //Grab a list of every player in range
                PlayerControllerB[] players = self.GetAllPlayersInLineOfSight();
                if (players.Length > 0 && SelfEyeSec.enemyRandom.Next(0, 50) > 35) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new ScanEnemies();
            }

        }
        private class LaserTripped : StateTransition {
            EyeSecAI SelfEyeSec;
            bool ShouldDoScan;
            public override bool CanTransitionBeTaken() {
                //Grab a list of every player in range
                PlayerControllerB[] players = self.GetAllPlayersInLineOfSight();
                if (players.Length > 0 && SelfEyeSec.enemyRandom.Next(0, 50) > 35) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new ScanEnemies();
            }

        }
        private class Disabled : StateTransition {
            EyeSecAI SelfEyeSec;
            bool ShouldDoScan;
            public override bool CanTransitionBeTaken() {
                //Grab a list of every player in range
                PlayerControllerB[] players = self.GetAllPlayersInLineOfSight();
                if (players.Length > 0 && SelfEyeSec.enemyRandom.Next(0, 50) > 35) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new ScanEnemies();
            }

        }
        private class StartUp : StateTransition {
            EyeSecAI SelfEyeSec;
            bool ShouldDoScan;
            public override bool CanTransitionBeTaken() {
                //Grab a list of every player in range
                PlayerControllerB[] players = self.GetAllPlayersInLineOfSight();
                if (players.Length > 0 && SelfEyeSec.enemyRandom.Next(0, 50) > 35) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new ScanEnemies();
            }

        }

        private BehaviorState InitialState = new Move();
        private BehaviorState ActiveState = null;
        private System.Random enemyRandom;
        private RoundManager roundManager;
        private float AITimer;
        protected override string __getTypeName() {
            return "SecBotAI";
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
    }*/
}
