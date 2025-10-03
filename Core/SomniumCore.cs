global using System;
global using System.Collections.Generic;
global using System.Reflection;

global using HarmonyLib;

global using UnityEngine;
global using uObject = UnityEngine.Object;

global using static SomniumCustomFixes.SomniumCore;
global using SomniumCustomFixes.Config;

using System.Linq;

[assembly: AssemblyProduct(ModTitle)]
[assembly: AssemblyTitle($"{ModTitle}.{GameTarget}")]

[assembly: AssemblyDescription($"A {LoaderTarget} mod to help with increasing the visual quality in \"{GameTitle}\".")]

[assembly: AssemblyCompany(ModAuthor)]
[assembly: AssemblyCopyright(ModAuthor)]

[assembly: AssemblyVersion(ModVersion)]
[assembly: AssemblyFileVersion(ModVersion)]

namespace SomniumCustomFixes;

static partial class SomniumCore {
	public const string ModTitle = "SomniumCustomFixes";
	public const string ModVersion = "1.0.0";
	public const string ModAuthor = "Wryyyong";
	public const string ModReverseDNS = $"org.{ModAuthor}.{ModTitle}";

	public const string GameDeveloper = "SpikeChunsoft";

	internal static ISomniumLoader Loader {
		get;
		private set {
			field = value;

			HarmonyInstance = value.HarmonyInstance;
			ConfHandler = value.ConfHandler;
		}
	}

	internal static HarmonyLib.Harmony HarmonyInstance;
	internal static ConfigHandler ConfHandler;

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
			false
		);

		typeof(SomniumCore).Assembly.GetTypes()
			.Select(static type => type.GetMethod("PatchInit",AccessTools.all))
			.ToList().ForEach(static method => method?.Invoke(null,null));

		ConfHandler.LoadConfig();
		ConfHandler.SaveConfig();
	}
}
