using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleProxy : MonoBehaviour, ITFuncStr
{
    public MonoBehaviour Obj { get; }

    public void Func(List<string> args, List<object> oargs)
    {
        if (args.Count == 0) return;
        if(args[0] == "Flee")
        {
            Flee();
        }
        else if(args[0] == "Escape")
        {
            Escape();
        }
    }

    private void Escape()
    {
        Debug.Log("Escape");
        UIManager.GetUI("Escape")?.Run("show");
        Dungeon.EscapedBattle();
    }

    private void Flee()
    {
        Debug.Log("Flee");
        UIManager.GetUI("Flee")?.Run("show");
        Dungeon.FleedBattle();
    }
}

public class Dungeon
{
    internal static void EscapedBattle()
    {
        throw new NotImplementedException();
    }

    internal static void FleedBattle()
    {
        throw new NotImplementedException();
    }

    internal static void WonBattle()
    {
        throw new NotImplementedException();
    }

    internal static void Died()
    {

    }

    internal static void FinishedDungeon()
    {

    }
    internal static void ExitedDungeon()
    {

    }
}
