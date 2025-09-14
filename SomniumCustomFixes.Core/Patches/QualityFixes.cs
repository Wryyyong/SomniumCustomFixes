using System;
using System.Collections.Generic;
using System.Reflection;

using Il2CppInterop.Runtime.InteropTypes.Arrays;

using uObject = UnityEngine.Object;
using URP = UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SomniumCustomFixes;

[HarmonyPatch]
static class QualityFixes {
	class SettingInfo {
		internal Type Type { get; init; }
		internal PropertyInfo Property { get; init; }
		internal MelonPreferences_Entry PrefEntry { get; init; }

		internal object TargetValue { get; init; }

		internal bool DoPatchStatic { get; init; }
	}

	class SettingInfo<T> : SettingInfo where T : uObject {
		internal SettingInfo(string name,object trgt) {
			var type = typeof(T);
			var property = type.GetProperty(name,AccessTools.all);

			if (property is null) return;

			Type = type;
			Property = property;

			TargetValue = trgt;

			var hasSetter = property.SetMethod is not null;

			DoPatchStatic =
				hasSetter
			&&	AccessTools.IsStatic(property)
			&&	Type.GetField($"NativeFieldInfoPtr_{name}",AccessTools.all) is null
			;

			if (!hasSetter) return;

			// Do not ignore method results
		#pragma warning disable CA1806
			new TypeData<T>();
		#pragma warning restore CA1806
		}

		internal SettingInfo(string name,MelonPreferences_Entry entry) : this(name,entry.BoxedValue) =>
			PrefEntry = entry;
	}

	class TypeData {
		internal static readonly Dictionary<Type,TypeData> RegisteredTypes = [];

		internal Func<Il2CppArrayBase> GetAllObjects { get; init; }
		internal Dictionary<MethodBase,MethodBase> Properties { get; init; } = [];
		internal Dictionary<MelonPreferences_Entry,HashSet<MethodBase>> PreferenceBindings { get; init; } = [];
		internal Dictionary<object,Dictionary<MethodBase,object>> Cache { get; init; } = [];

		internal void CleanCache() {
			foreach (var obj in Cache.Keys) {
				if (
					!(
						obj is null
					||	(
							obj is uObject uObj
						&&	!uObj
					)
				)) continue;

				Cache.Remove(obj);
			}
		}

		internal void UpdateCache() {
			foreach (var obj in GetAllObjects()) {
				if (Cache.ContainsKey(obj)) continue;

				var oldValList = new Dictionary<MethodBase,object>();
				Cache.TryAdd(obj,oldValList);
			}
		}

		internal void Refresh() {
			var paramList = new object[1];
			ref var newVal = ref paramList[0];

			var logMsgs = new List<string>();

			foreach (var set in Cache) {
				var obj = set.Key;
				var oldValList = set.Value;

				foreach (var property in Properties) {
					var setter = property.Key;
					var oldVal = property.Value?.Invoke(obj,null);

					oldValList.TryAdd(setter,oldVal);

					if (
						!TargetSettings.TryGetValue(setter,out newVal)
					||	newVal.Equals(oldVal)
					) continue;

					logMsgs?.Add($"{(obj as uObject).name} :: {setter.Name} | {oldVal} -> {newVal}");
					setter.Invoke(obj,paramList);
				}
			}

			SomniumMelon.EasyLog([.. logMsgs]);
		}

		internal void FullUpdate() {
			CleanCache();
			UpdateCache();
			Refresh();
		}
	}

	class TypeData<T> : TypeData where T : uObject {
		internal TypeData() {
			var type = typeof(T);
			if (RegisteredTypes.ContainsKey(type)) return;

			GetAllObjects = Resources.FindObjectsOfTypeAll<T>;

			RegisteredTypes.TryAdd(type,this);
		}
	}

	static MelonPreferences_Category QualityPrefs;

	static MelonPreferences_Entry<URP.AntialiasingMode> AntialiasingMode;
	static MelonPreferences_Entry<URP.AntialiasingQuality> AntialiasingQuality;

	static readonly Dictionary<MethodBase,object> TargetSettings = [];

	static void Init() {
		#region Preferences Setup

		QualityPrefs = SomniumMelon.PrefCategoryInit("QualitySettings");

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

			foreach (var binding in data.PreferenceBindings) {
				var newVal = binding.Key.BoxedValue;

				foreach (var setter in binding.Value)
					TargetSettings[setter] = newVal;
			}

			data.Refresh();
		}

		static void RefreshUACD(object oldVal = null,object newVal = null) =>
			RefreshSettings<URP.UniversalAdditionalCameraData>();

		AntialiasingMode.OnEntryValueChangedUntyped.Subscribe(RefreshUACD);
		AntialiasingQuality.OnEntryValueChangedUntyped.Subscribe(RefreshUACD);

		#endregion
		#region SettingInfo Setup

		const int DefaultPixelLights = 4;
		const URP.ShadowResolution DefaultShadowRes = URP.ShadowResolution.
		#if AINI
			_4096
		#elif AINS
			_8192
		#endif
		;

		SettingInfo[] index = [
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
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.particleRaycastBudget),
				4096
			),
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.pixelLightCount),
				DefaultPixelLights
			),
			new SettingInfo<QualitySettings>(
				nameof(QualitySettings.shadows),
				ShadowQuality.All
			),
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
				DefaultShadowRes
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_LocalShadowsAtlasResolution),
				DefaultShadowRes
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_MainLightShadowmapResolution),
				DefaultShadowRes
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_MaxPixelLights),
				DefaultPixelLights
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_ShadowAtlasResolution),
				DefaultShadowRes
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.m_ShadowType),
				URP.ShadowQuality.SoftShadows
			),
			new SettingInfo<URP.UniversalRenderPipelineAsset>(
				nameof(URP.UniversalRenderPipelineAsset.shadowCascadeOption),
				URP.ShadowCascadesOption.FourCascades
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
		];

		var harmony = SomniumMelon.HarmonyInst;
		var paramList = new object[1];
		ref var defaultVal = ref paramList[0];

		var staticPatch = new HarmonyMethod(typeof(QualityFixes).GetMethod(nameof(StaticPatch),AccessTools.all));

		foreach (var info in index) {
			var pref = info.PrefEntry;
			var property = info.Property;
			var getter = property.GetMethod;
			var setter = property.SetMethod;

			if (info.DoPatchStatic)
				harmony.Patch(setter,staticPatch);

			TypeData.RegisteredTypes.TryGetValue(info.Type,out var data);
			data.Properties.TryAdd(setter,getter);

			TargetSettings.TryAdd(setter,info.TargetValue);

			if (pref is null) continue;

			var bindings = data.PreferenceBindings;

			if (!bindings.TryGetValue(pref,out var prefSetters)) {
				prefSetters = [];
				bindings.TryAdd(pref,prefSetters);
			}

			prefSetters.Add(setter);
		}

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
			!TargetSettings.TryGetValue(__originalMethod,out var newVal)
		||	newVal.Equals(__0)
		) return;

		SomniumMelon.EasyLog($"{__originalMethod.Name} | {__0} -> {newVal}");
		__0 = newVal;
	}
}
