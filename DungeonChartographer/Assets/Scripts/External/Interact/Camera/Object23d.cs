using UnityEngine;

[ExecuteInEditMode]
public class Object23d : MonoBehaviour
{
    public enum o23dType
    {
        NonMoving,
        Moving,
    }
	public enum o23dPrecision{
		Exact
	}
    public float additionalZ3dRadius = 0.01f; // How far back should cameras consider drawing extra. For 3d.
    public float additionalXYRadius = 0.1f;
    public Camera23d.Bucket23.cam23Type camType;
    public o23dType type = o23dType.Moving;
	private o23dPrecision refreshPrecision = o23dPrecision.Exact;
	
    [SerializeField] Vector3 offset;

    internal bool changes;
	internal Vector3 DynamicDirChange {get;private set;} // predictive of where movement will happen, so that camera can adapt to it.
    Vector3 lastPos;
    Camera23d.Bucket23.cam23Type lastCamType;
    float lastRadius = 0;
    float lastXYRadius = 0;

    public bool useTemplateFromCamera = true;
    public Vector3 Pos => transform.position + offset;
    public Color radiusColor = Color.grey;

    int frameTick = 0; Vector3 diff;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = radiusColor;
        Gizmos.DrawWireCube(Pos, DynamicDirChange + new Vector3(additionalXYRadius*2, additionalXYRadius*2, additionalZ3dRadius * 2));
    }

    private void Start()
    {
        Camera23d.Register(this);
    }

    // Late update to allow multiple translations to happen without triggering change.
    private void LateUpdate()
    {
        frameTick++; frameTick %= 10000;
        if (!Application.isPlaying && useTemplateFromCamera && frameTick % 20 == 0){
            var template = Camera23d.FindLayerMask(lastCamType);
            if(template!= null){
                gameObject.layer = LayerMask.NameToLayer(template.mask);
            }
        }
		var dif = Pos - lastPos;
        diff += dif;
		if(refreshPrecision == o23dPrecision.Exact)
			DynamicDirChange = dif;
        if(additionalZ3dRadius - lastRadius != 0 
            || additionalXYRadius - lastXYRadius != 0
            || Mathf.Abs(diff.x) + Mathf.Abs(diff.y) + Mathf.Abs(diff.z) > 0.5f
            || lastCamType != camType)
            changes = true;
		lastPos = Pos;
    }

    private void OnEnable()
    {
        Camera23d.Register(this);
    }
	
	public void OnApplied(){
        diff = Vector3.zero;
        lastPos = Pos;
        lastRadius = additionalZ3dRadius;
        lastXYRadius = additionalXYRadius;
        lastCamType = camType;
        changes = false;
	}
}
