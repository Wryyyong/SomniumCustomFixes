namespace SomniumCustomFixes.Helpers;

delegate bool CacheCondition<Class>(Class obj) where Class : uObject;
delegate bool SetCondition<Class,Value>(Class obj,ref Value newVal) where Class : uObject;

abstract class SettingInfo {
	internal abstract TypePair Types { get; }

	internal PropertyInfo Property { get; init; }
	internal MethodInfo Setter { get; init; }
	internal MethodInfo Getter { get; init; }

	internal bool DoTypeDataPatch { get; init; }
	internal bool DoAutoPatch { get; init; }
	internal bool DoAutoPatchLogging { get; init; }

	internal abstract bool InitializeTypeData();
}

sealed class SettingInfo<Class,Value> : SettingInfo where Class : uObject {
	const bool Default_DoAutoPatchLogging = true;
	static readonly CacheCondition<Class> Default_CacheConditional = static _ => true;
	static readonly SetCondition<Class,Value> Default_SetConditional = static (_,ref _) => true;

	static readonly TypePair _types = new(typeof(Class),typeof(Value));
	internal override TypePair Types => _types;

	internal Value TargetValue { get; set; }

	internal ConfigElement<Value> ConfigElement { get; init; }
	internal CacheCondition<Class> CacheCondition { get; init; }
	internal SetCondition<Class,Value> SetCondition { get; init; }

	internal override bool InitializeTypeData() {
		TypeData<Class,Value>.SetupInfo(this,out var doPatch);

		return doPatch;
	}

	internal SettingInfo(
		string propertyName,
		ConfigElement<Value> element,
		bool doAutoPatchLogging = Default_DoAutoPatchLogging,
		CacheCondition<Class> cacheCondition = null,
		SetCondition<Class,Value> setCondition = null
	) : this(propertyName,element.Value,doAutoPatchLogging,cacheCondition,setCondition) =>
			ConfigElement = element;

	internal SettingInfo(
		string propertyName,
		Value targetVal,
		bool doAutoPatchLogging = Default_DoAutoPatchLogging,
		CacheCondition<Class> cacheCondition = null,
		SetCondition<Class,Value> setCondition = null
	) {
		var typeClass = Types.Class;

		var property = typeClass.GetProperty(propertyName,AccessTools.all);
		Property = property;
		ArgumentNullException.ThrowIfNull(property);

		Getter = property.GetMethod;
		Setter = property.SetMethod;
		ArgumentNullException.ThrowIfNull(Setter);

		TargetValue = targetVal;

		DoTypeDataPatch = !AccessTools.IsStatic(typeClass);
		DoAutoPatch = typeClass.GetField($"NativeFieldInfoPtr_{propertyName}",AccessTools.all) is null;
		DoAutoPatchLogging = doAutoPatchLogging;

		CacheCondition = cacheCondition ?? Default_CacheConditional;
		SetCondition = setCondition ?? Default_SetConditional;
	}

	~SettingInfo() => TypeData<Class,Value>.RemoveInfo(this);
}
