using System.Collections.Generic;
using System.Text;

using Il2CppGame;

using UnityEngine.Rendering.Universal;

namespace SomniumCustomFixes;

[HarmonyPatch(typeof(CameraController),nameof(CameraController.OnEnable))]
static class UACDFixes {
	static readonly Dictionary<AntialiasingMode,string> AntialiasingModes = new() {
		{AntialiasingMode.None,"None"},
		{AntialiasingMode.FastApproximateAntialiasing,"FXAA"},
		{AntialiasingMode.SubpixelMorphologicalAntiAliasing,"SMAA"},
		#if AINS
		{AntialiasingMode.TemporalAntiAliasing,"TAA"},
		#endif
	};
	static readonly Dictionary<AntialiasingQuality,string> AntialiasingQualities = new() {
		{AntialiasingQuality.Low,"Low"},
		{AntialiasingQuality.Medium,"Medium"},
		{AntialiasingQuality.High,"High"},
	};
	static readonly AntialiasingMode TargetMode = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
	static readonly AntialiasingQuality TargetQuality = AntialiasingQuality.High;

	static void Postfix(CameraController __instance) {
		string TargetModeString = AntialiasingModes.GetValueOrDefault(TargetMode);
		string TargetQualityString = AntialiasingQualities.GetValueOrDefault(TargetQuality);

		foreach (UniversalAdditionalCameraData uacd in __instance.GetComponentsInChildren<UniversalAdditionalCameraData>(true)) {
			StringBuilder logMsg = new();

			if (!(
				uacd.antialiasing == TargetMode
			&& uacd.antialiasingQuality == TargetQuality
			)) {
				logMsg.Append($" Set [{AntialiasingModes.GetValueOrDefault(uacd.antialiasing)},{AntialiasingQualities.GetValueOrDefault(uacd.antialiasingQuality)}] to [{TargetModeString},{TargetQualityString}];");

				uacd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
				uacd.antialiasingQuality = AntialiasingQuality.High;
			}

			if (!uacd.renderPostProcessing) {
				logMsg.Append($" Enabled renderPostProcessing;");

				uacd.renderPostProcessing = true;
			}

			if (string.IsNullOrWhiteSpace(logMsg.ToString())) return;

			SomniumMelon.EasyLog(logMsg
				.Insert(0,$"[{uacd.name}]")
				.ToString(0,logMsg.Length - 1)
			);
		}
	}
}
