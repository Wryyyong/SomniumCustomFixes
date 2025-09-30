namespace SomniumCustomFixes.Helpers;

abstract class TypeData {
	protected static readonly Dictionary<(Type Class,Type Value),TypeData> RegisteredTypes = [];

	internal abstract void CleanCache();
	internal abstract void UpdateCache();
	internal abstract void Refresh();
	internal abstract void FullUpdate();
}

class TypeData<Class,Value> : TypeData where Class : uObject {
	static readonly object[] ParamList = [null];
	static readonly List<string> LogMsgs = [];

	internal static (Type Class,Type Value) Types => (typeof(Class),typeof(Value));

	internal readonly Dictionary<MethodBase,SettingInfo<Class,Value>> InfoData = [];
	internal readonly Dictionary<ConfigElement<Value>,HashSet<SettingInfo<Class,Value>>> ConfigBindings = [];
	internal readonly Dictionary<Class,Dictionary<SettingInfo<Class,Value>,Value>> Cache = [];

	internal static TypeData<Class,Value> GetTypeData() =>
		(TypeData<Class,Value>)RegisteredTypes[Types];

	internal static bool SetCheck(Class obj,SettingInfo<Class,Value> info,Value oldVal,ref Value newVal) =>
		info.SetCondition(obj,ref newVal)
	&&	!newVal.Equals(oldVal)
	;

	internal override void CleanCache() {
		foreach (var obj in Cache.Keys) {
			if (
				!(
					obj is null
				||	!obj
			)) continue;

			Cache.Remove(obj);
		}
	}

	internal override void UpdateCache() {
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

	internal override void Refresh() {
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

						if (info.DoLogging)
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
		}

		SomniumCore.EasyLog([.. LogMsgs]);
		LogMsgs.Clear();
	}

	internal override void FullUpdate() {
		CleanCache();
		UpdateCache();
		Refresh();
	}

	internal TypeData(SettingInfo<Class,Value> info,out bool doPatch) {
		doPatch = RegisteredTypes.TryAdd(Types,this);
		var data = GetTypeData();

		data.InfoData.TryAdd(info.Setter,info);

		var element = info.ConfigElement;

		if (element is null) return;

		var bindings = data.ConfigBindings;

		if (!bindings.TryGetValue(element,out var confInfos)) {
			confInfos = [];
			bindings.TryAdd(element,confInfos);
		}

		confInfos.Add(info);
	}
}
