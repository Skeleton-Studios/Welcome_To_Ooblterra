using HarmonyLib;
using LethalLevelLoader;
using System;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using UnityEditor;

namespace Welcome_To_Ooblterra.Patches;
    
internal class TerminalPatch {

    [HarmonyPatch(typeof(RoundManager), "Start")]
    [HarmonyPostfix]
    [HarmonyPriority(500)]
    private static void ChangeRouteConfirmText(RoundManager __instance) {
        ExtendedLevel Ooblterra = PatchedContent.ExtendedLevels.Find(x => x.selectableLevel.PlanetName == MoonPatch.MoonFriendlyName);
        Ooblterra.routeConfirmNode.displayText = "Routing autopilot to 523-Ooblterra.\r\nYour new balance is [playerCredits].\r\n\r\nRouting to external planets may take a while.\r\nPlease enjoy your flight.\r\n\r\n";
    }
    [HarmonyPatch(typeof(Terminal), "TextPostProcess")]
    [HarmonyPostfix]
    [HarmonyPriority(500)]
    private static void ChangeOoblterraWeatherCondition(string ModifiedDisplayText) {
        //This is the dirtiest possible way to perform this but it should work
        try {
            ModifiedDisplayText.Replace("Ooblterra (Foggy)", "Ooblterra (Hazy)");
        } catch { }
        try {
            ModifiedDisplayText.Replace("Ooblterra (Stormy)", "Ooblterra (Cyclonic)");
        } catch { }
        try {
            ModifiedDisplayText.Replace("Ooblterra (Flooded)", "Ooblterra (Hallucinogenic)");
        } catch { }

    }

}
