using Combat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : LiveBehaviour, IUnitReliant, IUnitInfo
{

    public string alias;
    [SerializeField] float moveSpeed = 10f;
    public int alliance;
    public Transform visuals;
    public Ai ai;
    public string jointName;
    public string energyProxy;
    public int givedmgMap = 1;
    public int recvDmgMap = 1;

    Animator animator;
    Vector2 target;
    public Skill skill;

    [SerializeField] int moveAmount = 3;
    [SerializeField] int energyAmount = 2;

    static Vector3 offset = Vector2.one / 2f;
    public static int playerAlliance = 0;
    public static List<Unit> units;

    public Vector2Int Pos { get => Vector2Int.FloorToInt(transform.position); }
    IUnitInfo IUnitReliant.Unit { get => this; }

    public int energyLeft { get; private set; }
    public int movesLeft { get; private set; }

    protected override void LiveAwake()
    {
        if(units == null) units = new List<Unit>();
        this.GetComponentIfNull(ref ai);
        Logs.ExistsInspector(animator, this, "no animator");
        if (visuals == null) visuals = transform;
        units.Add(this);
        StartTurn();
    }

    private void OnDestroy()
    {
        units.Remove(this);
    }

    public void SkipTurn()
    {
        movesLeft = 0;
        energyLeft = 0;
    }

    public void StartTurn()
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

    public static UnitList GetUniqueUnits(Vector2Int pos)
    {
        return GetUniqueUnits((unit) => unit.Pos == pos);
    }

    public static UnitList GetUnits(Func<Unit, bool> filter)
    {
        UnitList foundUnits = new UnitList();
        foreach (var item in units)
        {
            if (!filter(item)) continue;
            foundUnits.Add(item);
        }
        return foundUnits;
    }

    public static UnitList GetUniqueUnits(Func<Unit, bool> filter)
    {
        UnitList foundUnits = new UnitList();
        HashSet<string> joinNames = new HashSet<string>();
        foreach (var item in units)
        {
            if (!filter(item)) continue;
            if (item.jointName == "" || !joinNames.Contains(item.jointName))
            {
                foundUnits.Add(item);
                if (item.jointName != "")
                    joinNames.Add(item.jointName);
            }
        }
        return foundUnits;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="onEnd"></param>
    /// <param name="preMove">Jumps to end and moves only visuals</param>
    public void MovePath(Vector2Int pos, Action onEnd = null, bool preMove = false)
    {
        StartCoroutine(MovePathCoro(pos, onEnd, preMove));
    }

    public void Move(Vector2Int pos, Action onEnd, bool preMove = false)
    {
        StartCoroutine(MoveCoro(transform, (Vector3Int)pos, onEnd, true, preMove));
    }

    public IEnumerator MovePathCoro(Vector2Int pos, Action onEnd = null, bool preMove = false, bool energy = true)
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
        yield return null;
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
        skillAttack.self = this;
        var unit = GetUniqueUnits((Vector2Int)slot)[0];
        Debug.Log($"Attacking {name}{slot}({unit.name}) {skillAttack}");
        HashSet<Unit> joins = new HashSet<Unit>();
        if (jointName != "")
        {
            var jointUnits = GetUnits((unit) => unit.jointName == jointName);
            joins = new HashSet<Unit>(jointUnits);
            foreach (var item in jointUnits)
            {
                if(item != this)
                    item.energyLeft -= 1;
            }
        }
        energyLeft -= 1;
        if (energyProxy != "")
        {
            var proxy = GetUnits((unit) => unit.energyProxy == energyProxy);
            foreach (var item in proxy)
            {
                if (item != this && !joins.Contains(item))
                    item.energyLeft -= 1;
            }
        }
        if ((unit.recvDmgMap & givedmgMap) != 0)
        {
            if (skill)
                skill.Activate(skillAttack);
            else
                Destroy(unit.gameObject);
        }
        onEnd?.Invoke();
    }
}
