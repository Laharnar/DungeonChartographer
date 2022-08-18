using UnityEngine;

namespace Interact
{
    public class InteractProxy : LiveBehaviour, IPropWriter
    {
        public InteractState proxy;

        public void Write(string prop, string value)
        {
            proxy.store.SetPropInt(prop, value);
        }
    }
}