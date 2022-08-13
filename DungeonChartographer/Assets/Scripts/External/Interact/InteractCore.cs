using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Interact
{

    public class InteractCore
    {
        static InteractCore one;
        Dictionary<int, InteractState> cache = new Dictionary<int, InteractState>();
#if UNITY_EDITOR
        [MenuItem("DEV/Reload scene")]
#endif
        public static void ResetScene()
        {
            if(Application.isPlaying && Application.isEditor)
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public static InteractCore ONE {
            get => one ?? new InteractCore();
            private set => one = value;
        }

        public InteractCore()
        {
            ONE = this;
        }

        internal void Run(MonoBehaviour obj, string trigger, GameObject target = null)
        {
            InteractState ochache = UpdateCache(target);
            Run(obj, ochache != null ? ochache.module : null, trigger);
        }

        private InteractState UpdateCache(GameObject target)
        {
            InteractState ochache = null;
            if (target != null)
            {
                if (!cache.ContainsKey(target.GetInstanceID()))
                {
                    ochache = target.GetComponentInChildren<InteractState>();
                    if (ochache != null)
                    {
                        cache.Add(target.GetInstanceID(), ochache);
                    }
                }
                else ochache = cache[target.GetInstanceID()];
            }

            return ochache;
        }

        internal void Run(MonoBehaviour obj, InteractModule target, string trigger)
        {
            if (target != null && !cache.ContainsKey(target.gameObject.GetInstanceID()))
            {
                InteractState ostate = null;
                ostate = target.GetComponent<InteractState>();
                if (ostate != null)
                {
                    cache.Add(target.gameObject.GetInstanceID(), ostate);
                }
            }
            if (!cache.ContainsKey(obj.GetInstanceID()))
            {
                cache.Add(obj.GetInstanceID(), obj.GetComponentInChildren<InteractState>());
            }
            var state = cache[obj.GetInstanceID()];
            if (state != null)
            {
                state.CustomTrigger(trigger, target);
            }
            else
            {
                Logs.E($"Failed to run custom trigger {trigger}", obj, alwaysLog:true);
            }
        }

        internal void SetProp(MonoBehaviour unitCombat, string v, string value)
        {
            var state = UpdateCache(unitCombat.gameObject);
            state.store.SetPropInt(v, value);
        }
    }

    public class LockGlobal
    {
        public static LockGlobal ONE = new LockGlobal();

        readonly Dictionary<ITFuncStr, List<string>> locks = new Dictionary<ITFuncStr, List<string>>();

        int hashId = 0;// unique id

        public void EndLock(ITFuncStr func, string key)
        {
            locks[func].Remove(key);
            if (locks[func].Count == 0)
                locks.Remove(func);
        }

        public bool IsLocked(ITFuncStr func, string key)
        {
            if (!locks.ContainsKey(func)) return false;
            return locks[func].Contains(key);
        }

        public void InitLock(ITFuncStr func, out int id)
        {
            if (!locks.ContainsKey(func))
                locks.Add(func, new List<string>());
            locks[func].Add(hashId.ToString());
            id = hashId;
            hashId++;
        }
    }

    /// <summary>
    /// Call static start coroutine to use, local stop coroutine to end lock.
    /// </summary>
    /// <remarks>Make sure to unlock coroutine when it's stopped.</remarks>
    public class InteractCoroutine
    {
        // note that this is very unsafe.
        readonly static Queue<InteractCoroutine> unhandled = new Queue<InteractCoroutine>();
        public InteractAction action;
        int coroutineLockId;
        readonly ITFuncStr obj;
        readonly IEnumerator enumerator;
        public bool Locked { get; private set; } = false;


        /// <summary>
        /// </summary>
        /// <remarks>
        /// If you want this to be handled as part of coroutines, call only inside ITFuncStr/Func.
        /// </remarks>
        /// <param name="obj"></param>
        /// <param name="enumerator"></param>
        public static void Run(ITFuncStr obj, IEnumerator enumerator)
        {
            if (obj != null)
            {
                var coro = new InteractCoroutine(obj, enumerator);
                unhandled.Enqueue(coro);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="counter"></param>
        /// <returns>Proceed with count</returns>
        public static void NextUnhandled(InteractCodeCounter counter, System.Action<object> OnEnd, int end, int maxCount = 1)
        {
			LoadUnhandled(counter);
            for (int i = 0; i != maxCount && counter.HasCoroutines(); i++)
            {
                var coroutine = counter.DequeueUseCoroutine();
                coroutine.obj.Obj.StartCoroutine(coroutine.RunCoroutine(OnEnd, new Vector2Int(counter.i, end)));
            }
        }

        /// <summary>
        /// leftovers or added MID execution
        /// </summary>
        /// <param name="counter"></param>
        public static void NextLeftovers(InteractCodeCounter counter)
        {
            NextUnhandled(counter, null, 0, -1);
        }

        public static void NextChained(InteractCodeCounter counter, System.Action<object> OnEnd)
        {
            if (!counter.HasCoroutines()) return;
            var coroutine = counter.First();
            coroutine.obj.Obj.StartCoroutine(coroutine.Chain(counter, OnEnd));
        }

        IEnumerator Chain(InteractCodeCounter counter, System.Action<object> OnEnd)
        {
            while (counter.HasCoroutines())
            {
                var coroutine = counter.DequeueUseCoroutine();
                yield return coroutine.RunCoroutine(OnEnd, counter.i);
            }
        }

        public static void LoadUnhandled(InteractCodeCounter counter)
        {
            if (unhandled.Count > 0)
            {
                counter.AddAllCoroutines(unhandled);
                unhandled.Clear();
            }
        }

        public IEnumerator RunCoroutine(System.Action<object> OnEnd, object arg)
        {
            Locked = true;
            LockGlobal.ONE.InitLock(obj, out coroutineLockId);
            yield return enumerator;
            LockGlobal.ONE.EndLock(obj, "" + coroutineLockId);
            Locked = false;
            OnEnd?.Invoke(arg);
        }

        /// <summary>
        /// Call StartCoroutine instead.
        /// </summary>
        /// <param name="obj"></param>
        InteractCoroutine(ITFuncStr obj, IEnumerator enumerator)
        {
            if (obj == null) Logs.E("Crash coroutine -> null obj.", alwaysLog: true);
            this.obj = obj;
            this.enumerator = enumerator;
        }
    }

    public class ExecutionPack
    {
        public IInteractCode code;
        public IInteractRunner runner;
        public InteractCodeCounter counter;

        public ExecutionPack(IInteractCode code, IInteractRunner runner)
        {
            this.code = code;
            this.runner = runner;
        }
    }

    public interface IInteractCode
    {
        GameObject GO { get; }
        string GetCode(int index, object optional = null);
        int CodeCount();
    }

    [System.Serializable]
    public class TriggerCounters
    {
        public string trigger;
        public List<InteractCodeCounter> counters = new List<InteractCodeCounter>();
        public string layer;
        public InteractCodeCounter counter = new InteractCodeCounter();

        public TriggerCounters(string trigger)
        {
            this.trigger = trigger;
        }

        public static TriggerCounters Find(string trigger, List<TriggerCounters> items)
        {
            foreach (var item in items)
            {
                if (item.trigger == trigger)
                    return item;
            }
            return null;
        }

        public void Init(int actionCount)
        {
            for (int i = counters.Count; i < actionCount; i++)
            {
                counters.Add(new InteractCodeCounter());
            }
        }
    }

    [System.Serializable]
    public class InteractCodeCounter
    {
        public int i;

        bool localLock = false;
        // coroutine support
        public bool Locked {
            get => active != null ? active.Locked : localLock;
            set { if (active == null) localLock = value; }
        }
        InteractCoroutine active = null;
        readonly List<InteractCoroutine> waiting = new List<InteractCoroutine>();

        internal void AddAllCoroutines(Queue<InteractCoroutine> unhandled)
        {
            waiting.AddRange(unhandled);
        }

        internal bool HasCoroutines()
        {
            return waiting.Count > 0;
        }

        internal InteractCoroutine DequeueUseCoroutine()
        {
            active = waiting[0];
            waiting.RemoveAt(0);
            return active;
        }
        internal InteractCoroutine First()
        {
            return waiting[0];
        }
    }

    public class InteractRunnerTrigger
    {
        public string code;
        public InteractState self;
        public InteractState target;
        public InteractPrefabs prefabs;
        public List<InteractStorage> storages;
        public bool log = false;
        public bool empty = true;

        public InteractRunnerTrigger(string code, InteractState self, InteractState target, InteractPrefabs prefabs, List<InteractStorage> storages, bool log)
        {
            this.code = code;
            this.self = self;
            this.target = target;if(target == null) Debug.Log("Storage:Null target");
            this.prefabs = prefabs;
            this.storages = storages;
            this.log = log;
            empty = false;
        }

        public InteractRunnerTrigger(string code, bool log, InteractState sample)
        {
            this.code = code;
            this.log = log;
            self = sample;
            target = self;
            prefabs = null;
            storages = new List<InteractStorage>() { self.store };
        }

        public static InteractState TempInit(InteractState sample)
        {
            var obj = new GameObject();
            obj.AddComponent<InteractSetup>().Validate(sample);
            return obj.GetComponent<InteractState>();
        }

        public static void CleanupIfEmpty(InteractState sample)
        {
            if (sample != null)
                UnityEngine.Object.DestroyImmediate(sample.gameObject);
        }
    }

    public interface IInteractRunner
    {
        bool Run(InteractRunnerTrigger trigger);
    }

    // basic runner
    public class InteractCode : IInteractCode
    {
        public bool log { get; }
        public bool timeBound { get; }

        List<InteractRules> interactions;
        GameObject go;
        private InteractState stateSource;

        public GameObject GO { get => go; }
        public List<InteractRules> Interactions { get => interactions; }
        public InteractState Target => stateSource;
        public string trigger { get; private set; }

        public InteractCode(InteractState target, List<InteractRules> interactions, string trigger, bool log = false, bool timeBound = true)
        {
            go = target.gameObject;
            this.stateSource = target;
            this.interactions = interactions;
            this.trigger = trigger;
            this.timeBound = timeBound;
            this.log = log;
        }


        public string GetCode(int codeIndex, object optional = null)
        {
            int index = Find(codeIndex, interactions);
            int countBefore = CountCode(interactions, codeIndex - 1);
            int actualIndex = codeIndex - countBefore;
            return interactions[index].action.codes[actualIndex];
        }

        public static int Find(int stopAtCount, List<InteractRules> interactions)
        {
            int count = 0;
            for (int i = 0; i < interactions.Count; i++)
            {
                count += interactions[i].Count;
                if (count >= stopAtCount)
                    return i;
            }
            return -1;
        }

        public static int CountCode(List<InteractRules> interactions, int stopListId = -1)
        {
            int count = 0;
            for (int i = 0; i < interactions.Count && i != stopListId; i++)
            {
                count += interactions[i].Count;
            }
            return count;
        }

        public int CodeCount()
        {
            return CountCode(interactions);
        }
    }

    public class InteractScope : IInteractCode
    {
        public bool log { get; }
        public bool timeBound { get; }

        List<InteractAction> actions;
        GameObject go;
        private InteractState stateSource;
        public string trigger;
        public TriggerCounters counters;
        public GameObject GO { get => go; }
        public List<InteractAction> Actions { get => actions; }
        public InteractState Source => stateSource;

        public InteractScope(InteractState src, List<InteractAction> actions, TriggerCounters counters, string trigger, bool log = false, bool timeBound = true)
        {
            go = src.gameObject;
            this.stateSource = src;
            this.actions = actions;
            this.timeBound = timeBound;
            this.log = log;
            this.trigger = trigger;
            this.counters = counters;
        }

        // no else code support
        public string GetCode(int codeIndex, object optional = null)
        {
            int index = Find(codeIndex, actions);
            int countBefore = CountCode(actions, codeIndex - 1);
            int actualIndex = codeIndex - countBefore;
            return actions[index].codes[actualIndex];
        }

        // items -> icountable
        public static int Find(int stopAtCount, List<InteractAction> items)
        {
            int count = 0;
            for (int i = 0; i < items.Count; i++)
            {
                count += items[i].codes.Count;
                if (count >= stopAtCount)
                    return i;
            }
            return -1;
        }

        // no else code support
        // items -> icountable
        public static int CountCode(List<InteractAction> items, int stopListId = -1)
        {
            int count = 0;
            for (int i = 0; i < items.Count && i != stopListId; i++)
                count += items[i].codes.Count;
            return count;
        }


        public int CodeCount()
        {
            return CountCode(actions);
        }
    }
    public interface ICountable
    {
        public int Count { get; }
    }

}