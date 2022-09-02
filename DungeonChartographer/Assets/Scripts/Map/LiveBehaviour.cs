using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public abstract class LiveEditorBehaviour : LiveBehaviour
{

}

public abstract class LiveBehaviour : MonoBehaviour
{
    public static float reloadTime = 0;

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
        reloadTime = Time.time;
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
