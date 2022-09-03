using UnityEngine;

public class SpriteObj: LiveBehaviour, IGridItem
{
    public SpriteRenderer obj;
    public Color color;

    protected override void LiveAwake()
    {
        base.LiveAwake();
        if (obj == null) obj = GetComponent<SpriteRenderer>();
    }

    public void Init(object pixel)
    {
        Color color = (Color)pixel;
        Debug.Log(color);
        if(color != Color.black && color.a == 1) {
            this.color = color;
        }
        obj.color = this.color;
    }
}
