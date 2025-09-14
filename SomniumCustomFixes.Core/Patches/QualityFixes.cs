using URP = UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

using SomniumCustomFixes.Helpers;

namespace SomniumCustomFixes.Patches;

[HarmonyPatch]
static class QualityFixes {
	static MelonPreferences_Category QualityPrefs;

	static MelonPreferences_Entry<URP.ShadowResolution> URPShadowResolution;
	static MelonPreferences_Entry<URP.AntialiasingMode> AntialiasingMode;
	static MelonPreferences_Entry<URP.AntialiasingQuality> AntialiasingQuality;

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

		#region Preferences Setup

		QualityPrefs = SomniumMelon.PrefCategoryInit("QualitySettings");

		URPShadowResolution = QualityPrefs.CreateEntry(
			"URP_ShadowResolution",
			MaxURPShadowRes,
			"Shadow Resolution",
			$"The resolution to render shadows at"
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
			URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing,
			"Antialiasing Mode",

			$"The type of antialiasing to set UniversalAdditionalCameraData instances to use"
		+	$"\nPossible values:"
		+	$"\n- \"{URP.AntialiasingMode.None}\""
		+	$"\n- \"{URP.AntialiasingMode.FastApproximateAntialiasing}\""
		+	$"\n- \"{URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing}\""

		#if AINS
		+	$"\n- \"{URP.AntialiasingMode.TemporalAntiAliasing}\""
		#endif
		);

		AntialiasingQuality = QualityPrefs.CreateEntry(
			"AntialiasingQuality",
			URP.AntialiasingQuality.High,
			"SMAA Quality",

			$"The level of quality to use for SMAA"
		+	$"\nHas no effect unless AntialiasingMode is set to \"{URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing}\""
		+	$"\nPossible values:"
		+	$"\n- \"{URP.AntialiasingQuality.Low}\""
		+	$"\n- \"{URP.AntialiasingQuality.Medium}\""
		+	$"\n- \"{URP.AntialiasingQuality.High}\""
		);

		static void RefreshSettings<T>() {
			if (!TypeData.RegisteredTypes.TryGetValue(typeof(T),out var data)) return;

			var settings = data.TargetSettings;

			foreach (var binding in data.PreferenceBindings) {
				var newVal = binding.Key.BoxedValue;

				foreach (var setter in binding.Value)
					settings[setter] = newVal;
			}

			data.Refresh();
		}

		static void RefreshURPAsset(object oldVal = null,object newVal = null) =>
			RefreshSettings<URP.UniversalRenderPipelineAsset>();

		static void RefreshUACD(object oldVal = null,object newVal = null) =>
			RefreshSettings<URP.UniversalAdditionalCameraData>();

		URPShadowResolution.OnEntryValueChangedUntyped.Subscribe(RefreshURPAsset);

		AntialiasingMode.OnEntryValueChangedUntyped.Subscribe(RefreshUACD);
		AntialiasingQuality.OnEntryValueChangedUntyped.Subscribe(RefreshUACD);

		#endregion
		#region SettingInfo Setup

		var harmony = SomniumMelon.HarmonyInst;
		var paramList = new object[1];
		ref var defaultVal = ref paramList[0];

		var staticPatch = new HarmonyMethod(typeof(QualityFixes).GetMethod(nameof(StaticPatch),AccessTools.all));

		Array.ForEach<SettingInfo>([
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.anisotropicFiltering),
				AnisotropicFiltering.ForceEnable
			),
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.currentLevel),
				QualityLevel.Fantastic
			),
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.lodBias),
				100f
			),
			/*
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.particleRaycastBudget),
				4096
			),
			*/
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.pixelLightCount),
				DefaultPixelLights
			),
			/*
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.shadows),
				ShadowQuality.All
			),
			*/
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.shadowCascades),
				4
			),
			/*
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.shadowDistance),
				130f
			),
			*/
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.shadowProjection),
				ShadowProjection.CloseFit
			),
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.shadowResolution),
				ShadowResolution.VeryHigh
			),
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.softParticles),
				true
			),

			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_AdditionalLightsShadowmapResolution),
				URPShadowResolution
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_LocalShadowsAtlasResolution),
				URPShadowResolution
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_MainLightShadowmapResolution),
				URPShadowResolution
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_MaxPixelLights),
				DefaultPixelLights
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_ShadowAtlasResolution),
				URPShadowResolution
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_ShadowCascades),
				ShadowCascades
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_ShadowType),
				URP.ShadowQuality.SoftShadows
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.shadowCascadeOption),
				ShadowCascades
			),

			new SettingInfo<URP.UniversalAdditionalCameraData>(
				nameof(URP.UniversalAdditionalCameraData.antialiasing),
				AntialiasingMode
			),
			new SettingInfo<URP.UniversalAdditionalCameraData>(
				nameof(URP.UniversalAdditionalCameraData.antialiasingQuality),
				AntialiasingQuality
			),

		#if AINS
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.softShadowQuality),
				URP.SoftShadowQuality.High
			),
		#endif
		],info => {
			var pref = info.PrefEntry;
			var property = info.Property;
			var getter = property.GetMethod;
			var setter = property.SetMethod;

			if (info.DoPatchStatic)
				harmony.Patch(setter,staticPatch);

			TypeData.RegisteredTypes.TryGetValue(info.Type,out var data);
			data.Properties.TryAdd(setter,getter);

			data.TargetSettings.TryAdd(setter,info.TargetValue);

			if (pref is null) return;

			var bindings = data.PreferenceBindings;

			if (!bindings.TryGetValue(pref,out var prefSetters)) {
				prefSetters = [];
				bindings.TryAdd(pref,prefSetters);
			}

			prefSetters.Add(setter);
		});

		#endregion
	}

	[HarmonyPatch(typeof(uObject),nameof(uObject.Destroy),[typeof(uObject),typeof(float)])]
	[HarmonyPatch(typeof(uObject),nameof(uObject.DestroyImmediate),[typeof(uObject),typeof(bool)])]
	[HarmonyPrefix]
	static void DestroyMonitor(uObject obj) {
		if (!TypeData.RegisteredTypes.TryGetValue(obj.GetType(),out var data)) return;

		data.Cache.Remove(obj);
	}

	[HarmonyPatch(typeof(SceneManager),nameof(SceneManager.Internal_SceneLoaded))]
	[HarmonyPostfix]
	static void CacheObjects() {
		foreach (var data in TypeData.RegisteredTypes.Values)
			data.FullUpdate();
	}

	[HarmonyPatch(typeof(SceneManager),nameof(SceneManager.Internal_SceneUnloaded))]
	[HarmonyPostfix]
	static void CleanCaches() {
		foreach (var data in TypeData.RegisteredTypes.Values)
			data.CleanCache();
	}

	static void StaticPatch(MethodBase __originalMethod,ref object __0) {
		if (
			!(
				TypeData.RegisteredTypes.TryGetValue(__originalMethod.DeclaringType,out var data)
			&&	data.TargetSettings.TryGetValue(__originalMethod,out var newVal)
		)
		||	newVal.Equals(__0)
		) return;

		SomniumMelon.EasyLog($"{__originalMethod.Name} | {__0} -> {newVal}");
		__0 = newVal;
	}
}
