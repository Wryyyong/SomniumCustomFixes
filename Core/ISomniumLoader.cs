namespace SomniumCustomFixes;

interface ISomniumLoader {
	internal HarmonyLib.Harmony HarmonyInstance { get; }
	internal ConfigHandler ConfigHandler { get; }
	internal Action<string> LogMsg { get; }
}
