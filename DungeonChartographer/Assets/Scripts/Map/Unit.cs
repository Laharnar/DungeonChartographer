using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : MonoBehaviour, IUnitReliant, IUnitInfo
{
    public Vector2Int Pos { get => Vector2Int.FloorToInt(transform.position); }
    IUnitInfo IUnitReliant.Unit { get => this; }
    static Vector3 offset = Vector2.one / 2f;

    [SerializeField] float moveSpeed = 10f;
    public int alliance;
    Animator animator;
    public Transform visuals;
    Vector2 target;

    public static int playerAlliance = 0;

    public static List<Unit> units = new List<Unit>();
    [SerializeField] int moveAmount = 3;
    [SerializeField] int energyAmount = 2;
    public int energyLeft { get; private set; }
    public int movesLeft { get; private set; }

    private void Awake()
    {
        Logs.ExistsInspector(animator, this, "no animator");
        if (visuals == null) visuals = transform;
        units.Add(this);
        ResetTurn();
    }

    private void OnDestroy()
    {
        units.Remove(this);
    }

    public void LockTurn()
    {
        movesLeft = 0;
        energyLeft = 0;
    }

    public void ResetTurn()
    {
        movesLeft = moveAmount;
        energyLeft = energyAmount;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, Vector3.one/2);
        if(visuals)
            Gizmos.DrawLine(visuals.position, target);
        Gizmos.DrawWireCube(Vector3Int.FloorToInt(transform.position) + Vector3.one/2f, Vector3.one);
    }

    public static List<Unit> GetUnits(Vector2Int pos)
    {
        List<Unit> foundUnits = new List<Unit>();
        foreach (var item in units)
        {
            if (item.Pos == pos)
                foundUnits.Add(item);
        }
        return foundUnits;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="onEnd"></param>
    /// <param name="preMove">Jumps to end and moves only visuals</param>
    public void MovePath(Vector2Int pos, Action onEnd, bool preMove = false)
    {
        StartCoroutine(MovePathCoro(pos, onEnd, preMove));
    }

    public void Move(Vector2Int pos, Action onEnd, bool preMove = false)
    {
        StartCoroutine(MoveCoro(transform, (Vector3Int)pos, onEnd, true, preMove));
    }

    public IEnumerator MovePathCoro(Vector2Int pos, Action onEnd, bool preMove = false, bool energy = true)
    {
        Path path = Pathfinding.FindPath(Pos, pos);
        if(energy) path.CutToRange(movesLeft);
        Debug.Log($"Move path: {name} -> {pos} | path:{path.StepCount} ");
        Transform obj = transform;
        if (preMove && visuals != obj)
        {
            var current = transform.position;
            transform.position = (Vector3Int)path.Last + offset;
            obj = visuals;
            obj.position = current;
        }
        if(energy) movesLeft -= path.StepCount;
        while (path.StepCount > 0)
        {
            yield return MoveCoro(obj, (Vector2)path.First, onEnd:null, last:path.StepCount == 1, preMove:false);
            path.RemoveFirst();
        }
        onEnd?.Invoke();
    }

    public IEnumerator MoveCoro(Transform obj, Vector3 pos, Action onEnd, bool last = true, bool preMove = false)
    {
        pos += offset;
        PlayBoolAnimation("move", true);
        
        var mdir = Vector2Int.FloorToInt(pos) - Vector2Int.FloorToInt(obj.position);
        if (preMove)
        {
            var current = transform.position;
            transform.position = pos + offset;
            obj.position = current;
        }
        var dir = pos - obj.position;
        var magnitude = dir.magnitude;
        while (magnitude > 0.2f)
        {
            dir = pos - obj.position;
            magnitude = dir.magnitude;
            obj.Translate(dir / magnitude * moveSpeed * Time.deltaTime);
            target = obj.position;
            yield return null;
        }
        obj.position = pos;
        target = obj.position;
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
        energyLeft -= 1;
        onEnd();
    }
}
