using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class SlotInfo
{
    public Unit unit = null;// first unit out of units
    public List<Unit> units = new List<Unit>();
    public Vector2Int slot;

    public SlotInfo(Vector2Int slot, List<Unit> units)
    {
        this.slot = slot;
        this.units = units;
        if(units.Count > 0)
            this.unit = units[0];
    }
}
