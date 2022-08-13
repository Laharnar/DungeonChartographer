using UnityEngine;

namespace Interact
{
    [CreateAssetMenu]
    public class FadeCurves:ScriptableObject
    {
        public AnimationCurve fadeIn;
        public AnimationCurve fadeOut;
    }

}