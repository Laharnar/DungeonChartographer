using System;
using UnityEngine;
using UnityEngine.Assertions;

public class OnPickCombat : MonoBehaviour, IPlayerPicker
{
    SlotInfo playerPicked;

    [SerializeField] string pickMode = "free";
    ISlotPicker slotPicker => BattleManager.I;
    public SkillAttack activeSkill;
    [SerializeField] Unit activeUnit;
    CombatFlow combatFlow;

    private void Awake()
    {
        Init.GetComponentIfNull(this, ref combatFlow);
    }

    void AfterMoveOrAfterAttack()
    {
        if (pickMode == "moving" || pickMode == "attacking")
            pickMode = "unit";
        else Debug.LogError($"Invalid mode {pickMode}");
    }

    internal void StartTurn()
    {
        Deselect();
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
                        if (activeUnit.alliance == Unit.playerAlliance)
                        {
                            if (activeUnit.movesLeft > 0)
                            {
                                activeUnit.MovePath(slot.slot, AfterMoveOrAfterAttack);
                                pickMode = "moving";
                            }
                        }
                        else
                        {
                            Deselect();
                        }
                    }
                    else
                    {
                        if (slot.unit.alliance == Unit.playerAlliance)
                        {
                            Deselect();
                            pickMode = "unit";
                            activeUnit = slot.unit;
                        }
                        else if (activeUnit.energyLeft > 0)
                        {
                            pickMode = "attack";
                        }
                    }
                }
                else if (pickMode == "attack")
                {
                    pickMode = "attacking";
                    activeUnit.Attack((Vector3Int)slot.slot, activeSkill, AfterMoveOrAfterAttack);
                }
            }
            else if (sid == "right")
            {
                Deselect();
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

    private void Deselect()
    {
        playerPicked = null;
        activeUnit = null;
        pickMode = "free";
    }
}
