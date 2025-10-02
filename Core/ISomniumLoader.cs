namespace SomniumCustomFixes;

interface ISomniumLoader {
	internal HarmonyLib.Harmony HarmonyInstance { get; }
	internal ConfigHandler ConfHandler { get; }
	internal Action<string> LogMsg { get; }
}
