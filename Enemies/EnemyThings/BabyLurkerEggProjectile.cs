using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace Welcome_To_Ooblterra.Enemies.EnemyThings
{
    public class BabyLurkerEggProjectile : NetworkBehaviour 
    {
        public int BabiesToSpawn = 20;
        public EnemyType BabyLurker;
        public MeshRenderer EggMesh;
        public MeshRenderer InnerEggMesh;
        public AudioClip[] Splat;
        public AudioClip[] Boom;
        public ParticleSystem ExplodeParticle;
        private System.Random EggRandom;

        private static readonly WTOBase.WTOLogger Log = new(typeof(BabyLurkerEggProjectile), LogSourceType.Thing);

        private void Start() 
        {
            EggRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
        }

        private void OnTriggerEnter(Collider other) 
        {
            if (!IsServer)
            {
                return;
            }

            Log.Debug($"Collision registered! Collider: {other.gameObject}");

            if (other.GetComponent<BabyLurkerAI>() != null || other.GetComponent<BabyLurkerProjectile>() != null) 
            {
                // avoid hitting the lurker that chucked us
                return; 
            } 

            // Try *hard* to find the right position
            NavMeshHit? hit = Utils.GetRandomNavMeshPositionInRadiusExtended(transform.position, radius: 1);

            // play impact sound always, even if it hits out of bounds, but only play the egg animation if it hits a valid nav mesh position.
            // otherwise it will just get destroyed below.
            OnImpactClientRpc(playEggAnimation: hit.HasValue);

            if (!hit.HasValue)
            {
                Log.Warning("Lurker projectile hit too far from nav mesh to spawn enemies, not spawning");
                // short delay to allow impact sound to finish on client side before destroying.
                Destroy(gameObject, 0.6f);
                return;
            }

            GetComponent<Rigidbody>().isKinematic = true;
            StartCoroutine(StartEggExplodingServer(hit.Value.position));
        }

        [ClientRpc]
        private void OnImpactClientRpc(bool playEggAnimation)
        {
            GetComponent<AudioSource>().PlayOneShot(Splat[EggRandom.Next(0, Splat.Length - 1)]);

            if(playEggAnimation)
            {
                StartCoroutine(ClientEggExplodingAnimation());
            }
        }

        const float ExpandTime = 3f;

        private IEnumerator ClientEggExplodingAnimation()
        {
            float startTime = Time.time;
            float endTime = startTime + ExpandTime;

            while (Time.time < endTime)
            {
                float f = Mathf.InverseLerp(startTime, endTime, Time.time);
                float l = Mathf.Lerp(0.5f, 0.8f, f);
                EggMesh.transform.localScale = new Vector3(l, l, l);
                yield return null;
            }

            EggMesh.enabled = false;
            InnerEggMesh.enabled = false;
            ExplodeParticle.Play();
            GetComponent<BoxCollider>().enabled = false;
            GetComponent<AudioSource>().PlayOneShot(Boom[EggRandom.Next(0, Boom.Length)]);
        }

        private IEnumerator StartEggExplodingServer(Vector3 spawnPosition) 
        {
            // wait for the client animation to finish.
            yield return new WaitForSeconds(ExpandTime);

            for(int i = 0; i < BabiesToSpawn; i++) 
            {
                RoundManager.Instance.SpawnEnemyGameObject(spawnPosition, 0, 1, BabyLurker);
                yield return new WaitForSeconds(0.05f);
            }

            // short delay to allow audio to finish on client side before destroying.
            Destroy(gameObject, 1.0f);
        }
    }
}
