using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;

namespace Welcome_To_Ooblterra.Enemies {
    public class EnforcerAI : EnemyAI {
        public DeadBodyInfo bodyBeingCarried;
        private RoundManager roundManager;
        private float AITimer;

        private EnforcerWeapon MyWeapon;

        private List<PlayerControllerB> ArmedPlayers = new List<PlayerControllerB>();
        private List<PlayerControllerB> HostilePlayers = new List<PlayerControllerB>();

        private bool isBeingObserved = false;
        private bool carryingBody;
        private System.Random enemyRandom;

        private bool stateInterrupted = false;
        private int TimeStandingStill;
        private int TimeMovingWhenSeen;
        private int RangedAttackCooldown;
        private int TimeSinceLastRangedAttack;


        protected override string __getTypeName() {
            return "EnforcerAI";
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

            //Custom Enforcer Code
            GetArmedEnemyList();
            //Check to see if anyone in the player list is currently holding a weapon
            foreach (PlayerControllerB player in StartOfRound.Instance.playersContainer) {
                foreach (GrabbableObject HeldItem in player.ItemSlots) {
                    if (HeldItem is Shovel) {
                        if (!ArmedPlayers.Contains(player)) ArmedPlayers.Add(player);
                        if (currentBehaviourStateIndex < 1) currentBehaviourStateIndex = 1;
                    }
                    if (HeldItem is ShotgunItem || HeldItem is StunGrenadeItem || HeldItem is PatcherTool || HeldItem is EnforcerWeapon ) {
                        if (!ArmedPlayers.Contains(player)) ArmedPlayers.Add(player);
                        if (currentBehaviourStateIndex < 3) currentBehaviourStateIndex = 3;
                    }
                }
            }

            if (agent.isOnNavMesh) {

                switch (currentBehaviourStateIndex) {
                    case 0:
                        agent.speed = 0f;
                        break;


                    //Begin hunting a melee threat by getting as close to it as possible without being spotted 
                    case 1: 
                        if (targetPlayer == null) {
                            targetPlayer = NearestArmedPlayer();
                            agent.speed = 10f;
                            destination = RoundManager.Instance.GetNavMeshPosition(targetPlayer.transform.position, RoundManager.Instance.navHit, 2.7f);
                        }
                        if (Vector3.Distance(targetPlayer.transform.position, transform.position) < 5) {
                            currentBehaviourStateIndex = 2;
                            break;
                        }
                        if (targetPlayer.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f) && TimeStandingStill < 1050) {
                            agent.speed = 0f;
                            TimeStandingStill++;
                            break;
                        }
                        agent.speed = 10f;
                        if (targetPlayer.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f)) {
                            TimeMovingWhenSeen++;
                            targetPlayer.JumpToFearLevel(1f);
                        }
                        if (TimeMovingWhenSeen > enemyRandom.Next(30, 80)) {
                            TimeStandingStill = 0;
                        }
                        break;
                    //Melee Attack target player
                    case 2:
                        if(Vector3.Distance(targetPlayer.transform.position, transform.position) < 5) {
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
                    //Get in a good nearby position to ranged attack a player
                    case 3:
                        if (targetPlayer == null) {
                            targetPlayer = NearestArmedPlayer();
                            agent.speed = 10f;
                            targetNode = ChooseClosestNodeToPosition(targetPlayer.transform.position, true);
                            SetDestinationToPosition(targetNode.position);
                        }
                        if (Vector3.Distance(targetNode.position, transform.position) < 5) {
                            currentBehaviourStateIndex = 4;
                            break;
                        }
                        if (targetPlayer.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f) && TimeStandingStill < 750) {
                            agent.speed = 0f;
                            TimeStandingStill++;
                            break;
                        }
                        agent.speed = 10f;
                        if (targetPlayer.HasLineOfSightToPosition(transform.position + Vector3.up * 1.6f)) {
                            TimeMovingWhenSeen++;
                            targetPlayer.JumpToFearLevel(1f);
                        }
                        if (TimeMovingWhenSeen > enemyRandom.Next(30, 80)) {
                            TimeStandingStill = 0;
                            currentBehaviourStateIndex = 5;
                        }
                        break;
                    case 4:
                        //Begin attacking at range. if multiple ranged enemies are all near each other, prefer the RPG, or else choose randomly
                        if (TimeSinceLastRangedAttack > RangedAttackCooldown && !MyWeapon.isLaunchingRocket && !MyWeapon.isTargetingLaser) {

                            /*if ( Enemies are all within explosion radius of each other) {
                                MyWeapon.LaunchRocket(targetPlayer);
                                break;
                            }*/
                            if (enemyRandom.Next(0, 100) % 4 == 0) {
                                MyWeapon.LaunchRocket(targetPlayer);
                            } else {
                                MyWeapon.TargetLaser(targetPlayer);
                            }

                        }
                        break;
                    case 5:
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

        private void GetArmedEnemyList() {
            foreach (PlayerControllerB player in StartOfRound.Instance.playersContainer) {
                foreach (GrabbableObject HeldItem in player.ItemSlots) {
                    if (HeldItem is Shovel) {
                        if (!ArmedPlayers.Contains(player)) ArmedPlayers.Add(player);
                        if (currentBehaviourStateIndex < 1) currentBehaviourStateIndex = 1;
                    }
                    if (HeldItem is ShotgunItem || HeldItem is StunGrenadeItem || HeldItem is PatcherTool || HeldItem is EnforcerWeapon ) {
                        if (!ArmedPlayers.Contains(player)) ArmedPlayers.Add(player);
                        if (currentBehaviourStateIndex < 3) currentBehaviourStateIndex = 3;
                    }
                }
            }
        }
        private PlayerControllerB NearestArmedPlayer() {
            if (!ArmedPlayers.Any()) {
                return null;
            }
            PlayerControllerB nearest = ArmedPlayers[0];
            foreach (PlayerControllerB player in ArmedPlayers) {
                if (Vector3.Distance(player.transform.position, transform.position) < Vector3.Distance(nearest.transform.position, transform.position)) {
                    nearest = player;
                }
            }
            return nearest;
        }

        private void MeleeAttackPlayer(PlayerControllerB target) {
            target.DamagePlayer(70, hasDamageSFX: true, callRPC: true, CauseOfDeath.Mauling, 0);
            target.JumpToFearLevel(1f);
        }
    }
}
