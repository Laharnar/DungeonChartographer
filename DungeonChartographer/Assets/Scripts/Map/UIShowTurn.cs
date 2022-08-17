using System;
using UnityEngine;

public class UIShowTurn : LiveBehaviour, IDisplayUI
{
    TMPro.TextMeshProUGUI text;
    public string uiKey = "show turn";
    public string displayText;
    string ash;
    protected override void LiveAwake()
    {
        Init.GetComponentIfNull(this, ref text);
        this.RegisterUI(uiKey);
    }

    // Update is called once per frame
    void Update()
    {
        text.text = displayText;
    }

    public void ShowPlayerTurn()
    {
        displayText = "Player turn";
    }

    public void ShowEnemyTurn()
    {
        displayText = "Enemy turn";
    }

    public void Run(object data)
    {
        Debug.Log("Show turn");
        if ((string)data == "%PlayerTurn")
            ShowPlayerTurn();
        else if ((string)data == "%EnemyTurn")
            ShowEnemyTurn();
    }
}
