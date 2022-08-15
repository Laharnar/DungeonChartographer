using Combat;
using System.Collections;
using UnityEngine;

public class SelfCorrection : MonoBehaviour
{
    public bool destroyOnUse = true;
    public Unit unit;

    private IEnumerator Start()
    {
        var units = Unit.GetUnits(Vector2Int.FloorToInt(transform.position));
        if (units.Count > 1)
        {
            this.GetComponentIfNull(ref unit);
            Vector2Int pos = Pathfinding.GetClosestFreeSlot(unit.Pos, Random.onUnitSphere);
            yield return unit.MovePathCoro(pos, null, preMove: true);
        }
        if (destroyOnUse)
            Destroy(this);
    }
}
