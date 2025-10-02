using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;

namespace SomniumCustomFixes;

[BepInPlugin(ModReverseDNS,ModTitle,ModVersion)]
[BepInProcess($"{GameTarget}.exe")]

sealed class SomniumLoaderBepis : BasePlugin,ISomniumLoader {
	Harmony ISomniumLoader.HarmonyInstance => new(ModReverseDNS);
	ConfigHandler ISomniumLoader.ConfHandler => new ConfigHandlerBepis();
	Action<string> ISomniumLoader.LogMsg => Log.LogMessage;

	public override void Load() => Init(this);
}
