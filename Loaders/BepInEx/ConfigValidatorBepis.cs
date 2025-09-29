using BepInEx.Configuration;

namespace SomniumCustomFixes.Config;

partial class ConfigRange<Type> : ConfigValidator where Type : IComparable {
	protected override object CreateLoaderValidator() => new AcceptableValueRange<Type>(ValueMin,ValueMax);
}
