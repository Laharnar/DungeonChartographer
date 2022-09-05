using System;
using UnityEngine;

public class PngToMap : LiveEditorBehaviour
{
    public Texture2D texture;
    public string optionalTextureResourcesPath;
    public ColorPrefab ground;
    public Transform parentItem;
    public bool update = false;
    TransformPool map;
    Texture2D tex;

    protected override UpdateImportance EditorLiveAwake()
    {
        if(map == null || ground.prefab != map.Prefab)
            map = new TransformPool(ground.prefab, parentItem);
        update = tex == null || map.root.childCount != tex.width * tex.height;
        return update ? UpdateImportance.High : UpdateImportance.None; 
    }

    private void Update()
    {
        if (update)
        {
            tex = texture;
            if (parentItem == null)
                parentItem = transform;
            Reader();
            update = false;
        }
    }

    private void Reader()
    {
        // Load image
        Texture2D image = texture;
        if (optionalTextureResourcesPath != "")
            image = (Texture2D)Resources.Load(optionalTextureResourcesPath);

        BlocksOutOfPixels(image, map);
    }

    private void BlocksOutOfPixels(Texture2D image, TransformPool map)
    {
        map.Depool();
        // Iterate through it's pixels
        int counter = 0;
        for (int i = 0; i < image.width; i++)
        {
            for (int j = 0; j < image.height; j++)
            {
                Color pixel = image.GetPixel(i, j);
                // if it's a white color then just debug...
                if (pixel == Color.clear || pixel.a == 0)
                {
                    //Debug.Log("Im white");
                }
                else
                {
                    var obj = map.Instantiate(transform.position + new Vector3(i, j), new Quaternion(), map.root);
                    if (pixel != Color.black && pixel.a == 1)
                    {
                        obj.GetComponent<DungeonMapUncover>().combatArgs.loadCombat = "" + counter;
                        counter++;
                    }
                    obj.GetComponent<IGridItem>()?.Init(pixel);
                }
            }
        }
    }
}

[Serializable]
public class TransformPool
{
    public Transform root { get; private set; }
    public Transform Prefab { get => prefab; }

    Transform prefab;
    public int SpawnCount;
    Transform parent;

    public TransformPool(Transform prefab, Transform parent)
    {
        this.prefab = prefab;
        this.parent = parent;
        OneLevelHierarchy();
    }

    Transform OneLevelHierarchy()
    {
        if (parent.childCount > 0)
            GameObject.DestroyImmediate(parent.GetChild(0).gameObject);
        var map = new GameObject("map").transform;
        map.parent = parent;
        root = map;
        SpawnCount = 0;
        return map;
    }

    public void Depool()
    {
        OneLevelHierarchy();
    }

    internal Transform Instantiate(Vector3 vector3, Quaternion quaternion, Transform root)
    {
        SpawnCount = root.childCount + 1;
        return Transform.Instantiate(prefab, vector3, quaternion, root);
    }
}

