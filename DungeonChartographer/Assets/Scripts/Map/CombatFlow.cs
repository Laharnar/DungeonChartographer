﻿using System;
using System.Collections;
using UnityEngine;

public class CombatFlow:LiveBehaviour, IDisplayUI
{
    [SerializeField] int turnOwner;
    static CombatFlow i;
    [SerializeField] string announcer;
    [SerializeField] string endTurnUiKey = "end turn";
    public bool forceEnd = false;
    OnPickCombat picker;

    protected override void LiveAwake()
    {
        Init.GetComponentIfNull(this, ref picker);
        UIManager.RegisterUI(this, endTurnUiKey);
        if (i == null)
            i = this;
    }


    public void UIForceEnd()
    {
        forceEnd = true;
    }

    void Update()
    {
        bool endTurn = true;
        if (!forceEnd)
        {
            foreach (var item in Unit.units)
            {
                if ((item.movesLeft > 0 || item.energyLeft > 0) && item.alliance == turnOwner)
                {
                    endTurn = false;
                }
            }
        }
        if (endTurn && Unit.units.Count > 0)
        {
            forceEnd = false;
            StartCoroutine(NextTurn());
        }
    }

    public IEnumerator NextTurn()
    {
        // end turn
        foreach (var item in Unit.units)
        {
            item.SkipTurn();
        }
        turnOwner = (turnOwner + 1) % 2;

        UIManager.GetUI(announcer).Run(turnOwner == 0 ? "%PlayerTurn" : "%EnemyTurn");
        // start turn
        picker.StartTurn();
        foreach (var item in Unit.units)
        {
            item.StartTurn();
        }
        yield return new WaitForSeconds(0.5f);
        if (!IsPlayerTurn())
        {
            var temp = new UnitList();
            temp.AddRange(Unit.units);
            for (int i = 0; i < temp.Count; i++)
            {
                var item = temp[i];
                if (item == null)
                    continue;
                if (item.alliance != Unit.playerAlliance)
                {
                    yield return item.ai.AiTurn();
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
    }

    internal static bool IsPlayerTurn()
    {
        return i.turnOwner == Unit.playerAlliance;
    }

    public void Run(object data)
    {
        if ((string)data == "End turn")
        {
            UIForceEnd();
        }
    }
}


