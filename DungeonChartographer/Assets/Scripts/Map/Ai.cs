using System.Collections;
using UnityEngine;

public class Ai : MonoBehaviour
{
    public Unit self;

    private void Awake()
    {
        Init.GetComponentIfNull(this, ref self);
    }

    internal IEnumerator AiTurn()
    {
        yield return null;
        var enemies = Unit.GetUniqueUnits((item) => item.alliance != self.alliance);
        float min = float.MaxValue;
        Unit target = null;
        foreach (var item in enemies)
        {
            if (item.jointName != "")
                continue;
            var lastMin = min;
            min = Mathf.Min(Vector2.Distance(item.Pos, self.Pos), min);
            if (min != lastMin)
                target = item;
        }
        if (target != null)
        {
            yield return self.MovePathCoro(target.Pos);
            self.Attack((Vector3Int)target.Pos, new SkillAttack() { unit = target, self = self }, null);
        }
        self.SkipTurn();
    }
}
