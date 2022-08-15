using Combat;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class RangeMapReadOnly : IEnumerable<Vector2Int>
    {
        protected HashSet<Vector2Int> map;
        protected bool fill = true; // added slots are filled/emptied
        protected Vector2Int center; // origin

        public RangeMapReadOnly(RangeMapReadOnly other)
        {
            this.map = new HashSet<Vector2Int>(other.map);
            this.fill = other.fill;
            this.center = other.center;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="isFilled">false = whole world is empty, true: whole world is obstacle</param>
        public RangeMapReadOnly(bool isFilled)
        {
            map = new HashSet<Vector2Int>();
            if (isFilled)
            {
                Invert();
            }
        }
        public RangeMapReadOnly(IEnumerable<Vector2Int> slots)
        {
            map = new HashSet<Vector2Int>(slots);
        }
        public RangeMapReadOnly(IEnumerable<IUnitInfo> units)
        {
            map = new HashSet<Vector2Int>();
            foreach (var item in units)
            {
                map.Add(item.Pos);
            }
        }
        public RangeMapReadOnly(int range) : this(range, (slot) => true) { }
        public RangeMapReadOnly(int range, System.Predicate<Vector2Int> condition)
        {
            map = new HashSet<Vector2Int>();
            for (int y = -range; y <= range; y++)
            {
                for (int x = -range; x <= range; x++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= range && condition(new Vector2Int(x, y)))
                    {
                        var slot = new Vector2Int(x, y);
                        map.Add(slot);
                    }
                }
            }
        }

        public bool IsFilled(int x, int y)
        {
            return IsFilled(new Vector2Int(x, y));
        }
        public bool IsFilled(Vector2Int slot)
        {
            return Filled == map.Contains(slot - center);
        }

        public void Invert()
        {
            fill = !fill;
        }

        public IEnumerator<Vector2Int> GetEnumerator()
        {
            foreach (var slot in map)
            {
                yield return slot + center;
            }
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public override string ToString()
        {
            if (map.Count == 0)
            {
                return "empty! (0, 0) -> (0, 0) ||";
            }

            Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
            foreach (var slot in map)
            {
                min.x = Mathf.Min(min.x, slot.x);
                min.y = Mathf.Min(min.y, slot.y);
                max.x = Mathf.Max(max.x, slot.x);
                max.y = Mathf.Max(max.y, slot.y);
            }

            string result = $"extents {min + center} -> {max + center} | origin {center}\n";
            for (int y = max.y; y >= min.y; y--)
            {
                result += "|";
                for (int x = min.x; x <= max.x; x++)
                {
                    result += $" {(IsFilled(x + center.x, y + center.y) ? '=' : 'X')}";
                }
                result += " |\n";
            }
            return result;
        }

        public static RangeMapReadOnly Empty { get { return new RangeMapReadOnly(new Vector2Int[0]); } }
        public static RangeMapReadOnly Single { get { return new RangeMapReadOnly(0); } }

        public bool IsFull { get { return !Filled && map.Count == 0; } }
        public bool Filled { get { return fill; } }
        public BoundsInt Bounds
        {
            get
            {
                Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
                Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);
                foreach (var slot in map)
                {
                    min = Vector2Int.Min(min, slot);
                    max = Vector2Int.Max(max, slot);
                }
                return new BoundsInt((Vector3Int)(min + center), (Vector3Int)(max - min + center));
            }
        }
        /// <summary>
        /// Some access problems inside RangeMap, do not use anywhere else since center is not taken into account
        /// </summary>
        public HashSet<Vector2Int> Map { get { return map; } }
        public Vector2Int Center { get { return center; } }
    }

    public class RangeMap : RangeMapReadOnly
    {
        public RangeMap(RangeMap other) : base(other) { }
        public RangeMap(bool isFilled = false) : base(isFilled) { }
        public RangeMap(IEnumerable<Vector2Int> slots) : base(slots) { }
        public RangeMap(IEnumerable<IUnitInfo> units) : base(units) { }
        public RangeMap(int range) : base(range) { }
        public RangeMap(int range, System.Predicate<Vector2Int> condition) : base(range, condition) { }

        public bool Remove(int x, int y)
        {
            return Remove(new Vector2Int(x, y));
        }
        public bool Remove(Vector2Int slot)
        {
            if (Filled)
            {
                return map.Remove(slot - center);
            }
            else
            {
                return map.Add(slot - center);
            }
        }
        public bool Add(Vector2Int slot)
        {
            if (Filled)
            {
                return map.Add(slot - center);
            }
            else
            {
                return map.Remove(slot - center);
            }
        }
        public void Remove(RangeMapReadOnly other)
        {
            if (other.Filled)
            {
                foreach (var slot in other)
                {
                    Remove(slot);
                }
            }
            else
            {
                if (Filled)
                {
                    List<Vector2Int> toRemove = new List<Vector2Int>();
                    foreach (var slot in this)
                    {
                        if (other.IsFilled(slot))
                        {
                            toRemove.Add(slot);
                        }
                    }
                    foreach (var slot in toRemove)
                    {
                        Remove(slot);
                    }
                }
                else
                {
                    this.fill = true;
                    HashSet<Vector2Int> temp = new HashSet<Vector2Int>(this.map);
                    foreach (var slot in other)
                    {
                        Add(slot);
                    }
                    foreach (var slot in temp)
                    {
                        Remove(slot + this.center);
                    }
                }
            }
        }
        public void And(RangeMapReadOnly other)
        {
            if (Filled)
            {
                List<Vector2Int> toRemove = new List<Vector2Int>();
                foreach (var slot in this)
                {
                    if (!other.IsFilled(slot))
                    {
                        toRemove.Add(slot);
                    }
                }
                foreach (var slot in toRemove)
                {
                    Remove(slot);
                }
            }
            else
            {
                if (!other.Filled)
                {
                    foreach (var slot in other)
                    {
                        Remove(slot);
                    }
                }
                else
                {
                    this.fill = false;
                    HashSet<Vector2Int> temp = new HashSet<Vector2Int>(this.map);
                    foreach (var slot in other)
                    {
                        Add(slot);
                    }
                    foreach (var slot in temp)
                    {
                        Remove(slot + this.center);
                    }
                }
            }
        }
        public void Or(RangeMapReadOnly other) 
        {
            if (!Filled)
            {
                List<Vector2Int> toAdd = new List<Vector2Int>();
                foreach (var slot in this)
                {
                    if (other.IsFilled(slot))
                    {
                        toAdd.Add(slot);
                    }
                }
                foreach (var slot in toAdd)
                {
                    Add(slot);
                }
            }
            else
            {
                if (other.Filled)
                {
                    foreach (var slot in other)
                    {
                        Add(slot);
                    }
                }
                else
                {
                    this.fill = true;
                    HashSet<Vector2Int> temp = new HashSet<Vector2Int>(this.map);
                    foreach (var slot in other)
                    {
                        Remove(slot);
                    }
                    foreach (var slot in temp)
                    {
                        Add(slot + this.center);
                    }
                }
            }
        }

        public RangeMap SetCenter(Vector2Int center)
        {
            this.center = center;
            return this;
        }
        public RangeMap TranslateSlots(Vector2Int translation)
        {
            center += translation;
            if (!IsFull)
            {
                /*HashSet<Vector2Int> temp = new HashSet<Vector2Int>(map);
                foreach (var slot in temp)
                {
                    map.Remove(slot);
                }
				foreach (var slot in temp)
                {
                    var calc = slot + translation;
                    map.Add(calc);
                }*/
            }
            return this;
        }

        public new static RangeMap Empty { get { return new RangeMap(new Vector2Int[0]); } }
        public new static RangeMap Single { get { return new RangeMap(0); } }
    }