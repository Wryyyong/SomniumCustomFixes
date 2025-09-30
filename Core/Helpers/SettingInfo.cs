namespace SomniumCustomFixes.Helpers;

delegate bool CacheCondition<Class>(Class obj) where Class : uObject;
delegate bool SetCondition<Class,Value>(Class obj,ref Value newVal) where Class : uObject;

abstract class SettingInfo {
	internal abstract (Type,Type) Types { get; }

	internal virtual PropertyInfo Property { get; init; }
	internal virtual MethodInfo Setter { get; init; }
	internal virtual MethodInfo Getter { get; init; }

	internal virtual bool DoAutoPatch { get; init; }
	internal virtual bool DoTypeDataPatch { get; init; }
	internal virtual bool DoLogging { get; init; }

	internal abstract bool InitializeTypeData();
}

class SettingInfo<Class,Value> : SettingInfo where Class : uObject {
	const bool Default_DoLogging = true;
	static readonly CacheCondition<Class> Default_CacheConditional = static _ => true;
	static readonly SetCondition<Class,Value> Default_SetConditional = static (_,ref _) => true;

	internal override (Type,Type) Types => (typeof(Class),typeof(Value));

	internal Value TargetValue { get; set; }

	internal ConfigElement<Value> ConfigElement { get; init; }
	internal CacheCondition<Class> CacheCondition { get; init; }
	internal SetCondition<Class,Value> SetCondition { get; init; }

	internal override bool InitializeTypeData() {
		_ = new TypeData<Class,Value>(this,out var doPatch);

		return doPatch;
	}

	internal SettingInfo(
		string propertyName,
		ConfigElement<Value> element,
		bool doLogging = Default_DoLogging,
		CacheCondition<Class> cacheCondition = null,
		SetCondition<Class,Value> setCondition = null
	) : this(propertyName,element.Value,doLogging,cacheCondition,setCondition) =>
			ConfigElement = element;

	internal SettingInfo(
		string propertyName,
		Value targetVal,
		bool doLogging = Default_DoLogging,
		CacheCondition<Class> cacheCondition = null,
		SetCondition<Class,Value> setCondition = null
	) {
		var typeClass = Types.Item1;
		var property = typeClass.GetProperty(propertyName,AccessTools.all);

		Setter = property.SetMethod;
		Getter = property.GetMethod;

		ArgumentNullException.ThrowIfNull(property);
		ArgumentNullException.ThrowIfNull(Setter);

		Property = property;

		TargetValue = targetVal;

		DoAutoPatch = typeClass.GetField($"NativeFieldInfoPtr_{propertyName}",AccessTools.all) is null;
		DoTypeDataPatch = !AccessTools.IsStatic(typeClass);
		DoLogging = doLogging;

		CacheCondition = cacheCondition ?? Default_CacheConditional;
		SetCondition = setCondition ?? Default_SetConditional;
	}
}
