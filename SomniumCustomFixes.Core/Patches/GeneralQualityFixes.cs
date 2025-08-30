using System;
using System.Collections.Generic;
using System.Reflection;

using UnityEngine;

namespace SomniumCustomFixes;

[HarmonyPatch]
static class GeneralQualityFixes {
	private class SettingInfo {
		readonly Type _type;
		readonly string _name;
		readonly PropertyInfo _property;
		readonly object _targetValue;

		internal Type Type { get => _type; }
		internal string Name { get => _name; }
		internal PropertyInfo Property { get => _property; }
		internal object TargetValue { get => _targetValue; }

		internal SettingInfo(Type type,string name,object trgt) {
			_type = type;
			_name = name;
			_property = AccessTools.Property(type,name);
			_targetValue = trgt;
		}
	}

	static readonly List<SettingInfo> SettingsIndex = [
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
		new(
			type: typeof(QualitySettings),
			name: nameof(QualitySettings.shadowDistance),
			trgt: 130f
		),
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

	internal static void Init() {
		object[] paramList = [null];

		foreach (SettingInfo info in SettingsIndex) {
			MethodInfo setter = info.Property.SetMethod;
			object defaultVal = info.Property.GetMethod?.Invoke(null,null);

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
