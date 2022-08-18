using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Interact
{

	// use for network
	public interface IInteractTunnel
	{
		void Tick(InteractState x, List<InteractRules> interactions, bool log, bool timeBound);
	}
	public interface IInteractTunnel2
	{
		void Tick2(IInteractCode runner);
	}

	public class InteractState : InteractProxy
	{

		public string state;
		protected SpriteRenderer sprite;
		public Collider2D _collider;

		[Header("Obsolete use Quick"), HideInInspector]
		public List<InteractRules> statics = new List<InteractRules>();

		[Header("Misc")]
		[SerializeField] InteractPrefabs color;

		[HideInInspector] public InteractModule module;
		[HideInInspector] public InteractPickup pickup;
		[HideInInspector] public InteractStorage store;

		[Header("Timed")]
		[SerializeField] bool autoFresh = false;
		[SerializeField] int skipTimes = 0;
		[SerializeField] float freshRate = 0.5f;

		public InteractLogs logs = new InteractLogs();

		internal List<InteractAction> actionsLogs = new List<InteractAction>();
		internal InteractState spawnBy;
		IInteractTunnel tunnel;

		Action<InteractState, List<InteractRules>, bool, bool> TickE;
		Action<IInteractCode> TickE2;

		List<TriggerCounters> counters = new List<TriggerCounters>();

		bool first = false;
		float tickTime = 0;
		float lastTime = -1;

		protected override void LiveAwake()
		{
			proxy = this;// always override
			ValidateComponents();
			sprite = transform.GetComponent<SpriteRenderer>();
			if (_collider == null) _collider = GetComponent<Collider2D>();
		}

		void OnDestroy()
		{
			TickE(this, module.GetRules("predestroy"), logs.logPreDestroy, false);
		}

		public void ValidateComponents()
		{
			if (module == null) module = GetComponent<InteractModule>();
			if (pickup == null) pickup = GetComponent<InteractPickup>();
			if (store == null) store = GetComponent<InteractStorage>();
			TickE2 = (runner) => Tick2((InteractCode)runner);
			TickE = (target, interactions, log, timeBound) =>
			{
				TickE2(new InteractCode(target, interactions, module.lastTrigger, log, timeBound));
			};
			if(tunnel == null) tunnel = GetComponent<IInteractTunnel>();
			if (tunnel != null)
				TickE = tunnel.Tick;
			if (tunnel != null)
				TickE2 = (tunnel as IInteractTunnel2).Tick2;
		}

		protected void Start()
		{
			if (state == "")
				Debug.LogWarning("Make sure to assign state here", this);

			// also starts self
			if (transform.parent == null || Time.time > 0 || transform.parent.GetComponentInParent<InteractState>() == null)
			{
				var states = GetComponentsInChildren<InteractState>();
				foreach (var item in states)
					item.StartInit();
			}
		}

		void StartInit()
		{
			pickup.LoadGlobals();
			store.stored.InitStr("state", state);
			lastTime = -1;
			TickE(this, module.GetRules("start"), logs.logStart, false);
		}

		public void StartReinitOnLayerUpdate()
		{
			TickE(this, module.GetRules("start"), logs.logStart, false);
		}

		void Update()
		{
			if (!first)
			{
				TickE(this, module.GetRules("start2"), logs.logStart, false);
				first = true;
			}
			if (autoFresh && Time.time > tickTime)
			{
				tickTime = Time.time + freshRate;
				if (skipTimes == 0)
				{
					if (_collider != null)
						_collider.enabled = !_collider.enabled;
					OnTimed();
				}
				if (skipTimes > 0)
					skipTimes--;
			}
			else TickE(this, module.GetRules("tick"), logs.logTick, true);
		}

		public void OnSpawn(List<InteractState> spawnBlock)
		{
			// spawnBlock: single set of spawn-spawner.
			for (int i = 0; i < spawnBlock.Count; i++)
			{
				var o = spawnBlock[i];
				if (o == null) continue;
				var mods = o.module.GetRules("spawn");
				mods.AddRange(module.GetRules("spawn"));
				if (logs.logSpawn)
					Debug.Log("OnSpawn " + " tick on " + o + " " + this + " " + mods.Count);
				o.TickE(o, mods, logs.logSpawn, true);
			}
		}

		public void CustomTrigger(string trigger, InteractModule target = null)
		{
			var mods = module.GetRules(trigger);
			if(target != null)
				mods.AddRange(target.GetRules(trigger));
			Logs.L($"Custom trigger: ({trigger}) ={mods.Count}, {module.name} & {(target != null ? target.name : "null") }");
			TickE(target!= null ? target.State : this, mods, logs.logCustom, false);
		}

		public void OnTimed(string timedRule = "timed")
		{
			TickE(this, module.GetRules(timedRule), logs.logTimed, false);
		}

		void OnTriggerEnter2D(Collider2D collider)
		{
			InteractState x;
			if (collider.gameObject != gameObject && collider.TryGetComponent(out x))
				TickE(x, module.GetRules("overlap"), logs.logOverlap || x.logs.logOverlap, false);
			else Logs.W($"Overlap: missing Interact system. Allowed if intentional. {collider.gameObject.name} -> {gameObject.name}", collider);
		}

		// Prefer other public functions. This is to be used IInteractTunnel.
		public void Tick2(InteractCode codes)
		{
			bool timeBound = codes.timeBound;
			var interactions = codes.Interactions;
			var log = codes.log;
			var trigger = codes.trigger;
#if UNITY_EDITOR
			if (codes.log && lastTime == Time.time)
				Debug.Log(interactions.Count + " same time");
#endif
			if (interactions.Count == 0)
			{ // prevents tick overriding
				return;
			}
			if (timeBound && lastTime == Time.time) // prevents overlapping from multiple cyclic calls
				return;
			var last = state;

			var other = codes.Target;
			actionsLogs.Clear();
			List<InteractAction> actions = new List<InteractAction>();

			// prefs, trigger on both, or ""="any" on self
			actions.AddRange(Action("", "", interactions)); // 0def 
			actions.AddRange(Action("", other.state, interactions)); // 0def - 1
			actions.AddRange(Action(state, "", interactions)); // self - 0def
			actions.AddRange(Action(state, other.state, interactions)); // self - 1
			if (log)
				Logs.L($"Interact action count: {actions.Count}");
			InitCounters(trigger, actions.Count);
			var localScope = new InteractScope(this, actions, TriggerCounters.Find(trigger, counters), trigger);
			Fill(actions, other);
			StartCoroutine(pickup.Trigger(new TriggerCount(other.state, interactions, localScope, log)));
			Clear(actions);
			actionsLogs = actions;

			actions = Action(other.state, last, interactions);
			RunAction(other, this, actions);
			void RunAction(InteractState run, InteractState target, List<InteractAction> actions)
			{
				run.InitCounters(trigger, actions.Count);
				localScope = new InteractScope(run, actions, TriggerCounters.Find(trigger, run.counters), trigger);
				Fill(actions, target);
				StartCoroutine(run.pickup.Trigger(new TriggerCount(last, interactions, localScope, log)));
				Clear(actions);
				run.actionsLogs = actions;
			}

			// must be run after, so times don't get overrun
			UpdateLocals();
			other.UpdateLocals();

			if (store.recentlySpawned.Count > 0)
			{
				store.recentlySpawned.Add(other);// add spawner
				List<InteractState> states = new List<InteractState>(store.recentlySpawned);
				store.recentlySpawned.Clear();
				OnSpawn(states);
			}
		}

		public void InitCounters(string trigger, int count)
		{
			if (TriggerCounters.Find(trigger, counters) == null)
				counters.Add(new TriggerCounters(trigger));
			TriggerCounters.Find(trigger, counters).Init(count);
		}

		void UpdateLocals()
		{
			lastTime = Time.time;
			if (color != null)
				sprite.color = color.FindColor(state);
		}

		public static List<InteractAction> Action(string from, string to, List<InteractRules> interactions)
		{
			List<InteractAction> actions = new List<InteractAction>();
			foreach (var item in interactions)
			{
				if (item.enabled && item.IsMatch(from, to))
					actions.Add(item.action);
			}
			return actions;
		}

		void Clear(List<InteractAction> actions)
		{
			Fill(actions, null);
		}

		void Fill(List<InteractAction> actions, InteractState target)
		{
			foreach (var action in actions)
			{
				action.target = target;
				action.self = this;
			}
		}
	}

	[System.Serializable]
	public class InteractLogs
	{
		public bool logOverlap = false;
		public bool logTimed = false;
		public bool logTick = false;
		public bool logStart = false;
		public bool logSpawn = false;
		public bool logPreDestroy = false;
		public bool logCustom = false;
	}
}