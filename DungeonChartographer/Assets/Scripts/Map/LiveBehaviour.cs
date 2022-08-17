using System.Collections;
using UnityEngine;

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
        if (Application.isPlaying)
        {
            Debug.Log("reinit ok");
            var items = FindObjectsOfType<LiveBehaviour>();
            foreach (var item in items)
            {
                item.LiveAwake();
            }
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
