using BepInEx.Configuration;

namespace SomniumCustomFixes.Config;

partial class ConfigRange<Type> : ConfigValidator<Type> where Type : IComparable {
	protected override object CreateLoaderValidator(Type[] parameters) =>
		new AcceptableValueRange<Type>(parameters[0],parameters[1]);
}
