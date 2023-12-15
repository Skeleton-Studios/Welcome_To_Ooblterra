using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Welcome_To_Ooblterra.Patches;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches {
    
    
    internal class TerminalPatch {

        private static AssetBundle LevelBundle = WTOBase.LevelAssetBundle;
        private static bool DontRun = false;


        //Add the custom moon to the terminal
        [HarmonyPatch(typeof(StartOfRound), "Awake")]
        [HarmonyPostfix]
        private static void AddMoonToTerminal(StartOfRound __instance) {

            Terminal ActiveTerminal = GameObject.Find("TerminalScript").GetComponent<Terminal>(); //Terminal object reference 
            TerminalKeyword RouteKeyword = ActiveTerminal.terminalNodes.allKeywords[26];
            TerminalKeyword InfoKeyword = ActiveTerminal.terminalNodes.allKeywords[6];

            TerminalKeyword TerminalEntry = LevelBundle.LoadAsset<TerminalKeyword>("Assets/CustomScene/523-Ooblterra.asset"); //get our bundle's Terminal Keyword 
            TerminalEntry.defaultVerb = RouteKeyword;
            Array.Resize<SelectableLevel>(ref ActiveTerminal.moonsCatalogueList, ActiveTerminal.moonsCatalogueList.Length + 1); //Resize list of moons displayed 
            ActiveTerminal.moonsCatalogueList[ActiveTerminal.moonsCatalogueList.Length - 1] = MoonPatch.MyNewMoon; //Add our moon to that list
            Array.Resize<TerminalKeyword>(ref ActiveTerminal.terminalNodes.allKeywords, ActiveTerminal.terminalNodes.allKeywords.Length + 1);
            ActiveTerminal.terminalNodes.allKeywords[ActiveTerminal.terminalNodes.allKeywords.Length - 1] = TerminalEntry; //Add our terminal entry 
            TerminalEntry.defaultVerb = RouteKeyword; //Set its default verb to "route"


            TerminalNode RouteNode = LevelBundle.LoadAsset<TerminalNode>("Assets/CustomScene/523route.asset");
            RouteNode.terminalOptions[0].noun = ActiveTerminal.terminalNodes.allKeywords[4];
            RouteNode.terminalOptions[0].result = LevelBundle.LoadAsset<TerminalNode>("Assets/CustomScene/523CancelRoute.asset");
            RouteNode.terminalOptions[1].noun = ActiveTerminal.terminalNodes.allKeywords[3];

            //Resize our RouteKeyword array and put our new route confirmation into it
            Array.Resize<CompatibleNoun>(ref RouteKeyword.compatibleNouns, RouteKeyword.compatibleNouns.Length + 1);
            RouteKeyword.compatibleNouns[RouteKeyword.compatibleNouns.Length - 1] = new CompatibleNoun {
                noun = TerminalEntry,
                result = RouteNode
            };

            //Resize InfoKeyword array and put our new info into it
            Array.Resize<CompatibleNoun>(ref InfoKeyword.compatibleNouns, InfoKeyword.compatibleNouns.Length + 1);
            InfoKeyword.compatibleNouns[InfoKeyword.compatibleNouns.Length - 1] = new CompatibleNoun {
                noun = TerminalEntry,
                result = LevelBundle.LoadAsset<TerminalNode>("Assets/CustomScene/OoblterraInfo.asset")
            };
        }

        

        public static bool AddMoonToList() {
            if (DontRun) {
                return true;
            }

            IEnumerable<KeyValuePair<string, SelectableLevel>> moons = MoonPatch.Moons;
            Terminal ActiveTerminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            TerminalNode specialKeywordResult = ActiveTerminal.terminalNodes.allKeywords[21].specialKeywordResult;
            specialKeywordResult.displayText.Substring(specialKeywordResult.displayText.Length - 3);

            foreach (KeyValuePair<string, SelectableLevel> keyValuePair in moons) {
                TerminalNode terminalNode = specialKeywordResult;
                terminalNode.displayText = terminalNode.displayText + "\n* " + keyValuePair.Value.PlanetName + " [planetTime]";
            }

            TerminalNode terminalNode2 = specialKeywordResult;
            terminalNode2.displayText += "\n\n";
            DontRun = true;
            return true;
        }

    }
}
