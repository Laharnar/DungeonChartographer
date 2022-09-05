using UnityEngine;

namespace Combat
{

    /// <summary>
    /// Functions used in filter functions <see cref="Map.Filter"/>...
    /// <para>They return true when processed slot should be removed from starting set</para>
    /// </summary>
    /// <seealso cref="Filters"/>
    public static partial class Filters
    {
        static RangeMap fullMap => FreshWorldMap();

        public static RangeMap FreshWorldMap() {
            return Battle.I.GetMap((slot) => !slot.IsWalkable);
        }

        /// <summary>
        /// Filters slots that are not reachable by path of length
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="maxPathLength">Maximum length of path to slot (inclusive)</param>
        public static bool NoPathFilter(NoPathFilterParam param, Vector2Int targetSlot)
        {
            var dir = targetSlot - param.startSlot;
            if (Mathf.Abs(dir.x) + Mathf.Abs(dir.y) > param.maxPathLength)
                return true;
            Path path = Pathfinding.FindPath(param.startSlot, targetSlot, fullMap, param.maxPathLength);
            return path.FoundLevel != Path.Levels.UnitsObstacles || path.StepCount > param.maxPathLength;
        }
        public class NoPathFilterParam
        {
            public Vector2Int startSlot;
            public uint maxPathLength;

            public NoPathFilterParam(Vector2Int startSlot, uint maxPathLength)
            {
                this.startSlot = startSlot;
                this.maxPathLength = maxPathLength;
            }
        }
        public static bool RaycastFilter(RaycastFilterParam param, Vector2Int targetPos)
        {
            if (param.type == Raycasting.None) { return false; }

            Vector2 origin = param.startPos;
            Vector2 direction = ((Vector2)(targetPos - param.startPos)).normalized;

            float[] magnitudes = new float[2] {
                (direction.x == 0) ? float.PositiveInfinity : 1 / Mathf.Abs(direction.x),
                (direction.y == 0) ? float.PositiveInfinity : 1 / Mathf.Abs(direction.y),
            };
            Vector2 originScale;
            Vector2[] origins;
            if (direction.x == 0)
            {
                originScale = new Vector2(float.NegativeInfinity, 0.5f - Mathf.Sign(direction.y) / 2.0f);
                originScale.y = Mathf.Abs(originScale.y / direction.y);
                origins = new Vector2[2] {
                    origin,
                    origin - direction * originScale.y
                };
            }
            else if (direction.y == 0)
            {
                originScale = new Vector2(0.5f - Mathf.Sign(direction.x) / 2.0f, float.NegativeInfinity);
                originScale.x = Mathf.Abs(originScale.x / direction.x);
                origins = new Vector2[2] {
                    origin - direction * originScale.x,
                    origin
                };
            }
            else
            {
                originScale = new Vector2(0.5f - Mathf.Sign(direction.x) / 2.0f, 0.5f - Mathf.Sign(direction.y) / 2.0f);
                originScale = new Vector2(Mathf.Abs(originScale.x / direction.x), Mathf.Abs(originScale.y / direction.y));
                origins = new Vector2[2] {
                    origin - direction * originScale.x,
                    origin - direction * originScale.y
                };
            }
            int[] order = (magnitudes[0] > magnitudes[1]) ? new int[2] { 1, 0 } : new int[2] { 0, 1 };
            int maxOriginScaleI = (originScale[0] > originScale[1]) ? 0 : 1;

            float[] startOffsets = new float[2] {
                Vector3.Distance(origins[0], origins[maxOriginScaleI]),
                Vector3.Distance(origins[1], origins[maxOriginScaleI])
            };
            float[] lengths = new float[2] {
                magnitudes[0] + 0.001f,
                magnitudes[1] + 0.001f,
            };

            int SAFETY = 100; // 100 is probably enough
            while (SAFETY > 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    int AXIS_SAFETY = 100; // 100 is probably enough

                    int i1 = order[i];
                    int i2 = order[(i + 1) % 2];
                    while (startOffsets[i1] + lengths[i1] <= startOffsets[i2] + lengths[i2])
                    {
                        Vector3 endPoint = origins[i1] + direction * lengths[i1];
                        Vector2Int hitBlock = Vector2Int.FloorToInt(endPoint);

                        if (!param.ignoreStart || hitBlock != param.startPos) // ignore starting slot (raycasting from unit)
                        {
                            /*SlotInfo info = BattleManager.GetSlotInfo(hitBlock);
                            bool forceEnd = false;
                            if (hitBlock == targetPos)
                            {
                                if (info.HasUnit)
                                {
                                    return false;
                                }
                                else
                                {
                                    forceEnd = true;
                                }
                            }

                            switch (param.type)
                            {
                                case FilteredSkillRange.Raycasting.High:
                                    if (info.HasHighObstacle)
                                    {
                                        return true;
                                    }
                                    else if (forceEnd)
                                    {
                                        return false;
                                    }
                                    break;
                                case FilteredSkillRange.Raycasting.Unit:
                                    if (info.HasObstacle)
                                    {
                                        return true;
                                    }
                                    else if (forceEnd)
                                    {
                                        return false;
                                    }
                                    break;
                                case FilteredSkillRange.Raycasting.Low:
                                    if (info.HasObstacle && info.ObstacleType != ObstacleHeight.Unit)
                                    {
                                        return true;
                                    }
                                    else if (forceEnd)
                                    {
                                        return false;
                                    }
                                    break;
                                default:
                                    Debug.LogError("Should never reach here");
                                    break;
                            }*/
                        }

                        lengths[i1] += magnitudes[i1];

                        AXIS_SAFETY--;
                        if (AXIS_SAFETY <= 0)
                        {
                            Debug.LogError(new System.OverflowException($"Too much depth on axis <{((i1 == 0) ? 'x' : 'y')}> in raycasting"));
                            break;
                        }
                    }
                }
                SAFETY--;
            }
            return false;
        }
        public class RaycastFilterParam
        {
            public Vector2Int startPos;
            public Raycasting type;
            public bool ignoreStart;

            public RaycastFilterParam(Vector2Int startSlot, Raycasting type, bool ignoreStart)
            {
                this.startPos = startSlot;
                this.type = type;
                this.ignoreStart = ignoreStart;
            }
        }

        public enum Raycasting
        {
            /// <summary> No raycast </summary>
            None,
            /// <summary> Raycast, hit only high obstacles </summary>
            High,
            /// <summary> Raycast, hit all obstacles, hit units </summary>
            Unit,
            /// <summary> Raycast, hit all obstacles, pass through units </summary>
            Low,
        }
    }
}