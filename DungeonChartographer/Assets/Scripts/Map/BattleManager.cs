using System.Collections.Generic;
using UnityEngine;

public class BattleManager: ISlotPicker
{
    public static BattleManager Instance = new BattleManager();

    public SlotInfo GetSlot(Vector2Int pos)
    {
        return new SlotInfo(pos, Unit.GetUnits(pos));
    }
}
