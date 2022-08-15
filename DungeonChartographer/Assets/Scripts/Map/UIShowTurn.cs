using System;
using System.Collections;
using UnityEngine;

public class UIShowTurn : MonoBehaviour, IDisplayUI
{
    TMPro.TextMeshProUGUI text;
    public string uiKey = "show turn";
    public string displayText;

    private void Awake()
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

    public void ShowText(string text)
    {
        if (text == "%ShowPlayerTurn")
            ShowPlayerTurn();
        else if (text == "%ShowEnemyTurn")
            ShowEnemyTurn();
        else displayText = text;
    }

    public void Show(object data)
    {
        Debug.Log("Show turn");
        if ((string)data == "%PlayerTurn")
            ShowPlayerTurn();
        else if ((string)data == "%EnemyTurn")
            ShowEnemyTurn();
    }
}
