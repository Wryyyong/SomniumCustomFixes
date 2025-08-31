using System.Collections.Generic;
using System.Linq;
using System.Text;

using MelonLoader;

using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SomniumCustomFixes;

[HarmonyPatch(typeof(SceneManager))]
static class UACDFixes {
	static MelonPreferences_Entry<AntialiasingMode> TargetMode;
	static MelonPreferences_Entry<AntialiasingQuality> TargetQuality;
	static readonly Dictionary<Scene,List<UniversalAdditionalCameraData>> Cache = [];

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

	internal static void Init() {
		TargetMode = SomniumMelon.Settings.CreateEntry(
			"AntialiasingMode",
			AntialiasingMode.SubpixelMorphologicalAntiAliasing,
			"Antialiasing Mode",

			$"The type of antialiasing to set UniversalAdditionalCameraData instances to use"
		+	$"\nPossible values:"
		+	$"\n- \"{AntialiasingMode.None}\""
		+	$"\n- \"{AntialiasingMode.FastApproximateAntialiasing}\""
		+	$"\n- \"{AntialiasingMode.SubpixelMorphologicalAntiAliasing}\""

		#if AINS
		+	$"\n- \"{AntialiasingMode.TemporalAntiAliasing}\""
		#endif
		);

		TargetQuality = SomniumMelon.Settings.CreateEntry(
			"AntialiasingQuality",
			AntialiasingQuality.High,
			"SMAA Quality",

			$"The level of quality to use for SMAA"
		+	$"\nHas no effect unless AntialiasingMode is set to \"{AntialiasingMode.SubpixelMorphologicalAntiAliasing}\""
		+	$"\nPossible values:"
		+	$"\n- \"{AntialiasingQuality.Low}\""
		+	$"\n- \"{AntialiasingQuality.Medium}\""
		+	$"\n- \"{AntialiasingQuality.High}\""
		);

		TargetMode.OnEntryValueChangedUntyped.Subscribe(RefreshAllUACD);
		TargetQuality.OnEntryValueChangedUntyped.Subscribe(RefreshAllUACD);
	}

	static void ModifyUACD(UniversalAdditionalCameraData uacd) {
		AntialiasingMode targetMode = TargetMode.Value;
		AntialiasingQuality targetQuality = TargetQuality.Value;

		StringBuilder logMsg = new();

		if (!(
			uacd.antialiasing == targetMode
		&&	uacd.antialiasingQuality == targetQuality
		)) {
			logMsg.Append($" Set [{AntialiasingModes.GetValueOrDefault(uacd.antialiasing)},{AntialiasingQualities.GetValueOrDefault(uacd.antialiasingQuality)}] to [{AntialiasingModes.GetValueOrDefault(targetMode)},{AntialiasingQualities.GetValueOrDefault(targetQuality)}];");

			uacd.antialiasing = targetMode;
			uacd.antialiasingQuality = targetQuality;
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

	static readonly LemonAction<object,object> RefreshAllUACD = (_,_) => {
		foreach (List<UniversalAdditionalCameraData> uacdList in Cache.Values)
			uacdList.ForEach(ModifyUACD);
	};

	[HarmonyPatch(nameof(SceneManager.Internal_SceneLoaded))]
	[HarmonyPostfix]
	static void Internal_SceneLoaded(Scene scene) {
		List<UniversalAdditionalCameraData> newList = [];

		scene
			.GetRootGameObjects().ToList()
			.ForEach(obj => obj
				.GetComponentsInChildren<UniversalAdditionalCameraData>(true).ToList()
				.ForEach(uacd => {
					ModifyUACD(uacd);
					newList.Add(uacd);
				})
			);

		if (!newList.Any()) return;

		Cache.Add(scene,newList);
	}

	[HarmonyPatch(nameof(SceneManager.Internal_SceneUnloaded))]
	[HarmonyPostfix]
	static void Internal_SceneUnloaded(Scene scene) =>
		Cache.Remove(scene);
}
