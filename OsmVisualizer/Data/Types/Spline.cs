using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Data.Types
{
    public class Spline : List<Vector3>
    {
        public bool HasElevation { get; protected set; }
        public readonly List<Vector3> Forward;
        public readonly List<float> Length;
        public float TotalLength { get; protected set; }
        
        public Vector3 Get(int index) => this[(Count + index) % Count];

        public Spline(IReadOnlyCollection<Vector3> points) : base(points)
        {
            Forward = new List<Vector3>(points.Count);
            Length = new List<float>(points.Count);
            Init();
        }

        public Spline(IReadOnlyList<Vector2> points, float height)
        {
            if(float.IsNaN(height))
                height = 0f;
            
            foreach (var p in points)
            {
                Add(new Vector3(p.x, height, p.y));
            }
            
            Forward = new List<Vector3>(points.Count);
            Length = new List<float>(points.Count);
            Init();
        }
        
        public Spline(IReadOnlyList<Vector2> points, IReadOnlyList<float> elevations = null)
        {
            var hasElevation = elevations != null && elevations.Count == points.Count;
            
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                var e = hasElevation ? elevations[i] : 0f;
                Add(new Vector3(p.x, e, p.y));
            }
            
            Forward = new List<Vector3>(points.Count);
            Length = new List<float>(points.Count);
            Init();
        }

        public void SetHeight(float height)
        {
            for (var i = 0; i < Count; i++)
            {
                var p = this[i];
                this[i] = new Vector3(p.x, height, p.z);
            }
        }
        
        public void SetHeight(float[] height)
        {
            if (height.Length != Count) return;

            for (var i = 0; i < Count; i++)
            {
                var p = this[i];
                this[i] = new Vector3(p.x, height[i], p.z);
            }
        }

        public virtual void SetForwardStartAndEnd(Vector2 rotationStart, Vector2 rotationEnd)
        {
            SetForwardStartAndEnd(rotationStart.ToVector3xz(), rotationEnd.ToVector3xz());
        }
        
        public virtual void SetForwardStartAndEnd(Vector3 rotationStart, Vector3 rotationEnd)
        {
            Forward[0] = rotationStart;
            Forward[Forward.Count - 1] = rotationEnd;
        }

        protected virtual void Init()
        {
            TotalLength = 0f;
            if (Count < 2)
            {
                HasElevation = Count == 1 && Mathf.Abs(this[0].y) > 0.001f;
                return;
            }

            var last = this[0];
            
            var elevationChange = 0f;

            Length.Add(0f);
            
            for (var i = 1; i < Count; i++)
            {
                var p = this[i];
                AddDirAndLength(p, last);
                
                last = p;
                elevationChange += p.y;
            }
            
            Forward.Add(Forward[Forward.Count - 1]);
            
            HasElevation = Mathf.Abs(elevationChange) > 0.001f;
        }

        protected void AddDirAndLength(Vector3 curr, Vector3 last) 
        {
            var l = Vector3.Distance(curr, last);
            TotalLength += l;
            Length.Add(l);
            Forward.Add((curr - last).normalized);
        }

        public void RecalculateTotalLenght()
        {
            TotalLength = 0f;
            if (Count < 2) return;
            
            var last = this[0];
            for (var i = 1; i < Count; i++)
            {
                var curr = this[i];
                var l = Vector3.Distance(curr, last);
                TotalLength += l;
                Length[i] = l;
                last = curr;
            }
        }
        
        public new Spline Reverse()
        {
            return new Spline(this.ToArray().Reverse().ToList());
        }

    }
}