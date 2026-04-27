using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Enemies.EnemyThings;
using LethalLevelLoader;

namespace Welcome_To_Ooblterra.Things
{
    public class BabyLurkerEgg : NetworkBehaviour {

        public GameObject HiveMesh;
        public GameObject projectileTemplate;
        public GameObject MapDot;
        public Transform DropTransform;
        public Transform TraceTransform;
        public AudioClip[] BreakoffSound;
        public ScanNodeProperties ScanNode;

        private System.Random enemyRandom;
        private bool EggSpawned = false;
        private bool EggDropped = false;

        private float NextSpawnAttemptTime = 0;

        private static readonly WTOBase.WTOLogger Log = new(typeof(BabyLurkerEgg), LogSourceType.Thing);

        private void OnTriggerStay(Collider other) 
        {
            if (IsServer)
            {
                if(EggSpawned && !EggDropped && other.gameObject.TryGetComponent<PlayerControllerB>(out PlayerControllerB victim))
                {
                    SpawnProjectile(victim.actualClientId);
                }
            }
        }

        private void Start() 
        {
            SetScanNodeId();

            enemyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
            NextSpawnAttemptTime = Time.time + enemyRandom.Next(15, 40);

            if (IsServer)
            {
                SetEggPosition();
            }
        }

        private void SetScanNodeId()
        {
            ExtendedEnemyType babyLurkerEggType = PatchedContent.ExtendedEnemyTypes.Find((ExtendedEnemyType x) => x.EnemyDisplayName == "Baby Lurker Egg");
            if (babyLurkerEggType == null)
            {
                Log.Error("Couldn't find Baby Lurker Egg enemy type in ExtendedEnemyTypes list!");
                return;
            }

            ScanNode.creatureScanID = babyLurkerEggType.EnemyInfoNode.creatureFileID;
            Log.Debug("Set scan node ID for Baby Lurker Egg: " + ScanNode.creatureScanID);
        }

        private void Update() 
        {
            if(IsServer)
            {
                if (!EggSpawned && Time.time >= NextSpawnAttemptTime)
                {
                    if (enemyRandom.Next(0, 100) < 60)
                    {
                        SetEggPosition();
                    }
                    else
                    {
                        NextSpawnAttemptTime = Time.time + enemyRandom.Next(15, 40);
                    }
                }
            }
        }

        private void SetEggPosition() 
        {
            if (EggSpawned) 
            {
                return;
            }

            if (Physics.Linecast(TraceTransform.position, TraceTransform.position + (Vector3.up * 5000), out RaycastHit HitResult, StartOfRound.Instance.collidersAndRoomMask, QueryTriggerInteraction.Ignore)) 
            {
                HiveMesh.transform.position = HitResult.point;
                HiveMesh.transform.rotation = new Quaternion(180, 0, 0, 0);
                Log.Debug("Server - Lurker Egg Line trace hit at position: " + HitResult.point);
                EggSpawned = true;

                SetEggPositionClientRpc(HitResult.point);
            } 
            else 
            {
                Log.Warning("Server - Lurker Egg Line trace failed!");
            }
        }

        [ClientRpc]
        private void SetEggPositionClientRpc(Vector3 position) 
        {
            Log.Debug("Client - Setting lurker egg position to: " + position);

            EggSpawned = true;
            HiveMesh.SetActive(true);
            MapDot.SetActive(true);
            HiveMesh.transform.position = position;
            HiveMesh.transform.rotation = new Quaternion(180, 0, 0, 0);
        }

        private void SpawnProjectile(ulong targetID) 
        {
            if(EggDropped)
            {
                // server already dropped egg - do nothing.
                return;
            }

            EggDropped = true;

            Instantiate(projectileTemplate, DropTransform.position, DropTransform.rotation).GetComponent<NetworkObject>().Spawn();

            SpawnProjectileClientRpc();
        }

        [ClientRpc] 
        public void SpawnProjectileClientRpc()
        {
            EggDropped = true;
            MapDot.SetActive(false);
            HiveMesh.SetActive(false);
            GetComponent<AudioSource>()?.PlayOneShot(BreakoffSound[enemyRandom.Next(0, BreakoffSound.Length)]);
        }
    }
}
