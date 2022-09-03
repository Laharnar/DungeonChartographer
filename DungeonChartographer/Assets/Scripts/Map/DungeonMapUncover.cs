using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CombatLoad
{
    public string loadCombat;
    public string combatArgs;
}

public class DungeonMapUncover : LiveBehaviour
{
    [System.Flags]
    public enum WallDirections
    {
        Left = 1, Up = 2, Right = 4, Down = 8
    }

    public Transform coveredObj;
    public Transform wallsLURD; // left up right down
    public Transform waterLURD; // left up right down
    public Transform waterCenter; // left up right down

    public bool uncovered = false;
    public WallDirections walls;
    public WallDirections explored;

    public CombatLoad combatArgs;

    protected override void LiveAwake()
    {
        Show();
        transform.position = Vector3Int.FloorToInt((Vector2)transform.position + Vector2.one / 2f);
        InvokeRepeating("Show", 1, 1);
    }

    public void Show()
    {
        coveredObj.gameObject.SetActive(!uncovered);
        waterCenter.gameObject.SetActive(explored != 0);
        waterLURD.gameObject.SetActive(explored != 0);
        for (int i = 0; i < 4; i++)
        {
            bool show = (walls & (WallDirections)(1 << i)) != 0;
            wallsLURD.GetChild(i).gameObject.SetActive(show);
        }
        for (int i = 0; i < 4; i++)
        {
            bool show = (explored & (WallDirections)(1 << i)) != 0;
            waterLURD.GetChild(i).gameObject.SetActive(show);
        }
    }
}
