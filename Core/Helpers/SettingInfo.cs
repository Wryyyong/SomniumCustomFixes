namespace SomniumCustomFixes.Helpers;

delegate bool CacheCondition<Class>(Class obj) where Class : uObject;
delegate bool SetCondition<Class,Value>(Class obj,ref Value newVal) where Class : uObject;

class SettingInfo {
	internal (Type,Type) Types { get; init; }
	internal PropertyInfo Property { get; init; }
	internal MethodBase Setter { get; init; }
	internal MethodBase Getter { get; init; }

	internal bool DoAutoPatch { get; init; }
	internal bool DoTypeDataPatch { get; init; }
	internal bool DoLogging { get; init; }

	internal virtual bool InitializeTypeData() => false;
}

class SettingInfo<Class,Value> : SettingInfo where Class : uObject {
	const bool Default_DoLogging = true;
	static readonly CacheCondition<Class> Default_CacheConditional = static _ => true;
	static readonly SetCondition<Class,Value> Default_SetConditional = static (_,ref _) => true;

	internal Value TargetValue { get; set; }

	internal ConfigElement<Value> ConfigElement { get; init; }
	internal CacheCondition<Class> CacheCondition { get; init; }
	internal SetCondition<Class,Value> SetCondition { get; init; }

	internal override bool InitializeTypeData() {
		_ = new TypeData<Class,Value>(this,out var doPatch);

		return doPatch;
	}

	internal SettingInfo(string name,ConfigElement<Value> element,bool doLogging = Default_DoLogging,CacheCondition<Class> cacheCondition = null,SetCondition<Class,Value> setCondition = null)
		: this(name,element.Value,doLogging,cacheCondition,setCondition) =>
			ConfigElement = element;

	internal SettingInfo(string name,Value targetVal,bool doLogging = Default_DoLogging,CacheCondition<Class> cacheCondition = null,SetCondition<Class,Value> setCondition = null) {
		var typeClass = typeof(Class);
		var property = typeClass.GetProperty(name,AccessTools.all);

		Setter = property.SetMethod;
		Getter = property.GetMethod;

		ArgumentNullException.ThrowIfNull(property);
		ArgumentNullException.ThrowIfNull(Setter);

		Types = (typeClass,typeof(Value));
		Property = property;

		TargetValue = targetVal;

		DoAutoPatch = typeClass.GetField($"NativeFieldInfoPtr_{name}",AccessTools.all) is null;
		DoTypeDataPatch = !AccessTools.IsStatic(typeClass);
		DoLogging = doLogging;

		CacheCondition = cacheCondition ?? Default_CacheConditional;
		SetCondition = setCondition ?? Default_SetConditional;
	}
}
