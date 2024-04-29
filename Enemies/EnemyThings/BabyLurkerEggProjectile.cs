using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;
using static UnityEngine.GraphicsBuffer;

namespace Welcome_To_Ooblterra.Enemies.EnemyThings;
public class BabyLurkerEggProjectile : NetworkBehaviour {

    public int TargetID = 0;
    public int BabiesToSpawn = 35;
    private Vector3 SpawnPosition;
    private EnemyType BabyLurker;
    private int iterator = 0;
    private float TimeTillExplode = 3f;
    public MeshRenderer EggMesh;
    public AudioClip Splat;
     
    private void OnTriggerEnter(Collider other) {
        WTOBase.LogToConsole($"Collision registered! Collider: {other.gameObject}");
        if (other.GetComponent<BoxCollider>() != null || other.GetComponent<BabyLurkerAI>() != null || other.GetComponent<BabyLurkerProjectile>() != null) {
            return;
        } 
        BabyLurker = MonsterPatch.InsideEnemies.First(x => x.enemyType.enemyName == "Baby Lurker").enemyType;
        SpawnPosition = RoundManager.Instance.GetRandomNavMeshPositionInRadius(transform.position, radius: 1);
        if(SpawnPosition == transform.position) {
            return;
        }
        GetComponent<AudioSource>().PlayOneShot(Splat);
        GetComponent<Rigidbody>().isKinematic = true;
        StartCoroutine(StartEggExploding());
    }

    private float timeElapsed;
    private float ExpandTime = 2f;
    private float LerpValue;
    IEnumerator StartEggExploding() {
        while ((timeElapsed / ExpandTime) < 1) {
            timeElapsed += Time.deltaTime;
            LerpValue = Mathf.Lerp(0.5f, 0.8f, timeElapsed / ExpandTime);
            EggMesh.transform.localScale = new Vector3(LerpValue, LerpValue, LerpValue);
            yield return null;
        }
        StartCoroutine(SpawnBabyLurkers());
        DestroyEgg();
        StopCoroutine(StartEggExploding());
    }

    private void DestroyEgg() {
        GetComponent<MeshRenderer>().enabled = false;
        GetComponent<BoxCollider>().enabled = false;
    }

    IEnumerator SpawnBabyLurkers() {
        while(iterator < BabiesToSpawn) {
            RoundManager.Instance.SpawnEnemyGameObject(SpawnPosition, 0, 1, BabyLurker);
            iterator++;
            yield return new WaitForSeconds(0.05f);
        }
        Destroy(this.gameObject);
    }
}
