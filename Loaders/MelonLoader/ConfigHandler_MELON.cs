using MelonLoader;
using MelonLoader.Preferences;

namespace SomniumCustomFixes.Config;

sealed class ConfigHandlerMelon : ConfigHandler {
	static readonly Dictionary<string,MelonPreferences_Category> Categories = [];

	static MelonPreferences_Category GetCategory(string name) {
		if (!Categories.TryGetValue(name,out var category)) {
			category = MelonPreferences.CreateCategory(name);
			category.SetFilePath($"UserData/{ModTitle}.ini",true,false);

			Categories.Add(name,category);
		}

		return category;
	}

	static bool GetEntryFromElement<Type>(ConfigElement<Type> element,out MelonPreferences_Entry<Type> entry) {
		entry = GetCategory(element.Category).GetEntry<Type>(element.Identifier);

		return entry is not null;
	}

	internal override void RegisterConfigElement<Type>(ConfigElement<Type> element) {
		var identifier = element.Identifier;

		if (Elements.ContainsKey(identifier))
			throw new Exception($"Tried to register duplicate ConfigElement");

		var entry = GetCategory(element.Category).CreateEntry(
			identifier,
			element.DefaultValue,
			element.LongName,
			element.Description,
			false,
			false,
			(ValueValidator)element.Validator?.LoaderValidator
		);

		entry.OnEntryValueChanged.Subscribe((_,newVal) => element.Value = newVal);

		Elements.Add(identifier,element);
	}

	internal override void SetConfigValue<Type>(ConfigElement<Type> element,Type value) {
		if (!GetEntryFromElement(element,out var entry)) return;

		entry.Value = value;
	}

	internal override Type GetConfigValue<Type>(ConfigElement<Type> element) =>
		GetEntryFromElement(element,out var entry)
	?	entry.Value
	:	default
	;

	internal override void SaveConfig() {
		foreach (var category in Categories.Values)
			category.SaveToFile(false);
	}
}
