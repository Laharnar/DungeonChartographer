using System.Collections.Generic;
using UnityEngine;

public class SkillDisplay : MonoBehaviour
{
    public GameObject picker;
    public GameObject ui;
    public IPlayerPicker playerPicker;
    public List<UnityEngine.UI.Button> buttons = new List<UnityEngine.UI.Button>();
    public List<TMPro.TextMeshProUGUI> texts = new List<TMPro.TextMeshProUGUI>();
    SlotInfo lastSlot = null;
    private int selectedSkill;

    private void Awake()
    {
        this.GetGameObjIfNull(ref picker);
        this.GetGameObjIfNull(ref ui);
        Init.GetComponentIfNull(picker, ref playerPicker);
        buttons.AddRange(ui.GetComponentsInChildren<UnityEngine.UI.Button>());
        texts.AddRange(ui.GetComponentsInChildren<TMPro.TextMeshProUGUI>());
        foreach (var item in buttons)
        {
            item.gameObject.SetActive(false);
        }
    }

    // [ui event]
    public void SelectSkill(int i)
    {
        selectedSkill = i;
        var units = Unit.GetUniqueUnits(lastSlot.unit.Pos);
        playerPicker.OnPickerPicks(new object[2]{ "skill", new SkillAttack(){
            unit = units[i],
            skillId = i
        }}, null);
    }

    private void Update()
    {
        if (playerPicker.SelectedSlot != lastSlot)
        {
            lastSlot = playerPicker.SelectedSlot;
            foreach (var item in buttons)
            {
                item.gameObject.SetActive(false);
            }
            if (lastSlot != null)
            {
                if (lastSlot.unit != null)
                {
                    var units = Unit.GetUniqueUnits((unit ) => unit.Pos == lastSlot.unit.Pos && unit.alliance == Unit.playerAlliance);
                    for (int i = 0; i < buttons.Count && i < units.Count; i++)
                    {
                        buttons[i].gameObject.SetActive(true);
                        texts[i].text = units[i].alias;
                    }
                }
            }
        }
    }
}
