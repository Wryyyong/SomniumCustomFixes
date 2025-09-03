using URP = UnityEngine.Rendering.Universal;

namespace SomniumCustomFixes;

[HarmonyPatch(typeof(URP.UniversalRenderPipelineAsset),nameof(URP.UniversalRenderPipelineAsset.CreatePipeline))]
static class URPAssetFixes {
	const URP.ShadowResolution TargetShadowRes = URP.ShadowResolution.
	#if AINI
		_4096
	#elif AINS
		_8192
	#endif
	;

	static void Postfix(URP.UniversalRenderPipelineAsset __instance) {
		__instance.m_AdditionalLightsShadowmapResolution = TargetShadowRes;
		__instance.m_LocalShadowsAtlasResolution = TargetShadowRes;
		__instance.m_MainLightShadowmapResolution = TargetShadowRes;
		__instance.m_MaxPixelLights = 4;
		__instance.m_ShadowAtlasResolution = TargetShadowRes;
		__instance.m_ShadowType = URP.ShadowQuality.SoftShadows;
		__instance.shadowCascadeOption = URP.ShadowCascadesOption.FourCascades;

	#if AINS
		__instance.softShadowQuality = URP.SoftShadowQuality.High;
	#endif
	}
}
