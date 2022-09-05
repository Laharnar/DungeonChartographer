using System;
using UnityEngine;
using UnityEngine.Assertions;

public class OnPickCombat : MonoBehaviour, IPlayerPicker
{
    [SerializeField] string pickMode = "free";

    ISlotPicker slotPicker => Battle.I;

    public SlotInfo SelectedSlot { get => selectedSlot; }

    [Header("Realtime")]
    public SkillAttack activeSkill = new SkillAttack();
    [SerializeField] SlotInfo selectedSlot;
    [SerializeField] Unit activeUnit;

    CombatFlow combatFlow;
    Interact.InteractProxy proxy;

    private void Awake()
    {
        Init.GetComponentIfNull(this, ref combatFlow);
        Init.GetComponentIfNull(this, ref proxy);
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

        if (id is string sid)
        {
            if (sid == "left")
            {
                var slot = slotPicker.GetSlot(picker.leftSelected);
                if(proxy)proxy.proxy.store.SetPropInt("slotCount", slot.units.Count.ToString());
                selectedSlot = slot;
                if (pickMode == "free" && slot.unit != null)
                {
                    activeUnit = slot.unit;
                    pickMode = "unit";
                }
                else if (pickMode == "unit" || (activeSkill.unit != null && activeSkill.unit != slot.unit))
                {
                    if (slot.unit == null)
                    {
                        if (activeUnit.alliance == Unit.playerAlliance)
                        {
                            if (activeUnit.movesLeft > 0 && activeUnit.jointName == "")
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
                        else if (activeUnit.energyLeft > 0 && activeUnit.alliance == Unit.playerAlliance)
                        {
                            pickMode = "attack";
                            activeSkill.unit = slot.unit;
                        }
                    }
                }
                else if (pickMode == "attack")
                {
                    if (activeSkill.unit == slot.unit)
                    {
                        pickMode = "attacking";
                        activeUnit.Attack((Vector3Int)slot.slot, activeSkill, AfterMoveOrAfterAttack);
                    }
                    else
                    {

                    }
                    activeSkill.unit = null;
                }
            }
            else if (sid == "right")
            {
                Deselect();
                selectedSlot = null;
            }
        }
        else
        {
            object[] oid = (object[])id;
            if ((string)oid[0] == "skill" && pickMode != "attack")
            {
                Assert.IsTrue(pickMode == "unit" && activeUnit.alliance == Unit.playerAlliance);
                if (activeUnit.energyLeft > 0)
                {
                    SkillAttack skillAttack = (SkillAttack)oid[1];
                    activeSkill = skillAttack;
                    pickMode = "attack";
                }
            }
        }
    }

    private void Deselect()
    {
        activeUnit = null;
        pickMode = "free";
    }
}
