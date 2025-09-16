namespace SomniumCustomFixes.Helpers;

class TypeData {
	internal static readonly Dictionary<Type,TypeData> RegisteredTypes = [];

	internal Dictionary<MethodBase,MethodBase> Properties { get; init; } = [];
	internal Dictionary<MethodBase,object> TargetSettings { get; init; } = [];
	internal Dictionary<MelonPreferences_Entry,HashSet<MethodBase>> PreferenceBindings { get; init; } = [];

	// Overriden with Generic versions
	internal Dictionary<uObject,Dictionary<MethodBase,object>> Cache { get; init; }
	internal Dictionary<MethodBase,SettingInfo.Condition<uObject>> Conditionals { get; init; }

	internal virtual void CleanCache() {}
	internal virtual void UpdateCache() {}
	internal virtual void Refresh() {}
	internal virtual void FullUpdate() {}
}

class TypeData<T> : TypeData where T : uObject {
	internal new Dictionary<T,Dictionary<MethodBase,object>> Cache { get; init; } = [];
	internal new Dictionary<MethodBase,SettingInfo.Condition<T>> Conditionals { get; init; } = [];

	internal override void CleanCache() {
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

	internal override void UpdateCache() {
		foreach (var obj in Resources.FindObjectsOfTypeAll<T>()) {
			if (Cache.ContainsKey(obj)) continue;

			var oldValList = new Dictionary<MethodBase,object>();
			Cache.TryAdd(obj,oldValList);
		}
	}

	internal override void Refresh() {
		var paramList = new object[1];
		ref var newVal = ref paramList[0];

		var logMsgs = new List<string>();

		foreach (var set in Cache) {
			var obj = set.Key;
			var oldValList = set.Value;

			foreach (var property in Properties) {
				var setter = property.Key;
				var oldVal = property.Value?.Invoke(obj,null);

				Conditionals.TryGetValue(setter,out var condition);

				if (
					!(
						TargetSettings.TryGetValue(setter,out newVal)
					&&	condition(obj,ref newVal)
				)
				||	newVal.Equals(oldVal)
				) continue;

				oldValList[setter] = oldVal;

				logMsgs?.Add($"{obj.name} :: {setter.Name} | {oldVal} -> {newVal}");
				setter.Invoke(obj,paramList);
			}
		}

		SomniumMelon.EasyLog([.. logMsgs]);
	}

	internal override void FullUpdate() {
		CleanCache();
		UpdateCache();
		Refresh();
	}

	internal TypeData() {
		var type = typeof(T);
		if (RegisteredTypes.ContainsKey(type)) return;

		RegisteredTypes.TryAdd(type,this);
	}
}
