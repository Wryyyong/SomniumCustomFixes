global using System;
global using System.Collections.Generic;
global using System.Reflection;

global using HarmonyLib;

global using UnityEngine;
global using uObject = UnityEngine.Object;

global using SomniumCustomFixes.Config;

using System.Linq;

namespace SomniumCustomFixes;

static class SomniumCore {
	public const string ModTitle = "SomniumCustomFixes";
	public const string ModVersion = "1.0.0";
	public const string ModAuthor = "Wryyyong";

	public const string GameDeveloper = "SpikeChunsoft";
	public const string GameTarget =
	#if AITSF
		"AI_TheSomniumFiles"
	#elif AINI
		"AI_TheSomniumFiles2"
	#elif AINS
		"NoSleepForKanameDate"
	#endif
	;

	internal static ISomniumLoader Loader {
		get;
		private set {
			field = value;

			HarmonyInstance = value.HarmonyInstance;
			ConfigHandler = value.ConfigHandler;
		}
	}

	internal static HarmonyLib.Harmony HarmonyInstance;
	internal static ConfigHandler ConfigHandler;

	static ConfigElement<bool> LogVerbose;

	internal static void EasyLog(params string[] logMsgs) {
		if (!LogVerbose.Value) return;

		foreach (var msg in logMsgs) {
			if (string.IsNullOrWhiteSpace(msg)) continue;

			Loader.LogMsg(msg);
		}
	}

	internal static void Init(ISomniumLoader loader) {
		if (Loader is not null)
			throw new Exception($"Tried to call SomniumCore.Init again");

		Loader = loader;

		LogVerbose = new(
			"Debugging",
			"LogVerbose",
			false,
			"Verbose logging"
		);

		typeof(SomniumCore).Assembly.GetTypes()
			.Select(static type => type.GetMethod("PatchInit",AccessTools.all))
			.ToList().ForEach(static method => method?.Invoke(null,null));

		ConfigHandler.LoadConfig();
		ConfigHandler.SaveConfig();
	}
}
