using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OsmVisualizer.Data.Types
{
    public class Shape : List<Vector3>
    {
        public readonly List<Vector3> Normals;
        public readonly List<float> Vs;
        public readonly List<bool> Faces;
        
        public Shape(IEnumerable<Vector3> shape, List<Vector3> normals, List<float> vs, List<bool> faces) : base(shape)
        {
            Normals = normals;
            Vs = vs;
            Faces = faces;
        }

        public Shape(float width, float heightOffset)
        {
            var normal = width > 0 ? Vector3.up : Vector3.down;
            
            Add(new Vector3(0f,heightOffset, 0f));
            Add(new Vector3(0f,heightOffset, width));

            Normals = new List<Vector3> {normal, normal};
            Vs = new List<float> {0f, width};
            Faces = new List<bool> {true};
        }
        
        public Shape(float width, float heightOffset, float borderHeight)
        {
            var normalL = width > 0 ? Vector3.back : Vector3.forward;
            var normalC = width > 0 ? Vector3.up : Vector3.down;
            var normalR = width > 0 ? Vector3.forward : Vector3.back;
            
            Add(new Vector3(0f,heightOffset-borderHeight, 0f));
            Add(new Vector3(0f,heightOffset, 0f));
            
            Add(new Vector3(0f,heightOffset, 0f));
            Add(new Vector3(0f,heightOffset, width));
            
            Add(new Vector3(0f,heightOffset, width));
            Add(new Vector3(0f,heightOffset-borderHeight, width));

            Normals = new List<Vector3> {normalL, normalL, normalC, normalC, normalR, normalR};
            Vs = new List<float>
            {
                0f, 
                borderHeight,
                
                borderHeight,
                borderHeight + width,
                
                borderHeight + width,
                borderHeight + width + borderHeight
            };
            
            Faces = new List<bool> {true, false, true, false, true};
        }

        public void Offset(Vector3 offset)
        {
            for (var i = 0; i < Count; i++)
            {
                this[i] = this[i] + offset;
            }
        }

        public Shape Combine(Shape other)
        {
            return Combine(other, false, Vector3.zero);
        }

        public Shape Combine(Shape other, Vector3 faceBetweenNormals, float v0, float v1)
        {
            return Combine(other, true, faceBetweenNormals, v0, v1);
        }

        private Shape Combine(Shape other, bool addFace, Vector3 faceBetweenNormals, float v0 = 0f, float v1 = 0f)
        {
            var shape = this.ToList();
            if (addFace)
            {
                shape.Add(this[Count - 1]);
                shape.Add(other[0]);
            }
            shape.AddRange(other);
            
            var normals = Normals.ToList();
            if (addFace)
            {
                normals.Add(faceBetweenNormals);
                normals.Add(faceBetweenNormals);
            }
            normals.AddRange(other.Normals);

            var vs = Vs.ToList();
            if (addFace)
            {
                vs.Add(v0);
                vs.Add(v1);
            }
            vs.AddRange(other.Vs);

            var faces = Faces.ToList();
            faces.Add(false);
            if (addFace)
            {
                faces.Add(true);
                faces.Add(false);
            }
            faces.AddRange(other.Faces);

            return new Shape(shape, normals, vs, faces);
        }

        public static Shape WidthCentered(float width, float heightOffset = 0f, float widthOffset = 0f)
        {
            // var normal = height > 0 ? Vector3.back : Vector3.forward;
            var normal = Vector3.up;
            var wHalf = width * .5f;
            
            return new Shape(
                new[]
                {
                    new Vector3(0, heightOffset, -wHalf + widthOffset),
                    new Vector3(0, heightOffset, wHalf + widthOffset)
                },
                new List<Vector3> {normal, normal},
                new List<float> {-wHalf, wHalf},
                new List<bool> {true}
            );
        }
        
        public static Shape WallLeft(float height, float heightOffset = 0f)
        {
            var normal = height > 0 ? Vector3.back : Vector3.forward;

            return new Shape(
                new[]
                {
                    new Vector3(0f, heightOffset, 0f),
                    new Vector3(0f, height + heightOffset, 0f)
                },
                new List<Vector3> {normal, normal},
                new List<float> {0f, height},
                new List<bool> {true}
            );
        }
        
        public static Shape WallRight(float height, float heightOffset = 0f)
        {
            var normal = height > 0 ? Vector3.forward : Vector3.back;

            return new Shape(
                new[]
                {
                    new Vector3(0f, height + heightOffset, 0f),
                    new Vector3(0f, heightOffset, 0f)
                },
                new List<Vector3> {normal, normal},
                new List<float> {height, 0f},
                new List<bool> {true}
            );
        }

        public static Shape TunnelWallRight(float tunnelHeight, float wallThickness = .3f)
        {
            return new Shape(
                new[]
                {
                    // inner
                    new Vector3(0f, 0f, 0f),
                    new Vector3(0f, .25f, 0f),
                    new Vector3(0f, .35f, .10f),
                    new Vector3(0f, tunnelHeight - .10f, .10f),
                    new Vector3(0f, tunnelHeight, 0f),
                    
                    // outer
                    new Vector3(0f, tunnelHeight, wallThickness),
                    new Vector3(0f, 0f, wallThickness),
                    
                },
                
                new List<Vector3>
                {
                    new Vector3(0, 0, -1),
                    new Vector3(0, 1, -1).normalized,
                    new Vector3(0, 1, -1).normalized,
                    new Vector3(0, -1, -1).normalized,
                    new Vector3(0, -1, -1).normalized,

                    new Vector3(0,0, 1),
                    new Vector3(0,0, 1),
                },
                new List<float>
                {
                    0f, 
                    .25f, 
                    .25f + .14f, 
                    tunnelHeight + .14f, 
                    tunnelHeight + .28f, 
                    
                    0f,
                    tunnelHeight
                },
                new List<bool>
                {
                    true, true, true, true, false,
                    true
                }
            );
        }

        public static Shape TunnelCeiling(float tunnelHeight, float offsetLeft, float offsetRight,
            float wallThickness = .3f)
        {
            offsetLeft = Mathf.Abs(offsetLeft);
            offsetRight = Mathf.Abs(offsetRight);

            var totalWidth = offsetLeft + offsetRight;
            offsetLeft = -offsetLeft;

            var wallUVOffset = Mathf.Sqrt(wallThickness * wallThickness * 2);

            return new Shape(
                new[]
                {
                    // outer
                    new Vector3(0f, tunnelHeight, offsetLeft - wallThickness),
                    new Vector3(0f, tunnelHeight + wallThickness, offsetLeft),
                    
                    new Vector3(0f, tunnelHeight + wallThickness, offsetLeft),
                    new Vector3(0f, tunnelHeight + wallThickness, offsetRight),
                    
                    new Vector3(0f, tunnelHeight + wallThickness, offsetRight),
                    new Vector3(0f, tunnelHeight, offsetRight + wallThickness),

                    // inner
                    new Vector3(0f, tunnelHeight, offsetRight),
                    new Vector3(0f, tunnelHeight + .1f, offsetRight - 1f),
                    new Vector3(0f, tunnelHeight + .1f, offsetLeft + 1f),
                    new Vector3(0f, tunnelHeight, offsetLeft)
                },

                new List<Vector3>
                {
                    // outer
                    new Vector3(0, 1, -1).normalized,
                    new Vector3(0, 1, -1).normalized,

                    new Vector3(0, 1, 0),
                    new Vector3(0, 1, 0),
                    
                    new Vector3(0, 1, 1).normalized,
                    new Vector3(0, 1, 1).normalized,

                    // inner
                    new Vector3(0, -1, -.1f).normalized,
                    new Vector3(0, -1, 0),
                    new Vector3(0, -1, 0),
                    new Vector3(0, -1, .1f).normalized,
                },
                new List<float>
                {
                    // outer
                    0f,
                    wallUVOffset,
                    wallUVOffset,
                    wallUVOffset + totalWidth,
                    wallUVOffset + totalWidth,
                    wallUVOffset * 2 + totalWidth,

                    // inner
                    0f,
                    1f,
                    totalWidth - 1f,
                    totalWidth
                },
                new List<bool>
                {
                    // outer
                    true, false,
                    true, false,
                    true, false,
                    // inner
                    true, true, true
                }
            );
        }

        public static Shape TunnelCeilingLights(float tunnelHeight, float offsetLeft, float offsetRight)
        {
            offsetLeft = -Mathf.Abs(offsetLeft);
            offsetRight = Mathf.Abs(offsetRight);

            const float lampWidth = .3f;

            return new Shape(
                new[]
                {
                    new Vector3(0f, tunnelHeight + .07f, offsetLeft + 1f + lampWidth),
                    new Vector3(0f, tunnelHeight + .05f, offsetLeft + 1f + lampWidth),
                    
                    new Vector3(0f, tunnelHeight + .05f, offsetLeft + 1f + lampWidth),
                    new Vector3(0f, tunnelHeight + .05f, offsetLeft + 1f),
                    
                    new Vector3(0f, tunnelHeight + .05f, offsetLeft + 1f),
                    new Vector3(0f, tunnelHeight + .07f, offsetLeft + 1f),
                    
                    
                    new Vector3(0f, tunnelHeight + .07f, offsetRight - 1f),
                    new Vector3(0f, tunnelHeight + .05f, offsetRight - 1f),
                    
                    new Vector3(0f, tunnelHeight + .05f, offsetRight - 1f),
                    new Vector3(0f, tunnelHeight + .05f, offsetRight - 1f - lampWidth),
                    
                    new Vector3(0f, tunnelHeight + .05f, offsetRight - 1f - lampWidth),
                    new Vector3(0f, tunnelHeight + .07f, offsetRight - 1f - lampWidth),
                },

                new List<Vector3>
                {
                    new Vector3(0, 0, -1),
                    new Vector3(0, 0, -1),
                    
                    new Vector3(0, -1, 0),
                    new Vector3(0, -1, 0),
                    
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, 1),
                    
                    new Vector3(0, 0, -1),
                    new Vector3(0, 0, -1),
                    
                    new Vector3(0, -1, 0),
                    new Vector3(0, -1, 0),
                    
                    new Vector3(0, 0, 1),
                    new Vector3(0, 0, 1),
                },
                new List<float>
                {
                    0f, .02f,
                    0f, lampWidth,
                    0f, .02f,
                    0f, .02f,
                    0f, lampWidth,
                    0f, .02f
                },
                new List<bool>
                {
                    true, false, 
                    true, false, 
                    true, false, 
                    true, false, 
                    true, false, 
                    true
                }
            );
        }

        public static void TunnelCap(MeshHelper wallsMesh, float width, float height, float wallThickness, float offsetR = 0f, float offsetL = 0f)
        {
            TunnelCap(wallsMesh, width, height, wallThickness, offsetR, offsetL, Vector3.zero, Vector3.back);
        }
        public static void TunnelCap(MeshHelper wallsMesh, float width, float height, float wallThickness, 
            float offsetR, float offsetL, Vector3 offset, Vector3 normals)
        {
            var widthHalf = width * .5f;
            var triangleOffset = wallsMesh.Vertices.Count;

            var l = -widthHalf - offsetL;
            var r = widthHalf + offsetR;
            
            var points = new[]
            {
                new Vector3(l - wallThickness, 0, 0),
                new Vector3(l - wallThickness, height, 0),
                new Vector3(l, height + wallThickness, 0),
                new Vector3(r, height + wallThickness, 0),
                new Vector3(r + wallThickness, height, 0),
                new Vector3(r + wallThickness, 0, 0),
                new Vector3(r + .1f, 0, 0),
                new Vector3(r, 0, 0),
                new Vector3(r, .25f, 0),
                new Vector3(r + .1f, .35f, 0),
                new Vector3(r + .1f, height - .1f, 0),
                new Vector3(r, height, 0),
                new Vector3(r - 1f, height + .1f, 0),
                new Vector3(l + 1f, height + .1f, 0),
                new Vector3(l, height, 0),
                new Vector3(l - .1f, height - .1f, 0),
                new Vector3(l - .1f, .35f, 0),
                new Vector3(l, .25f, 0),
                new Vector3(l, 0, 0),
                new Vector3(l - .1f, 0, 0),

                new Vector3(l - .1f, height, 0),
                new Vector3(l, height + .1f, 0),
                new Vector3(r, height + .1f, 0),
                new Vector3(r + .1f, height, 0),
            };

            if (!normals.Equals(Vector3.back))
            {
                var rotation = Quaternion.FromToRotation(Vector3.back, normals);
                for (var i = 0; i < points.Length; i++)
                    points[i] = rotation * points[i];
            }

            if (!offset.Equals(Vector3.zero))
            {
                for (var i = 0; i < points.Length; i++)
                    points[i] = offset + points[i];
            }
            
            wallsMesh.Vertices.AddRange(points);

            wallsMesh.AddTriangle(0, 1, 20, triangleOffset);
            wallsMesh.AddTriangle(0, 20, 19, triangleOffset);
            wallsMesh.AddTriangle(19, 16, 17, triangleOffset);
            wallsMesh.AddTriangle(19, 17, 18, triangleOffset);
            wallsMesh.AddTriangle(1, 2, 14, triangleOffset);
            wallsMesh.AddTriangle(20, 14, 15, triangleOffset);
            wallsMesh.AddTriangle(2, 3, 22, triangleOffset);
            wallsMesh.AddTriangle(2, 22, 21, triangleOffset);
            wallsMesh.AddTriangle(21, 13, 14, triangleOffset);
            wallsMesh.AddTriangle(12, 22, 11, triangleOffset);
            wallsMesh.AddTriangle(3, 4, 11, triangleOffset);
            wallsMesh.AddTriangle(11, 23, 10, triangleOffset);
            wallsMesh.AddTriangle(23, 4, 6, triangleOffset);
            wallsMesh.AddTriangle(4, 5, 6, triangleOffset);
            wallsMesh.AddTriangle(6, 7, 8, triangleOffset);
            wallsMesh.AddTriangle(6, 8, 9, triangleOffset);

            for (var i = triangleOffset; i < wallsMesh.Vertices.Count; i++)
            {
                var v = wallsMesh.Vertices[i];
                wallsMesh.UV.Add(new Vector2(v.x + widthHalf, v.y));
                wallsMesh.Normals.Add(normals);
            }
        }
        
    }
}