using UnityEngine;

namespace Interact
{
    public class InteractProxy : LiveBehaviour, IPropWriter
    {
        public InteractState proxy;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            proxy.OnTriggerEnter2D(collision);
        }

        public void Write(string prop, string value)
        {
            proxy.store.SetPropInt(prop, value);
        }
    }
}