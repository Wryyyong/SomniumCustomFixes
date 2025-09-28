using MelonLoader;

using SomniumCustomFixes;

[assembly: MelonInfo(typeof(SomniumLoaderMelon),SomniumCore.ModTitle,SomniumCore.ModVersion,SomniumCore.ModAuthor)]
[assembly: MelonGame(SomniumCore.GameDeveloper,SomniumCore.GameTarget)]

[assembly: VerifyLoaderVersion(0,6,0,true)]
[assembly: MelonPlatformDomain(MelonPlatformDomainAttribute.CompatibleDomains.IL2CPP)]

[assembly: MelonColor(255,224,140,255)]
[assembly: MelonAuthorColor(255,224,140,255)]

namespace SomniumCustomFixes;

sealed class SomniumLoaderMelon : MelonMod,ISomniumLoader {
	HarmonyLib.Harmony ISomniumLoader.HarmonyInstance => HarmonyInstance;
	ConfigHandler ISomniumLoader.ConfigHandler => new ConfigHandlerMelon();
	Action<string> ISomniumLoader.LogMsg => LoggerInstance.Msg;

	public override void OnInitializeMelon() => SomniumCore.Init(this);
}
