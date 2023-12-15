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
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies {


    public class WandererAI : EnemyAI {

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
            if (agent.isOnNavMesh) {
                if (Vector3.Distance(NearestPlayer(RegisteredThreats).transform.position, transform.position) < 150 && currentBehaviourStateIndex != 2) {
                    currentBehaviourStateIndex = 2;
                } 
                switch (currentBehaviourStateIndex) {
                    case 0:
                        agent.speed = 0f;
                        if (Vector3.Distance(NearestPlayer(PlayersCuriousAbout).transform.position, transform.position) < 50){
                            //Look at the player
                            if (AITimer % 2 == 0) {
                                InvestigatingTime++;
                            }
                            break;
                        }
                        if (InvestigatingTime > TotalInvestigationTime) {
                            InvestigatingTime = -1;
                            HasReachedDestination = false;
                            HasFoundNextSearchPoint = false;
                            currentBehaviourStateIndex = 1;
                            WTOBase.LogToConsole("Wanderer done investigating.");
                            break;
                        }
                        InvestigatingTime++;
                        break;


                    case 1:
                        if (HasReachedDestination){
                            InvestigatingTime = 0;
                            TotalInvestigationTime = enemyRandom.Next(300, 2001);
                            currentBehaviourStateIndex = 0;
                            creatureAnimator.SetBool("Investigating", value: true);
                            WTOBase.LogToConsole("Wanderer changing state to investigate");
                            break;
                        }

                        if (HasFoundNextSearchPoint){
                            //WTOBase.LogToConsole("Distance from destination" + Vector3.Distance(destination, transform.position));
                            if (Vector3.Distance(destination, transform.position) < 2f) {
                                HasReachedDestination = true;
                            }
                            break;
                        }

                        if (IsOwner){
                            agent.speed = 7f;
                            SetDestinationToPosition(RoundManager.Instance.GetRandomNavMeshPositionInRadius(allAINodes[enemyRandom.Next(allAINodes.Length -1)].transform.position, 5), checkForPath: true);
                            HasFoundNextSearchPoint = true;
                            WTOBase.LogToConsole("Wanderer Searching!");
                            creatureAnimator.SetBool("Investigating", value: false);
                            break;
                        }
                        break;
                    case 2:
                        agent.speed = 10f;
                        if (AITimer % 5 == 0) { 
                            SetDestinationToPosition(ChooseFarthestNodeFromPosition(NearestPlayer(RegisteredThreats).transform.position, avoidLineOfSight: false, UnityEngine.Random.Range(0, allAINodes.Length / 2)).position);
                            currentBehaviourStateIndex = 1;
                            HasFoundNextSearchPoint = true;
                        }
                        break;
                }
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
                    KillEnemyOnOwnerClient();
                    return;
                }
            }
            if(!RegisteredThreats.Contains(playerWhoHit)) RegisteredThreats.Add(playerWhoHit);
        }
    }
}

