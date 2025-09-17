namespace SomniumCustomFixes.Helpers;

delegate bool Condition<Class,Value>(Class obj,ref Value newVal) where Class : uObject;

class SettingInfo {
	internal (Type,Type) Types { get; init; }
	internal PropertyInfo Property { get; init; }
	internal MethodBase Setter { get; init; }
	internal MethodBase Getter { get; init; }

	internal bool DoAutoPatch { get; init; }
	internal bool DoLogging { get; init; }

	internal virtual bool InitializeTypeData() => false;
}

class SettingInfo<Class,Value> : SettingInfo where Class : uObject {
	const bool Default_DoLogging = true;

	internal Value TargetValue { get; set; }

	internal MelonPreferences_Entry<Value> PrefEntry { get; init; }
	internal Condition<Class,Value> Conditional { get; init; } = static (_,ref _) => true;

	// Ignore method results
#pragma warning disable CA1806
	internal override bool InitializeTypeData() {
		new TypeData<Class,Value>(this,out var doPatch);

		return doPatch;
	}
#pragma warning restore CA1806

	internal SettingInfo(string name,MelonPreferences_Entry<Value> entry)
		: this(name,entry,Default_DoLogging,null) {}
	internal SettingInfo(string name,MelonPreferences_Entry<Value> entry,bool doLogging)
		: this(name,entry,doLogging,null) {}
	internal SettingInfo(string name,MelonPreferences_Entry<Value> entry,Condition<Class,Value> conditional)
		: this(name,entry,Default_DoLogging,conditional) {}
	internal SettingInfo(string name,MelonPreferences_Entry<Value> entry,bool doLogging,Condition<Class,Value> conditional)
		: this(name,entry.Value,doLogging,conditional) =>
			PrefEntry = entry;

	internal SettingInfo(string name,Value targetVal)
		: this(name,targetVal,Default_DoLogging,null) {}
	internal SettingInfo(string name,Value targetVal,bool doLogging)
		: this(name,targetVal,doLogging,null) {}
	internal SettingInfo(string name,Value targetVal,Condition<Class,Value> conditional)
		: this(name,targetVal,Default_DoLogging,conditional) {}
	internal SettingInfo(string name,Value targetVal,bool doLogging,Condition<Class,Value> conditional) {
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
		DoLogging = doLogging;

		Conditional = conditional;
	}
}
