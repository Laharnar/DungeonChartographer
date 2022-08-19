using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill : MonoBehaviour
{
    public int givedmgMap = 1;
    public Transform spawnOnTargetPos;

    internal void Activate(SkillAttack skillAttack)
    {
        if ((skillAttack.self.recvDmgMap & givedmgMap) != 0)
        {
            Instantiate(spawnOnTargetPos, (Vector3Int)skillAttack.unit.Pos, new Quaternion());
            Destroy(skillAttack.unit.gameObject);
        }
    }
}
