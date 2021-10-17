using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using UnityEditor;
using UnityEngine;

namespace OsmVisualizer.Gizmos
{
    public class LaneCollection : MonoBehaviour
    {
        public OsmVisualizer.Data.LaneCollection fw;
        public OsmVisualizer.Data.LaneCollection bw;

        private Vector3 _offset = Vector3.zero;
        private Vector3 _offsetTri;

        public bool ShowOutlines = false;
        
#if (UNITY_EDITOR)
        private void OnDrawGizmosSelected()
        {
            if (_offset == Vector3.zero)
            {
                _offset = Vector3.up * .1f; // + gameObject.transform.parent.position;
                _offsetTri = Vector3.up * .5f; 
            }
            
            if (fw == null && bw == null)
                return;

            if (fw != null)
            {
                DrawLanes(fw, "FW");
                DrawOutline(fw);
                DrawNextLanes(fw);
            }
            
            if (bw != null)
            {
                DrawLanes(bw, "BW");
                DrawOutline(bw);
                DrawNextLanes(bw);
            }
        }

        private void DrawNextLanes(OsmVisualizer.Data.LaneCollection lc)
        {
            UnityEngine.Gizmos.color = Color.cyan;
            foreach (var lane in lc.Lanes)
            {
                var lastPoint = lane.Points[lane.Points.Count - 1];

                var label = lane.Directions?.Aggregate("", (current, dir) => current + dir switch
                {
                    Direction.LEFT => "L",
                    Direction.RIGHT => "R",
                    Direction.THROUGH => "T",
                    Direction.NONE => "N",
                    _ => "D"
                }) ?? "_";

                Handles.Label(lastPoint.ToVector3xz() + _offset, label);
                foreach (var n in lane.Next)
                {
                    DrawLine(new List<Vector2>{lastPoint, n.Points[0]});
                }
            }
        }

        private void DrawOutline(OsmVisualizer.Data.LaneCollection lc)
        {
            if (!ShowOutlines)
                return;
            
            UnityEngine.Gizmos.color = Color.blue;
            DrawLine(lc.OutlineLeft);
            UnityEngine.Gizmos.color = Color.yellow;
            DrawLine(lc.OutlineRight);
        }

        private void DrawLanes(OsmVisualizer.Data.LaneCollection lc, string label)
        {
            var pos = lc.Lanes[ (lc.Lanes.Length - 1) / 2].Points[0];
            Handles.Label(pos.ToVector3xz() + _offset, label);
            UnityEngine.Gizmos.color = Color.magenta;
            foreach(var lane in lc.Lanes)
                DrawLine(lane.Points);
        }

        private void DrawLine(IReadOnlyList<Vector3> points)
        {
            for (var i = 0; i < points.Count - 1; i++)
            {
                var v0 = points[i] + _offset;
                var v1 = points[i + 1] + _offset;
                
                UnityEngine.Gizmos.DrawLine(v0, v1);
                UnityEngine.Gizmos.DrawLine(v0, v0 + _offsetTri);
                UnityEngine.Gizmos.DrawLine(v1, v0 + _offsetTri);
            }     
        }
        
        private void DrawLine(IEnumerable<Vector2> points)
        {
            DrawLine(points.Select(p => p.ToVector3xz()).ToList());
        }
#endif
    }
}