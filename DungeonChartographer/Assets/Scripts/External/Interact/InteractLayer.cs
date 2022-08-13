﻿using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Interact
{
    [System.Serializable]
    public class InteractLayer
    {
        [SerializeField] internal string layer;
        [SerializeField] internal bool enabled;
        [SerializeField] internal List<InteractTrigger> triggers;
        private bool lastEnabled = false;

        public List<InteractTrigger> EditorTriggers => triggers;
        public bool EditorEnabled { get => enabled; set => enabled = value; }
        public string EditorLayer => layer;

        public InteractLayer()
        {
            lastEnabled = enabled;
        }

        public bool Matches(string layer)
        {
            return this.layer == layer;
        }

        public void Enabled(bool enabled)
        {
            this.enabled = enabled;
        }

        public void Add(InteractTrigger items)
        {
            if (items == null) return;
            triggers.Add(items);
        }

        public void AddList(List<InteractTrigger> items)
        {
            if (items == null) return;
            triggers.AddRange(items);
        }

        public void Clear()
        {
            triggers.Clear();
        }

        public bool HasToRefresh()
        {
            bool change = lastEnabled != enabled;
            lastEnabled = enabled;
            return change;
        }

        public InteractLayer Copy()
        {
            return new InteractLayer()
            {
                layer = this.layer,
                enabled = this.enabled,
                triggers = new List<InteractTrigger>(this.triggers),
                lastEnabled = this.lastEnabled
            };
        }

        public void Serialize(string path)
        {
            path = NodeManager.Path(path, layer);
            var node = NodeManager.Node(path, layer);
            node.WriteAttrib("enabled", enabled);

            var triggersPath = NodeManager.Path(path, "triggers");
            for (var i = 0; i < triggers.Count; i++)
            {
                triggers[i].Serialize(triggersPath);
            }
        }
    }

    [System.Serializable]
    public class InteractTrigger
    {
        // trigger names: timed, tick, overlap(trigger collision), collision
        public string trigger;
        public InteractRuleset rules;

        public void Serialize(string path)
        {
            var triggersPath = NodeManager.Path(path, trigger);
            var triggersNode = NodeManager.Node(triggersPath, trigger);
            triggersNode.WriteAttrib("rules", rules.name);
        }
    }

    // Group of separate layers
    [System.Serializable]
    public class InteractBox
    {
        public List<InteractLayer> layers = new List<InteractLayer>();

        public void AddLayer(InteractLayer add)
        {
            foreach (var item in layers)
            {
                if (item.Matches(add.layer))
                {
                    item.AddList(new List<InteractTrigger>(add.triggers));
                    return;
                }
            }
            layers.Add(add.Copy());
        }

        /// <summary>
        /// Adds this to other where layers match.
        /// </summary>
        /// <param name="other"></param>
        public void JoinInto(InteractBox other)
        {
            foreach (var item in layers)
            {
                if (item == null) continue;
                other.AddLayer(item);
            }
        }

        internal InteractLayer Get(string layerName)
        {
            foreach (var item in layers)
            {
                if (item.Matches(layerName))
                    return item;
            }
            var created = new InteractLayer()
            {
                enabled = true,
                layer = layerName,
                triggers = new List<InteractTrigger>()
            };
            layers.Add(created);
            return created;
        }

        public void Serialize(string path)
        {
            for (int i = 0; i < layers.Count; i++)
                layers[i].Serialize(NodeManager.Path(path, "boxes"));
        }
    }

    [System.Flags]
    public enum InteractActionCoroutine
    {
        UpNextWaits = 1<< 2,
    }

    [System.Serializable]
    public class InteractAction : IInteractCode
    {

        public List<string> conditions = new List<string>();
        public bool enableLockLocal = false;
        public InteractActionCoroutine coroutineSettings = InteractActionCoroutine.UpNextWaits;
        public List<string> codes = new List<string>();
        [Header("not supported in coroutines atm")]
        public List<string> elseCodes = new List<string>();
        public InteractPrefabs spawnSet;

        internal InteractState target { get; set; } = null;// realtime data
        internal InteractState self { get; set; } = null;// realtime data
        [Header("don't use, use codes")]
        [HideInInspector] public string code = ""; // obsolete applies quick command

        public GameObject GO { get; }
        public bool UpNextWaits { get => (coroutineSettings & InteractActionCoroutine.UpNextWaits) == InteractActionCoroutine.UpNextWaits; }

        public void Serialize(string path)
        {
            var actionNode = NodeManager.Node(path, "");

            NodeManager.List(path, "conditions", conditions);
            NodeManager.List(path, "codes", codes);
            NodeManager.List(path, "elseCodes", elseCodes);

            actionNode.WriteAttrib("spawnSet", spawnSet.name);
        }

        public string GetCode(int index, object optional = null)
        {
            if (index < codes.Count)
                return codes[index];
            else return elseCodes[index - codes.Count];
        }

        public int CodeCount()
        {
            return codes.Count + elseCodes.Count;
        }

        internal bool Condition(Func<InteractRunnerTrigger, bool> run, InteractRunnerTrigger setup, ref string failedConditionCode)
        {
            bool pass = true;
            if (conditions != null)
            {
                for (int j = 0; j < conditions.Count; j++)
                {
                    var condition = conditions[j];
                    setup.code = condition;
                    if (!run(setup))
                    {
                        pass = false;
                        failedConditionCode = condition;
                        break;
                    }
                    if (!pass)
                    {
                        break;
                    }
                }
            }
            return pass;
        }
    }

    [System.Serializable]
    public class InteractRules
    {
        public string note;
        public bool enabled = true;
        public string from, to, result;
        public InteractAction action;

        public bool IsMatch(string from, string to)
        {
            return this.from == from && this.to == to;
        }

        public void Serialize(string path)
        {
            var ruleNode = NodeManager.Node(path, "");

            ruleNode.WriteAttrib("note", note);
            ruleNode.WriteAttrib("enabled", enabled);
            ruleNode.WriteAttrib("from", from);
            ruleNode.WriteAttrib("to", to);
            ruleNode.WriteAttrib("result", result);

            action.Serialize(NodeManager.Path(path, "action"));
        }

        public int Count => action.codes.Count;
    }


    [System.Serializable]
    public class InteractStored : IKeyValueBase
    {
        public string key;
        [FormerlySerializedAs("value")]
        public int _value;
        [FormerlySerializedAs("svalue")]
        public string _svalue;
        public int value {
            get => _value; set {
                _value = value;
                _svalue = value.ToString();
            }
        }

        public string Key { get => key; }
        public object Value { get => _svalue; }

        public string svalue {
            get => _svalue; set {
                int.TryParse(value, out _value);
                _svalue = value;
            }
        }

        public void Serialize(string path)
        {
            var node = NodeManager.Node(path, key);
            node.WriteAttrib(key, value);
            node.WriteAttrib("s" + key, svalue);
        }
    }
}