using UnityEngine;

public static class Init
{
    public static void GetComponentIfNull<T>(this MonoBehaviour mono, ref T value)
    {
        if (value == null)
            value = mono.GetComponent<T>();
    }

    public static void GetTransformIfNull(this MonoBehaviour mono, ref Transform value)
    {
        if (value == null)
            value = mono.transform;
    }

    public static void GetGameObjIfNull(this MonoBehaviour mono, ref GameObject value)
    {
        if (value == null)
            value = mono.gameObject;
    }
}
