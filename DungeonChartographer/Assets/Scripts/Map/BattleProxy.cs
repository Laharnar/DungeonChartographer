using System.Collections.Generic;
using UnityEngine;

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
    }

    private void Flee()
    {
        Debug.Log("Flee");
        UIManager.GetUI("Flee")?.Run("show");
    }
}
