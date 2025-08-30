using UnityEngine.Rendering.Universal;

namespace SomniumCustomFixes;

[HarmonyPatch(typeof(UniversalRenderPipelineAsset),nameof(UniversalRenderPipelineAsset.CreatePipeline))]
static class URPAssetFixes {
	const ShadowResolution TargetShadowRes = ShadowResolution.
	#if AINI
		_4096
	#elif AINS
		_8192
	#endif
	;

	static void Postfix(UniversalRenderPipelineAsset __instance) {
		__instance.m_AdditionalLightsShadowmapResolution = TargetShadowRes;
		__instance.m_LocalShadowsAtlasResolution = TargetShadowRes;
		__instance.m_MainLightShadowmapResolution = TargetShadowRes;
		__instance.m_MaxPixelLights = 4;
		__instance.m_ShadowAtlasResolution = TargetShadowRes;
		__instance.m_ShadowType = ShadowQuality.SoftShadows;
		__instance.shadowCascadeOption = ShadowCascadesOption.FourCascades;

		#if AINS
		__instance.softShadowQuality = SoftShadowQuality.High;
		#endif
	}
}
