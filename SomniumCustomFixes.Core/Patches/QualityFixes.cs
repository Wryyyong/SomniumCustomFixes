using MelonLoader.Preferences;

using Il2CppInterop.Runtime;

using URP = UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

using SomniumCustomFixes.Helpers;

namespace SomniumCustomFixes.Patches;

record QualityFixPatchSet(
	HarmonyMethod AutoPatch,
	HarmonyMethod CacheObjects,
	HarmonyMethod CleanCaches,
	HarmonyMethod RemoveFromCache
);

[HarmonyPatch]
static class QualityFixes {
	static MelonPreferences_Category StylePrefs;
	static MelonPreferences_Category QualityPrefs;

	// Stylistic preferences
	static MelonPreferences_Entry<bool> RenderCharacterModelOutlines;

	// Quality preferences
	static MelonPreferences_Entry<AnisotropicFiltering> AnisoMode;
	static MelonPreferences_Entry<int> AnisoLevel;
	static MelonPreferences_Entry<FilterMode> TextureFilteringMode;

	static MelonPreferences_Entry<URP.ShadowResolution> URPShadowResolution;
	static MelonPreferences_Entry<URP.AntialiasingMode> AntialiasingMode;
	static MelonPreferences_Entry<URP.AntialiasingQuality> SMAAQuality;

#if AINS
	static MelonPreferences_Entry<URP.TemporalAAQuality> TAAQuality;

	static URP.TemporalAA.Settings CustomTAASettings;
#endif

	static void Init() {
		const int DefaultPixelLights = 4;
		const URP.ShadowCascadesOption ShadowCascades = URP.ShadowCascadesOption.FourCascades;

		const URP.ShadowResolution MaxURPShadowRes = URP.ShadowResolution.
		#if AINI
			_4096
		#elif AINS
			_8192
		#endif
		;

		const URP.AntialiasingMode AntialiasingModeDefault = URP.AntialiasingMode.
		#if AINI
			SubpixelMorphologicalAntiAliasing
		#elif AINS
			TemporalAntiAliasing
		#endif
		;

	#region Preferences Setup

		// Stylistic preferences
		StylePrefs = SomniumMelon.PrefCategoryInit("StylisticSettings");

		RenderCharacterModelOutlines = StylePrefs.CreateEntry(
			"RenderCharacterModelOutlines",
			true,
			"Render character model outlines"
		);

		// Quality preferences
		QualityPrefs = SomniumMelon.PrefCategoryInit("QualitySettings");

		AnisoMode = QualityPrefs.CreateEntry(
			"AnisotropicFilteringMode",
			AnisotropicFiltering.ForceEnable,
			"Anisotropic filtering mode",

			$"Anisotropic filtering makes Textures look better when viewed at a shallow angle."
		+	$"\nPossible values:"
		+	$"\n- \"{AnisotropicFiltering.Disable}\""
		+	$"\n- \"{AnisotropicFiltering.Enable}\""
		+	$"\n- \"{AnisotropicFiltering.ForceEnable}\""
		);

		AnisoLevel = QualityPrefs.CreateEntry(
			"AnisotropicFilteringLevel",
			16,
			"Anisotropic filtering level",

			$"Defines the anisotropic filtering level of textures."
		+	$"\nPossible values: 0-16"
		+	$"\nHas certain effects when AnisotropicFilteringMode is set to \"{AnisotropicFiltering.ForceEnable}\":"
		+	$"\n- If set to 0, Unity does not apply anisotropic filtering."
		+	$"\n- If set between 1-9, Unity sets the value to 9.",
			false,
			false,
			new ValueRange<int>(0,16)
		);

		TextureFilteringMode = QualityPrefs.CreateEntry(
			"TextureFilteringMode",
			FilterMode.Trilinear,
			"Texture filtering mode",

			$"Sets how textures are filtered."
		+	$"\nPossible values:"
		+	$"\n- \"{FilterMode.Point}\""
		+	$"\n- \"{FilterMode.Bilinear}\""
		+	$"\n- \"{FilterMode.Trilinear}\""
		);

		URPShadowResolution = QualityPrefs.CreateEntry(
			"URP_ShadowResolution",
			MaxURPShadowRes,
			"Shadow Resolution",

			$"The resolution to render shadows at, through the Universal Rendering Pipeline."
		+	$"\nPossible values:"
		+	$"\n- \"{URP.ShadowResolution._256}\""
		+	$"\n- \"{URP.ShadowResolution._512}\""
		+	$"\n- \"{URP.ShadowResolution._1024}\""
		+	$"\n- \"{URP.ShadowResolution._2048}\""
		+	$"\n- \"{URP.ShadowResolution._4096}\""

		#if AINS
		+	$"\n- \"{URP.ShadowResolution._8192}\""
		#endif
		);

		AntialiasingMode = QualityPrefs.CreateEntry(
			"AntialiasingMode",
			AntialiasingModeDefault,
			"Antialiasing Mode",

			$"The type of antialiasing to set UniversalAdditionalCameraData instances to use."
		+	$"\nPossible values:"
		+	$"\n- \"{URP.AntialiasingMode.None}\""
		+	$"\n- \"{URP.AntialiasingMode.FastApproximateAntialiasing}\""
		+	$"\n- \"{URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing}\""

		#if AINS
		+	$"\n- \"{URP.AntialiasingMode.TemporalAntiAliasing}\""
		#endif
		);

		SMAAQuality = QualityPrefs.CreateEntry(
			"SMAAQuality",
			URP.AntialiasingQuality.High,
			"SMAA Quality",

			$"The level of quality to use for Subpixel Morphological Anti-Aliasing."
		+	$"\nHas no effect unless AntialiasingMode is set to \"{URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing}\"."
		+	$"\nPossible values:"
		+	$"\n- \"{URP.AntialiasingQuality.Low}\""
		+	$"\n- \"{URP.AntialiasingQuality.Medium}\""
		+	$"\n- \"{URP.AntialiasingQuality.High}\""
		);

		static void RefreshSettings<Class,Value>() where Class : uObject {
			var data = TypeData<Class,Value>.GetTypeData();

			foreach (var binding in data.PreferenceBindings) {
				var newVal = binding.Key.Value;

				foreach (var info in binding.Value)
					info.TargetValue = newVal;
			}

			data.Refresh();
		}

		static void RefreshAniso(object oldVal,object newVal) {
			RefreshSettings<QualitySettings,AnisotropicFiltering>();
			RefreshSettings<Texture,int>();
		}

		RenderCharacterModelOutlines.OnEntryValueChanged.Subscribe(static (_,_) =>
			RefreshSettings<URP.ScriptableRendererFeature,bool>()
		);

		AnisoMode.OnEntryValueChangedUntyped.Subscribe(RefreshAniso);
		AnisoLevel.OnEntryValueChangedUntyped.Subscribe(RefreshAniso);

		TextureFilteringMode.OnEntryValueChanged.Subscribe(static (_,_) =>
			RefreshSettings<Texture,FilterMode>()
		);

		URPShadowResolution.OnEntryValueChanged.Subscribe(static (_,_) =>
			RefreshSettings<URP.UniversalRenderPipelineAsset,URP.ShadowResolution>()
		);

		AntialiasingMode.OnEntryValueChanged.Subscribe(static (_,_) =>
			RefreshSettings<URP.UniversalAdditionalCameraData,URP.AntialiasingMode>()
		);

		SMAAQuality.OnEntryValueChanged.Subscribe(static (_,_) =>
			RefreshSettings<URP.UniversalAdditionalCameraData,URP.AntialiasingQuality>()
		);

	#if AINS
		TAAQuality = QualityPrefs.CreateEntry(
			"TAAQuality",
			URP.TemporalAAQuality.VeryHigh,
			"TAA Quality",

			$"The level of quality to use for Temporal Anti-Aliasing."
		+	$"\nHas no effect unless AntialiasingMode is set to \"{URP.AntialiasingMode.TemporalAntiAliasing}\"."
		+	$"\nPossible values:"
		+	$"\n- \"{URP.TemporalAAQuality.VeryLow}\""
		+	$"\n- \"{URP.TemporalAAQuality.Low}\""
		+	$"\n- \"{URP.TemporalAAQuality.Medium}\""
		+	$"\n- \"{URP.TemporalAAQuality.High}\""
		+	$"\n- \"{URP.TemporalAAQuality.VeryHigh}\""
		);

		CustomTAASettings = new() {
		//	jitterFrameCountOffset = 0,
			m_ContrastAdaptiveSharpening = .3f,
			m_FrameInfluence = .1f,
			m_JitterScale = 1f,
			m_MipBias = -1f,
			m_Quality = TAAQuality.Value,
			m_VarianceClampScale = .9f,
		//	resetHistoryFrames = 0,
		};

		TAAQuality.OnEntryValueChanged.Subscribe(static (_,newVal) => {
			CustomTAASettings.m_Quality = newVal;

			RefreshSettings<URP.UniversalAdditionalCameraData,URP.AntialiasingMode>();
		});
	#endif

	#endregion
	#region SettingInfo Setup

		var harmony = SomniumMelon.HarmonyInst;
		var autoPatchCache = new Dictionary<(Type,Type),(Type[],HarmonyMethod)>();

		// Our methods
		var autoPatch = typeof(QualityFixes).GetMethod(nameof(AutoPatch),AccessTools.all);

		var cacheObjs = typeof(QualityFixes).GetMethod(nameof(CacheObjects),AccessTools.all);
		var cleanCaches = typeof(QualityFixes).GetMethod(nameof(CleanCaches),AccessTools.all);
		var removeFromCache = typeof(QualityFixes).GetMethod(nameof(RemoveFromCache),AccessTools.all);

		// Unity methods
		var uObjDestroy = typeof(uObject).GetMethod(nameof(uObject.Destroy),AccessTools.all,[typeof(uObject),typeof(float)]);
		var uObjDestroyImmediate = typeof(uObject).GetMethod(nameof(uObject.DestroyImmediate),AccessTools.all,[typeof(uObject),typeof(bool)]);

		var sceneLoad = typeof(SceneManager).GetMethod(nameof(SceneManager.Internal_SceneLoaded),AccessTools.all);
		var sceneUnload = typeof(SceneManager).GetMethod(nameof(SceneManager.Internal_SceneUnloaded),AccessTools.all);

		static bool TextureCheck(Texture obj) => obj.TryCast<RenderTexture>() is null;

		static bool ModelOutlineCheck(URP.ScriptableRendererFeature obj) =>
			obj.name is
				"CharaOutLine"
			or	"CharaOutLineMirror";

		Array.ForEach<SettingInfo>([
			new SettingInfo<Light,LightRenderMode>(
				nameof(Light.renderMode),
				LightRenderMode.ForcePixel
			),
			new SettingInfo<Light,LightShadows>(
				nameof(Light.shadows),
				LightShadows.Soft
			),

			new SettingInfo<Texture,int>(
				nameof(Texture.anisoLevel),
				AnisoLevel,
				true,
				cacheCondition: TextureCheck,
				setCondition: (obj,ref _) => TextureCheck(obj)
			),
			new SettingInfo<Texture,FilterMode>(
				nameof(Texture.filterMode),
				TextureFilteringMode,
				cacheCondition: obj =>
					TextureCheck(obj)
				&&	obj.filterMode is not FilterMode.Point
				,
				setCondition: (obj,ref _) => TextureCheck(obj)
			),

			new SettingInfo<QualitySettings,AnisotropicFiltering>(
				nameof(QualitySettings.anisotropicFiltering),
				AnisoMode
			),
			new SettingInfo<QualitySettings,QualityLevel>(
				nameof(QualitySettings.currentLevel),
				QualityLevel.Fantastic
			),
			new SettingInfo<QualitySettings,float>(
				nameof(QualitySettings.lodBias),
				100f
			),
			/*
			new SettingInfo<QualitySettings,int>(
				nameof(QualitySettings.particleRaycastBudget),
				4096
			),
			*/
			new SettingInfo<QualitySettings,int>(
				nameof(QualitySettings.pixelLightCount),
				DefaultPixelLights
			),
			/*
			new SettingInfo<QualitySettings,ShadowQuality>(
				nameof(QualitySettings.shadows),
				ShadowQuality.All
			),
			*/
			new SettingInfo<QualitySettings,int>(
				nameof(QualitySettings.shadowCascades),
				4
			),
			new SettingInfo<QualitySettings,float>(
				nameof(QualitySettings.shadowDistance),
				130f,
				false
			),
			new SettingInfo<QualitySettings,ShadowProjection>(
				nameof(QualitySettings.shadowProjection),
				ShadowProjection.CloseFit
			),
			new SettingInfo<QualitySettings,ShadowResolution>(
				nameof(QualitySettings.shadowResolution),
				ShadowResolution.VeryHigh
			),
			new SettingInfo<QualitySettings,bool>(
				nameof(QualitySettings.softParticles),
				true
			),

			new SettingInfo<URP.ScriptableRendererFeature,bool>(
				nameof(URP.ScriptableRendererFeature.m_Active),
				RenderCharacterModelOutlines,
				cacheCondition: ModelOutlineCheck,
				setCondition: (obj,ref _) => ModelOutlineCheck(obj)
			),

			new SettingInfo<URP.UniversalRenderPipelineAsset,URP.ShadowResolution>(
				nameof(URP.UniversalRenderPipelineAsset.m_AdditionalLightsShadowmapResolution),
				URPShadowResolution
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset,URP.ShadowResolution>(
				nameof(URP.UniversalRenderPipelineAsset.m_LocalShadowsAtlasResolution),
				URPShadowResolution
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset,URP.ShadowResolution>(
				nameof(URP.UniversalRenderPipelineAsset.m_MainLightShadowmapResolution),
				URPShadowResolution
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset,int>(
				nameof(URP.UniversalRenderPipelineAsset.m_MaxPixelLights),
				DefaultPixelLights
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset,URP.ShadowResolution>(
				nameof(URP.UniversalRenderPipelineAsset.m_ShadowAtlasResolution),
				URPShadowResolution
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset,URP.ShadowCascadesOption>(
				nameof(URP.UniversalRenderPipelineAsset.m_ShadowCascades),
				ShadowCascades
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset,URP.ShadowQuality>(
				nameof(URP.UniversalRenderPipelineAsset.m_ShadowType),
				URP.ShadowQuality.SoftShadows
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset,URP.ShadowCascadesOption>(
				nameof(URP.UniversalRenderPipelineAsset.shadowCascadeOption),
				ShadowCascades
			),

			new SettingInfo<URP.UniversalAdditionalCameraData,bool>(
				nameof(URP.UniversalAdditionalCameraData.renderPostProcessing),
				true,
				setCondition: (obj,ref _) => obj.requiresDepthTexture
			),
			new SettingInfo<URP.UniversalAdditionalCameraData,bool>(
				nameof(URP.UniversalAdditionalCameraData.renderShadows),
				true
			),
			new SettingInfo<URP.UniversalAdditionalCameraData,URP.AntialiasingMode>(
				nameof(URP.UniversalAdditionalCameraData.antialiasing),
				AntialiasingMode,
				setCondition: (obj,ref newVal) => {
					if (!obj.renderPostProcessing)
						newVal = URP.AntialiasingMode.None;

				#if AINS
					if (newVal is URP.AntialiasingMode.TemporalAntiAliasing)
						if (obj.GetComponent<Il2CppGame.BustShotCamera>())
							newVal = URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
						else
							obj.m_TaaSettings = CustomTAASettings;
				#endif

					return true;
				}
			),
			new SettingInfo<URP.UniversalAdditionalCameraData,URP.AntialiasingQuality>(
				nameof(URP.UniversalAdditionalCameraData.antialiasingQuality),
				SMAAQuality,
				setCondition: (obj,ref _) => obj.antialiasing is URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing
			),

		#if AINS
			new SettingInfo<URP.UniversalRenderPipelineAsset,URP.SoftShadowQuality>(
				nameof(URP.UniversalRenderPipelineAsset.softShadowQuality),
				URP.SoftShadowQuality.High
			),
		#endif
		],info => {
			var newTypeData = info.InitializeTypeData();

			var types = info.Types;

			if (!autoPatchCache.TryGetValue(types,out var cache)) {
				Type[] newArray = [types.Item1,types.Item2];
				cache = (newArray,new HarmonyMethod(autoPatch.MakeGenericMethod(newArray)));

				autoPatchCache.Add(types,cache);
			}

			var typeArray = cache.Item1;

			if (info.DoAutoPatch)
				harmony.Patch(
					info.Setter,
					prefix: cache.Item2
				);

			if (
				newTypeData
			&&	info.DoTypeDataPatch
			) {
				var removeFromCachePatch = new HarmonyMethod(removeFromCache.MakeGenericMethod(typeArray));

				harmony.Patch(
					uObjDestroy,
					prefix: removeFromCachePatch
				);
				harmony.Patch(
					uObjDestroyImmediate,
					prefix: removeFromCachePatch
				);

				harmony.Patch(
					sceneLoad,
					postfix: new HarmonyMethod(cacheObjs.MakeGenericMethod(typeArray))
				);
				harmony.Patch(
					sceneUnload,
					postfix: new HarmonyMethod(cleanCaches.MakeGenericMethod(typeArray))
				);
			}
		});

	#endregion
	}

	static void RemoveFromCache<Class,Value>(Class __0) where Class : uObject =>
		TypeData<Class,Value>.GetTypeData().Cache.Remove(__0);

	static void CacheObjects<Class,Value>() where Class : uObject =>
		TypeData<Class,Value>.GetTypeData().FullUpdate();

	static void CleanCaches<Class,Value>() where Class : uObject =>
		TypeData<Class,Value>.GetTypeData().CleanCache();

	static void AutoPatch<Class,Value>(MethodBase __originalMethod,Class __instance,ref Value __0) where Class : uObject {
		var data = TypeData<Class,Value>.GetTypeData();
		var info = data.InfoData[__originalMethod];
		var newVal = info.TargetValue;

		if (!TypeData<Class,Value>.SetCheck(__instance,info,__0,ref newVal)) return;

		var instNull = __instance is null;

		if (info.DoLogging)
			SomniumMelon.EasyLog(
				(instNull ? __originalMethod.DeclaringType.ToString() : __instance.name)
			+	$" :: {__originalMethod.Name} | {__0} -> {newVal}"
			);

		if (!instNull) {
			var cache = data.Cache;

			if (!cache.TryGetValue(__instance,out var oldValList)) {
				oldValList = [];
				cache[__instance] = oldValList;
			}

			oldValList[info] = __0;
		}

		__0 = newVal;
	}
}
