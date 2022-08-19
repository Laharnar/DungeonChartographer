[System.Serializable]
public class SkillAttack
{
    public int skillId;
    public Unit unit;
    public Unit self;

    public override string ToString()
    {
        return $"{unit}/skill/{skillId}";
    }
}
