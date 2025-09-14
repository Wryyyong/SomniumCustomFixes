using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine.SceneManagement;
using UnityEngine.UI;

using Il2CppGame;

namespace SomniumCustomFixes;

[HarmonyPatch]
static class UltrawideFixes {
	static readonly Dictionary<Type,string[]> TargetsExtend = new() {
		{typeof(CinemaScope),[
			nameof(CinemaScope.Show),
		]},
		{typeof(FilterController),[
			nameof(FilterController.Black),
			/*// Il2CppInterop currently doesn't like nullable parameters for some reason
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

	static MelonPreferences_Category UltrawidePrefs;

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
		var displayMain = Display.main;

		UltrawidePrefs = SomniumMelon.PrefCategoryInit("UltrawideFixes");

		DoUltrawideFixes = UltrawidePrefs.CreateEntry(
			"DoUltrawideFixes",
			true,
			"Fix ultrawide UI issues"
		);
		ResWidth = UltrawidePrefs.CreateEntry(
			"CustomResolutionWidth",
			displayMain.systemWidth,
			"Custom resolution (width)"
		);
		ResHeight = UltrawidePrefs.CreateEntry(
			"CustomResolutionHeight",
			displayMain.systemHeight,
			"Custom resolution (height)"
		);

		static void SetShouldBother(bool oldVal = false,bool newVal = false) {
			ShouldBotherWithFixes =
				DoUltrawideFixes.Value
			&&	AspectRatioCustom > AspectRatioNative
			;

			if (ShouldBotherWithFixes) return;

			foreach (var dict in Cache.Values)
				foreach (var set in dict)
					set.Key.transform.localScale = set.Value;
		}

		static void ResolutionChanged(int oldVal = 0,int newVal = 0) {
			AspectRatioCustom = (float)ResWidth.Value / ResHeight.Value;
			AspectRatioMultiplier = AspectRatioCustom / AspectRatioNative;

			UWExtend.x = 1f * AspectRatioMultiplier;
			UWHorizontal.x = 1f / AspectRatioMultiplier;

			SetCustomResolution();
			SetShouldBother();
		}

		DoUltrawideFixes.OnEntryValueChanged.Subscribe(SetShouldBother);
		ResWidth.OnEntryValueChanged.Subscribe(ResolutionChanged);
		ResHeight.OnEntryValueChanged.Subscribe(ResolutionChanged);

		ResolutionChanged();
	}

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

	static Dictionary<Component,Vector3> GetCacheScene(Scene scene) {
		if (!Cache.TryGetValue(scene,out var cacheScene)) {
			cacheScene = [];
			Cache.TryAdd(scene,cacheScene);
		}

		return cacheScene;
	}

	static Vector3 CacheComponent(Component component) {
		var cacheScene = GetCacheScene(component.gameObject.scene);

		if (!cacheScene.TryGetValue(component,out var vector)) {
			vector = component.transform.localScale;
			cacheScene.TryAdd(component,vector);
		}

		return vector;
	}

	[HarmonyPatch]
	static class FilterExtend {
		static bool Prepare() {
			foreach (var target in TargetsExtend.Values)
				if (target.Length > 0)
					return true;

			return false;
		}

		static IEnumerable<MethodBase> TargetMethods() {
			foreach (var target in TargetsExtend) {
				var type = target.Key;

				foreach (var methodName in target.Value)
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
		var image = __instance.world.Image;
		var cachedVector = CacheComponent(image);

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
	static void SceneUpdate(MethodBase __originalMethod,Scene scene) {
		switch (__originalMethod.Name) {
			case nameof(SceneManager.Internal_SceneLoaded):
				GetCacheScene(scene);
				break;

			case nameof(SceneManager.Internal_SceneUnloaded):
				Cache.Remove(scene);
				break;

			default:
				return;
		}
	}
}
