using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Interact
{
    public class FadeAction : MonoBehaviour, ITFuncStr
    {
        public SpriteRenderer sprite;
        public Image image;
        public FadeCurves fade;
        public TMPro.TextMeshPro text;
        public bool useStartOnInit = false;
        public Color start;
        public bool useCurrentAsEndInstead = false;
        public Color end;
        public MonoBehaviour Obj { get => this; }

        void Start()
        {
            if (useCurrentAsEndInstead)
                end = GetColor(end);
            if (useStartOnInit)
                SetColor(start);
        }

        public IEnumerator FadeIn()
        {
            yield return FadeTo(start, end, fade.fadeIn);
        }

        public IEnumerator FadeOut()
        {
            yield return FadeTo(end, start, fade.fadeOut);
        }

        IEnumerator FadeTo(Color start, Color end, AnimationCurve fadeCurve) 
        {
            float t = 0;
            float eval = 0;
            SetColor(start * (1 - eval) + end * eval);
            while (t < 1)
            {
                eval = Mathf.Clamp01(fadeCurve.Evaluate(t));
                SetColor(start * (1 - eval) + end * eval);
                yield return null;
                t += Time.deltaTime;
            }
            eval = 1;
            SetColor(start * (1 - eval) + end * eval);
        }

        private void SetColor(Color color)
        {
            if (sprite != null)
                sprite.color = color;
            if (image != null)
                image.color = color;
            if (text != null)
                text.color = color;
        }

        private Color GetColor(Color def)
        {
            if (sprite != null)
                return sprite.color;
            if (image != null)
                return image.color;
            if (text != null)
                return text.color;
            return def;
        }

        public void Func(List<string> args, List<object> oargs)
        {
            if (args.Count == 0) return;

            if (args[0] == "FadeIn")
            {
                InteractCoroutine.Run(this, FadeIn());
            }
            else if (args[0] == "FadeOut")
            {
                InteractCoroutine.Run(this, FadeOut());
            }

            if (oargs.Count == 0) return;

            if (args[0] == "SetFade")
            {
                if(oargs[0] is FadeCurves f)
                    fade = f;
            }
        }
    }
}