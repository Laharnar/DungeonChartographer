using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class DungeonMapExplorer : MonoBehaviour
{
    Vector2 lastMoveDir; // for dir.
    public float delay = 0.75f;
    float lastTime = -1;
    bool cinematics = false;
    DungeonMapUncover last;

    private void Update()
    {
        if (cinematics) return;
        var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (Time.time > lastTime + delay || lastMoveDir != input)
        {
            lastMoveDir = input;
            lastTime = Time.time;
            transform.Translate(input);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var uncover = collision.GetComponent<DungeonMapUncover>();
        if (uncover == null) return;
        uncover.uncovered = true;
        if (lastMoveDir.x > 0)
        {
            uncover.explored |= DungeonMapUncover.WallDirections.Left;
        }
        else if(lastMoveDir.x < 0)
        {
            uncover.explored |= DungeonMapUncover.WallDirections.Right;
        }
        else if(lastMoveDir.y > 0)
        {
            uncover.explored |= DungeonMapUncover.WallDirections.Down;
        }
        else if (lastMoveDir.y < 0)
        {
            uncover.explored |= DungeonMapUncover.WallDirections.Up;
        }
        uncover.Show();
        if(uncover.combatArgs.loadCombat != "")
            StartCoroutine(SlowLoad(uncover, last));
        last = uncover;
    }

    IEnumerator SlowLoad(DungeonMapUncover uncover, DungeonMapUncover last)
    {
        cinematics = true;
        yield return new WaitForSeconds(1.25f);
        Battle.I.Load(uncover.combatArgs.loadCombat, last, this);
        cinematics = false;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var uncover = collision.GetComponent<DungeonMapUncover>();
        if (uncover == null) return;
        uncover.uncovered = true;
        if (lastMoveDir.x > 0)
        {
            uncover.explored |= DungeonMapUncover.WallDirections.Right;
        }
        else if (lastMoveDir.x < 0)
        {
            uncover.explored |= DungeonMapUncover.WallDirections.Left;
        }
        else if (lastMoveDir.y > 0)
        {
            uncover.explored |= DungeonMapUncover.WallDirections.Up;
        }
        else if (lastMoveDir.y < 0)
        {
            uncover.explored |= DungeonMapUncover.WallDirections.Down;
        }
        uncover.Show();
    }
}
