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

namespace Welcome_To_Ooblterra.Enemies {
    public class LurkerAI : WTOEnemy {
        
        //BEHAVIOR STATES
        private class Roam : BehaviorState {
            private bool MovingToPosition;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerList[enemyIndex].SetAnimBoolOnServerRpc("Grabbing", false);
                MovingToPosition = LurkerList[enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(LurkerList[enemyIndex].allAINodes[enemyRandom.Next(LurkerList[enemyIndex].allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                LurkerList[enemyIndex].agent.speed = 5f;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (!MovingToPosition) {
                    MovingToPosition = LurkerList[enemyIndex].SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(LurkerList[enemyIndex].allAINodes[enemyRandom.Next(LurkerList[enemyIndex].allAINodes.Length - 1)].transform.position, 5), checkForPath: true);
                }
                if(Vector3.Distance(LurkerList[enemyIndex].transform.position, LurkerList[enemyIndex].destination) < 2) {
                    MovingToPosition = false;
                }

            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerList[enemyIndex].StopSearch(LurkerList[enemyIndex].roamMap);
                LurkerList[enemyIndex].creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new FindPlayer()
            };
        }
        private class Stalk : BehaviorState {
            //Find a spot behind the player and go to it
            Vector3 StalkPos;
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerList[enemyIndex].SetAnimBoolOnServerRpc("Grabbing", false);
                if (LurkerList[enemyIndex].PlayerIsTargetable(LurkerList[enemyIndex].targetPlayer)) {
                    float MaxRange = (0.6f) * -1;
                    StalkPos = LurkerList[enemyIndex].targetPlayer.transform.position - (Vector3.Scale(new Vector3(MaxRange, 0, MaxRange), (LurkerList[enemyIndex].targetPlayer.transform.forward * -1)));
                    LurkerList[enemyIndex].SetDestinationToPosition(StalkPos);
                    LurkerList[enemyIndex].agent.speed = 5f;
                }
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new WaitForPlayerEyes(),
                new HoldPosition()
            };
        }
        private class Wait : BehaviorState {
            public Wait() {
                RandomRange = new Vector2(3, 5);
            }
            //Cling to the ceiling and wait to be looked at
            //If the player moves out of range, go back to stalking
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                SwitchClingingToCeilingState(true);
                LurkerList[enemyIndex].agent.speed = 0;
                LurkerList[enemyIndex].MoveCooldownSeconds = MyRandomInt;
                LurkerList[enemyIndex].creatureVoice.Play();
                LurkerList[enemyIndex].finishPlayerDrag = false;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                if (!LurkerList[enemyIndex].WaitingForGrab) {
                    LurkerList[enemyIndex].WaitingForGrab = true;
                }
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerList[enemyIndex].creatureVoice.Stop();
                LurkerList[enemyIndex].SetAnimBoolOnServerRpc("Grabbing", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new WaitForPlayerEyes(),
                new PlayerOutOfRange()
            };
            private void SwitchClingingToCeilingState(bool shouldCling) {
                LurkerList[enemyIndex].SetAnimBoolOnServerRpc("Grabbing", shouldCling);
                if (LurkerList[enemyIndex].WaitingForGrab = shouldCling) {
                    return;
                }
                LurkerList[enemyIndex].WaitingForGrab = shouldCling;
            }
        }
        private class Drag : BehaviorState {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerList[enemyIndex].SetAnimBoolOnServerRpc("GrabDrag", true);
                if(LurkerPoints != null && LurkerPoints.Count > 0) { 
                    LurkerList[enemyIndex].SetDestinationToPosition(LurkerList[enemyIndex].ChooseClosestNodeToPosition(LurkerPoints[0].transform.position).position);
                } else {
                    LurkerList[enemyIndex].SetDestinationToPosition(LurkerList[enemyIndex].ChooseFarthestNodeFromPosition(LurkerList[enemyIndex].transform.position).position);
                }
                LurkerList[enemyIndex].agent.speed = 4f;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerList[enemyIndex].targetPlayer.transform.position = LurkerList[enemyIndex].transform.position;
                if(Vector3.Distance(LurkerList[enemyIndex].transform.position, LurkerList[enemyIndex].destination) < 1) {
                    LurkerList[enemyIndex].SetAnimTriggerOnServerRpc("Drop");
                    LurkerList[enemyIndex].finishPlayerDrag = true;
                } else {
                    if(LurkerList[enemyIndex].agent.speed < 25f) {
                        LurkerList[enemyIndex].agent.speed += Time.deltaTime * 5;
                    }
                    
                }
            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerList[enemyIndex].SetAnimTriggerOnServerRpc("Drop");
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new PlayerDroppedOff()
            };
        }
        private class Flee : BehaviorState {
            public override void OnStateEntered(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerList[enemyIndex].SetAnimBoolOnServerRpc("Grabbing", false);

                LurkerList[enemyIndex].WaitingForGrab = false;
                LurkerList[enemyIndex].SetDestinationToPosition(LurkerList[enemyIndex].ChooseFarthestNodeFromPosition(LurkerList[enemyIndex].transform.position).position);
                LurkerList[enemyIndex].agent.speed = 15f;
            }
            public override void UpdateBehavior(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {

            }
            public override void OnStateExit(int enemyIndex, System.Random enemyRandom, Animator creatureAnimator) {
                LurkerList[enemyIndex].creatureAnimator.SetBool("Moving", false);
            }
            public override List<StateTransition> transitions { get; set; } = new List<StateTransition> {
                new DoneFleeing()
            };
        }

        //STATE TRANSITIONS
        private class FindPlayer : StateTransition {

            public override bool CanTransitionBeTaken() {
                
                if (LurkerList[enemyIndex].GetAllPlayersInLineOfSight(90f) == null) {
                    return false;
                }
                foreach (var possiblePlayer in LurkerList[enemyIndex].GetAllPlayersInLineOfSight(90f)) { 
                    if(!possiblePlayer.HasLineOfSightToPosition(LurkerList[enemyIndex].transform.position /*+ Vector3.up * 1.6f*/)){
                        LurkerList[enemyIndex].targetPlayer = possiblePlayer;
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
                if(LurkerList[enemyIndex].targetPlayer == null) {
                    return true;
                }
                if (LurkerList[enemyIndex].targetPlayer.HasLineOfSightToPosition(LurkerList[enemyIndex].transform.position + LurkerList[enemyIndex].transform.up *2f)) {
                    return true;
                }
                return false;
            }
            public override BehaviorState NextState() {
                if(LurkerList[enemyIndex].targetPlayer == null) {
                    
                    return new Flee();
                }
                WTOBase.LogToConsole("Clinging to ceiling: " + LurkerList[enemyIndex].WaitingForGrab);
                WTOBase.LogToConsole("Distance: " + Vector3.Distance(LurkerList[enemyIndex].transform.position, LurkerList[enemyIndex].targetPlayer.transform.position));
                if (LurkerList[enemyIndex].WaitingForGrab && Vector3.Distance(LurkerList[enemyIndex].transform.position, LurkerList[enemyIndex].targetPlayer.transform.position) <= LurkerList[enemyIndex].GrabDistance) {
                    return new Drag();
                }
                return new Flee();
            }
        }
        private class HoldPosition : StateTransition {
            public override bool CanTransitionBeTaken() {
                return Vector3.Distance(LurkerList[enemyIndex].transform.position, LurkerList[enemyIndex].destination) <= 0.6f;
            }
            public override BehaviorState NextState() {
                return new Wait();
            }
        }
        private class PlayerOutOfRange : StateTransition {

            public override bool CanTransitionBeTaken() {
                if (Vector3.Distance(LurkerList[enemyIndex].transform.position, LurkerList[enemyIndex].targetPlayer.transform.position) > 0.8f && LurkerList[enemyIndex].MoveCooldownSeconds <= 0f) {
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
                return LurkerList[enemyIndex].finishPlayerDrag;
            }
            public override BehaviorState NextState() {
                return new Flee();
            }
        }
        private class DoneFleeing : StateTransition {

            public override bool CanTransitionBeTaken() {
                if(Vector3.Distance(LurkerList[enemyIndex].transform.position, LurkerList[enemyIndex].destination) < 2) {
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
                if (LurkerList[enemyIndex].targetPlayer == null || LurkerList[enemyIndex].PlayerIsTargetable(LurkerList[enemyIndex].targetPlayer, true, false)) {
                    return false;
                }
                return false;
            }
            public override BehaviorState NextState() {
                return new Roam();
            }
        }

        bool WaitingForGrab = false;
        bool finishPlayerDrag = false;
        public readonly float GrabDistance = 1f;
        public AISearchRoutine roamMap;
        public float MoveCooldownSeconds;
        public static Dictionary<int, LurkerAI> LurkerList = new Dictionary<int, LurkerAI>();
        public static List<GameObject> LurkerPoints;
        public override string __getTypeName() {
            return "LurkerAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }
        public override void Start() {
            //GetAllLurkerPoints();
            PrintDebugs = true;
            InitialState = new Roam();
            base.Start();
            LurkerList.Add(thisEnemyIndex, this);
        }
        public override void Update() {
            base.Update();
            LowerTimerValue(ref MoveCooldownSeconds);
        }
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            enemyHP -= force;
            if (base.IsOwner) {
                if (enemyHP <= 0) {
                    creatureAnimator.SetTrigger("Killed");
                    KillEnemyOnOwnerClient();
                    return;
                }
            }
            OverrideState(new Flee());
        }
        private static void GetAllLurkerPoints() {
            IEnumerable<UnityEngine.Object> LurkerPointsEnumerable = Resources.FindObjectsOfTypeAll(typeof(UnityEngine.Object)).Where(obj => obj.name.Contains("LurkerPoint"));
            foreach(GameObject LurkerPoint in LurkerPointsEnumerable) {
                LurkerPoints.Add(LurkerPoint);
            }
        }
    }
}
