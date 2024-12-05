using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Enemies.EnemyThings;
public class BabyLurkerEggProjectile : NetworkBehaviour
{

    public int TargetID = 0;
    public int BabiesToSpawn = 20;
    private Vector3 SpawnPosition;
    private EnemyType BabyLurker;
    private int iterator = 0;
    public MeshRenderer EggMesh;
    public MeshRenderer InnerEggMesh;
    public AudioClip[] Splat;
    public AudioClip[] Boom;
    public ParticleSystem ExplodeParticle;
    private System.Random EggRandom;

    private void Start()
    {
        EggRandom = new System.Random(StartOfRound.Instance.randomMapSeed);

    }

    private void OnTriggerEnter(Collider other)
    {
        WTOBase.LogToConsole($"Collision registered! Collider: {other.gameObject}");
        if (other.GetComponent<BoxCollider>() != null || other.GetComponent<BabyLurkerAI>() != null || other.GetComponent<BabyLurkerProjectile>() != null)
        {
            return;
        }
        BabyLurker = MonsterPatch.InsideEnemies.First(x => x.enemyType.enemyName == "Baby Lurker").enemyType;
        SpawnPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(transform.position, radius: 1);
        if (SpawnPosition == transform.position)
        {
            return;
        }
        try
        {
            GetComponent<AudioSource>().PlayOneShot(Splat[EggRandom.Next(0, Splat.Length - 1)]);
        }
        catch
        {
            WTOBase.LogToConsole("Couldn't play splat sound!");
        }
        GetComponent<Rigidbody>().isKinematic = true;
        StartCoroutine(StartEggExploding());
    }

    private float timeElapsed = 0f;
    private readonly float ExpandTime = 3f;
    private float LerpValue = 0f;
    IEnumerator StartEggExploding()
    {
        while ((timeElapsed / ExpandTime) < 1)
        {
            timeElapsed += Time.deltaTime;
            LerpValue = Mathf.Lerp(0.5f, 0.8f, timeElapsed / ExpandTime);
            EggMesh.transform.localScale = new Vector3(LerpValue, LerpValue, LerpValue);
            yield return null;
        }
        DestroyEgg();
        StartCoroutine(SpawnBabyLurkers());
        StopCoroutine(StartEggExploding());

    }

    private void DestroyEgg()
    {
        EggMesh.enabled = false;
        InnerEggMesh.enabled = false;
        ExplodeParticle.Play();
        GetComponent<BoxCollider>().enabled = false;
        GetComponent<AudioSource>().PlayOneShot(Boom[EggRandom.Next(0, Boom.Length)]);
    }

    IEnumerator SpawnBabyLurkers()
    {
        while (iterator < BabiesToSpawn)
        {
            RoundManager.Instance.SpawnEnemyGameObject(SpawnPosition, 0, 1, BabyLurker);
            iterator++;
            yield return new WaitForSeconds(0.05f);
        }
        Destroy(this.gameObject);
    }
}
