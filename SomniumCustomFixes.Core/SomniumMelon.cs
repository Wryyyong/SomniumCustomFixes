global using MelonLoader;

global using HarmonyLib;

global using UnityEngine;

using System.Linq;

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

	internal static HarmonyLib.Harmony HarmonyInst;
	static MelonLogger.Instance Logger;

	internal static MelonPreferences_Category PrefDebug;
	internal static MelonPreferences_Category PrefMisc;

	static MelonPreferences_Entry<bool> LogVerbose;

	internal static MelonPreferences_Category PrefCategoryInit(string categoryName) {
		var category = MelonPreferences.CreateCategory(categoryName);
		category.SetFilePath($"UserData/{ModTitle}.ini");

		return category;
	}

	internal static void EasyLog(params string[] logMsgs) {
		if (!LogVerbose.Value) return;

		foreach (var msg in logMsgs) {
			if (string.IsNullOrWhiteSpace(msg)) continue;

			Logger.Msg(msg);
		}
	}

	public override void OnInitializeMelon() {
		HarmonyInst = HarmonyInstance;
		Logger = LoggerInstance;

		PrefDebug = PrefCategoryInit("Debugging");
		PrefMisc = PrefCategoryInit("Miscellaneous");

		LogVerbose = PrefDebug.CreateEntry(
			"LogVerbose",
			false,
			"Verbose logging"
		);

		GetType().Assembly.GetTypes()
			.Select(type => type.GetMethod("Init",AccessTools.all))
			.ToList().ForEach(method => method?.Invoke(null,null));
	}
}
