using System;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{

    public static class Map
    {
        public static RangeMap FilterCopies(IUnitInfo unit, bool invertCondition, params TargetFilter[] filters)
        {
            return FilterCopies(new RangeMap(true), unit, invertCondition, filters);
        }
        public static RangeMap FilterCopies(RangeMapReadOnly map, IUnitInfo unit, bool invertCondition, params TargetFilter[] filters)
        {
            if (filters == null || filters.Length == 0)
            {
                return new RangeMap(map);
            }
            Func<IUnitInfo, Vector2Int, bool>[] c = new Func<IUnitInfo, Vector2Int, bool>[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                Func<IUnitInfo, Vector2Int, bool> p = filters[i].GetPredicate();
                c[i] = (obj, slot) => !invertCondition == p((IUnitInfo)obj, slot);
            }
            return FilterCopies<IUnitInfo>(map, unit, c);
        }
        public static RangeMap FilterCopies(params Predicate<Vector2Int>[] conditions)
        {
            return FilterCopies<UnityEngine.Object>(new RangeMapReadOnly(true), null, null, conditions);
        }
        public static RangeMap FilterCopies(params Func<UnityEngine.Object, Vector2Int, bool>[] conditions)
        {
            return FilterCopies<UnityEngine.Object>(new RangeMapReadOnly(true), null, conditions, null);
        }
        public static RangeMap FilterCopies<T>(T obj, params Func<T, Vector2Int, bool>[] conditions)
        {
            return FilterCopies<T>(new RangeMapReadOnly(true), obj, conditions, null);
        }
        public static RangeMap FilterCopies(RangeMapReadOnly map, params Predicate<Vector2Int>[] conditions)
        {
            return FilterCopies<UnityEngine.Object>(map, null, null, conditions);
        }
        public static RangeMap FilterCopies(RangeMapReadOnly map, params Func<UnityEngine.Object, Vector2Int, bool>[] conditions)
        {
            return FilterCopies(map, null, conditions, null);
        }
        /// <summary>
        /// Remove slots that don't satisfy all the conditions. Remove slots for which at least one of the conditions returns true
        /// </summary>
        /// <param name="obj">Input for condition</param>
        /// <param name="map">Map to filter</param>
        /// <param name="conditions">Conditions that are checked for each slot</param>
        /// <returns></returns>
        public static RangeMap FilterCopies<T>(RangeMapReadOnly map, T obj, params Func<T, Vector2Int, bool>[] conditions)
        {
            return FilterCopies<T>(map, obj, conditions, null);
        }
        public static RangeMap FilterCopies<T>(RangeMapReadOnly map, T obj, Func<T, Vector2Int, bool> func, Predicate<Vector2Int> pred)
        {
            return FilterCopies(map, obj, new Func<T, Vector2Int, bool>[1] { func }, new Predicate<Vector2Int>[1] { pred });
        }
        public static RangeMap FilterCopies<T>(RangeMapReadOnly map, T obj, Func<T, Vector2Int, bool>[] funcs, Predicate<Vector2Int>[] preds)
        {
            if ((funcs == null || funcs.Length == 0) && (preds == null || preds.Length == 0))
            {
                return new RangeMap(map);
            }
            RangeMap result = new RangeMap(false);
            foreach (var slot in map)
            {
                bool keep = true;
                if (funcs != null)
                {
                    foreach (var condition in funcs)
                    {
                        if (condition(obj, slot))
                        {
                            keep = false;
                            break;
                        }
                    }
                }
                if (preds != null)
                {
                    foreach (var condition in preds)
                    {
                        if (condition(slot))
                        {
                            keep = false;
                            break;
                        }
                    }
                }
                if (keep)
                {
                    result.Add(slot);
                }
            }

            return result;
        }


        public static void Filter(RangeMap map, IUnitInfo unit, bool invertCondition, params TargetFilter[] filters)
        {
            if (filters == null || filters.Length == 0)
            {
                return;
            }
            Func<IUnitInfo, Vector2Int, bool>[] c = new Func<IUnitInfo, Vector2Int, bool>[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                Func<IUnitInfo, Vector2Int, bool> p = filters[i].GetPredicate();
                c[i] = (obj, slot) => !invertCondition == p((IUnitInfo)obj, slot);
            }
            Filter<IUnitInfo>(map, unit, c);
        }
        public static void Filter(RangeMap map, params Predicate<Vector2Int>[] conditions)
        {
            Filter<UnityEngine.Object>(map, null, null, conditions);
        }
        public static void Filter(RangeMap map, params Func<UnityEngine.Object, Vector2Int, bool>[] conditions)
        {
            Filter(map, null, conditions, null);
        }
        public static void Filter<T>(RangeMap map, T obj, params Func<T, Vector2Int, bool>[] conditions)
        {
            Filter<T>(map, obj, conditions, null);
        }
        public static void Filter<T>(RangeMap map, T obj, Func<T, Vector2Int, bool> func, Predicate<Vector2Int> pred)
        {
            Filter(map, obj, new Func<T, Vector2Int, bool>[1] { func }, new Predicate<Vector2Int>[1] { pred });
        }
        public static void Filter<T>(RangeMap map, T obj, Func<T, Vector2Int, bool>[] funcs, Predicate<Vector2Int>[] preds)
        {
            if ((funcs == null || funcs.Length == 0) && (preds == null || preds.Length == 0))
            {
                return;
            }
            List<Vector2Int> toRemove = new List<Vector2Int>();
            foreach (var slot in map)
            {
                if (funcs != null)
                {
                    foreach (var condition in funcs)
                    {
                        if (condition(obj, slot))
                        {
                            toRemove.Add(slot);
                            break;
                        }
                    }
                }
                if (preds != null)
                {
                    foreach (var condition in preds)
                    {
                        if (condition(slot))
                        {
                            toRemove.Add(slot);
                            break;
                        }
                    }
                }
            }
            foreach (var slot in toRemove)
            {
                map.Remove(slot);
            }
        }

        internal static List<Vector2> GetLine(Vector2 position, Vector2 target, float detail = 1f)
        {
            List<Vector2> list = new List<Vector2>();
            var dir = target - position;
            var distance = Mathf.Ceil(dir.magnitude) * detail;
            for (int i = 0; i <= distance; i++)
            {
                Vector2 xy = (i / distance) * dir;
                list.Add(xy + position);
            }
            return list;
        }
    }
}