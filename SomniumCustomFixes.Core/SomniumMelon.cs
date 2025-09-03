global using System.Collections.Generic;

global using MelonLoader;

global using HarmonyLib;

global using UnityEngine;

using SomniumCustomFixes;

[assembly: MelonInfo(typeof(SomniumMelon),SomniumMelon.ModTitle,"1.0.0","Wryyyong")]
[assembly: MelonGame("SpikeChunsoft",SomniumMelon.ModTarget)]
[assembly: VerifyLoaderVersion(0,6,0,true)]

namespace SomniumCustomFixes;

class SomniumMelon : MelonMod {
	public const string ModTitle = "SomniumCustomFixes";
	public const string ModTarget =
	#if AITSF
		"AI_TheSomniumFiles"
	#elif AINI
		"AI_TheSomniumFiles2"
	#elif AINS
		"NoSleepForKanameDate"
	#endif
	;

	static MelonLogger.Instance Logger;

	internal static MelonPreferences_Category Settings = MelonPreferences.CreateCategory(ModTitle);

	static readonly MelonPreferences_Entry<bool> LogVerbose = Settings.CreateEntry(
		"LogVerbose",
		false,
		"Enable debug mode",
		"Set to true to enable verbose logging"
	);

	internal static void EasyLog(params string[] logMsgs) {
		if (!LogVerbose.Value) return;

		foreach (string msg in logMsgs) {
			if (string.IsNullOrWhiteSpace(msg)) continue;

			Logger.Msg(msg);
		}
	}

	public override void OnInitializeMelon() {
		Logger = LoggerInstance;

		DisableMouseCursor.Init();
		GeneralQualityFixes.Init();
		UACDFixes.Init();
		UltrawideFixes.Init();
	}
}
