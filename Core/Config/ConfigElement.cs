namespace SomniumCustomFixes.Config;

interface IConfigElement {
	internal string Category { get; }
	internal string Identifier { get; }
	internal string LongName { get; }
	internal string Description { get; }

	internal object BoxedValue { get; set; }
	internal object DefaultValue { get; }

	internal Action OnValueChangedNotify { get; }

	internal object GetLoaderConfigValue();
}

partial class ConfigElement<Type> : IConfigElement {
	// Explicit redirects
	string IConfigElement.Category => Category;
	string IConfigElement.Identifier => Identifier;
	string IConfigElement.LongName => LongName;
	string IConfigElement.Description => Description;

	object IConfigElement.BoxedValue {
		get => BoxedValue;
		set => BoxedValue = value;
	}
	object IConfigElement.DefaultValue => DefaultValue;

	Action IConfigElement.OnValueChangedNotify => OnValueChangedNotify;

	object IConfigElement.GetLoaderConfigValue() => GetLoaderConfigValue();

	// Implicit members
	internal string Category { get; init; }
	internal string Identifier { get; init; }
	internal string LongName { get; init; }
	internal string Description { get; init; }

	Type _value;
	internal Type Value {
		get => _value;
		set => SetValue(value);
	}
	internal object BoxedValue {
		get => _value;
		set => SetValue((Type)value);
	}
	internal Type DefaultValue { get; init; }
	internal ConfigValidator Validator { get; init; }

	internal Action<Type> OnValueChanged { get; set; }
	internal Action OnValueChangedNotify { get; set; }

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

		var handler = SomniumCore.ConfigHandler;

		if (Validator is not null)
			value = (Type)Validator.EnsureValid(value);

		_value = value;
		handler.SetConfigValue(this,value);

		OnValueChanged?.Invoke(value);
		OnValueChangedNotify?.Invoke();

		handler.OnAnyConfigChanged();
	}

	internal Type GetLoaderConfigValue() => SomniumCore.ConfigHandler.GetConfigValue(this);

	internal ConfigElement(
		string category,
		string identifier,
		Type defaultVal,
		string desc = null,
		ConfigValidator validator = null
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

		SomniumCore.ConfigHandler.RegisterConfigElement(this);
	}
}
