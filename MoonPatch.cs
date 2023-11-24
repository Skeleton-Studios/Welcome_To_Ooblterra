using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using System;
using LC_API.BundleAPI;
using WonderAPI;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using System.IO;
using System.Reflection;
using GameNetcodeStuff;
using System.Collections;
using System.Reflection.Emit;
using LethalLib.Modules;
using static LethalLib.Modules.Levels;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace Welcome_To_Ooblterra.Patches {
    internal class MoonPatch {
        //Identifiers for the Moon
        private const string MoonFriendlyName = "Ooblterra";
        private const string MoonDescription = "POPULATION: Entirely wiped out.\nCONDITIONS: Hazardous. Deadly air, mostly clear skies. Always nighttime. The ground is alive.\nFAUNA: The unique conditions of Ooblterra allow for lots of strange beings.";
        private const string MoonRiskLevel = "A";
        private const string MoonConfirmation = "Are you certain you want to go to Ooblterra?\n\nPlease CONFIRM or DENY.\n\n";
        private const string MoonTravelText = "Routing to Ooblterra... Travel to new planets may take a while.";
        private const string MoonPrefabDir = "";

        //Defining the custom moon for the API
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPrefix]
        private static bool AddMoonToList(StartOfRound __instance) {
            SelectableLevel vow = __instance.GetComponent<StartOfRound>().levels[2];
            //Create new moon based on vow
            SelectableLevel MyNewMoon = vow;
            
            //override relevant properties
            MyNewMoon.PlanetName = MoonFriendlyName;
            MyNewMoon.name = MoonFriendlyName;
            MyNewMoon.LevelDescription = MoonDescription;
            MyNewMoon.riskLevel = MoonRiskLevel;
            MyNewMoon.timeToArrive = 3f;

            //Add moon to API
            Core.AddMoon(MyNewMoon);
            return true;
        }

        //Add the custom moon to the terminal
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void AddMoonToTerminal(StartOfRound __instance) {

            GameObject gameObject = GameObject.Find("TerminalScript");
            Terminal component = gameObject.GetComponent<Terminal>();
            TerminalKeyword terminalKeyword = new TerminalKeyword();
            terminalKeyword.name = MoonFriendlyName;
            terminalKeyword.word = MoonFriendlyName.ToLower();
            int num = component.moonsCatalogueList.Length;
            Array.Resize<SelectableLevel>(ref component.moonsCatalogueList, num + 1);
            component.moonsCatalogueList[num] = Core.GetMoons()[MoonFriendlyName];
            Array.Resize<TerminalKeyword>(ref component.terminalNodes.allKeywords, component.terminalNodes.allKeywords.Length + 1);
            component.terminalNodes.allKeywords[component.terminalNodes.allKeywords.Length - 1] = terminalKeyword;
            TerminalNode terminalNode = new TerminalNode();
            terminalNode.name = MoonFriendlyName;
            terminalNode.itemCost = 0;
            terminalNode.displayText = MoonTravelText;
            terminalNode.buyRerouteToMoon = Core.ModdedIds[MoonFriendlyName];
            terminalNode.clearPreviousText = true;
            terminalNode.buyUnlockable = false;
            CompatibleNoun compatibleNoun = new CompatibleNoun();
            compatibleNoun.noun = component.terminalNodes.allKeywords[3];
            compatibleNoun.result = terminalNode;
            TerminalNode terminalNode2 = new TerminalNode();
            terminalNode2.name = MoonFriendlyName;
            terminalNode2.buyItemIndex = -1;
            terminalNode2.clearPreviousText = true;
            terminalNode2.buyUnlockable = false;
            terminalNode2.displayText = MoonConfirmation;
            terminalNode2.isConfirmationNode = false;
            terminalNode2.itemCost = 0;
            terminalNode2.overrideOptions = true;
            terminalNode2.maxCharactersToType = 15;
            terminalNode2.terminalOptions = new CompatibleNoun[]
            {
                compatibleNoun,
                component.terminalNodes.allKeywords[26].compatibleNouns[0].result.terminalOptions[1]
            };
            CompatibleNoun compatibleNoun2 = new CompatibleNoun();
            compatibleNoun2.noun = terminalKeyword;
            compatibleNoun2.result = terminalNode2;
            terminalKeyword.defaultVerb = component.terminalNodes.allKeywords[26];
            Array.Resize<CompatibleNoun>(ref component.terminalNodes.allKeywords[26].compatibleNouns, component.terminalNodes.allKeywords[26].compatibleNouns.Length + 1);
            component.terminalNodes.allKeywords[26].compatibleNouns[component.terminalNodes.allKeywords[26].compatibleNouns.Length - 1] = compatibleNoun2;
            component.terminalNodes.allKeywords[component.terminalNodes.allKeywords.Length - 1] = terminalKeyword;
        }

        //Destroy the necessary actors and set our scene
        [HarmonyPatch(typeof(StartOfRound), "SceneManager_OnLoadComplete1")]
        [HarmonyPostfix]
        private static void StartGameReplaceActors(StartOfRound __instance) {
            if (__instance.currentLevel.name == MoonFriendlyName) {
                WTOBase.PrintToConsole("Loading into level " + MoonFriendlyName);
                GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
                GameObject[] trees = new GameObject[] {};
                //Destroy all trees (currently not working?)
                foreach (GameObject obj in allObjects) {
                    if (obj.name.Contains("tree")) {
                        GameObject.Destroy(obj);
                    }
                }
                GameObject TerrainObject = GameObject.Find("CompletedVowTerrain");
                WTOBase.PrintToConsole("Destroying " + TerrainObject.name);
                GameObject.Destroy(TerrainObject);
                GameObject FogObject = GameObject.Find();
                WTOBase.PrintToConsole("Destroying " + FogObject.name);
                GameObject.Destroy(FogObject);

                GameObject MyLevelAsset = WTOBase.MyAssets.LoadAsset(WTOBase.GetBundledAssetPath()) as GameObject;
                GameObject MyInstantiatedLevel = GameObject.Instantiate(MyLevelAsset);
                WTOBase.PrintToConsole("Loaded custom object!");
                /*
                 * I can access things added to the custom prefab programatically. Honestly, this can probably  
                 * be used to add custom teleport doors and shit without having to worry about the script being 
                 * in the prefab. Can just add it here. Though it's probably a better idea to have a class that
                 * marks a transform and teleport one of the already working doors to it. :)
                 */
            }
        }
    }
}
