using System;
using System.Collections;
using UnityEngine;

public class CombatFlow:MonoBehaviour
{
    [SerializeField] int turnOwner;
    static CombatFlow i;
    [SerializeField] string announcer;

    private void Awake()
    {
        if (i == null)
            i = this;
    }

    public IEnumerator NextTurn()
    {
        foreach (var item in Unit.units)
        {
            item.LockTurn();
        }
        turnOwner = (turnOwner + 1) % 2;
        UIManager.GetUIPostAwake(announcer).Show(turnOwner == 0 ? "%PlayerTurn" : "%EnemyTurn");
        foreach (var item in Unit.units)
        {
            item.ResetTurn();
        }
        yield return new WaitForSeconds(0.5f);
        if (!IsPlayerTurn())
            yield return NextTurn();
    }

    void Update()
    {
        bool endTurn = true;
        foreach (var item in Unit.units)
        {
            if (item.movesLeft > 0 && item.alliance == turnOwner)
            {
                endTurn = false;
            }
        }
        if (endTurn)
        {
            StartCoroutine(NextTurn());
        }
    }

    internal static bool IsPlayerTurn()
    {
        return i.turnOwner == Unit.playerAlliance;
    }
}
