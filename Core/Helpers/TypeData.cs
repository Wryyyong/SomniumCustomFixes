namespace SomniumCustomFixes.Helpers;

static class TypeData<Class,Value> where Class : uObject {
	static readonly HashSet<(Type,Type)> RegisteredTypes = [];

	static readonly object[] ParamList = [null];
	static readonly List<string> LogMsgs = [];

	internal static readonly Dictionary<MethodInfo,SettingInfo<Class,Value>> InfoData = [];
	internal static readonly Dictionary<ConfigElement<Value>,HashSet<SettingInfo<Class,Value>>> ConfigBindings = [];
	internal static readonly Dictionary<Class,Dictionary<SettingInfo<Class,Value>,Value>> Cache = [];

	internal static bool SetCheck(Class obj,SettingInfo<Class,Value> info,Value oldVal,ref Value newVal) =>
		info.SetCondition(obj,ref newVal)
	&&	!newVal.Equals(oldVal)
	;

	internal static void CleanCache() {
		foreach (var obj in Cache.Keys) {
			if (
				!(
					obj is null
				||	!obj
			)) continue;

			Cache.Remove(obj);
		}
	}

	internal static void UpdateCache() {
		foreach (var obj in Resources.FindObjectsOfTypeAll<Class>()) {
			if (Cache.ContainsKey(obj)) continue;

			foreach (var info in InfoData.Values)
				if (info.CacheCondition(obj))
					goto Add;

			continue;

		Add:
			Cache.Add(obj,[]);
		}
	}

	internal static void Refresh() {
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

						if (!SetCheck(obj,info,oldVal,ref newVal)) continue;

						LogMsgs.Add($"{obj.name} :: {setter.Name} | {oldVal} -> {newVal}");
						oldValList[info] = oldVal;
					}

					paramVal = newVal;
					setter.Invoke(obj,ParamList);
				}
			}
		} catch {
			throw;
		} finally {
			paramVal = null;

			EasyLog([.. LogMsgs]);
			LogMsgs.Clear();
		}
	}

	internal static void FullUpdate() {
		CleanCache();
		UpdateCache();
		Refresh();
	}

	internal static void SetupInfo(SettingInfo<Class,Value> info,out bool doPatch) {
		doPatch = RegisteredTypes.Add(info.Types);

		InfoData.TryAdd(info.Setter,info);

		var element = info.ConfigElement;

		if (element is null) return;

		if (!ConfigBindings.TryGetValue(element,out var confInfos)) {
			confInfos = [];
			ConfigBindings.TryAdd(element,confInfos);
		}

		confInfos.Add(info);
	}
}
