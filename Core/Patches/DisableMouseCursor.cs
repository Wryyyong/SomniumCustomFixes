namespace SomniumCustomFixes.Patches;

[HarmonyPatch(typeof(Cursor),nameof(Cursor.visible),MethodType.Setter)]
static class DisableMouseCursor {
	static ConfigElement<bool> RunPatch;

	static void PatchInit() =>
		RunPatch = new(
			"Miscellaneous",
			"DisableMouseCursor",
			false
		);

	static void Prefix(ref bool __0) {
		if (!RunPatch.Value) return;

		__0 = false;
	}
}
