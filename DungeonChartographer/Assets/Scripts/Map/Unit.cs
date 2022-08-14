using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour, IUnitReliant, IUnitInfo
{
    public Vector2Int Slot { get => Vector2Int.FloorToInt(transform.position); }
    IUnitInfo IUnitReliant.Unit { get => this; }

    public float moveSpeed = 10f;
    Animator animator;

    static Vector3 offset = Vector2.one / 2f;

    public static List<Unit> units = new List<Unit>();

    private void Awake()
    {
        Logs.ExistsInspector(animator, this, "no animator");
        units.Add(this);
    }

    private void OnDestroy()
    {
        units.Remove(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, Vector3.one/2);
        Gizmos.DrawWireCube(Vector3Int.FloorToInt(transform.position) + Vector3.one/2f, Vector3.one);
    }

    public static List<Unit> GetUnits(Vector2Int pos)
    {
        List<Unit> foundUnits = new List<Unit>();
        foreach (var item in units)
        {
            if (item.Slot == pos)
                foundUnits.Add(item);
        }
        return foundUnits;
    }

    public void MovePath(Vector2Int pos, Action onEnd)
    {
        StartCoroutine(MovePathCoro(pos, onEnd));
    }

    public void Move(Vector2Int pos, Action onEnd)
    {
        StartCoroutine(MoveCoro((Vector3Int)pos, onEnd, true));
    }

    public IEnumerator MovePathCoro(Vector2Int pos, Action onEnd)
    {
        Path path = Pathfinding.FindPath(Slot, pos);
        Debug.Log($"Move path: {name} -> {pos} | path:{path.StepCount} ");
        while (path.StepCount > 0)
        {
            yield return MoveCoro((Vector2)path.First, null, path.StepCount == 1);
            path.RemoveFirst();
        }
        onEnd?.Invoke();
    }

    public IEnumerator MoveCoro(Vector3 pos, Action onEnd, bool last = true)
    {
        pos += offset;
        PlayBoolAnimation("move", true);
        
        var dir = pos - transform.position;
        var magnitude = dir.magnitude;
        while (magnitude > 0.2f)
        {
            dir = pos - transform.position;
            magnitude = dir.magnitude;
            transform.Translate(dir / magnitude * moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = pos;
        onEnd?.Invoke();
        if (last)
            PlayBoolAnimation("move", false);
    }

    private void PlayBoolAnimation(string v, bool value)
    {
        if (!animator) return;
        animator.SetBool(v, value);
    }

    internal void Attack(Vector3Int slot, SkillAttack skillAttack, Action onEnd)
    {
        Debug.Log("impl attack");
    }
}
