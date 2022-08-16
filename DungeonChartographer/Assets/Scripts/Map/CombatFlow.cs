using System;
using System.Collections;
using UnityEngine;

public class CombatFlow:MonoBehaviour, IDisplayUI
{
    [SerializeField] int turnOwner;
    static CombatFlow i;
    [SerializeField] string announcer;
    [SerializeField] string endTurnUiKey = "end turn";
    public bool forceEnd = false;
    OnPickCombat picker;

    private void Awake()
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

    public IEnumerator NextTurn()
    {
        // end turn
        foreach (var item in Unit.units)
        {
            item.LockTurn();
        }
        turnOwner = (turnOwner + 1) % 2;

        UIManager.GetUI(announcer).Run(turnOwner == 0 ? "%PlayerTurn" : "%EnemyTurn");
        // start turn
        picker.StartTurn();
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
        if (endTurn)
        {
            forceEnd = false;
            StartCoroutine(NextTurn());
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


