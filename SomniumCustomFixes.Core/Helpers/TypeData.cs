using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace SomniumCustomFixes.Helpers;

class TypeData {
	internal static readonly Dictionary<Type,TypeData> RegisteredTypes = [];

	internal Func<Il2CppArrayBase> GetAllObjects { get; init; }
	internal Dictionary<MethodBase,MethodBase> Properties { get; init; } = [];
	internal Dictionary<MethodBase,object> TargetSettings { get; init; } = [];
	internal Dictionary<MelonPreferences_Entry,HashSet<MethodBase>> PreferenceBindings { get; init; } = [];
	internal Dictionary<object,Dictionary<MethodBase,object>> Cache { get; init; } = [];

	internal void CleanCache() {
		foreach (var obj in Cache.Keys) {
			if (
				!(
					obj is null
				||	(
						obj is uObject uObj
					&&	!uObj
				)
			)) continue;

			Cache.Remove(obj);
		}
	}

	internal void UpdateCache() {
		foreach (var obj in GetAllObjects()) {
			if (Cache.ContainsKey(obj)) continue;

			var oldValList = new Dictionary<MethodBase,object>();
			Cache.TryAdd(obj,oldValList);
		}
	}

	internal void Refresh() {
		var paramList = new object[1];
		ref var newVal = ref paramList[0];

		var logMsgs = new List<string>();

		foreach (var set in Cache) {
			var obj = set.Key;
			var oldValList = set.Value;

			foreach (var property in Properties) {
				var setter = property.Key;
				var oldVal = property.Value?.Invoke(obj,null);

				oldValList.TryAdd(setter,oldVal);

				if (
					!TargetSettings.TryGetValue(setter,out newVal)
				||	newVal.Equals(oldVal)
				) continue;

				logMsgs?.Add($"{(obj as uObject).name} :: {setter.Name} | {oldVal} -> {newVal}");
				setter.Invoke(obj,paramList);
			}
		}

		SomniumMelon.EasyLog([.. logMsgs]);
	}

	internal void FullUpdate() {
		CleanCache();
		UpdateCache();
		Refresh();
	}
}

class TypeData<T> : TypeData where T : uObject {
	internal TypeData() {
		var type = typeof(T);
		if (RegisteredTypes.ContainsKey(type)) return;

		GetAllObjects = Resources.FindObjectsOfTypeAll<T>;

		RegisteredTypes.TryAdd(type,this);
	}
}
