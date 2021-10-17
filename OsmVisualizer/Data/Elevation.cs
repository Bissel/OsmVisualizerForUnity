using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Math;
using OsmVisualizer.Mesh;
using OsmVisualizer.Visualisation.Components;
using UnityEngine;

namespace OsmVisualizer.Data
{

    public abstract class Elevation : WayInterpretation
    {
        public const float RampLength = 40f;
        public abstract float GetBaseHeightOffset();
        protected Elevation(string id, Type wayType) : base(id, wayType)
        {
        }
    }

    public class Tunnel : Elevation
    {
        public const float BaseHeightOffset = -5.5f;
        
        public override float GetBaseHeightOffset() => BaseHeightOffset;

        private readonly LaneCollection _lc;
        
        public Types.Spline OutlineLeft; 
        public Types.Spline OutlineRight;

        public bool OutlineLeftIsReversed;
        public bool OutlineRightIsReversed;

        public float OffsetLeft;
        public float OffsetRight;

        public float BaseHeight;
        public float InnerHeight;
        
        public Tunnel(string id, LaneCollection lc) : base(id, Type.TUNNEL)
        {
            _lc = lc;
            lc.Elevation = this;
            if (lc.OtherDirection is { } olc)
            {
                olc.Elevation = this;
                OutlineLeft = olc.OutlineRight;
                OutlineLeftIsReversed = true;
            }
            else
            {
                OutlineLeft = lc.OutlineLeft;
            }

            OutlineRight = lc.OutlineRight;

            lc.PrevIntersection?.SetElevation(this, !lc.PrevIntersection.IsTunnel);
            lc.NextIntersection?.SetElevation(this, !lc.NextIntersection.IsTunnel);

            if (!float.IsNaN(lc.Characteristics.MaxHeight))
            {
                InnerHeight = lc.Characteristics.MaxHeight;
                BaseHeight = (System.Math.Abs(lc.Characteristics.Layer) - 1) * BaseHeightOffset
                             - (lc.Characteristics.MaxHeight + .3f);
            }
            else
            {
                InnerHeight = BaseHeightOffset;
                BaseHeight = System.Math.Max(1, System.Math.Abs(lc.Characteristics.Layer)) 
                             * BaseHeightOffset - .3f;
            }
            
            SetOffsetLeft(lc);
            SetOffsetRight(lc);
        }

        public void MergeTunnel(Tunnel other, bool left, bool otherLeft)
        {
            BaseHeight = Mathf.Max(BaseHeight, other.BaseHeight);

            other._lc.Elevation = this;
            other._lc.PrevIntersection?.SetElevation(this, !other._lc.PrevIntersection.IsTunnel);
            other._lc.NextIntersection?.SetElevation(this, !other._lc.NextIntersection.IsTunnel);
            
            if (left)
            {
                if (otherLeft)
                {
                    OutlineLeft = other.OutlineRight;
                    OutlineLeftIsReversed = other.OutlineRightIsReversed;
                    OffsetLeft = other.OffsetRight;
                }
                else
                {
                    OutlineLeft = other.OutlineLeft;
                    OutlineLeftIsReversed = other.OutlineLeftIsReversed;
                    OffsetLeft = other.OffsetLeft;
                }
            }
            else
            {
                if (otherLeft)
                {
                    OutlineRight = other.OutlineRight;
                    OutlineRightIsReversed = other.OutlineRightIsReversed;
                    OffsetRight = other.OffsetRight;
                }
                else
                {
                    OutlineRight = other.OutlineLeft;
                    OutlineRightIsReversed = other.OutlineLeftIsReversed;
                    OffsetRight = other.OffsetLeft;
                }
            }
        }

        private void SetOffsetLeft(LaneCollection lc)
        {
            OffsetLeft = lc.OtherDirection?.GetOffsetRight() ?? lc.GetOffsetLeft();

            // bool cycleL, sidewalkL;
            //
            // if(lc.OtherDirection is { } olc)
            // {
            //     cycleL = !olc.Characteristics.CyclewayIsShared && olc.Characteristics.CyclewayRight != null;
            //     sidewalkL = !olc.Characteristics.SidewalkIsSeparate && olc.Characteristics.SidewalkRight != null;
            // }
            // else
            // {
            //     cycleL = !lc.Characteristics.CyclewayIsShared && lc.Characteristics.CyclewayLeft != null;
            //     sidewalkL = !lc.Characteristics.SidewalkIsSeparate && lc.Characteristics.SidewalkLeft != null;
            // }
            //
            // OffsetLeft = Sidewalks.GuttersWidth + Sidewalks.SidewalkHeight
            //              + (cycleL ? Sidewalks.CyclewayDividerWidth + Sidewalks.CyclewayWidth : 0f)
            //              + (cycleL && sidewalkL ? Sidewalks.CyclewayDividerWidth : 0f)
            //              + (sidewalkL ? Sidewalks.SidewalkWidth : 0f);
        }
        
        private void SetOffsetRight(LaneCollection lc)
        {
            OffsetRight = lc.GetOffsetRight();
            // var cycleR = !lc.Characteristics.CyclewayIsShared && lc.Characteristics.CyclewayRight != null;
            // var sidewalkR = !lc.Characteristics.SidewalkIsSeparate && lc.Characteristics.SidewalkRight != null;
            //
            // OffsetRight = Sidewalks.GuttersWidth
            //               + (cycleR ? Sidewalks.CyclewayDividerWidth + Sidewalks.CyclewayWidth : 0f)
            //               + (cycleR && sidewalkR ? Sidewalks.CyclewayDividerWidth : 0f)
            //               + (sidewalkR ? Sidewalks.SidewalkWidth : 0f);
        }

    }
    
    public class BridgeSupport : Elevation
    {
        public override float GetBaseHeightOffset() => 0f;
        
        public readonly string ElevationType;
        public readonly Vector2[] Points;
        public readonly long[] Nodes;
        
        public BridgeSupport(string id, string type, Vector2[] points, long[] nodes) : base(id, Type.BRIDGE_SUPPORT)
        {
            ElevationType = type;
            
            points = points.ToList().Distinct().ToArray();
            
            var clockwise = points.IsOrientationClockwise();
            Points = clockwise ? points : points.Reverse().ToArray();
            Nodes = clockwise ? nodes : nodes.Reverse().ToArray();
        }
    }
    
    public class Bridge : Elevation
    {
        public const float BaseHeightOffset = 5.5f;
        public override float GetBaseHeightOffset() => BaseHeightOffset;
        
        public readonly string ElevationType;
        public readonly Vector2[] Points;
        public Area Area { get; private set; }
        public readonly List<long> Nodes;
        public readonly int Layer;

        public readonly float BaseHeight;
        private readonly List<long> _intersectionNodes = new List<long>();
        private Dictionary<long, Bridge> _adjacentBridge;

        private readonly List<LaneCollection> _laneCollections = new List<LaneCollection>();
        private readonly List<Intersection> _innerIntersections = new List<Intersection>();
        private readonly List<Intersection> _rampIntersections = new List<Intersection>();
        
        public List<Area> Ramps { get; private set; }
        
        public Bridge(string id, string type, Vector2[] points, IEnumerable<long> nodes, int layer, float height = float.NaN) : base(id, Type.BRIDGE)
        {
            ElevationType = type;
            Layer = layer;
            
            points = points.ToList().Distinct().ToArray();
            
            var clockwise = points.IsOrientationClockwise();
            Points = clockwise ? points : points.Reverse().ToArray();
            Nodes = clockwise ? nodes.ToList() : nodes.Reverse().ToList();
            
            BaseHeight = float.IsNaN(height) ? BaseHeightOffset * System.Math.Abs(Layer) : height;
        }

        public void SetIntersectionNodes(MapTile tile)
        {
            var tileIntersectionNodes = tile.Intersections;
            // _intersectionNodes.AddRange(Nodes.Where(node => tileIntersectionNodes.ContainsKey(node)));

            var bounds = new SimpleBounds(Points);

            foreach (var inter in tileIntersectionNodes.Values)
            {
                if(inter == null)
                    continue;
                
                if (Nodes.Contains(inter.Node))
                {
                    _intersectionNodes.Add(inter.Node);
                    _rampIntersections.Add(inter);
                    continue;
                }

                if (!inter.IsBridge || !bounds.Intersects(inter.Bounds) || !bounds.Intersects(inter.Points) || !inter.Bounds.Intersects(Points))
                    continue;
                
                // @todo proper check (are points of intersection in bridge-polygon)

                _intersectionNodes.Add(inter.Node);
                
                if (inter.Points.Any(p => !p.IsInBounds(bounds.Min, bounds.Max)))
                {
                    _rampIntersections.Add(inter);
                }
                else
                {
                    _innerIntersections.Add(inter);
                }
            }
            
            _innerIntersections.ForEach(i => i.SetElevation(this));
            _rampIntersections.ForEach(i => i.SetElevation(this, true));
            
            foreach (var inter in _innerIntersections)
            {
                inter.LanesIn.ForEach(lc => AddLaneCollection(lc));
                inter.LanesOut.ForEach(lc => AddLaneCollection(lc));
            }

            foreach (var inter in _rampIntersections)
            {
                inter.LanesIn.ForEach(lc =>
                {
                    if(lc.PrevIntersection == null || lc.PrevIntersection.Elevation != null)
                        AddLaneCollection(lc);
                });
                inter.LanesOut.ForEach(lc =>
                {
                    if(lc.NextIntersection == null || lc.NextIntersection.Elevation != null)
                        AddLaneCollection(lc);
                });
            }

            _laneCollections.ForEach(lc => lc.Elevation = this);
        }

        private bool AddLaneCollection(LaneCollection lc, bool other = true)
        {
            if (_laneCollections.Contains(lc))
                return false;
            
            _laneCollections.Add(lc);
            if (other && lc.OtherDirection != null)
                AddLaneCollection(lc.OtherDirection, false);

            return true;
        }

        public void SetAdjacentBridges(IEnumerable<Bridge> bridges)
        {
            _adjacentBridge = new Dictionary<long, Bridge>();
                        
            foreach (var bridge in bridges)
            {
                if (bridge._adjacentBridge == null)
                    continue;

                foreach (var iNode in bridge._intersectionNodes)
                {
                    if (!_intersectionNodes.Contains(iNode)) continue;
                    
                    if(!_adjacentBridge.ContainsKey(iNode)) _adjacentBridge.Add(iNode, bridge);
                    if(!bridge._adjacentBridge.ContainsKey(iNode)) bridge._adjacentBridge.Add(iNode, this);
                }
            }
        }

        public void SetHeight()
        {
            var nodeCount = Nodes.Count;
            var elevation = new List<float>(nodeCount);
            for (var i = 0; i < nodeCount; i++)
            {
                var node = Nodes[i];
                var height = BaseHeight;
                
                if (_adjacentBridge.ContainsKey(node))
                {
                    height = (_adjacentBridge[node].BaseHeight + BaseHeight) * .5f;
                } 
                // else if (_intersectionNodes.Contains(node))
                // {
                //     height = (0 + _baseHeight) * .5f;
                // }
                
                elevation.Add(height);
            }
            
            while(Points.Length > elevation.Count)
                elevation.Add(elevation[0]);
            
            
            Area = new Area(Points, elevation);
            
            // @todo create ramps
            // Ramps = new List<Area>();
            //
            // const float rampLengthPerHeight = RampLength / BaseHeightOffset;
            // var usedNodes = new List<long>();
            //
            // foreach (var inter in _rampIntersections)
            // {
            //     if (usedNodes.Contains(inter.Node))
            //         continue;
            //     
            //     GenerateRamp(inter, nodeCount, rampLengthPerHeight, ref usedNodes);
            // }

            // foreach (var lc in _laneCollections)
            // {
            //     GenerateRamp(lc, nodeCount, rampLengthPerHeight, +1, ref usedNodes);
            //     GenerateRamp(lc, nodeCount, rampLengthPerHeight, -1, ref usedNodes);
            // }
            
        }

        // private void GenerateRamp(LaneCollection lc, int nodeCount, float rampLengthPerHeight, int direction, ref List<long> usedNodes)
        private void GenerateRamp(Intersection inter, int nodeCount, float rampLengthPerHeight, ref List<long> usedNodes)
        {
            var node = inter.Node;
            // var node = direction > 0 ? lc.Id.StartNode : lc.Id.EndNode;
            // if (usedNodes.Contains(node))
            //     return;
            
            var nodeIndex = Nodes.IndexOf(node);
            if (nodeIndex < 0)
                return;

            var lc = inter.LanesIn.FirstOrDefault(l => l.Elevation == this) 
                             ?? inter.LanesOut.FirstOrDefault(l => l.Elevation == this);

            if (lc == null)
                return;
            
            var forward = lc.PrevIntersection == inter 
                ? (lc.GetPointLS0() - lc.GetPointRS0()).normalized.Rotate(90)
                : (lc.GetPointRE0() - lc.GetPointLE0()).normalized.Rotate(90);

            // var forward = direction > 0 
            //     ? (lc.GetPointLS0() - lc.GetPointRS0()).normalized.rotate(90)
            //     : (lc.GetPointRE0() - lc.GetPointLE0()).normalized.rotate(90);

            var forwardV3 = forward.ToVector3xz();

            var nodeIndices = new List<int>();
            var nodeIndicesR = new List<int>();

            var b = Area[nodeIndex]; 
            for (var i = 1; i < nodeCount - 1; i++)
            {
                var index = (nodeIndex - i + nodeCount) % nodeCount;
                var a = Area[index];

                if (Mathf.Abs(Vector3.SignedAngle(a - b, -forwardV3, Vector3.up)) < 45)
                    break;
                
                nodeIndices.Add(index);
                b = a;
            }
            
            b = Area[nodeIndex]; 
            for (var i = 1; i < nodeCount - 1; i++)
            {
                var index = (nodeIndex + i) % nodeCount;
                var a = Area[index];

                if (Mathf.Abs(Vector3.SignedAngle(a - b, -forwardV3, Vector3.up)) < 45)
                    break;
                
                nodeIndicesR.Add(index);
                b = a;
            }

            nodeIndices.Reverse();
            nodeIndices.Add(nodeIndex);
            nodeIndices.AddRange(nodeIndicesR);

            var points = new List<Vector3>();
            
            foreach (var i in nodeIndices)
            {
                usedNodes.Add(Nodes[i]);
                points.Add(Area[i]);    
            }

            nodeIndices.Reverse();
            
            foreach (var i in nodeIndices)
            {
                var p = Area[i];
                points.Add((p.ToVector2xz() + rampLengthPerHeight * p.y * forward).ToVector3xz());
            }

            points.Reverse();

            Ramps.Add(new Area(points));




            // var a = Area[(nodeIndex - 1 + nodeCount) % nodeCount];
            // var b = Area[nodeIndex];
            // var c = Area[(nodeIndex + 1) % nodeCount];
            //
            // var d = (c.ToVector2xz() + rampLength * c.y * forward).ToVector3xz();
            // var e = (b.ToVector2xz() + rampLength * b.y * forward).ToVector3xz();
            // var f = (a.ToVector2xz() + rampLength * a.y * forward).ToVector3xz();
            //
            // if (Mathf.Abs(Vector3.SignedAngle(a - b, forwardV3, Vector3.up)) > 45)
            // {
            //     Ramps.Add(new Area(new[] {e, d, c, b}));
            // }
            // else if (Mathf.Abs(Vector3.SignedAngle(c - b, forwardV3, Vector3.up)) > 45)
            // {
            //     Ramps.Add(new Area(new[] {f, e, b, a}));
            // }
            // else
            // {
            //     Ramps.Add(new Area(new[] {f, e, d, c, b, a}));
            // }
        }
    }
    
}