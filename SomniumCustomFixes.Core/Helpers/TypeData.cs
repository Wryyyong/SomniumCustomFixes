namespace SomniumCustomFixes.Helpers;

class TypeData {
	protected static readonly Dictionary<(Type,Type),TypeData> RegisteredTypes = [];
}

class TypeData<Class,Value> : TypeData where Class : uObject {
	static readonly object[] ParamList = [null];
	static readonly List<string> LogMsgs = [];

	internal Dictionary<MethodBase,SettingInfo<Class,Value>> InfoData { get; init; } = [];
	internal Dictionary<MelonPreferences_Entry<Value>,HashSet<SettingInfo<Class,Value>>> PreferenceBindings { get; init; } = [];
	internal Dictionary<Class,Dictionary<SettingInfo<Class,Value>,Value>> Cache { get; init; } = [];

	internal static TypeData<Class,Value> GetTypeData() =>
		(TypeData<Class,Value>)RegisteredTypes[(typeof(Class),typeof(Value))];

	internal void CleanCache() {
		foreach (var obj in Cache.Keys) {
			if (
				!(
					obj is null
				||	!obj
			)) continue;

			Cache.Remove(obj);
		}
	}

	internal void UpdateCache() {
		foreach (var obj in Resources.FindObjectsOfTypeAll<Class>()) {
			if (Cache.ContainsKey(obj)) continue;

			Cache.Add(obj,[]);
		}
	}

	internal void Refresh() {
		ref var paramVal = ref ParamList[0];

		try {
			foreach (var set in Cache) {
				var obj = set.Key;
				var oldValList = set.Value;

				foreach (var info in InfoData.Values) {
					var setter = info.Setter;
					var oldVal = (Value)info.Getter?.Invoke(obj,null);
					Value newVal;

					if (info.DoAutoPatch)
						newVal = oldVal;
					else {
						newVal = info.TargetValue;

						if (
							!info.Conditional(obj,ref newVal)
						||	newVal.Equals(oldVal)
						) continue;

						if (info.DoLogging)
							LogMsgs.Add($"{obj.name} :: {setter.Name} | {oldVal} -> {newVal}");
					}

					paramVal = newVal;
					oldValList[info] = oldVal;

					setter.Invoke(obj,ParamList);
				}
			}
		} catch {
		} finally {
			paramVal = null;
		}

		SomniumMelon.EasyLog([.. LogMsgs]);
		LogMsgs.Clear();
	}

	internal void FullUpdate() {
		CleanCache();
		UpdateCache();
		Refresh();
	}

	internal TypeData(SettingInfo<Class,Value> info,out bool doPatch) {
		doPatch = RegisteredTypes.TryAdd(info.Types,this);
		var data = GetTypeData();

		data.InfoData.TryAdd(info.Setter,info);

		var prefEntry = info.PrefEntry;

		if (prefEntry is null) return;

		var bindings = data.PreferenceBindings;

		if (!bindings.TryGetValue(prefEntry,out var prefInfos)) {
			prefInfos = [];
			bindings.TryAdd(prefEntry,prefInfos);
		}

		prefInfos.Add(info);
	}
}
