using System.Collections;
using System.Collections.Generic;
using System.Text;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Interact
{
	public class InteractStorage : MonoBehaviour
	{

		public string key = "self";
		public InteractStorageList stored = new InteractStorageList();
		public InteractTransformList objects = new InteractTransformList();
		public InteractScriptList scripts = new InteractScriptList();
		public List<TransformsList> tbuckets;
		public InteractScriptObjList scriptObjs = new InteractScriptObjList();


        [Header("Act on proxy instead. Use when destroying obj would cancel events.")]
		public InteractStorage proxy;

		internal List<InteractState> recentlySpawned = new List<InteractState>();

		internal InteractState state;
		public static InteractStorage global;

		static object TARGET => String.Intern("target");
		static object PARENT => String.Intern("parent");
		static object PARENT2 => String.Intern("parentparent");
		static object PARENTANY => String.Intern("anyparent");
		static object SPAWNBY => String.Intern("spawnBy");
		static object SCRIPT => String.Intern("script");
		static object SCRIPTOBJ => String.Intern("scriptobj");
		static object OBJ => String.Intern("obj");
		static object SELF => String.Intern("self");
		static object NULL => String.Intern("null");
		delegate void TriggerDelegate(string trigger, InteractModule module = null);

		static List<string> modules = new List<string> { "obj", "prefs", (string)SCRIPT, "trigger", "layer", "prop", (string)SCRIPTOBJ };

		static InteractRunnerTrigger run;
		static string code { get => run.code; set => run.code = value; }
		static InteractState Self => run.self;
		static InteractState Target => run.target;
		static List<InteractStorage> storages => run.storages;
		static bool log => run.log;

		internal void ValidateComponents(InteractStorage sample = null)
		{
			if(state == null) state = GetComponent<InteractState>();
			if(sample != null)
            {
				stored.Init(sample.stored);
				objects.Init(sample.objects);
				scripts.Init(sample.scripts);
				tbuckets = new List<TransformsList>();
				tbuckets.AddRange(sample.tbuckets);
				scriptObjs.Init(sample.scriptObjs);
            }
		}

		void Awake()
		{
			ValidateComponents();
			if (key == "globals" || key == "global")
			{
				global = this;
			}
		}
		static InteractStorage Redirect(string source, List<InteractStorage> storages = null, InteractStorage defValue = null)
        {
			return Redirect(source, Self, Target, storages, defValue);
		}

		///Finds correct storage based on source ("self", "target", "parent", "script+name", "obj+name", "anyparent", "parent")
		static InteractStorage Redirect(string source, InteractState self, InteractState target, List<InteractStorage> storages = null, InteractStorage defValue = null)
		{
			var searchInStorageSrc = source;
			var srcObj = (object)string.Intern(source);
			if (srcObj == SELF) // else is important here
			{
				return self.store;
			}
			else if (srcObj == TARGET)
			{
				if (target != null)
					storages = target.pickup.storages;
				else Logs.W($"Action is using target, but target is null, doesn't have storage. {source}", logAlways:true);
				searchInStorageSrc = (string)SELF;
			}
			else if (srcObj == NULL)
			{
				return null;
			}
			else if (srcObj == PARENT)
			{
				if (self.transform.parent)
					storages = self.transform.parent.GetComponent<InteractState>().pickup.storages;
				searchInStorageSrc = (string)SELF;
			}
			else if (srcObj == PARENT2)
			{
				if (self.transform.parent && self.transform.parent.parent)
					storages = self.transform.parent.parent.GetComponent<InteractState>().pickup.storages;
				searchInStorageSrc = (string)SELF; // flexSelf
			}
			else if (srcObj == PARENTANY)
			{
				if (self.transform.parent)
					storages = self.transform.parent.GetComponentInParent<InteractState>().pickup.storages;
				searchInStorageSrc = (string)SELF; // flexSelf
			}
			else if (srcObj == SPAWNBY)
			{
				return self.spawnBy.store;
			}
			else if (source.Contains('+'))
			{
				var items = source.Split("+");
#pragma warning disable CS0252
				if (SCRIPT == string.Intern(items[0]))
					return (InteractStorage)self.store.scripts[items[1]].script;
#pragma warning restore CS0252
#pragma warning disable CS0253
				else if (string.Intern(items[0]) == OBJ)
					return self.store.objects[items[1]].prefab.GetComponent<InteractStorage>();
#pragma warning restore CS0253
			}

			InteractStorage found = null;
			if (storages != null)
				found = Find(searchInStorageSrc, storages, self.transform);
			if (found == null)
			{
				if (defValue != null) // if it's null, expect null to be allowed
					Logs.L("No matches, using object of storage. " + searchInStorageSrc + " -> " + defValue, self, log: log);
				found = defValue;
			}
			return found;
		}

		// invoked by "Respawn"
		void Respawn()
		{
			Scene scene = SceneManager.GetActiveScene();
			SceneManager.LoadScene(scene.name);
		}

		public void SetPropInt(string prop, string value)
		{
			var props = stored;
			props.Init(prop, 0);

			int res;
			if (int.TryParse(value, out res))
				props[prop].value = res;
			props[prop].svalue = value.ToString();
		}

		static void SetValue(string source, string prop, string value, List<InteractStorage> storages)
		{
			// int/str value setter
			var storage = Redirect(source, storages);
			storage.SetPropInt(prop, value);
		}

		internal void SetObj(string objkey, Transform target)
		{
			objects.Init(objkey, null);
			objects[objkey].prefab = target;
		}

		public interface IValue
		{
			object Value();
		}

		class EncapsulateParser : IValue
		{
			StringBuilder items = new StringBuilder();
			List<object> outRes = new List<object>();
			public Type predictedType { get; private set; }

			char startKey;
			char endKey;

			public EncapsulateParser(char startKey, char endKey)
			{
				this.startKey = startKey;
				this.endKey = endKey;
			}

			public void Add(string str)
			{
				items.Append(str.Replace(" ", ""));
			}

			public void Complete()
			{
				// items -> outRes
				var str = items.ToString().Substring(1, items.Length - 2);
				var each = str.Split(',');
				bool allNums = true;
				bool allInts = true;
				for (int i = 0; i < each.Length; i++)
				{
					float fvalue = 0;
					if (float.TryParse(each[i], out fvalue))
					{
						if (fvalue % 1 != 0)
						{
							allInts = false;
							outRes.Add(fvalue);
						}
						else outRes.Add((int)fvalue);
					}
					else
					{
						allNums = false;
						outRes.Add(each[i]);
					}
				}
				PredictType(each.Length, allNums, allInts);
			}

			private void PredictType(int len, bool allNums, bool allInts)
			{
				if (allNums)
				{
					if (len == 2)
						predictedType = allInts ? typeof(Vector2Int) : typeof(Vector2);
					else if (len == 3)
						predictedType = allInts ? typeof(Vector3Int) : typeof(Vector3);
					else if (len > 1) predictedType = allInts ? typeof(int[]) : typeof(float[]);
					else predictedType = allInts ? typeof(int) : typeof(float);
				}
				else predictedType = null;
			}

			internal bool IsEnd(string v)
			{
				return v.EndsWith(endKey);
			}

			public object Value()
			{
				if (predictedType == typeof(Vector2Int))
				{
					return new Vector2((int)outRes[0], (int)outRes[1]);
				}
				else if (predictedType == typeof(Vector3Int))
				{
					return new Vector3((int)outRes[0], (int)outRes[1], (int)outRes[2]);
				}
				else if (predictedType == typeof(Vector3))
				{
					return new Vector3((float)outRes[0], (float)outRes[1], (float)outRes[2]);
				}
				else if (predictedType == typeof(Vector2))
				{
					return new Vector2((float)outRes[0], (float)outRes[1]);
				}
				else if (predictedType == typeof(int[]))
				{
					int[] ints = new int[outRes.Count];
					for (int i = 0; i < outRes.Count; i++)
						ints[i] = (int)outRes[i];
					return ints;
				}
				else if (predictedType == typeof(float[]))
				{
					float[] ints = new float[outRes.Count];
					for (int i = 0; i < outRes.Count; i++)
						ints[i] = (float)outRes[i];
					return ints;
				}
				return outRes;
			}
		}
		enum Subaction { None, Spawn, Assign, Disable, Enable, Logger }
		// self script Move Func MoveDir negative
		static bool SecondActivation(InteractRunnerTrigger trigger)
		{
			string code = trigger.code;
			var prefabs = trigger.prefabs;
			var actionSelf = Self;
			var storages = trigger.storages;
			var log = trigger.log;
			if (code == null)
				Logs.E("Empty code somewhere", actionSelf, alwaysLog: true);
			// parses by structure instead of by order
			// spawn prefs shields self parent=self
			// version command reference_Or_value subparams
			if (log)
				Debug.Log($"handling ver2: code:{code}", actionSelf);
			// iteration data
			InteractStorage source = null;
			string propName = "";
			string module = "";
			string tag = "";
			object ref1 = null;
			object ref2 = null;
			EncapsulateParser encapsulate = null;

			// states
			int mode = 1; // mode 0: unknown, mode 1: search
			int calculation = 0; // 0 : no calc, +1: calc modes(1:add)

			// result data
			Subaction subaction = Subaction.None;
			bool success = false;

			// results
			List<object> refs = new List<object>();
			Dictionary<string, object> parameters = new Dictionary<string, object>();
			// 2: source(self, target), 21: module(obj, script), 22:tag, 
			// 3: read reference
			// 4: calculation, 5: end check.

			// iterations
			StringBuilder logSeq = new StringBuilder();
			logSeq.Append(" (" + code + ")::  ");
			Queue<string> codes = new Queue<string>();
			codes.Enqueue(code);
			logSeq.Append(actionSelf.ToString() + " ");
			while (codes.Count > 0)
			{
				var icode = codes.Dequeue();
				List<string> items = new List<string>(icode.Split(" "));
                for (int i = 0; i < items.Count; i++)
                {
					if (char.IsDigit(items[i][0]))
						continue;
					var its = items[i].Split('.');
					if (its.Length > 1)
					{
						items.RemoveAt(i);
						items.InsertRange(i, its);
					}
				}

				int lastId = items.Count - 1;
				logSeq.Append("|~|" + mode + ", ");
				int FRESH = 1;
				for (int i = 0; i < items.Count;)
				{
					if (mode == 1)
					{
						// reset
						source = null;
						ref1 = null;
						ref2 = null;
						module = "";
						tag = "";
						propName = "";
						calculation = 0;

						var command = items[i];

						// recognize commaand
						if (command == "spawn")
						{
							subaction = Subaction.Spawn;
							mode = 2;
						}
						else if (command == "set" && items.Count == 5)
						{
							mode = 2; // current support: set spawnBy obj shields self
							subaction = Subaction.Assign;
						}
						else if (command == "trigger")
						{
							module = "trigger";
							mode = 2;// trigger self trigger customTrigger
									 // // trigger self customTrigger
						}
						else if (command == "add" && items.Count == 4)
						{
							mode = 2; // add self test 1
							calculation = 1;
						}
						else if (command == "Func")
						{
							// OBJ Func arg1 arg2... [Func arg21 arg 22]
							mode = 7;
							i--;
						}
						else if (command.Contains('=') && (command.Length > 2 && command != "="))
						{
							// it's dynamic argument
							parameters.Add(command, null);
							var arg = command.Split('=');
							codes.Enqueue(arg[1]);
							mode = FRESH;
						}
						else if (command == "log")
						{
							mode = 5;
						}
						else if (command.StartsWith('('))
						{
							if (encapsulate != null)
								Debug.LogError("Unsupported, nested ()");
							encapsulate = new EncapsulateParser('(', ')');
							mode = 8;
							encapsulate.Add(items[i]);
							if (encapsulate.IsEnd(items[i]))
							{
								encapsulate.Complete();
								mode = FRESH;
								refs.Add(encapsulate);
								encapsulate = null;
								logSeq.Append(mode + " '" + items[i] + "',");
							}
						}else if(command == "disable")
                        {
							mode = 201;
							subaction = Subaction.Disable;
						}
						else if (command == "enable")
						{
							mode = 202;
							subaction = Subaction.Enable;
						} 
						else
						{
							// maybe it's source
							if (i < items.Count - 1)
							{
								mode = 2;
								i--;
							}
						}
					}
					else if (mode == 8)
					{
						encapsulate.Add(items[i]);
						if (encapsulate.IsEnd(items[i]))
						{
							encapsulate.Complete();
							mode = FRESH;
							refs.Add(encapsulate);
							encapsulate = null;
						}
					}
					else if (mode == 7)
					{
						// FUNC:pack all to end
						refs.Add(items[i]);
					}else if(mode == 201 || mode == 202) // short 'self module' -> 'module'
					{
						if (modules.Contains(items[i]))
						{
							source = Redirect((string)SELF, storages, null);
							mode = 21;
						}
						else
						{
							mode = 2;
						}
						i--;
					}
					else if (mode == 2) // find storage with id (ex. 'self')
					{
						var src = items[i];
						source = Redirect(src, storages, null);
						if (source == null)
						{
							// failed to find any -> it's not source, or source ended
							mode = 4; // add item value
							i--;
						}
						else
						{
							ref1 = source;
							mode = 21;
						}
					}
					else if (mode == 21) // modules
					{
						if (module == "")
						{
							module = items[i];

							if (modules.Contains(module))
								mode = 22;
							else
							{
								// not valid module, or end of item, or property
								logSeq.Append($" Invalid module! ({module})");
								module = "";
								mode = 6;
								i--;
							}
						}
						else
						{
							mode = 22;
							// short skip: module was already assigned earlier
							if (items[i] != module)
								i--;
						}
						logSeq.Append($" module: '{module}' ");
					}
					else if (mode == 6)
					{ // redirect to end or auto expect prop
						if (calculation == 1)
						{
							propName = items[i];
							source.stored.Init(propName);
							module = "prop";
							mode = 23;
						}
						else
						{
							if (items[i] == "pos")
							{
								ref1 = "pos";
							}
							mode = 4;
							i--;
						}
					}
					else if (mode == 22)
					{
						tag = items[i];
						logSeq.Append($"tag: {tag} ");
						mode = 3;
						if (i == lastId) i--;
					}
					else if (mode == 23)
					{
						i--;
						mode = 22;
					}
					else if (mode == 3)
					{
						CollectModule(prefabs, source, propName, module, tag, ref ref1, ref ref2, logSeq);

						mode = 4;
						i--;
					}
					else if (mode == 4)
					{
						// apply(calc, ref)
						//if(calculation != 0)
						// todo
						// mode = 5
						//else {
						if (calculation == 1)
						{
							refs.Add("calc=" + calculation);
						}
						if (ref1 != null)
						{
							refs.Add(ref1);
							mode = FRESH;
							i--;
						}
						else
						{
							mode = -1;
							logSeq.Append("UNHANDLED-ABORT0REF1");
							break;
						}
						if (ref2 != null)
						{
							refs.Add(ref2);
						}
					}
					else if (mode == 5)
					{// log
						subaction = Subaction.Logger;
						ref1 = code;
						mode = 4;
					}

					i++;
					if (i < items.Count)
						logSeq.Append($"|^| {mode} i:'{items[i]}'");
				}

				if (ref1 != null)
				{
					refs.Add(ref1);
					ref1 = null;
				}
				if (ref2 != null)
				{
					refs.Add(ref1);
					ref2 = null;
				}
				if (items.Count > 0)
					logSeq.Append($"{mode} '{items[items.Count - 1]}' ");
			}
			if (log)
				Debug.Log("end parse -> proc args." + logSeq.ToString());

			// stage 2
			Transform GetTransform(object obj)
			{
				if (log)
					Debug.Log($"Get transform{obj}");
				return obj is Transform ? (Transform)obj : obj is InteractStorage ? ((InteractStorage)obj).transform : ((KeyTransform)obj).prefab;
			}

			int max = 100;
			for (int i = 0; i < refs.Count && i < max; i++)
			{
				if (refs[i] is string src)
				{
					source = Redirect(src, storages, null);
					if (source != null)
					{
						i++; module = (string)refs[i];
						i++; tag = (string)refs[i];
						i--; i--;
						if (CollectModule(prefabs, source, tag, module, tag, ref ref1, ref ref2, logSeq))
						{
							if (ref1 != null)
							{
								refs.RemoveRange(i, 3);
								refs.Insert(i, ref1);
								//Logs.L($"Compress 3 -> {ref1}");

								if (ref2 != null)
								{
									refs.Insert(i + 1, ref2);
								}
							}
							//else
								//Logs.L($"fail compression 2-> '{module}' {tag}");
						}
						else if (module == "pos")
						{
							refs.RemoveRange(i, 2);
							refs.Insert(i, source.transform.position);
							Logs.L($"compress to pos {source}-> {source.transform.position}");
						}
						else
						{
							refs.RemoveAt(i);
							refs.Insert(i, source);
							//Logs.L($"compress only source {source}-> {src}");
						}
					}
					//else Logs.L($"fail compression 1-> {src}");
					source = null;
					module = null;
					tag = null;

					if (src.StartsWith('('))
					{
						if (encapsulate != null)
							Debug.LogError("Unsupported, nested ()");
						encapsulate = new EncapsulateParser('(', ')');
						encapsulate.Add(src);
						if (encapsulate.IsEnd(src))
						{
							encapsulate.Complete();
							refs.RemoveAt(i);
							refs.Insert(i, encapsulate);
						}
					}
				}
			}

			success = true;
			if (refs.Count == 0)
			{
				Debug.Log("Fail: Refs count = 0");
				return false;
			}
			logSeq.Append($"  refs0|{(refs.Count > 0 ? refs[0] : "/")}");
			logSeq.Append($" refs1|{(refs.Count > 1 ? refs[1] : " / ")}");
			logSeq.Append($" refsCount|{refs.Count}");
			if(subaction == Subaction.Disable || subaction == Subaction.Enable)
            {
				bool enable = subaction == Subaction.Enable;
				if (refs[0] is KeyTransform kt)
					kt.prefab.gameObject.SetActive(enable);
				else if (refs[0] is KeyComponent kc) {
					if(kc.script is Rigidbody2D rig2)
						rig2.gravityScale = 0;
					else
						((Behaviour)kc.script).enabled = enable;
				}
				else if (refs[0] is InteractLayer il)
					il.enabled = enable;

				if (refs[0] is KeyTransform disable)
				disable.prefab.gameObject.SetActive(enable);
			}
			else if (subaction == Subaction.Spawn)
			{
				if (refs.Count < 2)
				{
					Debug.Log($"Spawn err: {code}. unknown. last mode: {mode}. found: {refs.Count}");
					return success = false;
				}

				var pref = GetTransform(refs[0]);
				var pos = refs[1] is Vector3 ? (Vector3)refs[1] : GetTransform(refs[1]).position;
				var parent = refs.Count > 2 ? GetTransform(refs[2]) : null;

				var t = Instantiate(pref, parent).transform;
#if NETWORK
			var netObj = t.GetComponent<NetworkObject>();
			if(netObj != null)
				netObj.Spawn();
#endif
				t.transform.position = pos;
				t.transform.rotation = parent != null ? parent.rotation : actionSelf.transform.rotation;
				InteractState[] tStore = t.GetComponentsInChildren<InteractState>();
				for (int i = 0; i < tStore.Length; i++)
				{
					tStore[i].spawnBy = actionSelf;
					actionSelf.store.recentlySpawned.Add(tStore[i]);
				}
			}
			else if (subaction == Subaction.Assign)
			{
				var refTarget = (KeyTransform)refs[0];
				var value = GetTransform(refs[1]);
				refTarget.prefab = value;
			}
			else if (refs[0] is TriggerDelegate actstr)
			{
				if (log)
					Debug.Log("activated " + refs[1].ToString());
				actstr(refs[1].ToString());
			}
			else if (subaction == Subaction.Logger && refs[0] is string msg)
			{
				Debug.Log(msg, actionSelf);
			}
			else if (refs[0] is string calc && calc.StartsWith("calc="))
			{
				int calcMode = int.Parse(calc.Split("=")[1]);
				if (calcMode == 1)
				{
					var target = (InteractStored)refs[1];
					var value = int.Parse((string)refs[2]);
					target.value += value;
				}
			}
			else if (refs.Count > 1 && refs[0] is KeyComponent keyScript && refs[1] is string funcStr && funcStr == "Func")
			{
				if (keyScript.script == null)
				{
					Logs.E($"No script in storage {trigger.self}/script/{keyScript.key}", trigger.self, trigger.self, alwaysLog: true);
				}
				else if (!(keyScript.script is ITFuncStr))
				{
					Logs.E($"Not implemented ITFuncStr {trigger.self}/script/{keyScript.key}", trigger.self, trigger.self, alwaysLog:true);
				}
				else
				{
					var funScript = (ITFuncStr)keyScript.script;
					logSeq.Append($"SCRIPT {funScript.Obj} ");
					List<string> args = new List<string>();
					List<object> oargs = new List<object>();
					// 0:ref obj
					for (int i = 1; i < refs.Count; i++)
					{
						if (refs[i] is string str)
						{
							if (str == "Func")
							{
								logSeq.Append($" Call args->");
								if (args.Count > 0)
								{
									funScript.Func(args, oargs);
									args.Clear();
									oargs.Clear();
								}
							}
							else
							{
								args.Add(str);
								logSeq.Append($"{refs[i]},");
							}
						}
						else
						{
							if (refs[i] is IValue val)
								oargs.Add(val.Value());
							else if (refs[i] is IKeyValueBase kv)
								oargs.Add(kv.Value);
							else oargs.Add(refs[i]);
							logSeq.Append($"{refs[i]},");
						}
					}
					if (args.Count > 0 || oargs.Count > 0)
					{
						logSeq.Append($"-> {args.Count},{oargs.Count}&");
						Logs.L($"v2 FUNC {code} -> {logSeq}", log: log);
						funScript.Func(args, oargs);
					}
				}
			}
			else
			{
				success = false;
			}
			if (log)
			{
				if (!success)
					Debug.Log($"exit fail v2 -> v1 {code} {logSeq}");
				else
					Debug.Log($"ok v2 {code} {logSeq}");
			}
			return success;
		}

		private static bool CollectModule(InteractPrefabs prefs, InteractStorage source, string propName, string module, string tag, ref object ref1, ref object ref2, StringBuilder logSeq)
		{
			var moduleObj = (object)String.Intern(module);
			logSeq.Append($" CollectModule (module: {module} tag: {tag} src:{source.name})");
			if (moduleObj == SCRIPT)
			{
				ref1 = source.scripts[tag];
				logSeq.Append($"ref1: {source}.{ref1}");
				if (ref1 == null) Debug.LogError($"Script IS NULL {tag}->{source}", source);
			}
			else if (moduleObj == SCRIPTOBJ)
			{
				ref1 = source.scriptObjs[tag];
				logSeq.Append($"ref1: {ref1}");
				if (ref1 == null) Debug.LogError($"Scriptobj IS NULL {tag}->{source}", source);
			}
			else if (moduleObj == OBJ)
				ref1 = source.objects.GetReserved(tag);
			else if (module == "prefs")
				ref1 = prefs.FindPrefab(tag);
			else if (module == "trigger")
			{
				ref1 = (TriggerDelegate)source.state.CustomTrigger;
				ref2 = tag;
			}
			else if (module == "prop")
			{
				ref1 = source.stored[propName];
				ref2 = tag;
			}else if(module == "layer")
			{
				ref1 = source.state.module.RealtimeLayers.Get(tag);
			}
			else return false;
			return true;
		}

		static KeyComponent GetReference(string[] items, ref int id)
		{
			var storage = Redirect(items[id], storages); id++;
			var typ = items[id]; id++;
			if (typ == "script")
			{
				var script = storage.scripts[items[id]]; id++;
				return script;
			}
			Debug.LogError($"Unsupported {typ} {code}");
			return null;
		}

		// return true -> handled/content matches, depending on type of command
		// see -> true/false by match
		// false if all fails.
		public static bool Activate(InteractRunnerTrigger run)
		{
			InteractStorage.run = run;

			var prefabs = run.prefabs;
			var actionSelf = run.self;
			// for new blocks, manually return true.
			code = ConvertKeys(code);

			// version 2 first, then v1.
			if (SecondActivation(run))
			{
				return true;
			}

			string ConvertKeys(string code)
			{
				// ... { targets+see self flag } ...{ .. 2 .. } ...
				StringBuilder build = new StringBuilder();

				for (int i = 0; i < code.Length; i++)
				{
					if (code[i] == '{')
					{
						StringBuilder subpiece = new StringBuilder();
						for (int j = i + 1; j < code.Length; j++)
						{
							i = j;
							if (code[j] == '}')
							{
								break;
							}
							subpiece.Append(code[j]);
						}
						var subs = subpiece.ToString().Split('+');
						foreach (var sub in subs)
						{
							int offset = sub[0] == ' ' ? 1 : 0;
							if (sub.Substring(offset, 3) == "see")
								build.Append(ReadInt(sub.Split(' '), 0));
							else build.Append(sub);
						}
					}
					else
					{
						build.Append(code[i]);
					}
				}
				return build.ToString();
			}

			int ReadInt(string[] items, int start)
			{
				// see self prop
				var result = 0;
				if (items[start] == "see")
				{
					var sourceNm = items[start + 1];
					var source = Redirect(sourceNm, storages);
					var prop = items[start + 2];
					source.stored.Init(prop);
					result = source.stored[prop].value;
					if (log) Debug.Log($"Redir {sourceNm} -> {source}", source);
				}
				else result = int.Parse(items[start]);
				if (log) Debug.Log($"ReadInt {code} -> {result}", actionSelf);
				return result; // optimized start
			}

			var selfStore = actionSelf.store;
			string[] items = code.Split(" ");
			if (items.Length == 1)
			{
				if (items[0] == "restart")
					selfStore.proxy.Invoke("Respawn", 2);
				else return false;
				return true;
			}
			#region globals
			if (items.Length == 2 && (items[0] == "global" || items[0] == "globals"))
			{
				if (items[1] == "restart")
				{
					selfStore.proxy.Invoke("Respawn", 2);
				}

				else return false;
				return true;
			}
			#endregion globals

			if (items.Length < 3)
				return false;

			#region special
			if (items[0] == "register")
			{ // register self globals targets
				var first = Redirect(items[1], storages);
				var second = Redirect(items[2], storages);
				var secondSub = items[3];
				if (log)
					Debug.Log("register transform to " + second.transform, actionSelf);
				second.tbuckets.Find(secondSub).transforms.Add(first.transform);
				return true;
			}
			else if (items[0] == "clear" && items.Length >= 3)
			{ // clear null tbucket
				if (items[1] == "null")
				{
					int count = actionSelf.store.tbuckets.Find(items[2]).ClearNulls();
					if (items.Length == 5)
					{
						if (items[3] == "reduce")
						{
							actionSelf.store.stored.Init(items[4]);
							actionSelf.store.stored[items[4]].value -= count;
						}
					}
				}
				else return false;
				return true;
			}
			#endregion special

			#region actions
			else if (items[1] == "action")
			{
				// target action picked
				// self action spawn ore random 1 local
				var source = items[0];
				var command = items[1];
				var doing = items[2];

				var target = Redirect(source, storages);
				var goSource = target.gameObject;
				if (doing == "pickup" || doing == "picked")
					target.transform.parent = actionSelf.transform;
				else if (doing == "drop")
					target.transform.parent = actionSelf.transform.parent;
				else if (doing == "death" || doing == "destroy")
				{
					Destroy(goSource);
				}
				else if (doing == "spawn")
				{
					if (items.Length < 4)
						Debug.LogError("wrong item count, need at least 4", actionSelf);
					// self action spawn ore
					// self action spawn ore random 1 local
					// self action spawn bullet child_0 
					var prefab = items[3];
					float range = 0;
					if (items.Length >= 6)
						range = float.Parse(items[5]);

					bool inLocalSpace = true;
					if (items.Length >= 7)
					{
						var spaceStr = items[6];
						if (spaceStr == "world")
							inLocalSpace = false;
					}

					Transform parent = goSource.transform;
					if (items.Length >= 8)
					{
						var spaceStr = items[7];
						if (spaceStr == "noparent")
							parent = null;
						else if (spaceStr == "parent")
							parent = parent.parent;
						else if (spaceStr == "parentparent")
							parent = parent.parent.parent;
						else if (spaceStr == "target")
							parent = target.transform;
					}
					if (items.Length == 5)
					{
						parent = null;
					}

					Vector3 pos = Vector3.zero;
					if (items.Length >= 5)
					{
						var posStrpre = items[4];
						var posItems = posStrpre.Split("+");

						foreach (var posStr in posItems)
						{
							if (posStr == "self")
								pos += actionSelf.transform.position;
							else if (posStr == "target")
								pos += target.transform.position;
							else if (posStr == "parent")
								pos += actionSelf.transform.parent.position;
							else if (posStr == "random")
								pos += (Vector3)UnityEngine.Random.insideUnitCircle * range;
							else if (posStr.StartsWith("child_"))
							{
								// child_0, child_1...
								var sub = int.Parse(posStr.Substring("child_".Length));
								pos += selfStore.transform.GetChild(sub).transform.position;
							}
							else if (posStr.StartsWith("obj_"))
							{
								// objstr, objname...
								var sub = posStr.Substring("obj_".Length);
								var tt = selfStore.objects[sub].prefab;
								pos += tt.transform.position;
							}
						}
					}
					var pref = prefabs.FindPrefab(prefab);
					if (pref == null) pref = actionSelf.store.objects[prefab].prefab;
					if (pref == null) Debug.LogError($"Miss pref {prefab} in action or on storage.", actionSelf);
					var t = Instantiate(pref, parent).transform;
					t.transform.position = pos;
					t.transform.rotation = actionSelf.transform.rotation;
					InteractState[] tStore = t.GetComponentsInChildren<InteractState>();
					for (int i = 0; i < tStore.Length; i++)
					{
						tStore[i].spawnBy = actionSelf;
						actionSelf.store.recentlySpawned.Add(tStore[i]);
					}
					if (inLocalSpace)
						t.transform.localPosition = pos;
				}
				else return false;
				return true;
			}
			#endregion actions
			#region setter
			// standard setter : set reference1 value_OR_valueAtReference2
			else if (items.Length == 4 && items[0] == "set")
			{
				var command = items[0];
				// set source prop value
				SetValue(items[1], items[2], items[3], storages);
				return true;
			}
			else if (items.Length == 8 && items[0] == "set")
			{


				// set self script Move = self script AiMove
				int id = 1;
				var ref1 = GetReference(items, ref id);
				var op = items[id++];
				var ref2 = GetReference(items, ref id);
				ref1.script = ref2.script;
				return true;
			}
			else if (items.Length == 5 && items[0] == "set")
			{

				// set reference reference_OR_value
				int id = 1;
				var ref1 = GetReference(items, ref id);
				var objStore = Redirect(items[id], storages);
				ref1.script = objStore;
				return true;
			}
			else if (items.Length == 4 || (items.Length == 6 && items[3] == "see")/*redirects second part*/)
			{
				// add, remove, set, enable, also gravity on rigbody
				var source = items[0];
				var command = items[1];
				var prop = items[2];
				var value = items[3];
				var storage = Redirect(source, storages);
				var dict = storage.stored;
				dict.Init(prop, 0);
				if (command == "store" || command == "add")
				{
					int ivalue = ReadInt(items, 3); // refacator up.
					dict[prop].value = Mathf.Max(dict[prop].value + ivalue, 0);
					dict[prop].svalue = dict[prop].value.ToString();
				}
				else if (command == "reduce" || command == "take")
				{
					int ivalue = ReadInt(items, 3); // refacator up.
					if (log)
						Debug.Log($"Reduce::{storage} {ivalue}", storage);
					dict[prop].value = Mathf.Max(dict[prop].value - ivalue, 0);
					dict[prop].svalue = dict[prop].value.ToString();
				}
				else if (command == "set")
				{
					SetValue(source, prop, value, storages);
				}
				else if (command == "enable")
				{
					var objects = storage.objects;
					var scripts = storage.scripts;
					var module = storage.state.module;
					if (prop == "obj")
					{
						if (log)
							Debug.Log($"Enable code: {code} {value} {storage} '{objects[value]}'", actionSelf);
						if (objects[value].prefab != null)
							objects[value].prefab.gameObject.SetActive(true);
						else Debug.LogError($"Objects prefab {value} is null", actionSelf);
					}
					else if (prop == "script")
						((Behaviour)scripts[value].script).enabled = true;
					else if (prop == "layer")
						module.RealtimeLayers.Get(value).enabled = true;
				}
				else if (command == "disable")
				{
					var objects = storage.objects;
					var scripts = storage.scripts;
					var module = storage.state.module;
					if (log)
						Debug.Log($"Disable code: {code}", actionSelf);
					if (prop == "obj")
						objects[value].prefab.gameObject.SetActive(false);
					else if (prop == "script")
						((Behaviour)scripts[value].script).enabled = false;
					else if (prop == "component")
					{
						if (value == "Rigidbody2D")
							((Rigidbody2D)scripts[value].script).gravityScale = 0;
					}
					else if (prop == "layer")
						module.RealtimeLayers.Get(value).enabled = false;
				}
				else return false;
				return true;
			}
			#endregion setter
			#region check
			else if (items[0] == "see")
			{
				var command = items[0];
				InteractStored stored;
				InteractStored stored2;
				string op;
				if (items.Length == 5)
				{

					// "see parent drill = 1"
					var source = items[1];
					var prop = items[2];
					op = items[3];
					var value = items[4];
					var ivalue = 0;
					bool isString = !int.TryParse(value, out ivalue);
					var target = Redirect(source);
					if (target != null)
					{
						target.stored.Init(prop, 0);

						stored = target.stored[prop];
						if (log)
							Debug.Log(code + ":: " + target + " value:" + stored.value, actionSelf);
						if (op == "=")
						{
							return (isString && stored.svalue.ToString() == value)
								|| (!isString && stored.value == ivalue);
						}
						else if (op == "!=")
						{
							return stored.svalue.ToString() != value
								|| stored.value != ivalue;
						}
						else if (op == "<")
						{
							return stored.value < ivalue;
						}
						else if (op == ">")
						{
							return stored.value > ivalue;
						}
						else if (op == ">=")
						{
							return stored.value >= ivalue;
						}
						else if (op == "<=")
						{
							return stored.value <= ivalue;
						}
						else Debug.LogError("Unknown operator" + op, actionSelf);
					}
					return false;
				}
				else if (items.Length == 7)
				{
					// "see parent drill = see self drill"
					var source = items[1];
					var prop = items[2];
					op = items[3];
					var source2 = items[5];
					var prop2 = items[6];
					var target = Redirect(source);
					var target2 = Redirect(source2);
					target.stored.Init(prop, 0);
					target2.stored.Init(prop2, 0);

					stored = target.stored[prop];
					stored2 = target2.stored[prop2];
					//Debug.Log(code + ":: "+target + " stored:"+stored.svalue);
					if (op == "=")
					{
						return stored.svalue.ToString() == stored2.svalue
						|| stored.value == stored2.value;
					}
					else if (op == "!=")
					{
						return stored.svalue.ToString() != stored2.svalue
						|| stored.value != stored2.value;
					}
					else if (op == "<")
					{
						return stored.value < stored2.value;
					}
					else if (op == ">")
					{
						return stored.value > stored2.value;
					}
					else if (op == ">=")
					{
						return stored.value >= stored2.value;
					}
					else if (op == "<=")
					{
						return stored.value <= stored2.value;
					}
					else Debug.LogError("Unknown operator" + op);
				}
				else return false;
				return true;
			}
			#endregion check
			#region copy-paste obj1 -> obj2
			else if (items[0] == "copyAdd")
			{
				if (items.Length == 5)
				{
					// copyAdd source1 source2 prop amount
					var command = items[0];
					var source1 = Redirect(items[1], storages);
					var source2 = Redirect(items[2], storages);
					var propName = items[3];
					var amount = Mathf.Max(ReadInt(items, 4), 0); // negatives are iffy.
					source1.stored.Init(propName, 0);
					source2.stored.Init(propName, 0);

					var taken = Mathf.Min(amount, source1.stored[propName].value);
					source2.stored[propName].value += taken;
				}
				else if (items.Length == 6)
				{
					// copyAdd 1 source bucket source2 bucket2
					var command = items[0];
					var count = int.Parse(items[1]);
					var first = Find(items[2], storages, actionSelf.transform);
					var tfirst = first.tbuckets.Find(items[3]);
					var second = Find(items[4], storages, actionSelf.transform);
					var tsecond = second.tbuckets.Find(items[5]);

					var temp = Redirect(items[4], storages);
					var dict = temp.stored;
					count = Mathf.Min(count, tfirst.transforms.Count);
					for (int i = 0; i < count; i++)
						tsecond.transforms.Add(tfirst.transforms[0]);
					dict.Init(tsecond.key, 0);
					dict[tsecond.key].value += count;
				}
				else return false;
				return true;
			}
			else if (items[0] == "copypaste")
			{
				// copypaste from to prop value|see
				// copyAdd source1 source2 prop amount
				var command = items[0];
				var source1 = Redirect(items[1], storages);
				var source2 = Redirect(items[2], storages);
				var propName = items[3];
				var amount = ReadInt(items, 4); // negatives are iffy.
				source1.stored.Init(propName, 0);
				source2.stored.Init(propName, 0);

				var taken = Mathf.Min(amount, source1.stored[propName].value);
				source2.stored[propName].value = taken;
				return true;
			}
			else if (items[0] == "copy*")
			{
				// copypaste from to prop value|see
				// copyAdd source1 source2 prop amount
				var command = items[0];
				var source1 = Redirect(items[1], storages);
				var source2 = Redirect(items[2], storages);
				var propName = items[3];
				var amount = ReadInt(items, 4); // negatives are iffy.
				source1.stored.Init(propName, 0);
				source2.stored.Init(propName, 0);

				var taken = Mathf.Min(amount, source1.stored[propName].value);
				source2.stored[propName].value *= taken;
				return true;
			}
			else if (items[0] == "transfer" || items[2] == "transfer")
			{
				if (items.Length == 5 && items[2] == "transfer")
				{
					var command = items[2];
					// a b transfer drill 1
					var first = Find(items[0], storages, actionSelf.transform);
					var target = Find(items[1], storages, actionSelf.transform);
					if (items[1] != "self")
						target = Redirect("self");
					var prop = items[3];
					var value = items[4];

					first.stored.Init(prop, 0);
					target.stored.Init(prop, 0);

					var prev = first.stored[prop].value;
					first.stored[prop].value = Mathf.Max(int.Parse(value) - prev, 0);
					var removed = prev - first.stored[prop].value;
					target.stored[prop].value = Mathf.Max(target.stored[prop].value + removed, 0);
				}
				else if (items.Length == 5 && items[0] == "transfer")
				{
					// transfer source1 source2 prop amount
					var command = items[0];
					var source1 = Redirect(items[1], storages);
					var source2 = Redirect(items[2], storages);
					var propName = items[3];
					var amount = Mathf.Max(int.Parse(items[4]), 0); // negatives are iffy.
					source1.stored.Init(propName, 0);
					source2.stored.Init(propName, 0);
					if (log)
						Debug.Log($"pretransfer {propName} {source1.stored[propName].value} {source2.stored[propName].value}");
					var taken = Mathf.Min(amount, source1.stored[propName].value);
					source1.stored[propName].value -= taken;
					source2.stored[propName].value += taken;
					if (log)
						Debug.Log($"transfer {propName} {taken}");
				}
				else if (items.Length == 6)
				{
					// transfer 1 source bucket source2 bucket2
					var command = items[0];
					var count = int.Parse(items[1]);
					var first = Find(items[2], storages, actionSelf.transform);
					var tfirst = first.tbuckets.Find(items[3]);
					var second = Find(items[4], storages, actionSelf.transform);
					var tsecond = second.tbuckets.Find(items[5]);

					var temp = Redirect(items[4], storages);
					var dict = temp.stored;
					count = Mathf.Min(count, tfirst.transforms.Count);
					for (int i = 0; i < count; i++)
					{
						tsecond.transforms.Add(tfirst.transforms[0]);
						tfirst.transforms.RemoveAt(0);
					}
					dict.Init(tsecond.key, 0);
					dict[tsecond.key].value += count;
				}
				else return false;
				return true;
			}
			else if (items.Length >= 4 && items[1] == "script")
			{
				// self script SetTarget targets 0      -> auto red self
				if (items.Length == 5)
				{
					var write = Redirect(items[0], storages);
					var read = actionSelf;
					var bucketName = items[3];
					var id = int.Parse(items[4]);
					var bucket = selfStore.tbuckets.Find(bucketName);
					if (bucket.transforms.Count > id)
					{
						Transform t = selfStore.tbuckets.Find(bucketName).transforms[id];
						var script = selfStore.scripts[items[2]];
						if (script != null)
						{
							if (script.script is ITFuncStr scrp)
								scrp.Func(new List<string>{ items[2] }, new List<object>{ t });
							else Debug.LogError($"Unsupported script type {items[2]}", actionSelf);
						}
						else Debug.LogError($"Missing script of type ITFunc under name {items[2]}", actionSelf);
					}
				}
				// self script SetTarget self targets 0
				// 0	1		2		 3 	  4		  5  | l6
				else if (items.Length == 6)
				{
					var write = Redirect(items[0], storages);
					var read = Redirect(items[3], storages);
					var id = items.Length == 6 ? int.Parse(items[5]) : 0;
					var bucket = selfStore.tbuckets.Find(items[4]);
					if (bucket.transforms.Count > id)
					{
						Transform t = selfStore.tbuckets.Find(items[4]).transforms[id];
						var script = selfStore.scripts[items[2]];
						if (script != null)
						{
							if (script.script is ITFuncStr scrp)
								scrp.Func(new List<string> { items[2] }, new List<object> { t });
							else Debug.Log($"Unsupported script type {items[2]}", actionSelf);
						}
						else Debug.LogError($"Missing script of type ITFunc under name {items[2]}", actionSelf);
					}
				}
				else return false;
				return true;
			}
			#endregion region copy-paste
			return false;
		}

		public static InteractStorage Find(string name, List<InteractStorage> storages, Transform t)
		{
			for (int i = 0; i < storages.Count; i++)
			{
				if (storages[i] == null)
					Debug.LogError($"Storage is null, assign it.id:{i}", t);
				else if (storages[i].key == name)
					return storages[i];
			}
			if (name == "global" || name == "globals")
				Debug.Log("If crash, from global, activate auto global on object.", t);
			return null;
		}

	}
}