using System;
using System.Reflection;

using Il2CppGame;

using UnityEngine.UI;

namespace SomniumCustomFixes;

[HarmonyPatch]
static class UltrawideFixes {
	static readonly Dictionary<Type,List<string>> TargetsExtend = new() {
		{typeof(CinemaScope),[
			nameof(CinemaScope.Show),
		]},
		{typeof(FilterController),[
			nameof(FilterController.Black),
			/* Il2CppInterop currently doesn't like nullable parameters for some reason
			nameof(FilterController.FadeIn),
			nameof(FilterController.FadeInWait),
			nameof(FilterController.FadeOut),
			nameof(FilterController.FadeOutWait),
			nameof(FilterController.Flash),
			nameof(FilterController.Set),
			//*/
			nameof(FilterController.SetValue),
		]},
		{typeof(EyeFadeFilter),[
			nameof(EyeFadeFilter.FadeIn),
			nameof(EyeFadeFilter.FadeInWait),
			nameof(EyeFadeFilter.FadeOut),
			nameof(EyeFadeFilter.FadeOutWait),
		]},
	};

	static MelonPreferences_Entry<bool> DoUltrawideFixes;
	static MelonPreferences_Entry<int> ResWidth;
	static MelonPreferences_Entry<int> ResHeight;

	const float AspectRatioNative = 16f / 9f;

	static float AspectRatioCustom;
	static float AspectRatioMultiplier;

	static bool ShouldBotherWithFixes;

	static readonly Vector3 UWBase = new(1f,1f,1f);
	static Vector3 UWExtend = new(1f,1f,1f);
	static Vector3 UWHorizontal = new(1f,1f,1f);

	internal static void Init() {
		MelonPreferences_Category settings = SomniumMelon.Settings;
		Display displayMain = Display.main;

		ResWidth = settings.CreateEntry(
			"CustomResolutionWidth",
			displayMain.systemWidth,
			"Custom resolution (width)"
		);
		ResHeight = settings.CreateEntry(
			"CustomResolutionHeight",
			displayMain.systemHeight,
			"Custom resolution (height)"
		);
		DoUltrawideFixes = settings.CreateEntry(
			"DoUltrawideFixes",
			true,
			"Fix ultrawide UI issues"
		);

		ResWidth.OnEntryValueChanged.Subscribe(ResolutionChanged);
		ResHeight.OnEntryValueChanged.Subscribe(ResolutionChanged);
		DoUltrawideFixes.OnEntryValueChanged.Subscribe(SetShouldBother);

		ResolutionChanged(0,0);
	}

	static readonly LemonAction<bool,bool> SetShouldBother = static (_,_) =>
		ShouldBotherWithFixes = DoUltrawideFixes.Value && AspectRatioCustom > AspectRatioNative;

	static readonly LemonAction<int,int> ResolutionChanged = static (_,_) => {
		AspectRatioCustom = (float)ResWidth.Value / ResHeight.Value;
		AspectRatioMultiplier = AspectRatioCustom / AspectRatioNative;

		UWExtend.x = 1f * AspectRatioMultiplier;
		UWHorizontal.x = 1f / AspectRatioMultiplier;

		SetCustomResolution();
		SetShouldBother(false,false);
	};

	[HarmonyPatch(typeof(LauncherArgs),nameof(LauncherArgs.OnRuntimeMethodLoad))]
	[HarmonyPostfix]
	static void SetCustomResolution() =>
		Screen.SetResolution(ResWidth.Value,ResHeight.Value,Screen.fullScreenMode);

	[HarmonyPatch(typeof(CanvasScaler),nameof(CanvasScaler.OnEnable))]
	[HarmonyPostfix]
	static void FixScreenMatchMode(CanvasScaler __instance) {
		if (!ShouldBotherWithFixes) return;

		__instance.m_ScreenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
	}

	[HarmonyPatch]
	private static class FilterExtend {
		static IEnumerable<MethodBase> TargetMethods() {
			foreach (KeyValuePair<Type,List<string>> target in TargetsExtend) {
				Type type = target.Key;

				foreach (string methodName in target.Value)
					yield return AccessTools.Method(type,methodName);
			}
		}

		static void Postfix(Component __instance) {
			if (!ShouldBotherWithFixes) return;

			__instance.transform.localScale = UWExtend;
		}
	}

	[HarmonyPatch(typeof(VideoController),nameof(VideoController.Prepare))]
	[HarmonyPatch(typeof(VideoController),nameof(VideoController.Stop))]
	[HarmonyPostfix]
	static void FixViewport(MethodBase __originalMethod,VideoController __instance) {
		if (!ShouldBotherWithFixes) return;

		Vector3 targetVector;

		switch (__originalMethod.Name) {
			case nameof(VideoController.Prepare):
				targetVector = UWHorizontal;
				break;

			case nameof(VideoController.Stop):
				targetVector = UWBase;
				break;

			default:
				return;
		}

		__instance.world.Image.transform.localScale = targetVector;
	}
}
