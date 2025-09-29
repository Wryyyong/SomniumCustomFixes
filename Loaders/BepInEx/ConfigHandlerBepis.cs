using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace SomniumCustomFixes.Config;

sealed class ConfigHandlerBepis : ConfigHandler {
	readonly ConfigFile Config = (SomniumCore.Loader as BasePlugin).Config;

	internal override void RegisterConfigElement<Type>(ConfigElement<Type> element) {
		var identifier = element.Identifier;

		var entry = Config.Bind(
			new(
				element.Category,
				identifier
			),

			element.DefaultValue,

			new(
				element.Description ?? string.Empty,
				(AcceptableValueBase)element.Validator?.LoaderValidator
			)
		);

		entry.SettingChanged += (_,_) => element.Value = entry.Value;

		Elements.Add(identifier,element);
	}

	internal override void SetConfigValue<Type>(ConfigElement<Type> element,Type value) {
		var identifier = element.Identifier;

		if (!Config.TryGetEntry(element.Category,identifier,out ConfigEntry<Type> entry))
			throw new Exception($"Tried to set non-existent ConfigEntry {identifier}");

		entry.Value = value;
	}

	internal override Type GetConfigValue<Type>(ConfigElement<Type> element) =>
		Config.TryGetEntry(element.Category,element.Identifier,out ConfigEntry<Type> entry)
	?	entry.Value
	:	default
	;

	internal override void SaveConfig() => Config.Save();
}
