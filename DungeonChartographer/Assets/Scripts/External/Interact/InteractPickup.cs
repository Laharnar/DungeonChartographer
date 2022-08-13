using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interact
{

	// Handles links to other storages, and passes triggers to them.
	public class InteractPickup : MonoBehaviour, IInteractRunner
	{

		[Header("Auto use storage from scene, with key 'global'")]
		public bool autoLoadGlobals = false;
		public List<InteractStorage> storages = new List<InteractStorage>(); // Can use storage from other objects.
		InteractState states;

		void Awake()
		{
			ValidateComponents();
		}

		public void LoadGlobals()
		{
			if (autoLoadGlobals)
			{
				storages.Add(InteractStorage.global);
			}
		}

		public bool Run(InteractRunnerTrigger run)
		{
			return InteractStorage.Activate(run);
		}

        internal void ValidateComponents()
        {
			if(states == null) states = GetComponent<InteractState>();

			InteractStorage storage;
			if (TryGetComponent(out storage))
			{
				if(!storages.Contains(storage))
					storages.Add(storage);
			}
		}

        public IEnumerator Trigger(TriggerCount trig)
		{
			var actions = trig.scope.Actions;
			if (actions.Count == 0) yield break;
			var transitionTo = trig.transitionTo;
			var interactions = trig.interactions;
			var trigger = trig.scope.trigger;
			var log = trig.log;
			var upCounter = trig.scope.counters.counter;
			trig.scope.Source.InitCounters(trigger, actions.Count);
			Logs.L("start trig " + trig.scope.trigger, log: log);

			// run whole cycle from 0 to count(non up coroutinable)
			for (int id = 0; id < actions.Count; id++)
			{
				var action = actions[id];
				if (action == null) continue;
				var counter = trig.scope.counters.counters[id];
				if (action.UpNextWaits)
				{
					Logs.L($"Locked:{counter.Locked} {id}", action.self, log: log);
					continue;
				}

				bool isLast = false;
				RunLocal(transitionTo, interactions, log, action, counter, (id) =>
				{
					// EDITOR: special case where call started here
					if (upCounter.Locked) // unlock only if it's coroutinable ATM
					{
						isLast = (int)id >= interactions.Count;
						Debug.Log("Unlock 2");
						if (!action.UpNextWaits || isLast)
							upCounter.i++;
						if(action.UpNextWaits)
							upCounter.Locked = false;
					}
				});
			}

			// and circle from counter to counter (coroutinable)
			var startUpI = upCounter.i;

			for (int j = 0; j < actions.Count; j++)
			{
				int id = (j + startUpI) % actions.Count;
				var action = actions[id];
				if (action == null) continue;
				if (!action.UpNextWaits) continue;
				while (action.UpNextWaits && upCounter.Locked)
					yield return null;
				var counter = trig.scope.counters.counters[id];
				RunLocal(transitionTo, interactions, log, action, counter, (idd) =>
				{
					var idCount = ((Vector2Int) idd);
					bool isLast = idCount.x >= idCount.y-1;
					if (!action.UpNextWaits || isLast)
					{
						//if(!action.OneTickForAll)
						//upCounter.i++;
						upCounter.Locked = false;
					}
				});
				//Debug.Log("run1 " + upCounter.i + " " + trigger + " " + name + " " + id + " " + actions.Count + " " + startUpI);
				if (action.UpNextWaits && counter.Locked && action.UpNextWaits)
				{
					upCounter.Locked = true;
					while (upCounter.Locked)
						yield return null;
					upCounter.i++;
                }
                else
                {
					upCounter.i++;
				}
				//Debug.Log("unlocked "+ upCounter.i + " "+ trigger + " " + name +" " + id + " "+ actions.Count + " "+ startUpI);
			}
			//Debug.Log("unlocked end ");
			if (actions.Count > 0)
				upCounter.i %= actions.Count;
			else Debug.Log("is this impl err? 0 Action when they were before more.");
			actions.Clear();
		}

		void RunLocal(string transitionTo, List<InteractRules> interactions, bool log, InteractAction action, InteractCodeCounter counter, Action<object> onUnlocked)
		{
			// why not action.self.pickup.storages = storages?
			InteractRunnerTrigger temp = new InteractRunnerTrigger("", action.self, action.target, action.spawnSet, storages, log);
			bool pass = true;
			string passFail = "";
			pass = action.Condition(Run, temp, ref passFail);
			Logs.L($"Passing: {pass} locked:{counter.Locked} condTrue: '{passFail}'", action.self, log: log);
			if (pass)
			{
				var x = Transition(states.state, transitionTo, interactions);
				states.state = x != "" ? x : states.state;
			}
			List<string> codes = pass ? action.codes : action.elseCodes;
			if (codes != null && codes.Count > 0)
			{
				// run all codes from lock to end
				bool oneTicksAll = action.UpNextWaits;
				if (oneTicksAll)
				{
					// whole sequence to end
					if(!counter.Locked)
						StartCoroutine(RunAllCodesSlow(counter, codes, log, action, onUnlocked));
				}
				else
				{
					// move next in sequence until 1 lock or end
					counter.i %= codes.Count;
					for (int i = counter.i; i < codes.Count; i++)
					{
						var code = codes[i];
						// counter can get relocked from coroutine.
						if (!action.enableLockLocal || !counter.Locked)
						{
							Handle(code, action, transform, log);
							InteractCoroutine.NextUnhandled(counter, onUnlocked, codes.Count);
							counter.i++;
						}
						if (!counter.Locked)
							onUnlocked(new Vector2Int(i, codes.Count));
						if (counter.Locked && action.enableLockLocal)
							break;
					}
				}
				InteractCoroutine.NextLeftovers(counter);
				counter.i %= codes.Count;
			}
		}

        private IEnumerator RunAllCodesSlow(InteractCodeCounter counter, List<string> codes, bool log, InteractAction action, System.Action<object> onUnlocked)
        {
			for (int i = counter.i; i < codes.Count; i++)
			{
				var code = codes[i];//(i + counter.i) % codes.Count];
									// can get relocked inside this loop
				while (counter.Locked)
					yield return null;
					
				Handle(code, action, transform, log);
				InteractCoroutine.LoadUnhandled(counter);
				if (counter.HasCoroutines())
				{
					var coroutine = counter.DequeueUseCoroutine();

					yield return coroutine.RunCoroutine(onUnlocked, new Vector2Int(counter.i, codes.Count));
				}

				counter.i++;
			}
			// leftovers or added MID execution
			if (!action.enableLockLocal)
				InteractCoroutine.NextLeftovers(counter);
			counter.i %= codes.Count;
		}

		static string Transition(string from, string to, List<InteractRules> interactions)
		{
			// "" -> from
			if (string.IsNullOrEmpty(to) || string.IsNullOrEmpty(from))
				return from;
			// first matching from and to -> first
			foreach (var item in interactions)
			{
				if (item.enabled && item.IsMatch(from, to))
					return item.result;
			}
			return from;
		}

		InteractRunnerTrigger Handle(string code, InteractAction action, Transform transform, bool log)
		{
			var target = action.target;
			if (string.IsNullOrEmpty(code))
				return null;
			if (log)
			{
				Debug.Log($"handling {action.self} '{code}' ->target:{target}", action.self);
			}

			bool exit = false;
			exit = LegacyShortCalls(code, transform, exit);
			if (exit) return null;

			for (int i = 0; i < storages.Count; i++)
			{
				InteractRunnerTrigger runner = new InteractRunnerTrigger(code, action.self, action.target, action.spawnSet, storages, log);
				if (Run(runner))
					return runner;
			}
			Debug.Log($"Unhandled code, missed code '{code}' {action.self}", action.self);
			return null;
		}

		private static bool LegacyShortCalls(string code, Transform transform, bool exit)
		{
			if (code.Length == 4 && code == "drop")
			{
				transform.parent = transform.parent.parent;
				exit = true;
			}
			if (code.Length == 5 && code == "death")
			{
				Destroy(transform.gameObject);
				exit = true;
			}

			return exit;
		}
	}

	public class TriggerCount
	{
		public string transitionTo;
		public List<InteractRules> interactions;
		public InteractScope scope;
		public bool log = false;

		public TriggerCount(string transitionTo, List<InteractRules> interactions, InteractScope scope, bool log)
		{
			this.transitionTo = transitionTo; this.interactions = interactions;
			this.scope = scope; this.log = log;
		}
	}
}