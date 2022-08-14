using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    [RequireComponent(typeof(DisplacementAction))]
    public class RunDisplace : MonoBehaviour, ITFuncStr, IUnitReliant
    {
        DisplacementAction action;
        public Displacement shape;
        public float delay = 1;
        public float randomSize = 0;
        public Vector3 dir;
        public IUnitReliant target;

        public bool onStart = false;
        public bool destroy = false;

        public MonoBehaviour Obj { get => this; }
        public IUnitInfo Unit { get => target.Unit; }

        private IEnumerator Start()
        {
            Logs.L("RUN DISPLACE");
            if (target == null)
                target = GetComponentInParent<IUnitReliant>();
            action = GetComponent<DisplacementAction>();
            if (onStart)
            {
                yield return new WaitForSeconds(delay);
                yield return StartCoroutine(action.Jump(target.Unit.transform.position, target.Unit.transform.position + dir + (Vector3)Random.insideUnitCircle, target, shape));
                if (destroy)
                    Destroy(target.Unit.gameObject);
            }
        }

        private void OnDrawGizmos()
        {
            if (target == null)
                target = GetComponentInParent<IUnitReliant>();

            Gizmos.DrawRay(target.Unit.transform.position, dir);
            if(randomSize > 0)
                Gizmos.DrawWireSphere(target.Unit.transform.position + dir, randomSize);
            Gizmos.color = Color.yellow;
            if (shape != null)
            {
                Gizmos.DrawRay(target.Unit.transform.position, DisplacementAction.CalcDisplacement(shape, dir, Time.realtimeSinceStartup % 1f));
            }
        }

        public void Func(List<string> args, List<object> oargs)
        {
            if (args[0] == "Push")
            {
                action.Jump(target.Unit.transform.position, target.Unit.transform.position + dir + (Vector3)Random.insideUnitCircle * randomSize, target, shape);
                if (destroy)
                    Destroy(target.Unit.gameObject, shape.Duration);
            }
        }
    }
}
