namespace SomniumCustomFixes.Helpers;

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
