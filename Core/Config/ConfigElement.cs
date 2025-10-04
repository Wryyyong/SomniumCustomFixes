namespace SomniumCustomFixes.Config;

abstract class ConfigElement {
	internal string Category { get; init; }
	internal string Identifier { get; init; }
	internal string LongName { get; init; }
	internal string Description { get; init; }

	internal abstract object BoxedValue { get; set; }

	internal Action OnValueChangedNotify { get; set; }

	internal abstract object GetLoaderConfigValue();
	internal abstract void ResetToDefault();
}

sealed partial class ConfigElement<Type> : ConfigElement {
	Type _value;
	internal Type Value {
		get => _value;
		set => SetValue(value);
	}
	internal override object BoxedValue {
		get => _value;
		set => SetValue((Type)value);
	}
	internal Type DefaultValue { get; init; }

	internal ConfigValidator<Type> Validator { get; init; }

	internal Action<Type> OnValueChanged { get; set; }

	void SetValue(Type value) {
		if (
			(
				_value is null
			&&	value is null
		)
		||	(
				_value is not null
			&&	_value.Equals(value)
		)
		) return;

		if (Validator is not null)
			value = Validator.EnsureValid(value);

		_value = value;
		ConfHandler.SetConfigValue(this,value);

		OnValueChanged?.Invoke(value);
		OnValueChangedNotify?.Invoke();

		ConfHandler.OnAnyConfigChanged();
	}

	internal override object GetLoaderConfigValue() => ConfHandler.GetConfigValue(this);

	internal override void ResetToDefault() => Value = DefaultValue;

	internal ConfigElement(
		string category,
		string identifier,
		Type defaultVal,
		string desc = null,
		ConfigValidator<Type> validator = null
	) {
		Category = category;
		Identifier = identifier;
		Description = desc;
		Validator = validator;

	#if MELON
		if (ConfigSets.TryGetValue(identifier,out var set)) {
			LongName = set.LongName;
			Description += "\n" + set.PossibleValues;
		}
	#endif

		_value = DefaultValue = defaultVal;

		ConfHandler.RegisterConfigElement(this);
	}
}
