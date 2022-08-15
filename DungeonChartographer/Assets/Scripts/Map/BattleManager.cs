using Combat;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager: MonoBehaviour, ISlotPicker
{
    public static BattleManager I = new BattleManager();

    public RangeMap GetMap(Func<SlotInfo, bool> predicate)
    {
        var map = new RangeMap();
        foreach (var item in Unit.units)
        {
            if(predicate(GetSlot(item.Pos)))
                map.Add(item.Pos);
        }
        return map;
    }

    public SlotInfo GetSlot(Vector2Int pos)
    {
        return new SlotInfo(pos, Unit.GetUnits(pos));
    }

    public SlotInfo GetSlot(Vector2 pos)
    {
        Vector2Int i2 = Vector2Int.FloorToInt(pos);
        return new SlotInfo(i2, Unit.GetUnits(i2));
    }
}
