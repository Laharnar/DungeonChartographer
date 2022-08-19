[System.Serializable]
public class SkillAttack
{
    public int skillId;
    public Unit unit;

    public override string ToString()
    {
        return $"{unit}/skill/{skillId}";
    }
}
