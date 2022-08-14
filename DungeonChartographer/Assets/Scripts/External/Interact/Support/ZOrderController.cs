using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public partial class ZOrderController : MonoBehaviour
    {
        private List<SpriteRenderer> rend = new List<SpriteRenderer>();

        public void InitRenderers(Transform container)
        {
            foreach (Transform child in container)
            {
                if (child.GetComponent<SpriteRenderer>() != null)
                {
                    rend.Add(child.GetComponent<SpriteRenderer>());
                }
            }
        }

        private void Update()
        {
            for (int i = 0; i < rend.Count; i++)
            {
                //rend[i].sortingOrder = -Mathf.FloorToInt(transform.position.y) * 10 + i;
                rend[i].transform.position = new Vector3(rend[i].transform.position.x,
                    rend[i].transform.position.y, transform.position.y - i * 0.001f);
            }
			if(rend.Count == 0){
				transform.position = new Vector3(transform.position.x,
                    transform.position.y, transform.position.y);
			}
        }

        public int SpriteCount { get { return rend.Count; } }
        public SpriteRenderer this[int i] { get { return rend[i]; } }
    }
}
