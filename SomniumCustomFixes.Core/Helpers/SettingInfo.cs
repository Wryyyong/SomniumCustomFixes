namespace SomniumCustomFixes.Helpers;

delegate bool Condition<T>(T obj,ref object newVal) where T : uObject;

class SettingInfo {
	internal PropertyInfo Property { get; init; }
	internal MethodBase Setter { get; init; }

	internal bool DoPatchStatic { get; init; }

	internal virtual void InitializeTypeData() {}
}

class SettingInfo<T> : SettingInfo where T : uObject {
	static readonly Condition<T> Default_Condition = static (_,ref _) => true;

	Type Type { get; init; }
	MelonPreferences_Entry PrefEntry { get; init; }
	object TargetValue { get; init; }
	Condition<T> Conditional { get; init; }

	internal override void InitializeTypeData() {
		TypeData.RegisteredTypes.TryGetValue(Type,out var data);
		data ??= new TypeData<T>();

		var dataT = (TypeData<T>)data;


		dataT.Properties.TryAdd(Setter,Property.GetMethod);
		dataT.TargetSettings.TryAdd(Setter,TargetValue);

		if (PrefEntry is not null) {
			var bindings = dataT.PreferenceBindings;

			if (!bindings.TryGetValue(PrefEntry,out var prefSetters)) {
				prefSetters = [];
				bindings.TryAdd(PrefEntry,prefSetters);
			}

			prefSetters.Add(Setter);
		}

		dataT.Conditionals.Add(Setter,Conditional);
	}

	internal SettingInfo(string name,MelonPreferences_Entry entry)
		: this(name,entry,null) {}
	internal SettingInfo(string name,MelonPreferences_Entry entry,Condition<T> conditional)
		: this(name,entry.BoxedValue,conditional) =>
			PrefEntry = entry;

	internal SettingInfo(string name,object targetVal)
		: this(name,targetVal,null) {}
	internal SettingInfo(string name,object targetVal,Condition<T> conditional) {
		var type = typeof(T);
		var property = type.GetProperty(name,AccessTools.all);

		Setter = property.SetMethod;

		ArgumentNullException.ThrowIfNull(property);
		ArgumentNullException.ThrowIfNull(Setter);

		Type = type;
		Property = property;

		TargetValue = targetVal;

		DoPatchStatic =
			AccessTools.IsStatic(property)
		&&	Type.GetField($"NativeFieldInfoPtr_{name}",AccessTools.all) is null
		;

		Conditional = conditional ?? Default_Condition;
	}
}
