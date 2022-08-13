using UnityEngine;

namespace Interact
{
    [CreateAssetMenu]
    public class Fade:ScriptableObject
    {
        public AnimationCurve fadeIn;
        public AnimationCurve fadeOut;
    }

}