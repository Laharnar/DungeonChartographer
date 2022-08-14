using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    public class CameraController : MonoBehaviour
    {
        public void Init(Vector2 initPos)
        {
            transform.position = new Vector3(initPos.x, initPos.y, transform.position.z);
            enabled = true;
        }

        void Update()
        {
            Vector2 move = Vector2.zero;
            if (Input.GetKey(KeyCode.D))
            {
                move.x = 1.0f;
            }
            if (Input.GetKey(KeyCode.A))
            {
                move.x = -1.0f;
            }
            if (Input.GetKey(KeyCode.W))
            {
                move.y = 1.0f;
            }
            if (Input.GetKey(KeyCode.S))
            {
                move.y = -1.0f;
            }
            move.Normalize();

            transform.Translate(move * 12.0f * Time.deltaTime);
        }
    }
}
