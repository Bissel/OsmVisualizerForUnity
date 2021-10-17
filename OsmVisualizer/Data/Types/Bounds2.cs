using System;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Data.Types
{
    [System.Serializable]
    public class Bounds2
    {
        public Vector2 Center { get; private set; }
        public Vector2 Extents { get; private set; }
        public Vector2 Max { get; private set; }
        public Vector2 Min { get; private set; }
        public Vector2 Size { get; private set; }
        public Bounds Bounds3 { get; private set; }

        private Vector2 _worldOffset = Vector2.zero;

        // public readonly bool isInLatLon;

        public Vector2 WorldCenterInMeter
        {
            get => _worldOffset;
            set => _worldOffset = value;
        }

        public Bounds2()
        {
            Center = new Vector2();
            Size = new Vector2();

            UpdateAttributes();
        }
        //
        // public Bounds2(Vector2 center, float size)
        // {
        //     Center = new Vector2(center.x, center.y);
        //     Size = new Vector2(size, size);
        //     isInLatLon = true;
        //     
        //     updateAttributes();
        // }

        public Bounds2(Vector2 center, Vector2 size)
        {
            Center = center;
            Size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));

            UpdateAttributes();
        }

        public Bounds2(Vector2 center, Vector2 size, Vector2 worldCenterInMeter)
        {
            Center = center;
            Size = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
            _worldOffset = worldCenterInMeter;

            UpdateAttributes();
        }

        protected void UpdateAttributes()
        {
            Extents = Size / 2;
            Min = Center - Extents;
            Max = Center + Extents;

            Bounds3 = new Bounds(Center.ToVector3xz(), Size.ToVector3xz() + Vector3.up);
        }

        /**
     * Is point contained in the bounding box?
     */
        public bool Contains(Vector2 point)
        {
            return Min.x < point.x && point.x < Max.x
                                   && Min.y < point.y && point.y < Max.y;
        }

        /**
     * Expand the bounds by increasing its size by amount along each side.
     */
        public void Expand(Vector2 amount)
        {
            Size += new Vector2(Mathf.Abs(amount.x), Mathf.Abs(amount.y));
            UpdateAttributes();
        }

        public bool Intersects(Bounds2 other)
        {
            return Min.x <= other.Max.x && Max.x >= other.Min.x
                                        && Min.y <= other.Max.y && Max.y >= other.Min.y;
        }

        public bool IntersectRay(Vector2 rayOrigin, Vector2 rayDirection)
        {
            return Bounds3.IntersectRay(new Ray(rayOrigin.ToVector3xz(), rayDirection.ToVector3xz()));
        }

        public bool IntersectRay(Vector2 rayOrigin, Vector2 rayDirection, out float scale)
        {
            return Bounds3.IntersectRay(new Ray(rayOrigin.ToVector3xz(), rayDirection.ToVector3xz()), out scale);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format))
                format = "F1";
            return
                $"Center: {(object) Center.ToString(format, formatProvider)}, Extents: {(object) Extents.ToString(format, formatProvider)}";
        }


        public Vector2 CenterInWorldCoords()
        {
            return Center - _worldOffset;
        }

        public Bounds2 InWorldCoords()
        {
            return new Bounds2(CenterInWorldCoords(), Size);
        }




    }
}