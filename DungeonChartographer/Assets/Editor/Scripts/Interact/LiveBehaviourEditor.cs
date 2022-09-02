using UnityEngine;
using UnityEditor;

namespace Interact
{
    [CustomEditor(typeof(LiveBehaviour), true)]
    public class LiveBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            GUILayout.Label("<color=yellow>[Live]</color>", style);
            base.OnInspectorGUI();
        }
    }

    [CustomEditor(typeof(LiveEditorBehaviour), true)]
    public class LiveEditorBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            GUILayout.Label($"<color=yellow>[InEditor({(int)LiveBehaviour.reloadTime}), Live]</color>", style);
            base.OnInspectorGUI();
        }
    }
}