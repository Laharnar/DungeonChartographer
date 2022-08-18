using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerPicker : MonoBehaviour
{
    Vector2 mouse;
    Vector2Int hovered;
    public Vector2Int selected { get; private set; }
    public Vector2Int leftSelected { get; private set; }
    public Vector2Int rightSelected { get; private set; }
    
    public IPlayerPicker route;
    public IPropWriter propWriter;

    private void Start()
    {
        this.GetComponentIfNull(ref route);
        this.GetComponentIfNull(ref propWriter);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere((Vector3Int)hovered + Vector3.one / 2f, 0.5f);
    }

    private void Update()
    {
        mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        hovered = Vector2Int.FloorToInt(mouse);
        transform.position = (Vector3Int)hovered;

        if (Input.GetKeyDown(KeyCode.Mouse0) || Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
            selected = Vector2Int.FloorToInt(mouse);
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                leftSelected = selected;
                if (CombatFlow.IsPlayerTurn())
                    route.OnPickerPicks("left", this);
            }
            else
            {
                rightSelected = selected;
                if (CombatFlow.IsPlayerTurn()) 
                    route.OnPickerPicks("right", this);
            }
        }
    }

    public void OnPickSkillUI(int id)
    {
        route.OnPickerPicks(new object[2]{ "skillui", id}, this);
    }
}
