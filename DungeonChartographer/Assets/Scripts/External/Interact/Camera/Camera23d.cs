using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// current flicker solution: manually set camera snap that minimizes flicker by jumping between 2 images that are sort-of same.
// flicker alternative test solution 1 : https://docs.unity3d.com/ScriptReference/Camera.Render.html
// flicker alternative solution 2 :  oversize rendertextures, then move render textures instead of cameras to keep image more stable.
// flicker alternative test solution 2.1 :  presample, then update only what is OUTSIDE camera view. inside view update only if object moves.
// flicker alternative test solution 2.2 : update only rect region object affects, good to combine with movement-only updating
// flicker alternative test solution 2.3 : use stabilizing sampling, that attempts to correct camera pos to give same result.

// This solution is dumb but works really elegantly.
// Split world by buckets, where each bucket has 1 camera that draws it and contains one type of objects, either 3d or 2d, and has 1 raw render image.
// Automatically generate buckets based on grouping together as many objects as possible.

public class Camera23d : MonoBehaviour
{
    
    static Camera23d singleton;
    List<Object23d> searchable = new List<Object23d>();

    public Camera cameraPref2d;
    public Camera cameraPref3d;

    public RenderTexture singleOutTexture;
    public bool firstLayerIs2d = true;
    public RenderTexture[] layersOutTexture;
    [Header("manual, not necessary usually")]
	public int backgroundLayer = -1;
    public bool reload = false;
	public float dynamicMoveScaling = 5f;
    [Header("gizmos")]
    [SerializeField] Color bucketCol1 = Color.green;
    [SerializeField] Color bucketCol2 = Color.black;
    [SerializeField] Color cameraCol1 = Color.yellow;
    [SerializeField] Color cameraCol2 = Color.gray;
    [SerializeField] Color overallColor = Color.black;
    
    [Header("logs")]
	public List<string> refreshFrom = new List<string>();
    List<Camera> cameras = new List<Camera>();
    Vector3 min;
    Vector3 max;
    Bucket23[] buckets;
    bool bucketsOnly = false;
    [SerializeField] List<BucketTemplate> templates;

    public static Vector3 Scale2d { get {
            if (singleton == null) return Vector3.one;
            int id = singleton.firstLayerIs2d ? 0 : 1;
            return new Vector3(
                singleton.layersOutTexture[id].width / Screen.width,
                singleton.layersOutTexture[id].height / Screen.height, 1);
        }
    }

    [System.Serializable]
    public class BucketTemplate{
        public Camera23d.Bucket23.cam23Type camType;
        public string mask;
    }

    public static BucketTemplate FindLayerMask(Camera23d.Bucket23.cam23Type lastCamType){
        if (singleton == null) return null;
        for (int i = 0; i < singleton.templates.Count; i++)
        {
            if(singleton.templates[i].camType == lastCamType){
                return singleton.templates[i];
            }
        }
        return null;
    }

    public static void Register(Object23d item)
    {
        if (singleton == null)
            singleton = FindObjectOfType<Camera23d>();

        if (singleton != null && !singleton.searchable.Contains(item))
        {
            singleton.searchable.Add(item);
            singleton.reload = true;
        }
    }

    private void OnEnable()
    {

        if (singleton != null && singleton != this)
            DestroyImmediate(this);
        else
        {
            Debug.Log("loaded cam23d singleton");
            singleton = this;
        }
    }

    private void Awake()
    {
        if (singleton != null && singleton != this)
            DestroyImmediate(this);
        else singleton = this;
    }

    void Start()
    {
        var items = FindObjectsOfType<Object23d>();
        foreach (var item in items)
        {
            if(!searchable.Contains(item))
                searchable.Add(item);
        }
        Camera[] otherCams = FindObjectsOfType<Camera>();
        var selfRoot = transform.root;
        foreach (var item in otherCams)
        {
            if (item.transform.root != selfRoot)
                Debug.Log($"Cam23d flagging other cameras '{item}'");
        }
        bucketsOnly = false;
        Reload();
    }
    

    private void OnDrawGizmos()
    {
        if(!Application.isPlaying){
            bucketsOnly = true;
            Tick();
        }
        Gizmos.color = Color.black;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawWireCube(transform.position + transform.forward, Vector3.one * 0.5f);

        if (searchable.Count == 0 || buckets == null) return;

        Gizmos.color = overallColor;
        Gizmos.DrawWireCube(min + (max - min) / 2f, (max - min)*1.05f);
        for (int i = 0; i < buckets.Length; i++)
        {
            var min = buckets[i].extendedMin;
            var max = buckets[i].extendedMax;
            var size = max - min;
			if(buckets[i].bucketType == Bucket23.cam23Type.dimensions2)
				Gizmos.color = bucketCol1;
			else
				Gizmos.color = bucketCol2;
			
            Gizmos.DrawWireCube(min + size / 2f, size);
        }
        for (int i = 0; i < cameras.Count; i++)
        {
            if (cameras[i] == null)
                continue;
            if(i%2 == 0)
                Gizmos.color = cameraCol1;
            else Gizmos.color = cameraCol2;

            var ortoSize = new Vector3(cameras[i].orthographicSize, cameras[i].orthographicSize, 0);
            var clip = new Vector3(0, 0, cameras[i].farClipPlane - cameras[i].nearClipPlane);
            var start = new Vector3(0, 0, cameras[i].nearClipPlane);
            var center = clip/2f + cameras[i].transform.position + start; 
            Gizmos.DrawWireCube(center, ortoSize * (i % 2  == 0 ? 1f : 0.8f) + clip);
        }
    }

    void Tick(){
        if (refreshFrom.Count > 5)
            refreshFrom.Clear();
        // control all triggers for reload from 1 space.
        for (int i = 0; i < searchable.Count; i++)
        {
            if(searchable[i] == null)
            {
                searchable.RemoveAt(i);
                i--;
                continue;
            }
            if(searchable[i].type == Object23d.o23dType.Moving)
            {
                if(searchable[i].changes)
                {
                    refreshFrom.Add($"Refreshed due to {searchable[i]}");
                    reload = true;
                    searchable[i].OnApplied();
                }
            }
        }
        
        if (reload)
        {
            reload = false;
            Reload();
        }

    }

    void Update()
    {
        bucketsOnly = false;
		Tick();
    }

    private void Reload()
    {
        if (searchable.Count == 0) return;

        for (int i = searchable.Count - 1; i >= 0; i--)
        {
            if (!searchable[i].gameObject.activeSelf)
                searchable.RemoveAt(i);
        }

        // create buckets separated by different types
        for (int i = searchable.Count - 1; i >= 0 ; i--)
        {
            if (searchable[i] == null)
                searchable.RemoveAt(i);
        }

        Bucket23 temp = new Bucket23();
        for (int i = 0; i < searchable.Count; i++)
            temp.Include(searchable[i]);

        min = temp.extendedMin;
        max = temp.extendedMax;

        List<Bucket23> sortedBuckets = new List<Bucket23>();
        sortedBuckets.Add(temp);
        SplitBuckets(sortedBuckets);
        buckets = sortedBuckets.ToArray();

        if(!bucketsOnly)
        {
            // create cameras
            for (int i = 0; i < cameras.Count; i++)
                Destroy(cameras[i].gameObject);
            cameras.Clear();
            for (int i = sortedBuckets.Count - 1; i >= 0; i--)
            {
				if(sortedBuckets[i].bucketType != Bucket23.cam23Type.Background){
					Camera cam = CreateCamera(sortedBuckets, i);
					InitCam(cam, sortedBuckets, i, true);
					cameras.Add(cam);
					cam.clearFlags = CameraClearFlags.SolidColor;
					var bg1 = cam.backgroundColor;
					cam.backgroundColor = new Color(bg1.r, bg1.g, bg1.b, 0);
				}
            }

            // first camera(last drawn, first raw) clears canvas
            var lastCam = cameras[0];
            lastCam.clearFlags = CameraClearFlags.SolidColor;
            var bg = lastCam.backgroundColor;
            lastCam.backgroundColor = new Color(bg.r, bg.g, bg.b, 1);
        }
    }

    private static void SplitBuckets(List<Bucket23> sortedBuckets)
    {
        // buckets with multiple types are split into new buckets and readded in sorted order
        for (int i = sortedBuckets.Count - 1; i >= 0; i--)
        {
            var extracted = sortedBuckets[i].RemoveOtherTypes();
            for (int j = 0; j < extracted.Count; j++){
				sortedBuckets.Insert(i, extracted[j]);
			}
        }
		// pull background out to be at end
		// 0 : back
		for (int i = sortedBuckets.Count - 1; i >= 0; i--)
        {
			if(sortedBuckets[i].bucketType == Bucket23.cam23Type.Background){
				var temp = sortedBuckets[i];
				sortedBuckets.RemoveAt(i);
				sortedBuckets.Insert(0, temp);
			}
        }
		// compress duplicates
		for (int i = sortedBuckets.Count - 1; i >= 1; i--)
        {
			if(sortedBuckets[i].bucketType == sortedBuckets[i-1].bucketType){
				sortedBuckets[i-1].Add(sortedBuckets[i].objects);
				sortedBuckets.RemoveAt(i);
			}
        }
    }

    private Camera CreateCamera(List<Bucket23> sortedBuckets, int i)
    {
        Camera spawn = cameraPref2d;
        if (sortedBuckets[i].bucketType == Bucket23.cam23Type.dimensions3)
            spawn = cameraPref3d;

        var cam = Instantiate(spawn, transform, false);
        return cam;
    }

    void InitCam(Camera cam, List<Bucket23> sortedBuckets, int i, bool create = true)
    {
        if (create)
        {
            cam.transform.SetAsFirstSibling();
            cam.orthographic = true;
            cam.orthographicSize = cameraPref3d.orthographicSize;
            cam.clearFlags = CameraClearFlags.Nothing;
        }
        cam.nearClipPlane = sortedBuckets[i].extendedMin.z - cam.transform.position.z;
        cam.farClipPlane = sortedBuckets[i].extendedMax.z - cam.transform.position.z;
        cam.depth = -cam.nearClipPlane;
        if (singleOutTexture != null)
        {
            cam.forceIntoRenderTexture = true;
            cam.targetTexture = singleOutTexture;
        }
        else if (layersOutTexture.Length > 0)
        {
            bool firstBuckedIs2d = sortedBuckets.Count > 0 &&
                sortedBuckets[0].bucketType == Bucket23.cam23Type.dimensions2;
            int texId = i;
            if ((firstLayerIs2d && !firstBuckedIs2d) || (!firstLayerIs2d && firstBuckedIs2d))
            {
                texId++;
            }
            if (texId < layersOutTexture.Length)
            {
                if (layersOutTexture[texId] == null)
                {
                    Debug.LogError("Layer texture is null", this);
                }
                else
                {
                    cam.forceIntoRenderTexture = true;
                    cam.targetTexture = layersOutTexture[texId];
                }
            }
            else
            {
                Debug.LogError("Not enough render textures to support that many 23d layers.", this);
            }
        }
    }

    [System.Serializable]
    public class Bucket23
    {
        public enum cam23Type
        {
            dimensions2, dimensions3, Background
        }

        const cam23Type defaultType = cam23Type.dimensions2;
        public cam23Type bucketType;
        public List<Object23d> objects = new List<Object23d>();
        public Vector3 extendedMin, extendedMax;
        const float MINBOUNDSRADIUS = 0.1f;

		public void Add(IEnumerable<Object23d> obj){
			foreach(var o in obj){
				Include(o);
			}
		}

        public void Include(Object23d obj)
        {
            if (objects.Count == 0)
            {
                bucketType = obj.camType;
                extendedMin = obj.Pos; // necessary override for first
                extendedMax = obj.Pos; // because min and max can't start at 0 to start properly.
            }
            ClearNulls();
            UpdateBounds(obj);
            bool added = false;
            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].Pos.z < obj.Pos.z)
                {
                    objects.Insert(i, obj);
                    added = true;
                    break;
                }
            }
            if (!added)
                objects.Add(obj);
        }

        public void UpdateBounds(Object23d obj)
        {
            float padding = Mathf.Max(obj.additionalXYRadius, MINBOUNDSRADIUS);
            var xyz = Vector3.forward * (obj.additionalZ3dRadius + 
				Mathf.Abs(obj.DynamicDirChange.z) * Camera23d.singleton.dynamicMoveScaling + 
				MINBOUNDSRADIUS);
			xyz += new Vector3(padding, padding, 0);
            extendedMin = Vector3.Min(extendedMin, obj.Pos - xyz);
            extendedMax = Vector3.Max(extendedMax, obj.Pos + xyz);
        }

        internal List<Bucket23> RemoveOtherTypes()
        {
            ClearNulls();
            List<Bucket23> extracted = new List<Bucket23>();
			// modify type
            bucketType = objects.Count > 0 ? objects[0].camType : defaultType;
			
			// extract
            Bucket23 current = this;
            int firstCount = 0;
            for (int i = 0; i < objects.Count; i++)
            {
                if (current.bucketType != objects[i].camType)
                {
                    if(firstCount == 0)
                        firstCount = i;
                    current = new Bucket23();
                    extracted.Add(current);
                }
                if (firstCount != 0)
                    current.Include(objects[i]);
            }

            // recalc min max by reincluding
            List<Object23d> items = new List<Object23d>(objects.GetRange(0, firstCount));
            objects.Clear();
            foreach (var c in items)
                Include(c);

            return extracted;
        }

        void ClearNulls(){
            for (int i = objects.Count - 1; i >= 0 ; i--)
            {
                 if (objects[i] == null)
                    objects.RemoveAt(i);
            }
        }
    }
}
