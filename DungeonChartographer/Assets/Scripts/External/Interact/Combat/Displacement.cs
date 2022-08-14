using UnityEngine;

namespace Combat
{
    [CreateAssetMenu]
    public class Displacement : ScriptableObject
    {
		[Header("Non projectile distance")]
        [SerializeField] float duration = 1;
		[Header("Relative offset")]
        [SerializeField] AnimationCurve x = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [SerializeField] AnimationCurve y = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [SerializeField] AnimationCurve scaleX = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        [SerializeField] AnimationCurve scaleY = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
        [SerializeField] AnimationCurve extraHeight = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
        [SerializeField] float heightScale = 1;
		[SerializeField] float rotZScale = 1f;
        [SerializeField] AnimationCurve rotZ = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
		[Header("Absolute offset")]
		[SerializeField] float offsetXScale = 1f;
		[SerializeField] float offsetYScale = 1f;
        [SerializeField] AnimationCurve extraX = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
        [SerializeField] AnimationCurve extraY = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
		
		[Header("Camera zoom -> offset from current zoom.(0 : stay same)")]
		[SerializeField] float zoomScale = 1;
		[SerializeField] AnimationCurve cameraZoomOffset = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 0));
		[SerializeField] bool useEndSize = false;
		[SerializeField] float endCamSize = 10;

        public bool pos = true, rot = true, scale = true;
        [SerializeField, Header("0: jump between 2 points, 1: circular point shockwave). 2: circular push up to")]
        int jumpMode = 0;
        [SerializeField]
        float directionScale = 1;

        [SerializeField, Header("on overlap 0: push units away, 1:bounce away from units")] int pushMode;

        public float Duration => duration;
        public float Height(float t) => extraHeight.Evaluate(t) * heightScale;
        public float OffsetX(float t) => extraX.Evaluate(t) * offsetXScale;
        public float OffsetY(float t) => extraY.Evaluate(t) * offsetYScale;
        public float RotationZ(float t) => rotZ.Evaluate(t) * rotZScale;
        public Vector2 XY(float t) => new Vector2(X(t), Y(t));
        public Vector3 ScaleXY(float t) => new Vector3(ScaleX(t), ScaleY(t), 1);
        public float X(float t) => x.Evaluate(t);
        public float Y(float t) => y.Evaluate(t);
        public float ScaleX(float t) => scaleX.Evaluate(t);
        public float ScaleY(float t) => scaleY.Evaluate(t);
        public float ZoomScale => zoomScale;
        public float CameraZoomOffset(float t) => cameraZoomOffset.Evaluate(t);
        public float EndCamSize => endCamSize;
        public bool UseEndSize => useEndSize;
        public int PushMode => pushMode;
        public int JumpMode { get => jumpMode; }
        public float DirectionScale { get => directionScale; }
    }
}
