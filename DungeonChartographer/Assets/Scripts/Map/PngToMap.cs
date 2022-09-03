using System;
using UnityEngine;

public class PngToMap : LiveEditorBehaviour
{
    public Texture2D texture;
    public string optionalTextureResourcesPath;
    public ColorPrefab ground;
    public Transform parentItem;
    public bool update = true;
    TransformPool map;
    Texture2D tex;

    protected override UpdateImportance EditorLiveAwake()
    {
        map = new TransformPool(ground.prefab, parentItem);
        update = tex != texture;
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
                    obj.GetComponent<IGridItem>()?.Init(pixel);
                }
            }
        }
    }
}

public class TransformPool
{
    public Transform root { get; private set; }
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

