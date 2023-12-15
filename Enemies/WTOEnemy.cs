using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies {
    public class WTOEnemy {
        
        public abstract class BehaviorState {
            public abstract void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator);
            public abstract void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator);
            public abstract void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator);
            public virtual List<StateTransition> transitions { get; set; }
        }
        public abstract class StateTransition {
            public EnemyAI self { get; set; }

            public abstract bool CanTransitionBeTaken();
            public abstract BehaviorState NextState();

            
        }
        public class Investigate : BehaviorState {
            private int InvestigatingTime;
            public int TotalInvestigationTime;
            public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Investigating", value: true);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Investigating", value: false);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
            
        }
        public class Wander : BehaviorState {

            private bool HasFoundNextSearchPoint;
            private Vector3 destination;


        public override void OnStateEntered(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                self.agent.speed = 7f;
                self.SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(self.allAINodes[enemyRandom.Next(self.allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                HasFoundNextSearchPoint = true;
                WTOBase.LogToConsole("Wanderer Searching!");
                creatureAnimator.SetBool("Searching", value: true);
            }
            public override void OnStateExit(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {
                creatureAnimator.SetBool("Searching", value: false);
            }
            public override void UpdateBehavior(EnemyAI self, System.Random enemyRandom, Animator creatureAnimator) {

            }
        }
        public class WanderToInvestigate : StateTransition {
            Vector3 location;
            public override bool CanTransitionBeTaken() {
                if (Vector3.Distance(location, self.transform.position) < 2f) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }
        public class InvestigateToWander : StateTransition {
            public override bool CanTransitionBeTaken() {
                return true;
            }
            public override BehaviorState NextState() {
                return new Investigate();
            }
        }
    }
}
