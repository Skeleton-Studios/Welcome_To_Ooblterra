using System.Collections;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using Welcome_To_Ooblterra.Things;

namespace Welcome_To_Ooblterra.RoomBehaviors;
public class FrankensteinVisuals : MonoBehaviour
{

    [InspectorName("SceneAnim")]
    public AudioClip ReviveSound;
    public AudioSource ReviveSoundPlayer;
    public Animator CoilAnim;
    public ParticleSystem[] LightningParticles;
    public LightComponent[] Lights;

    private bool ShouldAnimate;
    private bool AnimStarted;
    private bool AnimStopped;
    private System.Random MyRandom;

    private void Start()
    {
        MyRandom = new System.Random(StartOfRound.Instance.randomMapSeed);
    }
    private void Update()
    {
        if (ShouldAnimate)
        {
            StartCoroutine(VisualsHandler());
        }
    }

    public void StartVisuals()
    {
        WTOBase.LogToConsole("Visuals script starting coroutine ...");
        ShouldAnimate = true;
    }
    IEnumerator VisualsHandler()
    {
        if (!AnimStarted)
        {
            ReviveSoundPlayer.clip = ReviveSound;
            ReviveSoundPlayer.Play();
            CoilAnim.SetTrigger("HeatCoils");
            foreach (ParticleSystem LightningBolt in LightningParticles)
            {
                LightningBolt.Play();
            }
            AnimStarted = true;
        }
        if (!AnimStopped)
        {
            foreach (LightComponent Light in Lights)
            {
                Light.SetLightBrightness((MyRandom.Next(0, 10) % 2 == 0) ? 200 : 0);
            }
        }
        yield return new WaitForSeconds(3.4f);
        if (!AnimStopped)
        {
            foreach (ParticleSystem LightningBolt in LightningParticles)
            {
                LightningBolt.Stop();

            }
            foreach (LightComponent Light in Lights)
            {
                Light.SetLightBrightness(0);
            }
            AnimStopped = true;
        }
    }
}
