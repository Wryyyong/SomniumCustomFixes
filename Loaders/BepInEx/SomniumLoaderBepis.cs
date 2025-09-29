using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

using SomniumCustomFixes;

namespace SomniumCustomFixes;

[BepInPlugin(SomniumCore.ModReverseDNS,SomniumCore.ModTitle,SomniumCore.ModVersion)]
[BepInProcess($"{SomniumCore.GameTarget}.exe")]

sealed class SomniumLoaderBepis : BasePlugin,ISomniumLoader {
	Harmony ISomniumLoader.HarmonyInstance => new(SomniumCore.ModReverseDNS);
	ConfigHandler ISomniumLoader.ConfigHandler => new ConfigHandlerBepis();
	Action<string> ISomniumLoader.LogMsg => Log.LogMessage;

	public override void Load() => SomniumCore.Init(this);
}
