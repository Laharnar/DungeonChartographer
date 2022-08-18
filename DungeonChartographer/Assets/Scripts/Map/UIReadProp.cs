using UnityEngine;

public class UIReadProp:LiveBehaviour
{
    public string propName;
    TMPro.TextMeshProUGUI text;
    float lastTime;
    public Interact.InteractProxy proxy;

    protected override void LiveAwake()
    {
        Init.GetComponentIfNull(this, ref text);
    }

    private void Update()
    {
        if((int)Time.time != lastTime)
        {
            lastTime = Time.time;
            if (proxy == null) return;
            if (propName == "") return;
            var storage = proxy.proxy.store.stored;
            storage.InitStr(propName, "");
            text.text = storage[propName].svalue;
        }
    }
}
