#if UNITY_EDITOR
#endif

using UnityEngine;

namespace Combat
{
    public interface IUnitReliant
    {
        IUnitInfo Unit { get; }
    }

    public interface IUnitInfo
    {
        Vector2Int Slot { get; }
        Transform transform { get; }
        Transform GameObject { get; }
    }
}
