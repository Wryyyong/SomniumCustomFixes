namespace SomniumCustomFixes.Config;

abstract class ConfigValidator<Type> {
	protected abstract object CreateLoaderValidator(Type[] parameters);
	internal abstract Type EnsureValid(Type value);

	internal object LoaderValidator { get; init; }

	protected ConfigValidator(Type[] parameters) =>
		LoaderValidator = CreateLoaderValidator(parameters);
}

partial class ConfigRange<Type> : ConfigValidator<Type> where Type : IComparable {
	protected Type ValueMin { get; init; }
	protected Type ValueMax { get; init; }

	internal override Type EnsureValid(Type value) =>
		ValueMax.CompareTo(value) < 0 ?	ValueMax
	:	ValueMin.CompareTo(value) > 0 ?	ValueMin
	:	value
	;

	internal ConfigRange(Type valueMin,Type valueMax)
		: base([valueMin,valueMax])
	{
		ValueMin = valueMin;
		ValueMax = valueMax;
	}
}
