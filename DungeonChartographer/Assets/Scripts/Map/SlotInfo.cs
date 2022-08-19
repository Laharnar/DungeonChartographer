using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class UnitList:List<Unit>
{
    public Unit First()
    {
        foreach (var item in this)
        {
            if (item.jointName == "")
                return item;
        }
        foreach (var item in this)
        {
            if (item.jointName != "")
                return item;
        }
        return null;
    }

}

public class SlotInfo
{
    public Unit unit = null;// first unit out of units
    public List<Unit> units = new List<Unit>();
    public Vector2Int slot;

    public SlotInfo(Vector2Int slot, UnitList units)
    {
        this.slot = slot;
        this.units = units;
        this.unit = units.First();
    }

    public bool IsWalkable { get => !HasUnit(); }


    bool HasUnit()
    {
        foreach (var item in units)
        {
            if (item.jointName == "")
                return true;
        }
        return false;
    }
}
