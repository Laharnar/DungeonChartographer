using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zone : LiveBehaviour
{
    public bool refresh = false;
    public Transform prefab;
    Collider2D[] colliders;
    List<Bounds> bounds = new List<Bounds>();
    int floodCount;
    float time;
    Vector2 pos;
    HashSet<Vector2Int> flooded = new HashSet<Vector2Int>();
    public Transform parentHolder;

    protected override void LiveAwake()
    {
        refresh = true;
        if (parentHolder == null) parentHolder = new GameObject("holder").transform;
        parentHolder.parent = transform;
    }

    private void OnDrawGizmos()
    {
        if (parentHolder == null) parentHolder = new GameObject("holder").transform;
        parentHolder.parent = transform;
        if (refresh || colliders == null)
        {
            colliders = GetComponentsInChildren<Collider2D>();
            refresh = false;
        }
        if ((int)Time.realtimeSinceStartup != time)
        {
            time = (int)Time.realtimeSinceStartup;
            bounds.Clear();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null)
                {
                    colliders = null;
                    break;
                }
                bounds.Add(colliders[i].bounds);
            }
            flooded.Clear();
            foreach (var item in bounds)
            {
                FloodBound(item, flooded);
            }
            CreateSlots(flooded.Count);
        }
        foreach (var item in flooded)
        {
            Gizmos.DrawWireCube((Vector2)item + Vector2.one / 2f, Vector3.one);
        }
    }

    private void CreateSlots(int count)
    {
        if (prefab)
        {
            if (floodCount != count || count != parentHolder.childCount)
            {
                floodCount = count;
                DestroyImmediate(parentHolder.gameObject);
                parentHolder = new GameObject("holder").transform;
                foreach (var item in flooded)
                {
                    Instantiate(prefab, item + Vector2.one / 2f, Quaternion.identity, parentHolder);
                }
            }
            else
            {
                int i = 0;
                foreach (var item in flooded)
                {
                    if(i < parentHolder.childCount)
                        parentHolder.GetChild(i).position = (Vector2)item + Vector2.one / 2f;
                    i++;
                }
            }
        }
    }

    private static void FloodBound(Bounds item, HashSet<Vector2Int> flooded)
    {
        var start = Vector2Int.FloorToInt((Vector2)item.center);
        Queue<Vector2Int> que = new Queue<Vector2Int>();
        que.Enqueue(start);
        while (que.Count > 0)
        {
            var pos = Vector2Int.FloorToInt(que.Dequeue());
            if (flooded.Contains(pos)) continue;
            if (item.Contains((Vector2)pos))
            {
                flooded.Add(pos);
                que.Enqueue(pos + Vector2Int.right);
                que.Enqueue(pos - Vector2Int.right);
                que.Enqueue(pos + Vector2Int.up);
                que.Enqueue(pos - Vector2Int.up);
            }
        }
    }
}
