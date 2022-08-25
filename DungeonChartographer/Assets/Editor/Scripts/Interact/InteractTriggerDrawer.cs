using System.IO;
using UnityEngine;
using UnityEditor;

namespace Interact
{
    // IngredientDrawerUIE
    [CustomPropertyDrawer(typeof(InteractTrigger))]
    public class InteractTriggerDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent("trigger"));

            // Don't make child fields be indented
            var indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // Calculate rects
            var amountRect = new Rect(position.x, position.y, 100, position.height);
            var unitRect = new Rect(position.x + 105, position.y, 100, position.height);
            var buttonRect = new Rect(position.x + 210, position.y, 20, position.height);

            var rulesProp = property.FindPropertyRelative("rules");

            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            EditorGUI.PropertyField(amountRect, property.FindPropertyRelative("trigger"), GUIContent.none);
            EditorGUI.PropertyField(unitRect, rulesProp, GUIContent.none);
            if (GUI.Button(buttonRect, "+"))
            {
                rulesProp.objectReferenceValue = CreateScriptObj<InteractRuleset>("New rule");
            }
            if(rulesProp.objectReferenceValue!= null)
            {
                InteractRuleset set = (InteractRuleset)rulesProp.objectReferenceValue;
            }

            // Set indent back to what it was
            EditorGUI.indentLevel = indent;

            EditorGUI.EndProperty();
        }

        static T CreateScriptObj<T>(string name, bool focusAsset = false) where T: ScriptableObject
        {
            var path = "Assets/__news/";

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            T example = ScriptableObject.CreateInstance<T>();

            var assetPath = Path.Join(path, name+".asset");
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            AssetDatabase.CreateAsset(example, assetPath);
            AssetDatabase.SaveAssets();
            if (focusAsset)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = example;
            }
            return AssetDatabase.LoadAssetAtPath<T>(assetPath);
        }
    }
}