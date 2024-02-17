using HarmonyLib;
using LethalLevelLoader;
using System;
using UnityEngine;
using Welcome_To_Ooblterra.Properties;

namespace Welcome_To_Ooblterra.Patches;
    
internal class TerminalPatch {

    private static readonly AssetBundle LevelBundle = WTOBase.LevelAssetBundle;
    private static Terminal ActiveTerminal;
    private static TerminalKeyword RouteKeyword;
    private static TerminalKeyword CancelKeyword;
    private static TerminalKeyword ConfirmKeyword;
    public static TerminalKeyword InfoKeyword { get; private set; }
    private static TerminalKeyword MoonTerminalWord;
    private const string TerminalPath = "Assets/CustomTerminal/";
    private static bool DontRun = false;

    //PATCHES
    [HarmonyPatch(typeof(StartOfRound), "Awake")]
    [HarmonyPostfix]
    private static void ExpandTerminal(StartOfRound __instance) {
        return;
        GrabActiveTerminal();
        AddMoonToList("Ooblterra");
        CreateRoute();
        //Resize InfoKeyword array and put our new info into it
        AddToKeyword(InfoKeyword, MoonTerminalWord, LevelBundle.LoadAsset<TerminalNode>(TerminalPath + "OoblterraInfo.asset"));
            
    }

    [HarmonyPatch(typeof(RoundManager), "Start")]
    [HarmonyPostfix]
    [HarmonyPriority(500)]
    private static void SwitchNodes(RoundManager __instance) {
        ExtendedLevel OoblterraRef = LevelBundle.LoadAsset<ExtendedLevel>("Assets/CustomMoon/OoblterraExtendedLevel.asset");
        ExtendedLevel Ooblterra = PatchedContent.ExtendedLevels.Find(x => x.selectableLevel == OoblterraRef.selectableLevel);
        Ooblterra.routeConfirmNode.displayText = "Routing autopilot to 523-Ooblterra.\r\nYour new balance is [playerCredits].\r\n\r\nRouting to external planets may take a while.\r\nPlease enjoy your flight.\r\n\r\n";
    }

    //METHODS
    public static void Start() {
        ExtendedLevel Ooblterra = LevelBundle.LoadAsset<ExtendedLevel>("Assets/CustomMoon/OoblterraExtendedLevel.asset");
        Ooblterra.infoNode = LevelBundle.LoadAsset<TerminalNode>(TerminalPath + "OoblterraInfo.asset");
        Ooblterra.routeNode = LevelBundle.LoadAsset<TerminalNode>(TerminalPath + "523route.asset");
        Ooblterra.routeConfirmNode = LevelBundle.LoadAsset<TerminalNode>(TerminalPath + "523routeConfirm.asset");
        PatchedContent.RegisterExtendedLevel(Ooblterra);
        //TODO: Should probably (on this and the suit path) make it so that all the assetbundle loading and assignment is done *here*, and
        //then any corresponding code is done when it needs to be done
    }
    private static void GrabActiveTerminal() {
        ActiveTerminal = GameObject.Find("TerminalScript").GetComponent<Terminal>(); //Terminal object reference 
        RouteKeyword = ActiveTerminal.terminalNodes.allKeywords[26];
        InfoKeyword = ActiveTerminal.terminalNodes.allKeywords[6];
        CancelKeyword = ActiveTerminal.terminalNodes.allKeywords[4];
        ConfirmKeyword = ActiveTerminal.terminalNodes.allKeywords[3];

    }
    public static void AddToKeyword(TerminalKeyword KeywordToAddTo, TerminalKeyword NewNoun, TerminalNode NewResult) {
        Array.Resize<CompatibleNoun>(ref KeywordToAddTo.compatibleNouns, KeywordToAddTo.compatibleNouns.Length + 1);
        KeywordToAddTo.compatibleNouns[KeywordToAddTo.compatibleNouns.Length - 1] = new CompatibleNoun {
            noun = NewNoun,
            result = NewResult
        };
    }       
    public static void AddMoonToList(String MoonName) {
        if (DontRun) {
            return;
        }
        Array.Resize<SelectableLevel>(ref ActiveTerminal.moonsCatalogueList, ActiveTerminal.moonsCatalogueList.Length + 1); //Resize list of moons in catalogue 
        ActiveTerminal.moonsCatalogueList[ActiveTerminal.moonsCatalogueList.Length - 1] = MoonPatch.MyNewMoon; //Add our moon to that list

        MoonTerminalWord = LevelBundle.LoadAsset<TerminalKeyword>(TerminalPath + "523-Ooblterra.asset"); //get our moon's Terminal Keyword 
        MoonTerminalWord.defaultVerb = RouteKeyword; //Set its default verb to "route"
        Array.Resize<TerminalKeyword>(ref ActiveTerminal.terminalNodes.allKeywords, ActiveTerminal.terminalNodes.allKeywords.Length + 1); //resize list of terminal keywords
        ActiveTerminal.terminalNodes.allKeywords[ActiveTerminal.terminalNodes.allKeywords.Length - 1] = MoonTerminalWord; //Add our keyword

        //rewrite the catalogue display text to add our moon's name to it
        TerminalNode MoonCatalogue = ActiveTerminal.terminalNodes.allKeywords[21].specialKeywordResult;
        MoonCatalogue.displayText.Substring(MoonCatalogue.displayText.Length - 3);
        MoonCatalogue.displayText = MoonCatalogue.displayText + "\n* " + MoonName + " [planetTime]" + "\n\n";

        //flag it so this code runs only once 
        DontRun = true;
        return;
    }
    public static void CreateRoute() {
        TerminalNode RouteNode = LevelBundle.LoadAsset<TerminalNode>(TerminalPath + "523route.asset");
        RouteNode.terminalOptions[0].noun = CancelKeyword;
        //RouteNode.terminalOptions[0].result = LevelBundle.LoadAsset<TerminalNode>(TerminalPath + "523CancelRoute.asset");
        RouteNode.terminalOptions[1].noun = ConfirmKeyword;

        //Resize our RouteKeyword array and put our new route confirmation into it
        AddToKeyword(RouteKeyword, MoonTerminalWord, RouteNode);
    }
}
