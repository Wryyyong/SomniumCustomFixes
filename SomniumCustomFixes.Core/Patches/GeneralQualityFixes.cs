using System;
using System.Reflection;

namespace SomniumCustomFixes;

[HarmonyPatch]
static class GeneralQualityFixes {
	readonly struct SettingInfo {
		internal PropertyInfo Property { get; init; }
		internal object TargetValue { get; init; }

		internal SettingInfo(Type type,string name,object trgt) {
			PropertyInfo property = type?.GetProperty(name,AccessTools.all);
			if (property is null) return;

			Property = property;
			TargetValue = trgt;
		}
	}

	static readonly SettingInfo[] SettingsIndex = [
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.anisotropicFiltering),
			trgt: AnisotropicFiltering.ForceEnable
		),
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.currentLevel),
			trgt: QualityLevel.Fantastic
		),
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.lodBias),
			trgt: 100f
		),
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.particleRaycastBudget),
			trgt: 4096
		),
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.pixelLightCount),
			trgt: 4
		),
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.shadows),
			trgt: ShadowQuality.All
		),
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.shadowCascades),
			trgt: 4
		),
		/*
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.shadowDistance),
			trgt: 130f
		),
		*/
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.shadowProjection),
			trgt: ShadowProjection.CloseFit
		),
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.shadowResolution),
			trgt: ShadowResolution.VeryHigh
		),
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.softParticles),
			trgt: true
		),
	];

	static readonly Dictionary<MethodInfo,object> DefaultSettings = [];
	static readonly Dictionary<MethodInfo,object> TargetSettings = [];

	static void Init() {
		object[] paramList = [null];

		foreach (SettingInfo info in SettingsIndex) {
			PropertyInfo property = info.Property;

			MethodInfo setter = property.SetMethod;
			object defaultVal = property.GetMethod?.Invoke(null,null);

			DefaultSettings.Add(setter,defaultVal);

			paramList[0] = defaultVal;
			setter?.Invoke(null,paramList);
		}
	}

	static IEnumerable<MethodBase> TargetMethods() {
		foreach (SettingInfo info in SettingsIndex) {
			MethodInfo setter = info.Property.SetMethod;

			TargetSettings.Add(setter,info.TargetValue);

			yield return setter;
		}
	}

	static void Prefix(MethodInfo __originalMethod,ref object __0) {
		TargetSettings.TryGetValue(__originalMethod,out object newVal);

		if (
			newVal is null
		||	newVal.Equals(__0)
		) return;

		SomniumMelon.EasyLog($"{__originalMethod.Name}: {__0} -> {newVal}");
		__0 = newVal;
	}
}
