using System;
using System.Reflection;

using Il2CppGame;

using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SomniumCustomFixes;

[HarmonyPatch]
static class UltrawideFixes {
	static readonly Dictionary<Type,string[]> TargetsExtend = new() {
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

	static readonly Dictionary<Scene,Dictionary<Component,Vector3>> Cache = [];
	static Vector3 UWExtend = new(1f,1f,1f);
	static Vector3 UWHorizontal = new(1f,1f,1f);

	static void Init() {
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

	static readonly LemonAction<bool,bool> SetShouldBother = static (_,_) => {
		bool newVal = DoUltrawideFixes.Value && AspectRatioCustom > AspectRatioNative;
		ShouldBotherWithFixes = newVal;

		if (newVal) return;

		foreach (Dictionary<Component,Vector3> dict in Cache.Values)
			foreach (KeyValuePair<Component,Vector3> set in dict)
				set.Key.transform.localScale = set.Value;
	};

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

	static Vector3 CacheComponent(Component component) {
		Dictionary<Component,Vector3> cacheScene = Cache[component.gameObject.scene];

		if (!cacheScene.ContainsKey(component))
			cacheScene.Add(component,component.transform.localScale);

		return cacheScene[component];
	}

	[HarmonyPatch]
	private static class FilterExtend {
		static IEnumerable<MethodBase> TargetMethods() {
			foreach (KeyValuePair<Type,string[]> target in TargetsExtend) {
				Type type = target.Key;

				foreach (string methodName in target.Value)
					yield return type.GetMethod(methodName,AccessTools.all);
			}
		}

		static void Postfix(Component __instance) {
			CacheComponent(__instance);

			if (!ShouldBotherWithFixes) return;

			__instance.transform.localScale = UWExtend;
		}
	}

	[HarmonyPatch(typeof(VideoController),nameof(VideoController.Prepare))]
	[HarmonyPatch(typeof(VideoController),nameof(VideoController.Stop))]
	[HarmonyPostfix]
	static void FixViewport(MethodBase __originalMethod,VideoController __instance) {
		RawImage image = __instance.world.Image;
		Vector3 cachedVector = CacheComponent(image);

		if (!ShouldBotherWithFixes) return;

		Vector3 targetVector;

		switch (__originalMethod.Name) {
			case nameof(VideoController.Prepare):
				targetVector = UWHorizontal;
				break;

			case nameof(VideoController.Stop):
				targetVector = cachedVector;
				break;

			default:
				return;
		}

		image.transform.localScale = targetVector;
	}

	[HarmonyPatch(typeof(SceneManager),nameof(SceneManager.Internal_SceneLoaded))]
	[HarmonyPatch(typeof(SceneManager),nameof(SceneManager.Internal_SceneUnloaded))]
	[HarmonyPostfix]
	static void Internal_SceneLoaded(MethodBase __originalMethod,Scene scene) {
		switch (__originalMethod.Name) {
			case nameof(SceneManager.Internal_SceneLoaded):
				Cache.Add(scene,[]);
				break;

			case nameof(SceneManager.Internal_SceneUnloaded):
				Cache.Remove(scene);
				break;

			default:
				return;
		}
	}
}
