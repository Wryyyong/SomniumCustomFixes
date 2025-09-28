using UnityEngine.SceneManagement;
using UnityEngine.UI;

using
#if BEPIS
	Game
#elif MELON
	Il2CppGame
#endif
;

namespace SomniumCustomFixes.Patches;

[HarmonyPatch]
static class UltrawideFixes {
	static readonly Dictionary<Type,string[]> TargetsExtend = new() {
		[typeof(CinemaScope)] = [
			nameof(CinemaScope.Show),
		],

		[typeof(FilterController)] = [
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
		],

		[typeof(EyeFadeFilter)] = [
			nameof(EyeFadeFilter.FadeIn),
			nameof(EyeFadeFilter.FadeInWait),
			nameof(EyeFadeFilter.FadeOut),
			nameof(EyeFadeFilter.FadeOutWait),
		],
	};

	static ConfigElement<bool> DoUltrawideFixes;
	static ConfigElement<int> ResWidth;
	static ConfigElement<int> ResHeight;

	const float AspectRatioNative = 16f / 9f;

	static float AspectRatioCustom;
	static float AspectRatioMultiplier;

	static bool ShouldBotherWithFixes;

	static readonly Dictionary<Scene,Dictionary<Component,Vector3>> Cache = [];
	static Vector3 UWExtend = new(1f,1f,1f);
	static Vector3 UWHorizontal = new(1f,1f,1f);

	static void PatchInit() {
		var displayMain = Display.main;

		DoUltrawideFixes = new(
			"UltrawideFixes",
			"DoUltrawideFixes",
			true,
			"Fix ultrawide UI issues"
		);
		ResWidth = new(
			"UltrawideFixes",
			"CustomResolutionWidth",
			displayMain.systemWidth,
			"Custom resolution (width)"
		);
		ResHeight = new(
			"UltrawideFixes",
			"CustomResolutionHeight",
			displayMain.systemHeight,
			"Custom resolution (height)"
		);

		static void SetShouldBother() {
			ShouldBotherWithFixes =
				DoUltrawideFixes.Value
			&&	AspectRatioCustom > AspectRatioNative
			;

			if (ShouldBotherWithFixes) return;

			foreach (var dict in Cache.Values)
				foreach (var set in dict)
					set.Key.transform.localScale = set.Value;
		}

		static void ResolutionChanged() {
			AspectRatioCustom = (float)ResWidth.Value / ResHeight.Value;
			AspectRatioMultiplier = AspectRatioCustom / AspectRatioNative;

			UWExtend.x = 1f * AspectRatioMultiplier;
			UWHorizontal.x = 1f / AspectRatioMultiplier;

			SetCustomResolution();
			SetShouldBother();
		}

		DoUltrawideFixes.OnValueChangedNotify += SetShouldBother;
		ResWidth.OnValueChangedNotify += ResolutionChanged;
		ResHeight.OnValueChangedNotify += ResolutionChanged;

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
	[HarmonyPostfix]
	static void SceneUpdate_Load(Scene scene) => GetCacheScene(scene);

	[HarmonyPatch(typeof(SceneManager),nameof(SceneManager.Internal_SceneUnloaded))]
	[HarmonyPostfix]
	static void SceneUpdate_Unload(Scene scene) => Cache.Remove(scene);
}
