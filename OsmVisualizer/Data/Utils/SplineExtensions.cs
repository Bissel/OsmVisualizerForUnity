using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Types;
using UnityEngine;

namespace OsmVisualizer.Data.Utils
{
    public static class SplineExtensions
    {
        public static void ExtrudeShape(
            this Types.Spline spline, MeshHelper mesh, Shape shape,
            float chamferAngleStart = float.NaN, float chamferAngleEnd = float.NaN,
            Vector3 offset = new Vector3(), bool cutCorners = false, float maxHeight = float.NaN
        )
        {
            if (spline.Count < 2)
                return;
            
            var length = 0f;
            var meshVertexOffset = mesh.Vertices.Count;
            
            var addTriangles = false;
            var i = 0;
            var lastIndex = -1;
            var lastForward = spline.Forward[0].ToVector2xz().ToVector3xz();
            Vector3 forward;
            
            var tanChamferStart = float.IsNaN(chamferAngleStart) ? float.NaN : Mathf.Tan(Mathf.Deg2Rad * Mathf.Clamp(chamferAngleStart, -80f, 80f));
            var tanChamferEnd = float.IsNaN(chamferAngleStart) ? float.NaN : Mathf.Tan(Mathf.Deg2Rad * Mathf.Clamp(chamferAngleEnd, -80f, 80f));
            
            for (; i < spline.Count;)
            {
                length += spline.Length[i];
                forward = spline.Forward[i].ToVector2xz().ToVector3xz();
                ExtrudeShapeStep(spline, mesh, shape, tanChamferStart, tanChamferEnd, offset, length, meshVertexOffset, cutCorners, forward, lastForward, ref i, ref lastIndex, ref addTriangles, maxHeight);
                lastForward = forward;
                lastIndex = i;

                if (addTriangles) i++;
                else meshVertexOffset += shape.Count;
            }
            
            if (!(spline is Area)) return;
            
            meshVertexOffset += shape.Count;
            i = lastIndex;
            forward = lastForward;
            addTriangles = false;
            
            ExtrudeShapeStep(spline, mesh, shape, tanChamferStart, tanChamferEnd, offset, length, meshVertexOffset, cutCorners, forward, lastForward, ref i, ref lastIndex, ref addTriangles, maxHeight);
            
            length += spline.Length[i + 1];
            lastIndex = i;
            i = 0;
            
            ExtrudeShapeStep(spline, mesh, shape, tanChamferStart, tanChamferEnd, offset, length, meshVertexOffset, cutCorners, forward, lastForward, ref i, ref lastIndex, ref addTriangles, maxHeight);
        }

        private static void ExtrudeShapeStep(
            Types.Spline spline, MeshHelper mesh, Shape shape, float tanChamferStart,
            float tanChamferEnd, Vector3 offset, float length, int meshVertexOffset,
            bool cutCornerAngle, Vector3 forward, Vector3 lastForward, ref int i, ref int lastIndex, ref bool addTriangles,
            float maxHeight)
        {
            var rotation = Quaternion.FromToRotation(Vector3.left, forward);

            var useChamferStart = !float.IsNaN(tanChamferStart);
            var useChamferEnd = !float.IsNaN(tanChamferEnd);

            var pV3 = spline[i];

            for (var j = 0; j < shape.Count; j++)
            {
                var sP = shape[j];
                var sN = shape.Normals[j];
                var sV = shape.Vs[j];

                var regularPoint = pV3 + rotation * (sP + offset);
                var regularU = length;

                if (useChamferStart)
                {
                    var b = Vector2.Distance(regularPoint.ToVector2xz(), pV3.ToVector2xz());
                    var a = tanChamferStart * b;
                    if (length < Mathf.Abs(a))
                    {
                        regularPoint+= forward * a;
                        regularU += a;
                    }
                }
                
                if (useChamferEnd && lastIndex >= 0)
                {
                    var b = Vector2.Distance(regularPoint.ToVector2xz(), pV3.ToVector2xz());
                    var a = tanChamferEnd * b;
                    if (spline.TotalLength - length < Mathf.Abs(a))
                    {
                        regularPoint -= lastForward * a;
                        regularU -= a;
                    }
                }
                
                mesh.Vertices.Add(regularPoint.ClipYGreater(maxHeight));
                mesh.Normals.Add(rotation * sN);
                mesh.UV.Add(new Vector2(regularU, sV));
            }

            if (lastIndex < 0 || !addTriangles)
            {
                addTriangles = true;
                return;
            }

            var cnt = shape.Count;
            var vertexOffset = cnt * (i == 0 ? lastIndex + 1 : i) + meshVertexOffset;

            for (var j = 0; j < shape.Faces.Count; j++)
            {
                if (!shape.Faces[j]) continue;

                var tl = j;
                var bl = -cnt + j;
                var tr = tl + 1;
                var br = bl + 1;

                mesh.AddTriangle(bl, tl, br, vertexOffset);
                mesh.AddTriangle(br, tl, tr, vertexOffset);
            }

            if (!cutCornerAngle)
                return;

            var lastVertexOffset = cnt * lastIndex + meshVertexOffset;
            for (var j = 0; j < cnt; j++)
            {
                mesh.Normals[j + vertexOffset] = mesh.Normals[j + lastVertexOffset];
            }

            addTriangles = false;
        }

        public static void ExtrudeHorizontal(this Types.Spline spline, MeshHelper mesh, 
            float width, float heightOffset, float borderHeight = float.NaN, 
            float chamferAngleStart = float.NaN, float chamferAngleEnd = float.NaN,
            Vector3 offset = new Vector3()
        )
        {
            var shape = float.IsNaN(borderHeight) 
                ? new Shape(width, heightOffset) 
                : new Shape(width, heightOffset, borderHeight);
            
            ExtrudeShape(spline, mesh, shape, chamferAngleStart, chamferAngleEnd, offset);
        }

        public static void Fill(this Area area, MeshHelper mesh, Vector3 offset = new Vector3(), bool reverse = false)
        {
            var indices = area.GetIndices();

            if (reverse)
                FillInner(mesh, indices.Reverse(), area, Vector3.down, offset);
            else
                FillInner(mesh, indices, area, Vector3.up, offset);
                
            
        }

        private static void FillInner(MeshHelper mesh, IEnumerable<int> indices, IEnumerable<Vector3> points, Vector3 normals, Vector3 offset)
        {
            var triangleOffset = mesh.Vertices.Count;
            
            foreach (var p in points)
            {
                mesh.Vertices.Add(p + offset);
                mesh.Normals.Add(normals);
                mesh.UV.Add(p.ToVector2xz());
            }

            foreach (var index in indices)
            {
                mesh.Triangles.Add(index + triangleOffset);
            }
        }
        
        
    }
}