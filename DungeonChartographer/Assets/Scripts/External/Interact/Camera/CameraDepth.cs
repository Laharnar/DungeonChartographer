using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CameraDepth : MonoBehaviour
{
    void OnEnable()
    {
        Camera.main.depthTextureMode = DepthTextureMode.Depth;
    } 
}
