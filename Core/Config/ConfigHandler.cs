namespace SomniumCustomFixes.Config;

abstract class ConfigHandler {
	internal readonly Dictionary<string,ConfigElement> Elements = [];

	internal abstract void RegisterConfigElement<Type>(ConfigElement<Type> element);

	internal abstract void SetConfigValue<Type>(ConfigElement<Type> element,Type value);
	internal abstract Type GetConfigValue<Type>(ConfigElement<Type> element);

	internal virtual void LoadConfig() {
		foreach (var element in Elements.Values)
			element.BoxedValue = element.GetLoaderConfigValue();
	}

	internal abstract void SaveConfig();

	internal virtual void OnAnyConfigChanged() {}
}
