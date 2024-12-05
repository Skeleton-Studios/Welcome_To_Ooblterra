using HarmonyLib;
using LethalLevelLoader;
using System;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Welcome_To_Ooblterra.Patches;
    
internal class TerminalPatch {

    public static Dictionary<int, int> LogDictionary = [];
    private const string TerminalPath = WTOBase.RootPath + "CustomTerminal/";
    private static List<TerminalKeyword> KeywordList;
    private static List<TerminalNode> NodeList = [];

    [HarmonyPatch(typeof(StartOfRound), "Start")]
    [HarmonyPostfix]
    private static void AddLogs() {
        LoadLogKeywords();
        LoadLogNodes();
        LoadStoryLogs();
    }

    private static void LoadLogKeywords(){
        KeywordList = [
            WTOBase.ContextualLoadAsset<TerminalKeyword>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile1Keyword.asset"),
            WTOBase.ContextualLoadAsset<TerminalKeyword>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile2Keyword.asset"),
            WTOBase.ContextualLoadAsset<TerminalKeyword>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile3Keyword.asset"),
            WTOBase.ContextualLoadAsset<TerminalKeyword>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile4Keyword.asset"),
            WTOBase.ContextualLoadAsset<TerminalKeyword>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile5Keyword.asset")
        ];
    }
    private static void LoadLogNodes() {
        NodeList = [
            WTOBase.ContextualLoadAsset<TerminalNode>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile1.asset"),
            WTOBase.ContextualLoadAsset<TerminalNode>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile2.asset"),
            WTOBase.ContextualLoadAsset<TerminalNode>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile3.asset"),
            WTOBase.ContextualLoadAsset<TerminalNode>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile4.asset"),
            WTOBase.ContextualLoadAsset<TerminalNode>(MoonPatch.LevelBundle, TerminalPath + "WTOLogFile5.asset")
        ];
    }

    private static void LoadStoryLogs() {
        try {
            GameObject.FindObjectOfType<Terminal>().logEntryFiles.First(x => x.creatureName == NodeList[0].creatureName);
            return;
        } catch {
            WTOBase.LogToConsole("WTO Story logs not found in list. Attempting to add...");
        }
        //Grab the last index of this list
        int NextIndex = GameObject.FindObjectOfType<Terminal>().logEntryFiles.Count;

        TerminalKeyword ViewKeyword = GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords[19];
        List<int> IDList = [ 
            5231111,
            5231112,
            5231113,
            5231114,
            5231115
        ];
        bool SkipNouns = false;
        foreach (TerminalKeyword NextKeyword in GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords) {
            if (NextKeyword.word == "mack1") {
                SkipNouns = true;
            }
        }
        for (int i = 0; i < NodeList.Count; i++) {
            WTOBase.LogToConsole($"nextIndex = {NextIndex}");
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
        GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords = GameObject.FindObjectOfType<Terminal>().terminalNodes.allKeywords.Concat(KeywordList).ToArray();
        WTOBase.LogToConsole("END ADD WTO STORY LOGS!");
    }
}
