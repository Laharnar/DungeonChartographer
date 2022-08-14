using UnityEngine;
using UnityEngine.Assertions;

public class OnPickCombat : MonoBehaviour, IPlayerPicker
{
    SlotInfo playerPicked;

    [SerializeField] string pickMode = "free";
    ISlotPicker slotPicker => BattleManager.Instance;
    public SkillAttack activeSkill;
    public Unit activeUnit;

    void AfterMoveOrAfterAttack()
    {
        if (pickMode == "moving" || pickMode == "attacking")
            pickMode = "unit";
        else Debug.LogError($"Invalid mode {pickMode}");
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id">"skillui", "left", "right"</param>
    /// <param name="picker"></param>
    public void OnPickerPicks(object id, PlayerPicker picker)
    {
        Assert.IsNotNull(slotPicker);
        Assert.IsNotNull(picker);

        if (id is string sid)
        {
            if (sid == "left")
            {
                var slot = slotPicker.GetSlot(picker.leftSelected);
                playerPicked = slot;
                if (pickMode == "free" && slot.unit != null)
                {
                    activeUnit = slot.unit;
                    pickMode = "unit";
                }
                else if (pickMode == "unit")
                {
                    if (slot.unit == null)
                    {
                        activeUnit.MovePath(slot.slot, AfterMoveOrAfterAttack);
                        pickMode = "moving";
                    }
                }
                else if (pickMode == "attack")
                {
                    activeUnit.Attack((Vector3Int)slot.slot, activeSkill, AfterMoveOrAfterAttack);
                    pickMode = "attacking";
                }
            }
            else if (sid == "right")
            {
                var slot = slotPicker.GetSlot(picker.rightSelected);
                playerPicked = null;
                activeUnit = null;
                pickMode = "free";
            }
        }
        else
        {
            object[] oid = (object[])id;
            if ((string)oid[0] == "skillui")
            {
                Assert.IsTrue(pickMode != "unit");
                pickMode = "attack";
            }
        }
    }
}
