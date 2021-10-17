using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called before the first frame update
    // Vector2[] tmp1 = new[]
    // {
    //     new Vector2(1, 0),
    //     new Vector2(1, 1),
    //     new Vector2(0, 0),
    // };
    //     
    // Vector2[] tmp2 = new[]
    // {
    //     new Vector2(2, 1),
    //     new Vector2(2, 2),
    //     new Vector2(1, 1),
    // };
    //     
    // Vector2[] tmp3 = new[]
    // {
    //     new Vector2(0, 1),
    //     new Vector2(-1, 0),
    //     new Vector2(0, 0),
    // };
    //     
    // Vector2[] tmp4 = new[]
    // {
    //     new Vector2(0, 1),
    //     new Vector2(1, 0),
    //     new Vector2(0, 0),
    // };
    //     
    // Vector2[] tmp5 = new[]
    // {
    //     new Vector2(1, 1),
    //     new Vector2(1, -1),
    //     new Vector2(-1, -1),
    //     new Vector2(-1, 1),
    //     new Vector2(-1.5f, 0),
    //     new Vector2(-2, 1),
    //     new Vector2(-2, 2),
    // };
    
    void Start()
    {
        
        
        // Debug.Log($"TMP1: Orientation {tmp1.Orientation()}, isClockwise {tmp1.IsOrientationClockwise()}");
        // Debug.Log($"TMP2: Orientation {tmp2.Orientation()}, isClockwise {tmp2.IsOrientationClockwise()}");
        // Debug.Log($"TMP3: Orientation {tmp3.Orientation()}, isClockwise {tmp3.IsOrientationClockwise()}");
        // Debug.Log($"TMP4: Orientation {tmp4.Orientation()}, isClockwise {tmp4.IsOrientationClockwise()}");
        // Debug.Log($"TMP5: Orientation {tmp5.Orientation()}, isClockwise {tmp5.IsOrientationClockwise()}");
        //
        // var tmp1_rev = tmp1.Reverse().ToArray();
        // var tmp2_rev = tmp2.Reverse().ToArray();
        // var tmp3_rev = tmp3.Reverse().ToArray();
        // var tmp4_rev = tmp4.Reverse().ToArray();
        // var tmp5_rev = tmp5.Reverse().ToArray();
        //
        // Debug.Log($"TMP1 Rev: Orientation {tmp1_rev.Orientation()}, isClockwise {tmp1_rev.IsOrientationClockwise()}");
        // Debug.Log($"TMP2 Rev: Orientation {tmp2_rev.Orientation()}, isClockwise {tmp2_rev.IsOrientationClockwise()}");
        // Debug.Log($"TMP3 Rev: Orientation {tmp3_rev.Orientation()}, isClockwise {tmp3_rev.IsOrientationClockwise()}");
        // Debug.Log($"TMP4 Rev: Orientation {tmp4_rev.Orientation()}, isClockwise {tmp4_rev.IsOrientationClockwise()}");
        // Debug.Log($"TMP5 Rev: Orientation {tmp5_rev.Orientation()}, isClockwise {tmp5_rev.IsOrientationClockwise()}");
        //
        // Debug.Log(PolygonTriangulation.edge(tmp[0], tmp[1]));
        // Debug.Log(PolygonTriangulation.edge(tmp[1], tmp[2]));
        // Debug.Log(PolygonTriangulation.edge(tmp[2], tmp[0]));
        //
        // Debug.Log(PolygonTriangulation.crossProductZ(tmp[0], tmp[1]));
        // Debug.Log(PolygonTriangulation.crossProductZ(tmp[1], tmp[2]));
        // Debug.Log(PolygonTriangulation.crossProductZ(tmp[2], tmp[0]));

        var settings = GetComponent<SettingsProvider>();
        var center = settings.startPosition.InWorldCoords();
        var latInM = settings.startPosition.OneDegLatInMeters();
        var lonInM = settings.startPosition.OneDegLonInMeters();
        
        var positions = new[]
        {
            new Position2(53.0786395f, 8.8084951f, latInM, lonInM),
            new Position2(53.0787363f, 8.8085517f, latInM, lonInM),
            new Position2(53.0787636f, 8.8084220f, latInM, lonInM),
            new Position2(53.0786669f, 8.8083655f, latInM, lonInM),
            // new Position2(53.0786395f, 8.8084951f, latInM, lonInM),
        };


        var points = new Vector2[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            points[i] = positions[i].InWorldCoords() - center;
        }

        // var points = tmp5;

        var tris = points.Triangulate();

        var mesh = new MeshHelper();

        foreach (var v in points)
        {
            mesh.Vertices.Add(v.ToVector3xz());
            mesh.Normals.Add(Vector3.up);
        }

        foreach (var t in tris)
        {
            mesh.Triangles.Add(t);
        }

        var mat = Resources.Load("OSM/Materials/Building", typeof(Material)) as Material;
        mesh.AddMeshToGameObject(gameObject, mat, false);
        var gizmos = gameObject.AddComponent<OsmVisualizer.Gizmos.Mesh>();
        gizmos.ApplyParentTransform = false;
    }

    // private void OnDrawGizmosSelected()
    // {
    //     var points = tmp4;
    //     
    //     var up = Vector3.up * .2f;
    //     for (var i = 0; i < points.Length; i++)
    //     {
    //         var c = points[i].ToVector3xz();
    //         var n = points[(i + 1) % points.Length].ToVector3xz();
    //         Gizmos.DrawLine(c, n);
    //         Gizmos.DrawLine(c + up, c);
    //         Gizmos.DrawLine(c + up, n);
    //     }
    // }
}
