using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BattleScenes
{
    public string scene;
    public DungeonMapUncover enter;
    public Transform obj;
    public GameObject instance;
}

public class Battle: MonoBehaviour, ISlotPicker
{
    static Battle i;
    public static Battle I => Init.AutoSingleton(ref i);
    DungeonMapUncover previous;
    DungeonMapExplorer explorer;
    [SerializeField] List<BattleScenes> battles = new List<BattleScenes>();

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
        return new SlotInfo(pos, Unit.GetUniqueUnits(pos));
    }

    public SlotInfo GetSlot(Vector2 pos)
    {
        Vector2Int i2 = Vector2Int.FloorToInt(pos);
        return new SlotInfo(i2, Unit.GetUniqueUnits(i2));
    }

    public void Load(string combatScene, DungeonMapUncover last, DungeonMapExplorer explorer)
    {
        var load = battles.Find((item) => item.scene == combatScene);
        Debug.Log("instance");
        load.obj.gameObject.SetActive(false);
        load.instance = Instantiate(load.obj, transform).gameObject;
        load.instance.SetActive(true);
        load.enter = last;
        previous = last;
        this.explorer = explorer;
    }

    public void ConcludeBattle(string loadCombatName, BattleConclusion conclusion)
    {
        var load = battles.Find((item) => item.scene == loadCombatName);
        if (load.instance == null || load.enter == null) 
            throw new NotImplementedException("wrong impl"); 
        switch (conclusion)
        {
            case BattleConclusion.Win:
                load.enter.GetComponent<IGridItem>().Init(Color.white);
                break;
            case BattleConclusion.Lose:
                break;
            case BattleConclusion.Flee:
                explorer.transform.position = previous.transform.position;
                break;
            default:
                break;
        }
    }

    public enum BattleConclusion
    {
        Win,
        Lose,
        Flee
    }
}
