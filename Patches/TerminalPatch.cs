using HarmonyLib;
using LethalLevelLoader;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using System.Collections.Generic;
using System.Linq;

namespace Welcome_To_Ooblterra.Patches
{
    internal class TerminalPatch {

        public static Dictionary<int, int> LogDictionary = new();
        private const string TerminalPath = "CustomTerminal/";
        private static List<TerminalKeyword> KeywordList;
        private static List<TerminalNode> NodeList = new();

        private static readonly WTOBase.WTOLogger Log = new(typeof(TerminalPatch), LogSourceType.Generic);

        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        [HarmonyPostfix]
        private static void AddLogs() {
            LoadLogKeywords();
            LoadLogNodes();
            LoadStoryLogs();
        }

        private static void LoadLogKeywords(){
            KeywordList = new() 
            {
                WTOBase.ContextualLoadAsset<TerminalKeyword>(TerminalPath + "WTOLogFile1Keyword.asset"),
                WTOBase.ContextualLoadAsset<TerminalKeyword>(TerminalPath + "WTOLogFile2Keyword.asset"),
                WTOBase.ContextualLoadAsset<TerminalKeyword>(TerminalPath + "WTOLogFile3Keyword.asset"),
                WTOBase.ContextualLoadAsset<TerminalKeyword>(TerminalPath + "WTOLogFile4Keyword.asset"),
                WTOBase.ContextualLoadAsset<TerminalKeyword>(TerminalPath + "WTOLogFile5Keyword.asset")
            };
        }
        private static void LoadLogNodes() {
            NodeList = new()
            {
                WTOBase.ContextualLoadAsset<TerminalNode>(TerminalPath + "WTOLogFile1.asset"),
                WTOBase.ContextualLoadAsset<TerminalNode>(TerminalPath + "WTOLogFile2.asset"),
                WTOBase.ContextualLoadAsset<TerminalNode>(TerminalPath + "WTOLogFile3.asset"),
                WTOBase.ContextualLoadAsset<TerminalNode>(TerminalPath + "WTOLogFile4.asset"),
                WTOBase.ContextualLoadAsset<TerminalNode>(TerminalPath + "WTOLogFile5.asset")
            };
        }

        private static void LoadStoryLogs() {
            try {
                GameObject.FindObjectOfType<Terminal>().logEntryFiles.First(x => x.creatureName == NodeList[0].creatureName);
                return;
            } catch {
                Log.Info("WTO Story logs not found in list. Attempting to add...");
            }
            //Grab the last index of this list
            int NextIndex = GameObject.FindObjectOfType<Terminal>().logEntryFiles.Count;

            TerminalKeyword ViewKeyword = GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[19];
            List<int> IDList = new() {
                5231111,
                5231112,
                5231113,
                5231114,
                5231115
            };
            bool SkipNouns = false;
            foreach (TerminalKeyword NextKeyword in GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords) {
                if (NextKeyword.word == "mack1") {
                    SkipNouns = true;
                }
            }
            for (int i = 0; i < NodeList.Count; i++) {
                Log.Debug($"nextIndex = {NextIndex}");
                KeywordList[i].defaultVerb = ViewKeyword;
                NodeList[i].storyLogFileID = NextIndex;
                GameObject.FindObjectOfType<Terminal>().logEntryFiles.Add(NodeList[i]);
                try {
                    LogDictionary.Add(IDList[i], NextIndex);
                } catch {}
            
                if (SkipNouns) {
                    continue;
                }
                ViewKeyword.AddCompatibleNoun(KeywordList[i], NodeList[i]);
                NextIndex++;
            }
            List<TerminalKeyword> list_out = new();
            list_out.AddRange(GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords);
            list_out.AddRange(KeywordList);
            GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords = list_out.ToArray();
            Log.Info("END ADD WTO STORY LOGS!");
        }
    }
}
