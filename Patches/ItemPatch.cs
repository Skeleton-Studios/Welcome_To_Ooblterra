using HarmonyLib;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches
{
    internal class ItemPatch
    {
        private static AudioClip CachedDiscoBallMusic;
        private static AudioClip OoblterraDiscoMusic;

        private const string ItemPath = "CustomItems/";

        [HarmonyPatch(typeof(CozyLights), nameof(CozyLights.SetAudio))]
        [HarmonyPrefix]
        private static bool ReplaceDiscoBall(CozyLights __instance)
        {
            if (StartOfRound.Instance.currentLevel.PlanetName != MoonPatch.MoonFriendlyName)
            {
                //set the disco ball music back to the default
                if (__instance.turnOnAudio != null)
                {
                    __instance.turnOnAudio.clip = CachedDiscoBallMusic;
                }
                return true;
            }
            if (__instance.turnOnAudio != null)
            {
                __instance.turnOnAudio.clip = OoblterraDiscoMusic;
            }
            return true;
        }

        //METHODS
        public static void Start()
        {
            CachedDiscoBallMusic = WTOBase.ContextualLoadAsset<AudioClip>(ItemPath + "Boombox6QuestionMark.ogg", false);
            OoblterraDiscoMusic = WTOBase.ContextualLoadAsset<AudioClip>(ItemPath + "ooblboombox.ogg", false);
        }
    }
}