namespace SomniumCustomFixes.Config;

abstract class ConfigValidator(Type type) {
	protected abstract object CreateLoaderValidator();
	internal abstract object EnsureValid(object value);

	internal Type Type { get; init; } = type;
	internal object LoaderValidator { get; init; }
}

partial class ConfigRange<Type> : ConfigValidator where Type : IComparable {
	internal Type ValueMin { get; init; }
	internal Type ValueMax { get; init; }

	internal override object EnsureValid(object value) =>
		ValueMax.CompareTo(value) < 0 ?	ValueMax
	:	ValueMin.CompareTo(value) > 0 ?	ValueMin
	:	value
	;

	internal ConfigRange(Type valueMin,Type valueMax)
		: base(typeof(Type))
	{
		ArgumentNullException.ThrowIfNull(valueMin);
		ArgumentNullException.ThrowIfNull(valueMax);

		if (valueMin.CompareTo(valueMax) >= 0)
			throw new ArgumentException($"Min value ({valueMin}) must be less than max value ({valueMax})");

		ValueMin = valueMin;
		ValueMax = valueMax;

		LoaderValidator = CreateLoaderValidator();
	}
}
