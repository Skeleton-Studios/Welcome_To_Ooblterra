using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;
using System;
using LC_API.BundleAPI;
using MOON_API;
using UnityEngine;


namespace Welcome_To_Ooblterra.Patches {
    internal class MoonPatch {
        // Token: 0x06000006 RID: 6 RVA: 0x000020AC File Offset: 0x000002AC
        [HarmonyPatch (typeof (StartOfRound), "Awake")]
        [HarmonyPrefix]
        private static bool AwakePatch(StartOfRound __instance) {
            Core.AddMoon (new SelectableLevel {
                LevelDescription = "POPULATION: Entirely wiped out.\nCONDITIONS: Hazardous. Deadly air, mostly clear skies. Always nighttime. The ground is alive.\nFAUNA: The unique conditions of Ooblterra allow for lots of strange beings.",
                planetHasTime = true,
                name = "Ooblterra",
                PlanetName = "Ooblterra",
                sceneName = "InitSceneLaunchOptions",
                riskLevel = "Dangerous",
                spawnEnemiesAndScrap = true,
                timeToArrive = 10f,
                planetPrefab = __instance.GetComponent<StartOfRound> ().levels[0].planetPrefab
            }, BundleLoader.GetLoadedAsset<GameObject> ("assets/lcconstruct/main.prefab"));
            return true;
        }

        // Token: 0x06000007 RID: 7 RVA: 0x00002140 File Offset: 0x00000340
        [HarmonyPatch (typeof (StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void AwakePatch2(StartOfRound __instance) {
            GameObject gameObject = GameObject.Find ("TerminalScript");
            Terminal component = gameObject.GetComponent<Terminal> ();
            TerminalKeyword terminalKeyword = new TerminalKeyword ();
            terminalKeyword.name = "Ooblterra";
            terminalKeyword.word = "Ooblterra";
            int num = component.moonsCatalogueList.Length;
            Array.Resize<SelectableLevel> (ref component.moonsCatalogueList, num + 1);
            component.moonsCatalogueList[num] = Core.GetMoons ()["Ooblterra"];
            Array.Resize<TerminalKeyword> (ref component.terminalNodes.allKeywords, component.terminalNodes.allKeywords.Length + 1);
            component.terminalNodes.allKeywords[component.terminalNodes.allKeywords.Length - 1] = terminalKeyword;
            TerminalNode terminalNode = new TerminalNode ();
            terminalNode.name = "Ooblterra2";
            terminalNode.itemCost = 0;
            terminalNode.displayText = "Routing to Ooblterra... Travel to new planets may take a while.";
            terminalNode.buyRerouteToMoon = Core.ModdedIds["Ooblterra"];
            terminalNode.clearPreviousText = true;
            terminalNode.buyUnlockable = false;
            CompatibleNoun compatibleNoun = new CompatibleNoun ();
            compatibleNoun.noun = component.terminalNodes.allKeywords[3];
            compatibleNoun.result = terminalNode;
            TerminalNode terminalNode2 = new TerminalNode ();
            terminalNode2.name = "Ooblterra1";
            terminalNode2.buyItemIndex = -1;
            terminalNode2.clearPreviousText = true;
            terminalNode2.buyUnlockable = false;
            terminalNode2.displayText = "Are you certain you want to go to Ooblterra?\n\nPlease CONFIRM or DENY.\n\n";
            terminalNode2.isConfirmationNode = false;
            terminalNode2.itemCost = 0;
            terminalNode2.overrideOptions = true;
            terminalNode2.maxCharactersToType = 15;
            terminalNode2.terminalOptions = new CompatibleNoun[]
            {
                compatibleNoun,
                component.terminalNodes.allKeywords[26].compatibleNouns[0].result.terminalOptions[1]
            };
            CompatibleNoun compatibleNoun2 = new CompatibleNoun ();
            compatibleNoun2.noun = terminalKeyword;
            compatibleNoun2.result = terminalNode2;
            terminalKeyword.defaultVerb = component.terminalNodes.allKeywords[26];
            Array.Resize<CompatibleNoun> (ref component.terminalNodes.allKeywords[26].compatibleNouns, component.terminalNodes.allKeywords[26].compatibleNouns.Length + 1);
            component.terminalNodes.allKeywords[26].compatibleNouns[component.terminalNodes.allKeywords[26].compatibleNouns.Length - 1] = compatibleNoun2;
            component.terminalNodes.allKeywords[component.terminalNodes.allKeywords.Length - 1] = terminalKeyword;
        }
    }
}
