using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;

namespace Welcome_To_Ooblterra.Enemies {
    public class GallenarmaAI : EnemyAI, INoiseListener {

        public DeadBodyInfo bodyBeingCarried;
        private RoundManager roundManager;
        private float AITimer;
        public AISearchRoutine roamMap;


        private bool carryingBody;
        private System.Random enemyRandom;
        private bool IsAwake;
        private bool stateInterrupted = false;
        private int TimeSpentBreakingChains;
        private float hearNoiseCooldown;
        private bool inKillAnimation;
        private int TimeUntilChainsBroken;
        private int TimeSpentDancing;

        private struct NoiseInfo {
            public Vector3 Position { get; private set; }
            private float Loudness { get; }
            public NoiseInfo(Vector3 position, float loudness) {
                Position = position;
                Loudness = loudness;
            }


        }
        private List<NoiseInfo> NoiseStack = new List<NoiseInfo>();

        protected override string __getTypeName() {
            return "GallenarmaAI";
        }
        public override void DoAIInterval() {
            base.DoAIInterval();
            _ = StartOfRound.Instance.livingPlayers;
        }

        public override void Start() {
            base.Start();
            enemyHP = 20;
            //Mouthdog Start() code
            roundManager = FindObjectOfType<RoundManager>();
            useSecondaryAudiosOnAnimatedObjects = true;
            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed + thisEnemyIndex);
            //roamPlanet = new AISearchRoutine();
            if (!agent.isOnNavMesh) {
                Physics.Raycast(new Ray(new Vector3(0, 0, 0), Vector3.down), out var hit, LayerMask.GetMask("Terrain"));
                agent.Warp(hit.point);
            }
            WTOBase.LogToConsole("Enforcer on NavMesh: " + (agent.isOnNavMesh).ToString());

            //Debug for the animations not fucking working
            creatureAnimator.Rebind();
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

            //Custom Gallenarma Code

            
            if (agent.isOnNavMesh) {

                if(NoiseStack.Count > 0) {
                    currentBehaviourStateIndex = 3;
                } else if (currentBehaviourStateIndex == 3){
                    currentBehaviourStateIndex = 2;
                }


                switch (currentBehaviourStateIndex) {
                    //Asleep; don't do anything, just listen for noises
                    case 0:
                        agent.speed = 0f;
                        break;

                    //Play our wake up animation and begin breaking chains 
                    case 1:
                        if (!IsAwake) {
                            //play wake up animation
                            IsAwake = true;
                        }
                        TimeSpentBreakingChains++;
                        if (TimeSpentBreakingChains > TimeUntilChainsBroken) {
                            //play animation for breaking chains
                            currentBehaviourStateIndex = 2;
                            break;
                        }
                        break;
                    //Patrol nearby area 
                    case 2:
                        if (!roamMap.inProgress) {
                            StartSearch(base.transform.position, roamMap);
                        }
                        break;
                    //Investigate a heard noise
                    case 3:
                        if (roamMap.inProgress) {
                            StopSearch(roamMap);
                        }
                        if (Vector3.Distance(destination, transform.position) < 2f) {
                            NoiseStack.RemoveAt(0);
                            agent.speed = 0;
                            //play investigate anim

                            break;
                        }
                        SetDestinationToPosition(NoiseStack[0].Position, true);

                        break;
                    //We are at the noise source and should attack if we find something near it
                    case 4:
                        if (Vector3.Distance(targetPlayer.transform.position, transform.position) < 5) {
                            if (carryingBody) {
                                bodyBeingCarried.matchPositionExactly = false;
                                bodyBeingCarried.attachedTo = null;
                                bodyBeingCarried = null;
                                creatureAnimator.SetBool("carryingBody", value: false);
                            }
                            MeleeAttackPlayer(targetPlayer);
                        } else {
                            currentBehaviourStateIndex = 1;
                        }
                        break;
                    //We heard a boombox and are busting it down
                    case 5:
                        TimeSpentDancing++;
                        if (TimeSpentDancing > 10000) {
                            currentBehaviourStateIndex = 2;
                        }
                        break;
                    //we killed something and are stringing its body up as a warning
                    case 6:
                        if (inSpecialAnimationWithPlayer != null) {
                            inSpecialAnimationWithPlayer.inSpecialInteractAnimation = false;
                            inSpecialAnimationWithPlayer.snapToServerPosition = false;
                            inSpecialAnimationWithPlayer.inAnimationWithEnemy = null;
                            if (carryingBody) {
                                bodyBeingCarried = inSpecialAnimationWithPlayer.deadBody;
                                //TODO: ADD GRIP HERE -> bodyBeingCarried.attachedTo = rightHandGrip;
                                bodyBeingCarried.attachedLimb = inSpecialAnimationWithPlayer.deadBody.bodyParts[0];
                                bodyBeingCarried.matchPositionExactly = true;
                            }
                            SetDestinationToPosition(ChooseClosestNodeToPosition(RoundManager.FindMainEntrancePosition()).position, checkForPath: true);
                            //TODO: string body up once we make it to that position 
                        }
                        break;            
                }
            }
        }

        //If we're attacked by a player, they need to be immediately set to our target player
        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false) {
            base.HitEnemy(force, playerWhoHit, playHitSFX);
            enemyHP -= force;
            if (base.IsOwner) {
                if (enemyHP <= 0) {
                    KillEnemyOnOwnerClient();
                    return;
                }
            }
            //If we're attacked by a player, they need to be immediately set to our target player
            targetPlayer = playerWhoHit;
            currentBehaviourStateIndex = 2;
        }
        public override void DetectNoise(Vector3 noisePosition, float noiseLoudness, int timesNoisePlayedInOneSpot = 0, int noiseID = 0) {
            base.DetectNoise(noisePosition, noiseLoudness, timesNoisePlayedInOneSpot, noiseID);
            if (stunNormalizedTimer > 0f || noiseID == 7 || noiseID == 546 || inKillAnimation || hearNoiseCooldown >= 0f || timesNoisePlayedInOneSpot > 15) {
                return;
            }

            hearNoiseCooldown = 0.03f;
            float num = Vector3.Distance(base.transform.position, noisePosition);
            Debug.Log($"Gallenarma '{base.gameObject.name}': Heard noise! Distance: {num} meters");
            float num2 = 18f * noiseLoudness;
            if (Physics.Linecast(base.transform.position, noisePosition, 256)) {
                noiseLoudness /= 2f;
                num2 /= 2f;
            }

            if (noiseLoudness < 0.25f) {
                return;
            }
            switch (currentBehaviourStateIndex) {
                case 0:
                    currentBehaviourStateIndex = 1;
                    TimeUntilChainsBroken = enemyRandom.Next(1800, 3600);
                    return;
                case 1:
                    TimeSpentBreakingChains += enemyRandom.Next(50, 150);
                    return;
                case 2:
                case 3:
                    NoiseStack.Insert(0, new NoiseInfo(noisePosition, noiseLoudness));
                    return;
                default:
                    break;
            }
        }
        private void MeleeAttackPlayer(PlayerControllerB target) {
            target.DamagePlayer(100, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 0);
            target.JumpToFearLevel(1f);
        }
    }
}
