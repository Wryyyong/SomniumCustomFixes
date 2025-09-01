using UnityEngine;

namespace SomniumCustomFixes;

[HarmonyPatch(typeof(Cursor),nameof(Cursor.visible),MethodType.Setter)]
static class DisableMouseCursor {
	static MelonPreferences_Entry<bool> RunPatch;

	internal static void Init() {
		RunPatch = SomniumMelon.Settings.CreateEntry(
			"DisableMouseCursor",
			false,
			"Disable mouse cursor"
		);
	}

	static void Prefix(ref bool __0) {
		if (!RunPatch.Value) return;

		__0 = false;
	}
}
