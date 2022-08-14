using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Interact;

namespace Combat
{

    public struct DisplaceEvent
    {
        public Vector2 startPos;
        public Vector2 endPos;
        public IUnitReliant unit;
        public Displacement shape;
        public System.Action onEnd;
        public DisplacementAction displacement;
    }

    public class DisplacementAction:MonoBehaviour, ITFuncStr, IUnitReliant
    {
        public bool destroyAfterUse = true;
        public bool destroyObjAfterUse = false;
        public bool snap = true;
        bool running = false;
        public MonoBehaviour Obj { get => this; }
        public IUnitInfo Unit { get {
                if (unit == null)
                    unit = GetComponentInParent<IUnitInfo>();
                return unit;
            }
        }

        public bool applyToCam = true;
		Camera cam;
        int layer = 0;
        IUnitInfo unit;

        Vector3 GetVector(object vec)
        {
            if (vec is Vector2Int v2i)
                return (Vector2)v2i;
            if (vec is Vector2 v2)
                return v2;
            if (vec is Vector3Int v3i)
                return (Vector3)v3i;
            if (vec is Vector3 v3)
                return v3;
            Debug.LogError($"Not handled {vec} {vec.GetType()}");
            return Vector3.zero;
        }

        public void Func(List<string> args, List<object> oargs)
        {
            try
            {
                if (args[0].StartsWith("Push")) {
                    if (oargs.Count >= 3)
                    {
                        InteractJump(oargs);
                    }
                    else if (oargs.Count == 2)
                    {
                        Vector2 pos = transform.position + GetVector(oargs[0]);
                        Displacement shape = (Displacement)oargs[1];
                        InteractCoroutine.Run(this, Jump(gameObject.transform.position, pos, this, shape));
                    }
                }
                else if (oargs.Count == 2 && args[0] == "Jump")
                {
                    Vector2 pos = (Vector2)oargs[0];
                    Displacement shape = (Displacement)oargs[1];
                    InteractCoroutine.Run(this, Jump(gameObject.transform.position, pos, this, shape));
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError(e);
                for (int i = 0; i < args.Count; i++)
                {
                    Logs.E(i + ":" + args[i]);
                }
                for (int i = 0; i < oargs.Count; i++)
                {
                    Logs.E(i+":"+oargs[i].ToString() +" "+ oargs[i].GetType());
                }
            }
        }

        private void InteractJump(List<object> oargs)
        {
            GameObject obj = ((Component)oargs[1]).gameObject;
            Vector2 pos = obj.transform.position + GetVector(oargs[0]);
            Displacement shape = (Displacement)oargs[2];
            if (oargs.Count == 4)
            {
                cam = ((Camera)oargs[3]);
            }
            InteractCoroutine.Run(this, Jump(obj.transform.position, pos, obj.GetComponentInParent<IUnitReliant>(), shape));
        }

        public Vector2 FindEndPos(Vector2 startPos, Vector2 endPos)
        {
            List<Vector2> positions = Map.GetLine(startPos, endPos, 1);
            int stopAt = -1;
            for (int i = 0; i < positions.Count; i++)
            {
                var slot = positions[i];
                //if (Filters.ObstacleFilter(slot) && !BattleManager.GetUnitAtSlot(slot))
                {
                    stopAt = i - 1;
                    break;
                }
            }
            if (stopAt != -1)
                endPos = positions[stopAt];
            return endPos;
        }

        public IEnumerator Jump(DisplaceEvent e)
        {
            yield return Jump(e.startPos, e.endPos, e.unit, e.shape, e.onEnd);
        }

        /// <param name="pos"></param>
        /// <param name="obj"></param>
        /// <param name="shape"></param>
        /// <param name="onEnd"></param>
        /// <returns></returns>
        public IEnumerator Jump(Vector2 startPos, Vector2 endPos, IUnitReliant iunit, Displacement shape, System.Action onEnd = null)
        {
            if (running)
            {
                Debug.LogWarning($"Already jumping -> aborting jump {iunit}");
                yield break;
            }
            running = true;
            var unit = iunit.Unit;
            var tunit = unit.transform;
            if (layer != 0)
            {
                Debug.LogWarning($"Overlapping displacements. Possible weird behaviour. Use jump safer. layer: {layer}");
            }
            layer++;
            // pre-block at first obstacle in between obj and pos.
            Logs.L($"Jump: {iunit.Unit} {startPos} -> {endPos} slot:{iunit.Unit.Slot}");
            endPos = FindEndPos(startPos, endPos);

            // maybe unit is already there before jumping on it
            IUnitReliant unitAtPosBeforeJump = null;//BattleManager.GetUnitAtSlot(BattleManager.SlotFromWorldPos(endPos));
            if (unitAtPosBeforeJump == iunit.Unit)
                unitAtPosBeforeJump = null;

            // move & rotate
            float startCamSize = 10;
			if (applyToCam && cam != null) {
				startCamSize = cam.orthographicSize;
			}

            tunit.position = startPos;
            float start = Time.time;
            float end = Time.time + shape.Duration;
            Vector2 dir = endPos - startPos;
            Vector3 lastMod = Vector3.zero;
            while (Time.time <= end)
            {
                float t = (Time.time - start) / (end - start);
                DoCalc(iunit, shape, startCamSize, startPos, dir, t);
                yield return null;
            }
            if (applyToCam && cam != null)
			{
				if (shape.UseEndSize)
					cam.orthographicSize = shape.EndCamSize;
				else 
					cam.orthographicSize = (startCamSize + shape.CameraZoomOffset(1) * shape.ZoomScale);
			}

            DoCalc(iunit, shape, startCamSize, startPos, dir, 1);
            if (shape.rot)
                tunit.rotation = new Quaternion();
            if (tunit.localScale != Vector3.one && shape.scale) {
                tunit.localScale = Vector3.one;
            }

            if (shape.pos)
            {
                // snap self
                if (snap)
                {
                    tunit.position = tunit.position;
                }
            }

            layer--;

            if (unitAtPosBeforeJump != null && iunit.Unit != unitAtPosBeforeJump)
            {
                yield return OnLandOnOther(iunit, unitAtPosBeforeJump, dir, shape);
            }

            // maybe another unit jumped at same time to same slot
            IUnitReliant unitAtPosAfterJump = null;// BattleManager.GetUnitAtSlot(endPos);
            
            if (unitAtPosAfterJump != null && iunit.Unit != unitAtPosAfterJump)
            {
                yield return OnLandOnOther(iunit, unitAtPosAfterJump, dir, shape);
            }

            running = false;
            onEnd?.Invoke();
            if (destroyObjAfterUse)
                Destroy(iunit.Unit.GameObject);
            if (destroyAfterUse)
                Destroy(this);
        }

        private void DoCalc(IUnitReliant obj, Displacement shape, float startCamSize, Vector2 start, Vector2 dir, float t)
        {
            var modded = CalcDisplacement(shape, dir, t);
            if (shape.pos)
                obj.Unit.transform.position = start + modded;
            if (shape.rot)
                obj.Unit.transform.rotation = Quaternion.Euler(0, 0, shape.RotationZ(t) * 360);
            if (shape.scale)
                obj.Unit.transform.localScale = shape.ScaleXY(t);

            if (applyToCam && cam != null)
                cam.orthographicSize = startCamSize + shape.CameraZoomOffset(t) * shape.ZoomScale;
        }

        public static Vector2 CalcDisplacement(Displacement shape, Vector2 dir, float t)
        {
            //var angle = Quaternion.FromToRotation(Vector3.up, dir);
            var x =  new Vector2(shape.OffsetX(t), shape.OffsetY(t));
            return new Vector2(dir.x * shape.X(t), dir.y * shape.Y(t) + shape.Height(t)) + x;
        }

        IEnumerator OnLandOnOther(IUnitReliant self, IUnitReliant taken, Vector3 dir, Displacement shape)
        {
            if (self == taken)
                yield break;

            var slot = Vector2Int.FloorToInt(self.Unit.transform.position);
            var offPos = GetClosestFreeSlot(slot, dir);
            var bounce2Pos = offPos;

            switch (shape.PushMode)
            {
                case 0:
                    yield return Jump(taken.Unit.transform.position, bounce2Pos, taken, shape);
                    break;
                case 1:
                    yield return Jump(self.Unit.transform.position, bounce2Pos, self, shape);
                    break;
                default:
                    Debug.Log("Unhandled");
                    break;
            }
        }

        internal static Vector2Int GetClosestFreeSlot(Vector2Int slot, Vector2 searchDir)
        {
            searchDir = searchDir.normalized;
            HashSet<Vector2Int> filled = new HashSet<Vector2Int>();
            Queue<Vector2Int> slots = new Queue<Vector2Int>();
            slots.Enqueue(slot);
            for (int i = 0; i < 1000000 && slots.Count > 0; i++)
            {
                Vector2Int itemInt = slots.Dequeue();
                if (!filled.Contains(itemInt))
                {
                    /*if (FilledWithAnything(itemInt))
                    {
                        filled.Add(itemInt);
                        Vector3 item = (Vector2)itemInt;

                        slots.Enqueue(new Vector2Int(Mathf.FloorToInt(item.x + searchDir.x), Mathf.FloorToInt(item.y)));
                        slots.Enqueue(new Vector2Int(Mathf.FloorToInt(item.x), Mathf.FloorToInt(item.y + searchDir.y)));
                        slots.Enqueue(new Vector2Int(Mathf.FloorToInt(item.x - searchDir.x), Mathf.FloorToInt(item.y)));
                        slots.Enqueue(new Vector2Int(Mathf.FloorToInt(item.x), Mathf.FloorToInt(item.y - searchDir.y)));
                    }
                    else*/
                    {
                        return itemInt;
                    }
                }
            }
            Debug.LogError($"Couldn't a single free slot in range -> {filled.Count}");
            return slot;
        }

    }
}
