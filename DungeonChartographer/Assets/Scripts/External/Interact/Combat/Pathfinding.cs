using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
    public static class Pathfinding
    {
        private const uint PATHFINDING_MAX_DEPTH = 1000; // for debug purposes to avoid infinite loop
        static RangeMapReadOnly pathMap = new RangeMapReadOnly(false);

        public static RangeMap Flood(Vector2Int start, int distance, bool ignoreStartObstacles)
        {
            //if (!ignoreStartObstacles && !BattleManager.GetSlotInfo(start).IsWalkable) { return RangeMap.Empty; }

            RangeMap floodMap = new RangeMap();
            floodMap.Add(start);
            HashSet<Vector2Int> lastAdded = new HashSet<Vector2Int>(distance * 4);
            lastAdded.Add(start);
            HashSet<Vector2Int> newLastAdded = new HashSet<Vector2Int>();

            Flood(distance, floodMap, lastAdded, newLastAdded);
            return floodMap;
        }
        private static void Flood(int distance, RangeMap map, HashSet<Vector2Int> lastAdded1, HashSet<Vector2Int> lastAdded2)
        {
            if (distance <= 0) { return; }
            // input hashmaps exchange roles of last added and new last added
            HashSet<Vector2Int> lastAdded = (lastAdded1.Count == 0) ? lastAdded2 : lastAdded1;    // slots added in previous iteration
            HashSet<Vector2Int> newLastAdded = (lastAdded1.Count == 0) ? lastAdded1 : lastAdded2; // empty at this point

            throw new Exception("invlid impl, add get slot info to map");
            // flood all border tiles (saved in lastAdded)
            /* foreach (var slot in lastAdded)
            {
                Vector2Int temp = slot + new Vector2Int(1, 0);
                if (map.GetSlotInfo(temp).IsWalkable && map.Add(temp))
                {
                    newLastAdded.Add(temp);
                }
                temp = slot + new Vector2Int(-1, 0);
                if (map.GetSlotInfo(temp).IsWalkable && map.Add(temp))
                {
                    newLastAdded.Add(temp);
                }
                temp = slot + new Vector2Int(0, 1);
                if (map.GetSlotInfo(temp).IsWalkable && map.Add(temp))
                {
                    newLastAdded.Add(temp);
                }
                temp = slot + new Vector2Int(0, -1);
                if (map.GetSlotInfo(temp).IsWalkable && map.Add(temp))
                {
                    newLastAdded.Add(temp);
                }
            }*/

            lastAdded.Clear();
            Flood(distance - 1, map, lastAdded1, lastAdded2);
        }

        /// <summary>
        /// Finds nearest free path to slot next to target.
        /// </summary>
        /// <param name="startG"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Path FindPath(IUnitInfo unit, IUnitInfo target)
        {
            return FindPath(unit.Slot, target);
        }
        public static Path FindPath(Vector2Int startG, IUnitInfo target)
        {
            if (target == null) { return Path.Invalid; }
            return FindPath(startG, target.Slot);
        }
        public static Path FindPath(Vector2Int startG, Vector2Int endG)
        {
            return FindPath(startG, endG, pathMap, PATHFINDING_MAX_DEPTH);
        }
        /// <summary>
        /// Finds nearest free path to target. If target is unit, find slot next to it.
        /// </summary>
        /// <param name="startG"></param>
        /// <param name="endG"></param>
        /// <returns></returns>
        public static Path FindPath(Vector2Int startG, Vector2Int endG, RangeMapReadOnly map, uint maxMoveRange)
        {
            Vector2Int[] targets;
            // search for paths to 4 slots around endG
            /*if (BattleManager.GetSlotInfo(endG).Unit != null)
            {
                targets = new Vector2Int[4]
                {
                    endG + Vector2Int.right,
                    endG + Vector2Int.up,
                    endG + Vector2Int.left,
                    endG + Vector2Int.down
                };
            }
            else*/
            {
                targets = new Vector2Int[1] { endG };
            }

            map = ConditionedMap(map, Path.Levels.UnitsObstacles);
            Path[] paths = new Path[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                Path path = Path.Invalid;
                /*Path path = FindPath(startG, targets[i], Path.Levels.NoConditions);
                Path pathObstacles = FindPath(startG, targets[i], Path.Levels.Obstacles);*/
                Path pathObstaclesUnits = FindPath(map, startG, targets[i], Path.Levels.UnitsObstacles, maxMoveRange);
                if (pathObstaclesUnits.IsValidPath) { 
                    path = pathObstaclesUnits;
                }
                paths[i] = path;
            }
            Path shortestPath = ShortestPath(paths);
            return shortestPath;
        }

        private static RangeMap ConditionedMap(RangeMapReadOnly map, Path.Levels unitsObstacles)
        {
            List<Predicate<Vector2Int>> conditions = ApplyConditions(unitsObstacles);
            return Map.FilterCopies(map, conditions.ToArray());
        }
        private static Path ShortestPath(Path[] paths)
        {
            // return shortest path
            Path shortestPath = Path.Invalid;
            for (int i = 0; i < paths.Length; i++)
            {
                if (!paths[i].IsValidPath)
                    continue;
                if (!shortestPath.IsValidPath || paths[i].StepCount < shortestPath.StepCount)
                {
                    shortestPath = paths[i];
                }
            }
            return shortestPath;
        }

        /// <summary>
        /// Variation with less map allocations
        /// </summary>
        /// <param name="searchMap"></param>
        /// <param name="startG"></param>
        /// <param name="endG"></param>
        /// <param name="createdPathType">Must be same as conditioning on map</param>
        /// <returns></returns>
        private static Path FindPath(RangeMapReadOnly map, Vector2Int startG, Vector2Int endG, Path.Levels createdPathType)
        {
            return FindPath(map, startG, endG, createdPathType, PATHFINDING_MAX_DEPTH);
        }
        private static Path FindPath(RangeMapReadOnly map, Vector2Int startG, Vector2Int endG, Path.Levels createdPathType, uint maxDepth)
        {
            if (startG == endG)
            {
                return Path.Empty;
            }
            if (maxDepth > PATHFINDING_MAX_DEPTH)
                maxDepth = PATHFINDING_MAX_DEPTH;

            // A* algorithm
            // slot, parent slot, gCost, hCost: fCost = gCost + hCost
            List<Tuple<Vector2Int, Vector2Int, uint, uint>> open = new List<Tuple<Vector2Int, Vector2Int, uint, uint>>()
                { new Tuple<Vector2Int, Vector2Int, uint, uint>(startG, startG, 0, SlotDistanceCost(startG, endG)) };
            List<Tuple<Vector2Int, Vector2Int, uint, uint>> closed = new List<Tuple<Vector2Int, Vector2Int, uint, uint>>();
            while (open.Count > 0)
            {
                // find outline slot (open list) with minimum fCost
                int minSlot = 0;
                for (int i = 1; i < open.Count; i++)
                {
                    if (SlotDistanceCost(startG, open[i].Item1) + SlotDistanceCost(open[i].Item1, endG) < open[minSlot].Item3 + open[minSlot].Item4)
                    {
                        minSlot = i;
                    }
                }

                // mark minimum slot as traveled
                closed.Add(open[minSlot]);
                open.RemoveAt(minSlot);

                // end the algorithm, path has been found, trace back from end to start end create path
                if (closed[closed.Count - 1].Item1 == endG)
                {
                    List<Vector2Int> path = new List<Vector2Int>();

                    int i = closed.Count - 1;
                    do
                    {
                        path.Insert(0, closed[i].Item1);
                        for (int j = 0; j < closed.Count; j++)
                        {
                            if (closed[i].Item2 == closed[j].Item1)
                            {
                                i = j;
                            }
                        }
                    } while (closed[i].Item1 != closed[i].Item2);

                    return new Path(path, createdPathType);
                }

                //calculate costs of slots around the minimum slot and add them to outline slots (open list) for potential processing
                foreach (var dir in new Vector2Int[4] { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down })
                {
                    Vector2Int slot = closed[closed.Count - 1].Item1 + dir;
                    // skip slots that have already been processed
                    bool inClosed = false;
                    for (int i = 0; i < closed.Count; i++)
                    {
                        if (closed[i].Item1 == slot)
                        {
                            inClosed = true;
                            break;
                        }
                    }
                    if (inClosed) { continue; }
                    // filter slots that aren't usable by the algorithm -- disabled, to enable empty infinite maps
                    /*if (!map.IsFilled(slot))
                    {
                        Debug.Log($"skip {slot}");
                        continue;
                    }*/

                    int openI = -1;
                    for (int i = 0; i < open.Count; i++)
                    {
                        if (open[i].Item1 == slot)
                        {
                            openI = i;
                            break;
                        }
                    }

                    uint slotGCost = closed[closed.Count - 1].Item3 + 1;
                    uint slotHCost = SlotDistanceCost(slot, endG);
                    if (slotGCost + slotHCost > maxDepth)
                    {
                        if (slotGCost + slotHCost > PATHFINDING_MAX_DEPTH)
                        {
                            Debug.LogError($"Pathfinding: Over max depth error{slotGCost + slotHCost}>{PATHFINDING_MAX_DEPTH}. Increase Max path length to avoid error");
                        }
                        continue;
                    }
                    if (openI == -1)
                    {
                        open.Add(new System.Tuple<Vector2Int, Vector2Int, uint, uint>(slot, closed[closed.Count - 1].Item1, slotGCost, slotHCost));
                    }
                    // new fCost is smaller than currently stored fCost, so update it to smaller sum
                    else if (slotGCost + slotHCost < open[openI].Item3 + open[openI].Item4)
                    {
                        open[openI] = new System.Tuple<Vector2Int, Vector2Int, uint, uint>(open[openI].Item1, open[openI].Item2, slotGCost, slotHCost);
                    }
                }
            }

            return Path.Invalid;
        }

        private static List<Predicate<Vector2Int>> ApplyConditions(Path.Levels holes)
        {
            List<System.Predicate<Vector2Int>> conditions = new List<System.Predicate<Vector2Int>>();
            switch (holes)
            {
                case Path.Levels.Obstacles:
                    //conditions.Add(Filters.ObstacleFilter);
                    break;
                case Path.Levels.UnitsObstacles:
                case Path.Levels.AllConditions:
                    //conditions.Add(Filters.ObstacleFilter);
                    //conditions.Add(Filters.UnitFilter);
                    break;
                case Path.Levels.NoConditions:
                default:
                    break;
            }

            return conditions;
        }

        private static uint SlotDistanceCost(Vector2Int a, Vector2Int b)
        {
            return (uint)(Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y));
        }
    }

    public struct Path : IEnumerable<Vector2Int>
    {
        public enum Levels : uint
        {
            FailedToFind = 0,
            NoConditions = 1,
            Obstacles = 2 | NoConditions,
            UnitsObstacles = 4 | Obstacles,
            AllConditions = 0xFFFFFFFF,
        }

        private List<Vector2Int> path;
        private Levels level;

        private Path(bool valid = false)
        {
            path = null;
            level = Levels.FailedToFind;
        }
        public Path(IEnumerable<Vector2Int> path, Levels level)
        {
            this.path = new List<Vector2Int>(path);
            this.level = level;
        }

        public void RemoveFirst()
        {
            if (path.Count == 0) { return; }
            path.RemoveAt(0);
        }
        public void RemoveLast()
        {
            if (path == null || path.Count == 0) { return; }
            path.RemoveAt(path.Count - 1);
        }
        public void Invalidate()
        {
            path = new List<Vector2Int>();
        }

        public void CutToRange(int range)
        {
            path = path.GetRange(0, (path.Count < range ? path.Count : (int)range));
        }

        public IEnumerator<Vector2Int> GetEnumerator()
        {
            return path.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            if (path == null)
            {
                return "(invalid path)";
            }
            return string.Join("->", path);
        }

        public static Path Empty { get { return new Path(new Vector2Int[0], Levels.AllConditions); } }
        public static Path Invalid { get { return new Path(); } }

        public Levels FoundLevel { get { return level; } }
        public bool IsValidPath { get { return path != null; } }
        public bool IsTrivial { get { return path == null || path.Count == 0; } }
        public int StepCount { get { return path != null ? path.Count : 0; } }
        public Vector2Int First { get { return path[0]; } }
        public Vector2Int Last { get { return path[path.Count - 1]; } }
    }
}