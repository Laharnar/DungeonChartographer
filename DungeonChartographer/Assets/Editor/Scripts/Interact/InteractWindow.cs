using System.Runtime.CompilerServices;
using System.Data;
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.Text;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Interact
{


    public class InteractWindow : EditorWindow
    {
        public bool refresh = false;
        public List<InteractState> objs = new List<InteractState>();

        Vector2 scroll;
        int time;
        bool[] foldouts = new bool[300];
        GameObject manualRoot;
        string searchObj = "";
        string searchRule = "";
        string searchLine = "";
        private int mode;

        // Add menu named "My Window" to the Window menu
        [MenuItem("DEV/Interact")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            InteractWindow window = (InteractWindow)GetWindow(typeof(InteractWindow));
            window.Show();
        }

        static bool ContainsSearch(List<string> list, string search)
        {
            if (search != "")
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].ToLower().Contains(search))
                        return true;
                }
            }
            return false;
        }

        void DrawCodes(List<string> list, string label, InteractState obj)
        {
            if (label != "" && list.Count > 0)
                EditorGUILayout.LabelField(label);
            EditorGUI.indentLevel++;

            var col = GUI.color;
            for (int i = 0; i < list.Count; i++)
            {
                InteractRunnerTrigger trigger = new InteractRunnerTrigger(list[i], false, obj);
                try
                {
                    if (InteractStorage.Activate(trigger))
                        GUI.color = Color.green;
                    else GUI.color = Color.red;
                }
                finally
                {
                    list[i] = EditorGUILayout.TextField(list[i]);
                    GUI.color = col;
                }
            }
            EditorGUI.indentLevel--;
        }

        static void ClearField(string label, ref string value)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("x", GUILayout.Width(26)))
            {
                value = "";
            }
            value = EditorGUILayout.TextField(label, value).ToLower();
            EditorGUILayout.EndHorizontal();
        }

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(5));
            if (GUILayout.Button("S", GUILayout.Width(20))) mode = 0;
            if (GUILayout.Button("?", GUILayout.Width(20))) mode = 2;
            EditorGUILayout.EndVertical();

            if (mode == 0)
                Hierarchy();
            else
            {
                // docs
                EditorGUILayout.BeginVertical();
                HashSet<string> uniqueCodes = new HashSet<string>();
                for (int i = 0; i < objs.Count; i++)
                {
                    if (objs[i] == null) continue;
                    objs[i].ValidateComponents();
                    var layers = objs[i].module.EditorLayers();
                    for (int j = 0; j < layers.Count; j++)
                    {
                        var triggers = layers[j].EditorTriggers;
                        foreach (var trigger in triggers)
                        {
                            foreach (var interaction in trigger.rules.interactions)
                            {
                                foreach (var code in interaction.action.codes)
                                {
                                    uniqueCodes.Add(code);
                                }
                            }
                        }
                    }
                }

                for (int i = objs.Count - 1; i >= 0; i--)
                {
                    if (objs[i] == null)
                        objs.RemoveAt(i);
                }
                if (uniqueCodes.Count == 0)
                    GUILayout.Label("No codes in scene. ");
                StringBuilder sb = new StringBuilder();
                foreach (var item in uniqueCodes)
                {
                    sb.Append(item);
                    sb.Append("\n");
                }
                GUILayout.TextArea(sb.ToString());
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();

        }

        private List<InteractState> PrefixSearchObjects(List<InteractState> objs, string searchObj)
        {
            List<InteractState> filteredObjs = new List<InteractState>();
            for (int i = 0; i < objs.Count; i++)
            {
                var obj = objs[i];
                if (obj == null) continue;
                var tobj = obj.transform;
                if (!PrefixSearch(tobj.name, searchObj)) continue;
                filteredObjs.Add(obj);
            }
            return filteredObjs;
        }

        private void Hierarchy()
        {
            EditorGUILayout.BeginVertical();


            manualRoot = (GameObject)EditorGUILayout.ObjectField(manualRoot, typeof(GameObject), true);
            if (GUILayout.Button("refresh |" + time) || time + 5 < Time.time)
            {
                time = (int)Time.time;
                objs.Clear();
                if (manualRoot == null)
                    objs.AddRange(FindObjectsOfType<InteractState>());
                else objs.AddRange(manualRoot.GetComponentsInChildren<InteractState>());
            }
            ClearField("object  *..", ref searchObj);
            ClearField("rule  *..", ref searchRule);
            ClearField("line  *", ref searchLine);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            GUILayout.Label("roots");
            int count = 0;
            List<InteractState> filteredObjs = PrefixSearchObjects(objs, searchObj);

            bool ShowObjectLayers(InteractState obj, bool[] foldouts, int i)
            {
                // obj + show
                EditorGUILayout.BeginHorizontal();
                if (obj.transform.parent != null)
                    EditorGUILayout.ObjectField(obj.transform.parent, typeof(Transform), true);
                EditorGUILayout.ObjectField(obj, typeof(InteractState), true);
                foldouts[i] = EditorGUILayout.Toggle(foldouts[i]);
                EditorGUILayout.EndHorizontal();

                if (!foldouts[i])
                    return false;
                obj.ValidateComponents();
                EditorGUI.indentLevel++;
                var layers = obj.module.EditorLayers();
                var tempobj = InteractRunnerTrigger.TempInit(obj);
                for (int k = 0; k < layers.Count; k++)
                {
                    var layer = layers[k];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(layer.EditorLayer, GUILayout.Width(100));
                    layer.EditorEnabled = EditorGUILayout.Toggle("", layer.EditorEnabled, GUILayout.Width(20));
                    EditorGUILayout.EndHorizontal();

                    for (int j = 0; j < layer.EditorTriggers.Count; j++)
                    {
                        var rule = layer.EditorTriggers[j];
                        if (!PrefixSearch(rule.rules.name, searchRule)) continue;
                        DrawRule(rule, tempobj);
                    }
                }
                InteractRunnerTrigger.CleanupIfEmpty(tempobj);
                EditorGUI.indentLevel--;
                return true;
            }

            for (int i = 0; i < filteredObjs.Count; i++)
            {
                var obj = filteredObjs[i];
                var tobj = obj.transform;
                if (tobj.parent == null || (manualRoot != null && tobj.parent == manualRoot.transform))
                {
                    if (!ShowObjectLayers(obj, foldouts, i)) continue;
                    count++;
                    if (count % 3 == 0 && count > 0)
                        EditorGUILayout.Space();
                }
            }
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            count = 0;
            GUILayout.Label("nested");
            for (int i = 0; i < filteredObjs.Count; i++)
            {
                var obj = filteredObjs[i];
                if (obj == null) continue;
                var tobj = obj.transform;
                if (tobj.parent != null && (manualRoot == null || tobj.parent != manualRoot.transform))
                {
                    if (!ShowObjectLayers(obj, foldouts, i)) continue;

                    count++;
                    if (count % 3 == 0 && count > 0)
                        EditorGUILayout.Space();
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRule(InteractTrigger rule, InteractState obj)
        {

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(rule.trigger, GUILayout.Width(200));
            EditorGUILayout.ObjectField(rule.rules, typeof(InteractRuleset), false);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel++;

            foreach (var action in rule.rules.interactions)
            {
                var draw = searchLine == ""
                || ContainsSearch(action.action.conditions, searchLine)
                || ContainsSearch(action.action.codes, searchLine)
                || ContainsSearch(action.action.elseCodes, searchLine);
                if (draw)
                {
                    if (action.note != "")
                        EditorGUILayout.LabelField($"# {action.note}");
                    DrawCodes(action.action.conditions, "if", obj);
                    DrawCodes(action.action.codes, "do", obj);
                    DrawCodes(action.action.elseCodes, "elsedo", obj);
                }
            }
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="longName"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        /// <seealso cref="ContainsSearch(List{string}, string)"/>
        bool PrefixSearch(string longName, string search)
        {
            return search == "" || longName.ToLower().StartsWith(search);
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            time = 0;
        }
    }
}