using UnityEngine;

namespace Combat
{
    public abstract class TargetFilter : ScriptableObject
    {
        public abstract System.Func<IUnitInfo, Vector2Int, bool> GetPredicate();
    }
}