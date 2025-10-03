using MelonLoader.Preferences;

namespace SomniumCustomFixes.Config;

partial class ConfigRange<Type> : ConfigValidator<Type> where Type : IComparable {
	protected override object CreateLoaderValidator(Type[] parameters) =>
		new ValueRange<Type>(parameters[0],parameters[1]);
}
