using URP = UnityEngine.Rendering.Universal;

namespace SomniumCustomFixes.Config;

readonly record struct StringSet(
	string LongName = "",
	string PossibleValues = ""
);

sealed partial class ConfigElement<Type> : ConfigElement {
	static readonly Dictionary<string,StringSet> ConfigSets = new() {
		["LogVerbose"] = new(
			"Verbose logging"
		),

		["DisableMouseCursor"] = new(
			"Disable mouse cursor"
		),

		["RenderCharacterModelOutlines"] = new(
			"Render character model outlines"
		),

		["AnisotropicFilteringMode"] = new(
			"Anisotropic filtering mode",

			"Possible values:"
		+	$"\n- \"{AnisotropicFiltering.Disable}\""
		+	$"\n- \"{AnisotropicFiltering.Enable}\""
		+	$"\n- \"{AnisotropicFiltering.ForceEnable}\""
		),

		["AnisotropicFilteringLevel"] = new(
			"Anisotropic filtering level",

			"Possible values: 0-16"
		),

		["TextureFilteringMode"] = new(
			"Texture filtering mode",

			"Possible values:"
		+	$"\n- \"{FilterMode.Point}\""
		+	$"\n- \"{FilterMode.Bilinear}\""
		+	$"\n- \"{FilterMode.Trilinear}\""
		),

		["URP_ShadowResolution"] = new(
			"Shadow Resolution",

			"Possible values:"
		+	$"\n- \"{URP.ShadowResolution._256}\""
		+	$"\n- \"{URP.ShadowResolution._512}\""
		+	$"\n- \"{URP.ShadowResolution._1024}\""
		+	$"\n- \"{URP.ShadowResolution._2048}\""
		+	$"\n- \"{URP.ShadowResolution._4096}\""

		#if AINS
		+	$"\n- \"{URP.ShadowResolution._8192}\""
		#endif
		),

		["AntialiasingMode"] = new(
			"Antialiasing Mode",

			"Possible values:"
		+	$"\n- \"{URP.AntialiasingMode.None}\""
		+	$"\n- \"{URP.AntialiasingMode.FastApproximateAntialiasing}\""
		+	$"\n- \"{URP.AntialiasingMode.SubpixelMorphologicalAntiAliasing}\""

		#if AINS
		+	$"\n- \"{URP.AntialiasingMode.TemporalAntiAliasing}\""
		#endif
		),

		["SMAAQuality"] = new(
			"SMAA Quality",

			"Possible values:"
		+	$"\n- \"{URP.AntialiasingQuality.Low}\""
		+	$"\n- \"{URP.AntialiasingQuality.Medium}\""
		+	$"\n- \"{URP.AntialiasingQuality.High}\""
		),

	#if AINS
		["TAAQuality"] = new(
			"TAA Quality",

			"Possible values:"
		+	$"\n- \"{URP.TemporalAAQuality.VeryLow}\""
		+	$"\n- \"{URP.TemporalAAQuality.Low}\""
		+	$"\n- \"{URP.TemporalAAQuality.Medium}\""
		+	$"\n- \"{URP.TemporalAAQuality.High}\""
		+	$"\n- \"{URP.TemporalAAQuality.VeryHigh}\""
		),
	#endif

		/*//
		["DoUltrawideFixes"] = new(
			"Fix ultrawide UI issues"
		),

		["CustomResolutionWidth"] = new(
			"Custom resolution (width)"
		),

		["CustomResolutionHeight"] = new(
			"Custom resolution (height)"
		),
		//*/
	};
}
