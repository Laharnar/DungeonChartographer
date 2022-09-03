using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public abstract class LiveEditorBehaviour : LiveBehaviour
{

    public enum UpdateImportance
    {
        High,
        Low,
        None
    }
    /// <summary>
    /// Public only for editors
    /// </summary>
    [HideInInspector] public float editorAwake = 0;

    /// <summary>
    /// Use editor awake instead
    /// </summary>
    protected sealed override void LiveAwake()
    {
        if(EditorLiveAwake() == UpdateImportance.High)
            editorAwake = Time.time;
    }

    protected virtual UpdateImportance EditorLiveAwake() => UpdateImportance.High;
}

public abstract class LiveBehaviour : MonoBehaviour
{
    interface ILiveAwakeCoro
    {
        IEnumerator LiveAwakeCoro();
    }

#if UNITY_EDITOR
    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded()
    {
        // updates all live behaviour in PLAY mode, and also editor only 
        HashSet<LiveBehaviour> behaviours = new HashSet<LiveBehaviour>();
        if (Application.isPlaying)
        {
            var items = FindObjectsOfType<LiveBehaviour>();
            foreach (var item in items)
            {
                item.LiveAwake();
                behaviours.Add(item);
            }
        }
        var live = FindObjectsOfType<LiveEditorBehaviour>();
        foreach (var item in live)
        {
            if (behaviours.Contains(item)) continue;
            item.LiveAwake();
            behaviours.Add(item);
        }
    }
#endif

    private void Awake()
    {
        LiveAwake();
        if (this is ILiveAwakeCoro live)
            StartCoroutine(live.LiveAwakeCoro());
    }

    protected virtual void LiveAwake()
    {
    }
}
