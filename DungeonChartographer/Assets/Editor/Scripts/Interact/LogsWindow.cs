using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class LogsWindow:EditorWindow
{
    Vector2 scroll;

    List<Logs.LogInstance> logs = new List<Logs.LogInstance>();
    Dictionary<object, bool> filter = new Dictionary<object, bool>();

    [MenuItem("DEV/Logs")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        LogsWindow window = (LogsWindow)GetWindow(typeof(LogsWindow));
        window?.Show();
    }
	
    void OnGUI()
    {
        var logsSingleton = Logs.Singleton;
        if(logsSingleton.EditorLogs.Count == 0)
        {
            EditorGUILayout.LabelField("Call Logs.L to add new logs. Enter playmode.");
        }

        if (Application.isPlaying)
        {
            logs.Clear();
            logs.AddRange(logsSingleton.EditorLogs);
        }
        if (logs.Count > 201)
        {
            logs.RemoveRange(0, 100);
        }

        EditorGUILayout.BeginHorizontal();
        bool anyFilter = false;
        for (int i = logs.Count - 1; i >= logs.Count - 100 && i >= 0; i--)
        {
            var item = logs[i];
            if (item.obj == null) continue;
            if(!filter.ContainsKey(item.obj))
                filter.Add(item.obj, false);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(item.obj.name);
            filter[item.obj] = EditorGUILayout.Toggle(filter[item.obj]);
            EditorGUILayout.EndHorizontal();
            if(!anyFilter)
                anyFilter |= filter[item.obj];
        }
        EditorGUILayout.EndHorizontal();

        scroll = EditorGUILayout.BeginScrollView(scroll, true, true);

        for (int i = logs.Count -1; i >= logs.Count - 100 && i >= 0; i--){
            var item = logs[i];
            if (anyFilter)
            {
                if(item.obj!= null && !filter[item.obj])
                    continue;
            }
            EditorGUILayout.BeginHorizontal();

            if(logsSingleton.IsDebuggable(item))
                logsSingleton.SetDebuggable(item, EditorGUILayout.Toggle(logsSingleton.GetDebuggable(item), GUILayout.Width(20)));

            var prev = GUI.backgroundColor;
            GUI.backgroundColor = item.color;

            if (item.obj != null)
            {
                EditorGUILayout.ObjectField(item.obj, typeof(Object), true, GUILayout.MaxWidth(100));
            }

            if (GUILayout.Button("*", GUILayout.Width(10)))
                item.color = Random.ColorHSV(0.75f, 1, 1, 1, 0, 1, 1, 1);
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.richText = true;
            EditorGUILayout.LabelField($"{ColorTypeCode(item.code)}{i%10}<color={item.color}>[{item.time}]</color> ({(item.src != null && !(item.src is string) ? item.src : "" )})::{item.log}", style);

            EditorGUILayout.EndHorizontal();
            GUI.backgroundColor = prev;
        }
		
		EditorGUILayout.EndScrollView();
    }

    string ColorTypeCode(char code)
    {
        if (code == 'l')
            return "<color=white>[]</color>";
        else if(code == 'e')
            return "<color=red>[]</color>";
        else if (code == 'w')
            return "<color=yellow>[]</color>";
        return "?"+code;
    }

    void Update(){
        if(logs.Count != Logs.Singleton.EditorLogs.Count)
		    Repaint();
	}
}
