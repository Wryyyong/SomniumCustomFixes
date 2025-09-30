using MelonLoader.Preferences;

namespace SomniumCustomFixes.Config;

partial class ConfigRange<Type> : ConfigValidator where Type : IComparable {
	protected override object CreateLoaderValidator() => new ValueRange<Type>(ValueMin,ValueMax);
}
