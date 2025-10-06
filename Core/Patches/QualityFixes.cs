using Il2CppInterop.Runtime;

using URP = UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

#if AINS
	using BustShotCamera =
	#if BEPIS
		Game
	#elif MELON
		Il2CppGame
	#endif
		.BustShotCamera
	;
#endif

using SomniumCustomFixes.Helpers;

namespace SomniumCustomFixes.Patches;

[HarmonyPatch]
static class QualityFixes {
	// Stylistic preferences
	static ConfigElement<bool> RenderCharacterModelOutlines;

	// Quality preferences
	static ConfigElement<AnisotropicFiltering> AnisoMode;
	static ConfigElement<int> AnisoLevel;
	static ConfigElement<FilterMode> TextureFilteringMode;

	static ConfigElement<URP.ShadowResolution> URPShadowResolution;
	static ConfigElement<URP.AntialiasingMode> AntialiasingMode;
	static ConfigElement<URP.AntialiasingQuality> SMAAQuality;

#if AINS
	static ConfigElement<URP.TemporalAAQuality> TAAQuality;

	static URP.TemporalAA.Settings CustomTAASettings;
#endif

	static void PatchInit() {
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
		RenderCharacterModelOutlines = new(
			"StylisticSettings",
			"RenderCharacterModelOutlines",
			true
		);

		// Quality preferences
		AnisoMode = new(
			"QualitySettings",
			"AnisotropicFilteringMode",
			AnisotropicFiltering.ForceEnable,

			"Anisotropic filtering makes Textures look better when viewed at a shallow angle."
		);

		AnisoLevel = new(
			"QualitySettings",
			"AnisotropicFilteringLevel",
			16,

			"Defines the anisotropic filtering level of textures."
		+	$"\nHas certain effects when AnisotropicFilteringMode is set to \"{AnisotropicFiltering.ForceEnable}\":"
		+	"\n- If set to 0, Unity does not apply anisotropic filtering."
		+	"\n- If set between 1-9, Unity sets the value to 9.",

			new ConfigRange<int>(0,16)
		);

		TextureFilteringMode = new(
			"QualitySettings",
			"TextureFilteringMode",
			FilterMode.Trilinear,

			"Sets how textures are filtered."
		);

		URPShadowResolution = new(
			"QualitySettings",
			"URP_ShadowResolution",
			MaxURPShadowRes,

			"The resolution to render shadows at, through the Universal Rendering Pipeline."
		);

		AntialiasingMode = new(
			"QualitySettings",
			"AntialiasingMode",
			AntialiasingModeDefault,

			"The type of antialiasing to set UniversalAdditionalCameraData instances to use."
		);

		SMAAQuality = new(
			"QualitySettings",
			"SMAAQuality",
			URP.AntialiasingQuality.High,

			"The level of quality to use for Subpixel Morphological Anti-Aliasing."
		+	$"\nHas no effect unless AntialiasingMode is set to \"{URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing}\"."
		);

		static void RefreshSettings<Class,Value>() where Class : uObject {
			foreach (var binding in TypeData<Class,Value>.ConfigBindings) {
				var newVal = binding.Key.Value;

				foreach (var info in binding.Value)
					info.TargetValue = newVal;
			}

			TypeData<Class,Value>.Refresh();
		}

		static void RefreshAniso() {
			RefreshSettings<QualitySettings,AnisotropicFiltering>();
			RefreshSettings<Texture,int>();
		}

		AnisoMode.OnValueChangedNotify += RefreshAniso;
		AnisoLevel.OnValueChangedNotify += RefreshAniso;

		RenderCharacterModelOutlines.OnValueChangedNotify += static () =>
			RefreshSettings<URP.ScriptableRendererFeature,bool>();

		TextureFilteringMode.OnValueChangedNotify += static () =>
			RefreshSettings<Texture,FilterMode>();

		URPShadowResolution.OnValueChangedNotify += static () =>
			RefreshSettings<URP.UniversalRenderPipelineAsset,URP.ShadowResolution>();

		AntialiasingMode.OnValueChangedNotify += static () =>
			RefreshSettings<URP.UniversalAdditionalCameraData,URP.AntialiasingMode>();

		SMAAQuality.OnValueChangedNotify += static () =>
			RefreshSettings<URP.UniversalAdditionalCameraData,URP.AntialiasingQuality>();

	#if AINS
		TAAQuality = new(
			"QualitySettings",
			"TAAQuality",
			URP.TemporalAAQuality.VeryHigh,

			"The level of quality to use for Temporal Anti-Aliasing."
		+	$"\nHas no effect unless AntialiasingMode is set to \"{URP.AntialiasingMode.TemporalAntiAliasing}\"."
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

		TAAQuality.OnValueChanged += static newVal => {
			CustomTAASettings.m_Quality = newVal;

			RefreshSettings<URP.UniversalAdditionalCameraData,URP.AntialiasingMode>();
		};
	#endif

	#endregion

	#region SettingInfo Setup

		static MethodInfo GetMethod(Type type,string name,Type[] parameters = null) =>
			parameters is null
		?	type.GetMethod(name,AccessTools.all)
		:	type.GetMethod(name,AccessTools.all,parameters)
		;

		var autoPatchCache = new Dictionary<TypePair,(Type[] TypeArray,HarmonyMethod AutoPatch)>();

		var typeQualityFixes = typeof(QualityFixes);
		var typeUObject = typeof(uObject);
		var typeSceneManager = typeof(SceneManager);

		// Our methods
		var autoPatch = GetMethod(typeQualityFixes,nameof(AutoPatch));

		var cacheObjs = GetMethod(typeQualityFixes,nameof(CacheObjects));
		var cleanCaches = GetMethod(typeQualityFixes,nameof(CleanCaches));
		var removeFromCache = GetMethod(typeQualityFixes,nameof(RemoveFromCache));

		// Unity methods
		var uObjDestroy = GetMethod(typeUObject,nameof(uObject.Destroy),[typeUObject,typeof(float)]);
		var uObjDestroyImmediate = GetMethod(typeUObject,nameof(uObject.DestroyImmediate),[typeUObject,typeof(bool)]);

		var sceneLoad = GetMethod(typeSceneManager,nameof(SceneManager.Internal_SceneLoaded));
		var sceneUnload = GetMethod(typeSceneManager,nameof(SceneManager.Internal_SceneUnloaded));

		static bool TextureCheck(Texture obj) => obj.TryCast<RenderTexture>() is null;

		static bool ModelOutlineCheck(URP.ScriptableRendererFeature obj) =>
			obj.name is
				"CharaOutLine"
			or	"CharaOutLineMirror"
		;

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
				setCondition: static (obj,ref _) => TextureCheck(obj)
			),
			new SettingInfo<Texture,FilterMode>(
				nameof(Texture.filterMode),
				TextureFilteringMode,
				cacheCondition: static obj =>
					TextureCheck(obj)
				&&	obj.filterMode is not FilterMode.Point
				,
				setCondition: static (obj,ref _) => TextureCheck(obj)
			),

			new SettingInfo<QualitySettings,AnisotropicFiltering>(
				nameof(QualitySettings.anisotropicFiltering),
				AnisoMode
			),
			new SettingInfo<QualitySettings,BlendWeights>(
				nameof(QualitySettings.blendWeights),
				BlendWeights.FourBones
			),
			new SettingInfo<QualitySettings,QualityLevel>(
				nameof(QualitySettings.currentLevel),
				QualityLevel.Fantastic
			),
			new SettingInfo<QualitySettings,float>(
				nameof(QualitySettings.lodBias),
				100f
			),
			new SettingInfo<QualitySettings,int>(
				nameof(QualitySettings.particleRaycastBudget),
				4096
			),
			new SettingInfo<QualitySettings,int>(
				nameof(QualitySettings.pixelLightCount),
				DefaultPixelLights
			),
			new SettingInfo<QualitySettings,ShadowQuality>(
				nameof(QualitySettings.shadows),
				ShadowQuality.All
			),
			new SettingInfo<QualitySettings,int>(
				nameof(QualitySettings.shadowCascades),
				4
			),
			new SettingInfo<QualitySettings,float>(
				nameof(QualitySettings.shadowDistance),
				130f,
				false
			),
			new SettingInfo<QualitySettings,ShadowmaskMode>(
				nameof(QualitySettings.shadowmaskMode),
				ShadowmaskMode.DistanceShadowmask
			),
			new SettingInfo<QualitySettings,ShadowProjection>(
				nameof(QualitySettings.shadowProjection),
				ShadowProjection.CloseFit
			),
			new SettingInfo<QualitySettings,ShadowResolution>(
				nameof(QualitySettings.shadowResolution),
				ShadowResolution.VeryHigh
			),
			new SettingInfo<QualitySettings,SkinWeights>(
				nameof(QualitySettings.skinWeights),
				SkinWeights.Unlimited
			),
			new SettingInfo<QualitySettings,bool>(
				nameof(QualitySettings.softParticles),
				true
			),

			new SettingInfo<URP.ScriptableRendererFeature,bool>(
				nameof(URP.ScriptableRendererFeature.m_Active),
				RenderCharacterModelOutlines,
				cacheCondition: ModelOutlineCheck,
				setCondition: static (obj,ref _) => ModelOutlineCheck(obj)
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
				setCondition: static (obj,ref _) => obj.requiresDepthTexture
			),
			new SettingInfo<URP.UniversalAdditionalCameraData,bool>(
				nameof(URP.UniversalAdditionalCameraData.renderShadows),
				true
			),
			new SettingInfo<URP.UniversalAdditionalCameraData,URP.AntialiasingMode>(
				nameof(URP.UniversalAdditionalCameraData.antialiasing),
				AntialiasingMode,
				setCondition: static (obj,ref newVal) => {
					if (!obj.renderPostProcessing)
						newVal = URP.AntialiasingMode.None;

				#if AINS
					if (newVal is URP.AntialiasingMode.TemporalAntiAliasing)
						if (obj.GetComponent<BustShotCamera>())
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
				setCondition: static (obj,ref _) => obj.antialiasing is URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing
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
				Type[] newArray = [types.Class,types.Value];
				cache = (newArray,new(autoPatch.MakeGenericMethod(newArray)));

				autoPatchCache.Add(types,cache);
			}

			var typeArray = cache.TypeArray;

			if (info.DoAutoPatch)
				HarmonyInstance.Patch(
					info.Setter,
					prefix: cache.AutoPatch
				);

			if (
				newTypeData
			&&	info.DoTypeDataPatch
			) {
				var removeFromCachePatch = new HarmonyMethod(removeFromCache.MakeGenericMethod(typeArray));

				HarmonyInstance.Patch(
					uObjDestroy,
					prefix: removeFromCachePatch
				);
				HarmonyInstance.Patch(
					uObjDestroyImmediate,
					prefix: removeFromCachePatch
				);

				HarmonyInstance.Patch(
					sceneLoad,
					postfix: new(cacheObjs.MakeGenericMethod(typeArray))
				);
				HarmonyInstance.Patch(
					sceneUnload,
					postfix: new(cleanCaches.MakeGenericMethod(typeArray))
				);
			}
		});

	#endregion
	}

	static void RemoveFromCache<Class,Value>(Class __0) where Class : uObject =>
		TypeData<Class,Value>.Cache.Remove(__0);

	static void CacheObjects<Class,Value>() where Class : uObject =>
		TypeData<Class,Value>.FullUpdate();

	static void CleanCaches<Class,Value>() where Class : uObject =>
		TypeData<Class,Value>.CleanCache();

	static void AutoPatch<Class,Value>(MethodInfo __originalMethod,Class __instance,ref Value __0) where Class : uObject {
		var info = TypeData<Class,Value>.InfoData[__originalMethod];
		var newVal = info.TargetValue;

		if (!TypeData<Class,Value>.SetCheck(__instance,info,__0,ref newVal)) return;

		if (info.DoAutoPatchLogging)
			EasyLog(
				(__instance is null ? __originalMethod.DeclaringType.ToString() : __instance.name)
			+	$" :: {__originalMethod.Name} | {__0} -> {newVal}"
			);

		__0 = newVal;
	}
}
