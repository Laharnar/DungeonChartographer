using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Interact
{
    // Loads different variations of implementations into realtime.
    public class InteractModule : LiveBehaviour
    {
        [SerializeField] InteractBox layers;
        [SerializeField] List<InteractBoxPref> boxes = new List<InteractBoxPref>();
        [Header("Start:Loaded to 'base', then cleared")]
        [SerializeField] List<InteractTrigger> triggerRules;
        [SerializeField] List<InteractRules> quick = new List<InteractRules>();
        [Header("Preferably obsolete")]
        [FormerlySerializedAs("rules")]
        [SerializeField] InteractRuleset overlapRules; // trigger rules


        [SerializeField] InteractRuleset timedRules;

        [Header("Realtime")]
        [SerializeField] public InteractBox runtimeLayers;
        public InteractBox RealtimeLayers => runtimeLayers;
        InteractState state;
        public InteractState State => state;
        public bool editorRefresh;

        internal void ValidateComponents()
        {
            if(state == null) state = GetComponent<InteractState>();
            if(runtimeLayers== null ) runtimeLayers = new InteractBox();
        }

        protected override void LiveAwake()
        {
            ValidateComponents();
            runtimeLayers = CreateBox();
        }

        void Update()
        {
#if UNITY_EDITOR
            if ((int)Time.time % (5 + UnityEngine.Random.Range(-1, 4)) == 0)
            {
                var last = runtimeLayers;
                runtimeLayers = CreateBox();
                for (int i = 0; i < last.layers.Count; i++)
                {
                    var layer = last.layers[i];
                    runtimeLayers.Get(layer.layer)?.Enabled(layer.enabled);
                }
            }
#endif
            if (editorRefresh)
            {
                for (int i = 0; i < runtimeLayers.layers.Count; i++)
                {
                    if (runtimeLayers.layers[i].HasToRefresh())
                    {
                        state.StartReinitOnLayerUpdate();
                    }
                }
                editorRefresh = false;
            }
        }

        public List<InteractLayer> EditorLayers()
        {
            List<InteractLayer> group = new List<InteractLayer>();
            group.AddRange(layers.layers);
            foreach (var item in boxes)
            {
                if (item.box == null)
                    continue;
                group.AddRange(item.box.layers);
            }
            group.Add(new InteractLayer()
            {
                enabled = true,
                layer = "base",
                triggers = new List<InteractTrigger>(triggerRules)
            });
            return group;
        }

        /// <summary>
        /// Note that this is recreated for every call.
        /// </summary>
        public InteractBox CreateBox()
        {
            InteractBox temp = new InteractBox();
            foreach (var item in boxes)
            {
                if (item == null || item.box == null)
                    UnityEngine.Debug.LogError("Box is null", this);
                else item.box.JoinInto(temp);
            }
            // unique local load for current version.
            temp.Get("base").AddList(triggerRules);
            layers.JoinInto(temp);
            return temp;
        }

        public List<InteractRules> GetRules(string trigger)
        {
            // editor supported editable interactions(through trigger rules)
            List<InteractRules> interactions = new List<InteractRules>();
            foreach (var item in runtimeLayers.layers)
            {
                if (!item.enabled) continue;
                var triggers = item.triggers;
                for (int i = 0; i < triggers.Count; i++)
                {
                    if (triggers[i].trigger == trigger && triggers[i].rules != null)
                    {
                        interactions.AddRange(triggers[i].rules.interactions);
                    }
                }
            }

            // fast solutions that aren't editor supported
            if (trigger == "overlap" && overlapRules != null)
                interactions.AddRange(overlapRules.interactions);
            if (trigger == "timed" && timedRules != null)
                interactions.AddRange(timedRules.interactions);
            if (trigger == "tick" && quick != null)
                interactions.AddRange(quick);
            lastTrigger = trigger;
            return interactions;
        }
        public string lastTrigger;


    }
}