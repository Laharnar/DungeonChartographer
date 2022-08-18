using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public Transform spawnObject;
    public Transform spawnParent;
    public bool onlyOnMove = true;
    Vector3 lastPos;

    private void Awake()
    {
        this.GetTransformIfNull(ref spawnParent);
    }

    void Update()
    {
        if (onlyOnMove && transform.position != lastPos)
        {
            lastPos = transform.position;
            Instantiate(spawnObject, transform.position, new Quaternion(), spawnParent);
        }
    }
}
