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

namespace Welcome_To_Ooblterra {
    public class WandererAI : EnemyAI {

        private RoundManager roundManager;
        private float AITimer;

        private bool RunningAway = false;
        private int TimeSinceLastInRangeOfThreat = 0;
        private List<GameObject> RegisteredThreats = new List<GameObject>();

        private bool MovingToNextPoint = false;

        private int InvestigatingTime = 12;
        private bool Investigating = false;
        private int TotalInvestigationTime;
        
        private Ray ray;
        private RaycastHit rayHit;
        //private AISearchRoutine roamPlanet;
        private System.Random enemyRandom;
        private bool HasReachedDestination = false;
        private bool HasFoundNextSearchPoint = false;

        
        protected override string __getTypeName() {
            return "WandererAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }

        public override void Start() {
            base.Start();
            
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
            

        }
        public override void Update() {
            base.Update();
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
            if (agent.isOnNavMesh) { 
                switch (currentBehaviourStateIndex) {
                    case 0:
                        agent.speed = 0f;
                        if (InvestigatingTime > TotalInvestigationTime) {
                            InvestigatingTime = -1;
                            HasReachedDestination = false;
                            HasFoundNextSearchPoint = false;
                            currentBehaviourStateIndex = 1;
                            WTOBase.LogToConsole("Wanderer done investigating.");
                            break;
                        }
                        InvestigatingTime++;
                        //WTOBase.LogToConsole("Wanderer investigating for " + InvestigatingTime.ToString());
                        break;


                    case 1:
                        if (HasReachedDestination) {
                            InvestigatingTime = 0;
                            TotalInvestigationTime = enemyRandom.Next(300, 2001);
                            currentBehaviourStateIndex = 0;
                            creatureAnimator.SetBool("Investigating", value: true);
                            WTOBase.LogToConsole("Wanderer changing state to investigate");
                            break;
                        }

                        if (HasFoundNextSearchPoint) {
                            //WTOBase.LogToConsole("Distance from destination" + Vector3.Distance(destination, transform.position));
                            if (Vector3.Distance(destination, transform.position) < 2f) {
                                HasReachedDestination = true;
                            }
                            break;
                        }

                        if (IsOwner) {
                            agent.speed = 7f;
                            SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(allAINodes[enemyRandom.Next(allAINodes.Length -1)].transform.position, 5), checkForPath: true);
                            HasFoundNextSearchPoint = true;
                            WTOBase.LogToConsole("Wanderer Searching!");
                            creatureAnimator.SetBool("Investigating", value: false);
                            break;
                        }
                        

                        
                        break;
                    case 2:
                        //AssessThreat();
                    break;
                }
            }
        }
        private bool InRangeOfThreats() {
            bool InRange = false;
            foreach (GameObject threat in RegisteredThreats) {
                if (Vector3.Distance(base.transform.position, threat.transform.position) < 300) {
                    InRange = true;
                }
            }
            return InRange;
        }
        private void FindNextPoint() {
            Vector3 NextInvestigatePoint = base.transform.position + UnityEngine.Random.insideUnitSphere * 0.8f;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(NextInvestigatePoint, out hit, 1.0f, NavMesh.AllAreas)) {
                WTOBase.LogToConsole("Wanderer found new point!");
                agent.SetDestination(NextInvestigatePoint);
                MovingToNextPoint = true;
            }
            WTOBase.LogToConsole("Wanderer could not navigate to new point!");
        }
        private void AssessThreat() {
            if (InRangeOfThreats()) {
                //run away
                return;
            }
            TimeSinceLastInRangeOfThreat++;
            if (TimeSinceLastInRangeOfThreat > 30) {
                RunningAway = false;
                MovingToNextPoint = true;
            }
        }
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            enemyHP -= force;
            if (base.IsOwner) {
                if (enemyHP <= 0) {
                    KillEnemyOnOwnerClient();
                    return;
                }
                //this probably does something (stolen from mouthdog)
                /*
                if (inKillAnimation) {
                    StopKillAnimationServerRpc();
                }
                */
            }
            RegisteredThreats.Add(playerWhoHit.gameObject);
            RunningAway = true;
        }
    }
}

