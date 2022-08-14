using System;
using System.Collections.Generic;
using UnityEngine;

public class Logs
{
    static Logs singleton;
    public static Logs Singleton { get => singleton != null ? singleton : singleton = new Logs(); }

    public class LogInstance
    {
        public object src;
        public string log;
        public UnityEngine.Object obj;
        public float time;
        public char code;
        public Color color = Color.gray;
    }

    public Dictionary<object, bool> EditorLoggable = new Dictionary<object, bool>();

    internal static void ExistsInspector(Animator animator, UnityEngine.Object obj, string failMsg)
    {
        if (animator == null) E($"Not Assigned in inspector: {failMsg}", obj);
    }

    public List<LogInstance> EditorLogs => logs;
    List<LogInstance> logs = new List<LogInstance>();


    public static void L(string msg, UnityEngine.Object obj = null, object src = null, bool log = true)
    {
        if(log)
            Singleton.Log(src, msg, obj);
    }
    
    public static void E(string log, UnityEngine.Object obj = null, object src = null, bool alwaysLog = false)
    {
        Singleton.Err(src, log, obj, alwaysLog);
    }

    public static void W(string log, UnityEngine.Object obj = null, object src = null, bool logAlways = false)
    {
        Singleton.Warn(log, obj, src, logAlways);
    }

    public bool IsDebuggable(LogInstance obj)
    {
        return (obj.obj != null && EditorLoggable.ContainsKey(obj.obj)) || EditorLoggable.ContainsKey(string.Intern(obj.log));
    }

    public bool GetDebuggable(LogInstance obj)
    {
        return obj.obj != null ? EditorLoggable[obj.obj] : EditorLoggable[string.Intern(obj.log)];
    }

    public void SetDebuggable(LogInstance obj, bool v)
    {
        if (InitLoggable(obj)) {
            if(obj.obj != null)
                EditorLoggable[obj.obj]  = v;
            else
                EditorLoggable[string.Intern(obj.log)] = v;
        }
    }

    public bool InitLoggable(LogInstance obj)
    {
        if (obj == null)
            return false;
        if (obj.obj != null && !EditorLoggable.ContainsKey(obj.obj))
            EditorLoggable.Add(obj.obj, false);
        if (obj.obj == null && !EditorLoggable.ContainsKey(string.Intern(obj.log)))
            EditorLoggable.Add(string.Intern(obj.log), false);
        return true;
    }


    bool ShouldLogUnity(UnityEngine.Object src, string log)
    {
        if (src != null && !EditorLoggable.ContainsKey(src))
            EditorLoggable.Add(src, false);
        else if (!EditorLoggable.ContainsKey(string.Intern(log)))
            EditorLoggable.Add(string.Intern(log), false);

        if (src != null)
            return EditorLoggable[src];
        return EditorLoggable[string.Intern(log)];
    }

    public void Log(object src, string log, UnityEngine.Object obj)
    {
        bool logUnity = this.ShouldLogUnity(obj, log);
        if (logUnity)
            Debug.Log(log, obj);
        logs.Add(new LogInstance
        {
            src = src == null ? log : src,
            log = log,
            obj = obj,
            time = Time.time,
            code = 'l'
        }); ;
    }

    public void Warn(string log, UnityEngine.Object obj, object src, bool logAlways)
    {
        bool logUnity = this.ShouldLogUnity(obj, log);
        if (logUnity || logAlways)
            Debug.LogWarning(log, obj);
        logs.Add(new LogInstance
        {
            src = src == null ? log : src,
            log = log,
            obj = obj,
            time = Time.time,
            code = 'w'
        }); ;
    }

    public void Err(object src, string log, UnityEngine.Object obj, bool alwaysLog)
    {
        LogInstance instance = new LogInstance
        {
            src = src,
            log = log,
            obj = obj,
            time = Time.time,
            code = 'e'
        };
        logs.Add(instance);
        bool logUnity = alwaysLog || this.ShouldLogUnity(obj, log);
        if (logUnity)
            Debug.LogError(log, obj);
    }

}
