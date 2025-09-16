namespace SomniumCustomFixes.Helpers;

class SettingInfo {
	internal delegate bool Condition<T>(T obj,ref object newVal) where T : uObject;

	internal Type Type { get; init; }
	internal PropertyInfo Property { get; init; }
	internal MelonPreferences_Entry PrefEntry { get; init; }

	internal object TargetValue { get; init; }

	internal bool DoPatchStatic { get; init; }

	// Overriden with Generic versions
	internal Condition<uObject> Conditional { get; init; }

	internal virtual void InitializeTypeData() {}
}

class SettingInfo<T> : SettingInfo where T : uObject {
	internal static Condition<T> DefaultCondition = (_,ref _) => true;

	internal new Condition<T> Conditional { get; init; }

	internal override void InitializeTypeData() {
		TypeData.RegisteredTypes.TryGetValue(Type,out var data);
		data ??= new TypeData<T>();

		var dataT = (TypeData<T>)data;

		var setter = Property.SetMethod;

		dataT.Properties.TryAdd(setter,Property.GetMethod);
		dataT.TargetSettings.TryAdd(setter,TargetValue);

		if (PrefEntry is not null) {
			var bindings = dataT.PreferenceBindings;

			if (!bindings.TryGetValue(PrefEntry,out var prefSetters)) {
				prefSetters = [];
				bindings.TryAdd(PrefEntry,prefSetters);
			}

			prefSetters.Add(setter);
		}

		dataT.Conditionals.Add(setter,Conditional);
	}

	internal SettingInfo(string name,object targetVal,Condition<T> conditional = null) {
		var type = typeof(T);
		var property = type.GetProperty(name,AccessTools.all);

		ArgumentNullException.ThrowIfNull(property);
		ArgumentNullException.ThrowIfNull(property.SetMethod);

		Type = type;
		Property = property;

		TargetValue = targetVal;

		DoPatchStatic =
			AccessTools.IsStatic(property)
		&&	Type.GetField($"NativeFieldInfoPtr_{name}",AccessTools.all) is null
		;

		Conditional = conditional ?? DefaultCondition;
	}

	internal SettingInfo(string name,MelonPreferences_Entry entry,Condition<T> conditional = null) : this(name,entry.BoxedValue,conditional) =>
		PrefEntry = entry;
}
