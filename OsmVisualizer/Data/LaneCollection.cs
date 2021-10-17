
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using OsmVisualizer.Visualisation.Components;
using UnityEngine;

namespace OsmVisualizer.Data
{

    // public class LaneCollectionOriginal : LaneCollection
    // {
    //     public List<MapData.LaneId> SubCollections = new List<MapData.LaneId>();
    //     public LaneCollectionOriginal(long[] nodes) : base(null, null, null, null, null, nodes)
    //     {
    //     }
    //
    //     public void AddSubCollection(LaneCollection lc)
    //     {
    //         SubCollections.Add(lc.Id);
    //         lc.OriginalLaneCollection = Id;
    //     }
    // }

    public class LaneCollection
    {
        /// <summary>
        /// Can be null if underling Road is OneWay
        /// </summary>
        public LaneCollection OtherDirection = null;

        public readonly MapData.LaneId Id;
        public readonly IReadOnlyList<Vector2> Points;
        public readonly IReadOnlyList<long> Nodes;
        

        public float ApproxLength { get; private set; }
        
        public Types.Spline OutlineLeft { get; private set; }
        public Types.Spline OutlineRight { get; private set; }
        
        /// <summary>
        /// Ordered Left to Right
        /// </summary>
        public readonly Lane[] Lanes;

        public readonly WayCharacteristics Characteristics;

        public Intersection PrevIntersection = null;
        public Intersection NextIntersection = null;

        public bool IsDummy;
        public bool IsRemoved;

        public Elevation Elevation;

        public LaneCollection(
            WayCharacteristics wayCharacteristics, 
            IReadOnlyList<Vector2[]> lanes, 
            IReadOnlyList<Direction[]> lanesDirections, 
            Types.Spline outlineLeft, 
            Types.Spline outlineRight, 
            IReadOnlyList<long> nodes,
            IReadOnlyList<Vector2> points,
            bool isDummy,
            MapData.LaneId id = null 
        )
        {
            Characteristics = wayCharacteristics;

            OutlineLeft = outlineLeft;
            OutlineRight = outlineRight;
            Points = points;
            Nodes = nodes;
            
            IsDummy = isDummy;

            Id = id ?? new MapData.LaneId(
                nodes, 
                wayCharacteristics is RailWayCharacteristics 
                    ? MapData.LaneType.RAILWAY 
                    : wayCharacteristics.Type == "path" 
                        ? MapData.LaneType.PATHWAY
                        : MapData.LaneType.HIGHWAY
            );

            Lanes = new Lane[lanes.Count];
            for (var i = 0; i < lanes.Count; i++)
            {
                Lanes[i] = new Lane(
                    this, 
                    lanes[i], 
                    lanesDirections != null && i < lanesDirections.Count ? lanesDirections[i] : null
                );
            }


            // if (Characteristics.IsBridge)
            // {
            //     HeightOffsetBase = HeightOffsetBridge * System.Math.Abs(Characteristics.Layer);
            // }
            // else if (Characteristics.IsTunnel)
            // {
            //     // @todo set 0 if over is bridge
            //     HeightOffsetBase = HeightOffsetTunnel * System.Math.Abs(Characteristics.Layer);
            // }
            // else
            // {
            //     HeightOffsetBase = 0f;
            // }
            //
            // HasHeightOffset = Characteristics.IsBridge || Characteristics.IsTunnel;
        }

        private float _offsetRight = float.NaN;
        private float _offsetLeft = float.NaN;
        public float GetOffsetRight()
        {
            if (float.IsNaN(_offsetRight))
                _offsetRight = GetOffset(
                    !Characteristics.CyclewayIsShared && Characteristics.CyclewayRight != null, 
                    !Characteristics.SidewalkIsSeparate && Characteristics.SidewalkRight != null
                );

            return _offsetRight;
        }
        
        public float GetOffsetLeft()
        {
            if (float.IsNaN(_offsetLeft))
            {
                _offsetLeft = OtherDirection != null 
                    ? 0f 
                    : GetOffset(
                        !Characteristics.CyclewayIsShared && Characteristics.CyclewayLeft != null,
                        !Characteristics.SidewalkIsSeparate && Characteristics.SidewalkLeft != null
                    );
            }

            return _offsetLeft;
        }

        public List<Vector3> GetOutlineRightPoints(bool useOffset = false)
        {
            var offset = GetOffsetRight();
            if (!useOffset || offset < 0.01f)
                return OutlineRight;

            var outline = new List<Vector3>();
                
            for (var i = 0; i < OutlineRight.Count; i++)
            {
                var forward = OutlineRight.Forward[i];
                outline.Add(OutlineRight[i] + (forward.ToVector2xz().Rotate(-90) * offset).ToVector3xz());
            }

            return outline;
        }
        
        public List<Vector3> GetOutlineLeftPoints(bool useOffset = false)
        {
            var offset = GetOffsetLeft();
            if (!useOffset || offset < 0.01f)
                return OutlineLeft;

            var outline = new List<Vector3>();
                
            for (var i = 0; i < OutlineLeft.Count; i++)
            {
                var forward = OutlineLeft.Forward[i];
                outline.Add(OutlineLeft[i] + (forward.ToVector2xz().Rotate(90) * offset).ToVector3xz());
            }

            return outline;
        }

        private float GetOffset(bool hasCycle, bool hasSidewalk)
        {
            return Sidewalks.GuttersWidth + Sidewalks.SidewalkHeight
                      + (hasCycle ? Sidewalks.CyclewayDividerWidth + Sidewalks.CyclewayWidth : 0f)
                      + (hasCycle && hasSidewalk ? Sidewalks.CyclewayDividerWidth : 0f)
                      + (hasSidewalk ? Sidewalks.SidewalkWidth : 0f);
        }

        private void SetLenght()
        {
            ApproxLength = (OutlineLeft.TotalLength + OutlineRight.TotalLength) * .5f;
            // HeightOffsetRampLength = Mathf.Clamp(ApproxLength * .33f, HeightOffsetRampLengthMin, HeightOffsetRampLengthMax);
        }

        public void SetE(Vector2 left, Vector2 right)
        {
            if(OutlineLeft.Count < 2 && OutlineRight.Count < 2)
                return;
            
            var oldOffset = OutlineRight.Count;
            var offset = oldOffset - 1;
            
            // var oldOutlineLeftFw = OutlineLeft.Forward[offset];
            // var oldOutlineRightFw = OutlineRight.Forward[offset];

            while (offset > 0 
                   && Vector3.Cross(
                       OutlineLeft[offset] - OutlineRight[offset],
                       (left - OutlineRight[offset].ToVector2xz()).ToVector3xz()
                   ).y < -0.0000001f
                   // && Vector3.Cross(
                   //  (OutlineLeft.Points[offset] - OutlineRight.Points[offset]).ToVector3xz(),
                   //  (right - OutlineLeft.Points[offset]).ToVector3xz()
                   //  ).y > 0.0000001f
                )
                offset--;

            offset++;
            if (offset > oldOffset - 1)
                offset = oldOffset - 1;

            if (offset < 2)
            {
                // OutlineLeft = new Types.Spline(new []{left});
                // OutlineRight = new Types.Spline(new []{right});
                // ApproxLength = 0f;
                // return;
                offset = 1;
            }
            
            var a0 = left.ToVector3xz();
            var av = (right - left).ToVector3xz();
            
            foreach (var lane in Lanes)
            {
                var b0 = lane.Points[offset].ToVector3xz();
                var bv = lane.Points[offset - 1].ToVector3xz() - b0;
                if (Math.Math.LineLineIntersection(out var intersection, a0, av, b0, bv))
                    lane.Points[offset] = intersection.ToVector2xz();
                
                if(offset < oldOffset)
                    lane.Points = lane.Points.Take(offset + 1).ToList();
            }

            OutlineLeft[offset] = left.ToVector3xz();
            OutlineRight[offset] = right.ToVector3xz();
            
            if (offset < oldOffset)
            {
                OutlineLeft = new Types.Spline(OutlineLeft.Take(offset + 1).ToList());
                OutlineRight = new Types.Spline(OutlineRight.Take(offset + 1).ToList());
            }
            else
            {
                OutlineLeft.RecalculateTotalLenght();
                OutlineRight.RecalculateTotalLenght();
            }
            
            // if (
            //     Mathf.Abs(Vector3.SignedAngle(oldOutlineLeftFw, OutlineLeft.Forward[OutlineLeft.Forward.Count -1], Vector3.up)) > 90
            //     || Mathf.Abs(Vector3.SignedAngle(oldOutlineRightFw, OutlineRight.Forward[OutlineRight.Forward.Count -1], Vector3.up)) > 90
            // )
            // {
            //     OutlineLeft = new Types.Spline(new List<Vector3>());
            //     OutlineRight = new Types.Spline(new List<Vector3>());
            //     foreach (var lane in Lanes)
            //     {
            //         lane.Points = new List<Vector2>();
            //     }
            // }
            
            SetLenght();
        }

        public void SetS(Vector2 left, Vector2 right)
        {
            if(OutlineLeft.Count < 2 && OutlineRight.Count < 2)
                return;
            
            var outlineMax = OutlineRight.Count - 1;
            var offset = 0;
            
            // var oldOutlineLeftFw = OutlineLeft.Forward[offset];
            // var oldOutlineRightFw = OutlineRight.Forward[offset];

            while (offset < outlineMax && Vector3.Cross(
                OutlineLeft[offset] - OutlineRight[offset],
                (left - OutlineRight[offset].ToVector2xz()).ToVector3xz()
            ).y > 0.0000001f)
                offset++;

            if (offset > outlineMax - 1)
            {
                // OutlineLeft = new Types.Spline(new []{left});
                // OutlineRight = new Types.Spline(new []{right});
                // ApproxLength = 0f;
                // return;
                offset = outlineMax - 1;
            }
            
            offset--;
            if (offset < 0)
                offset = 0;
                    
            OutlineLeft[offset] = left.ToVector3xz();
            OutlineRight[offset] = right.ToVector3xz();
            
            var a0 = left.ToVector3xz();
            var av = (right - left).ToVector3xz();
            
            foreach (var lane in Lanes)
            {
                var b0 = lane.Points[offset].ToVector3xz();
                var bv = lane.Points[offset + 1].ToVector3xz() - b0;
                if (Math.Math.LineLineIntersection(out var intersection, a0, av, b0, bv))
                    lane.Points[offset] = intersection.ToVector2xz();
                
                if(offset > 0)
                    lane.Points = lane.Points.Skip(offset).ToList();
            }
            
            if (offset > 0)
            {
                OutlineLeft = new Types.Spline(OutlineLeft.Skip(offset).ToList());
                OutlineRight = new Types.Spline(OutlineRight.Skip(offset).ToList());
            }
            else
            {
                OutlineLeft.RecalculateTotalLenght();
                OutlineRight.RecalculateTotalLenght();
            }

            // if (
            //     Mathf.Abs(Vector3.SignedAngle(oldOutlineLeftFw, OutlineLeft.Forward[0], Vector3.up)) > 90
            //     || Mathf.Abs(Vector3.SignedAngle(oldOutlineRightFw, OutlineRight.Forward[0], Vector3.up)) > 90
            // )
            // {
            //     OutlineLeft = new Types.Spline(new List<Vector3>());
            //     OutlineRight = new Types.Spline(new List<Vector3>());
            //     foreach (var lane in Lanes)
            //     {
            //         lane.Points = new List<Vector2>();
            //     }
            // }
            
            SetLenght();
        }

        public void SetNextLanes(List<LaneCollection> left, List<LaneCollection> through, List<LaneCollection> right)
        {
            var offsetCollectionLeft = 0;
            var offsetCollectionThrough = 0;
            var offsetCollectionRight = 0;

            var offsetLeft = 0;
            var offsetThrough = 0;
            var offsetRight = 0;

            // forward
            for (var index = 0; index < Lanes.Length; index++)
            {
                var lane = Lanes[index];
                if (lane.Directions == null || lane.Directions.Length == 0)
                {
                    AddLaneDirection(lane, ref offsetLeft, ref offsetCollectionLeft, left);
                    AddLaneDirection(lane, ref offsetThrough, ref offsetCollectionThrough, through);
                    AddLaneDirection(lane, ref offsetRight, ref offsetCollectionRight, right);
                    continue;
                }

                foreach (var dir in lane.Directions)
                {
                    switch (dir)
                    {
                        case Direction.REVERSE:
                            if (OtherDirection != null && OtherDirection.Lanes.Length > 0)
                            {
                                lane.Next.Add(OtherDirection.Lanes[0]);
                            }
                            break;
                        
                        case Direction.LEFT:
                            if (left.Count == 0)
                            {
                                var nextLane = GetNextLane(ref offsetThrough, ref offsetCollectionThrough, through);
                                if (nextLane == null || !(
                                    nextLane.Directions == null || nextLane.Directions.Contains(Direction.LEFT) || nextLane.Directions.Contains(Direction.NONE)))
                                    break;

                                AddLaneDirection(lane, ref offsetThrough, ref offsetCollectionThrough, through);
                                break;
                            }
                            
                            AddLaneDirection(lane, ref offsetLeft, ref offsetCollectionLeft, left);
                            break;
                        
                        case Direction.THROUGH:
                            AddLaneDirection(lane, ref offsetThrough, ref offsetCollectionThrough, through);
                            break;
                        
                        case Direction.RIGHT:
                            if (right.Count == 0)
                            {
                                var nextLane = GetNextLane(ref offsetThrough, ref offsetCollectionThrough, through);
                                if (nextLane == null || !(nextLane.Directions == null || nextLane.Directions.Contains(Direction.RIGHT) || nextLane.Directions.Contains(Direction.NONE)))
                                    break;

                                AddLaneDirection(lane, ref offsetThrough, ref offsetCollectionThrough, through);
                                break;
                            }
                            
                            AddLaneDirection(lane, ref offsetRight, ref offsetCollectionRight, right);
                            break;
                        
                        case Direction.NONE:
                            AddLaneDirection(lane, ref offsetLeft, ref offsetCollectionLeft, left);
                            AddLaneDirection(lane, ref offsetThrough, ref offsetCollectionThrough, through);
                            AddLaneDirection(lane, ref offsetRight, ref offsetCollectionRight, right);

                            break;
                    }
                }
                
            }
            
            // backward
            if (left.Count == 0 && right.Count == 0 && through.Count == 1 && through[0].Lanes.Length > offsetThrough)
            {
                var inLane = Lanes[Lanes.Length - 1];
                for (var i = offsetThrough; i < through[0].Lanes.Length; i++)
                {
                    inLane.Next.Add(through[0].Lanes[i]);
                }
            }
        }

        private static Lane GetNextLane(ref int offset, ref int offsetCollection, List<LaneCollection> collection)
        {
            if (collection.Count == 0)
                return null;
                            
            if (offset == collection[offsetCollection].Lanes.Length && offsetCollection + 1 < collection.Count)
            {
                offset = 0;
                offsetCollection++;
            }
                            
            return collection[offsetCollection].Lanes.Length == 0 
                ? null 
                : collection[offsetCollection].Lanes[offset];
        }
        
        private static void AddLaneDirection(Lane lane, ref int offset, ref int offsetCollection, List<LaneCollection> collection)
        {
            var next = GetNextLane(ref offset, ref offsetCollection, collection);
            if (next == null)
                return;
            
            lane.Next.Add(next);
            if (offset != collection[offsetCollection].Lanes.Length - 1)
                offset++;

        }

        private float _startHeightOffset;
        private float _endHeightOffset;

        public void SetHeightOffset()
        {
            if (IsDummy)
                return;

            if (OutlineLeft.Count < 2 && OutlineRight.Count < 2)
                return;

            if (Id.Type == MapData.LaneType.RAILWAY)
            {
                // @todo ramp
                var offset = System.Math.Abs(Characteristics.Layer) * (Characteristics.IsBridge
                    ? Bridge.BaseHeightOffset
                    : Characteristics.IsTunnel
                        ? Tunnel.BaseHeightOffset
                        : 0f);

                _startHeightOffset = offset + Characteristics.CustomHeightOffset;
                _endHeightOffset = offset + Characteristics.CustomHeightOffset;
                
                return;
            }

            _startHeightOffset = (PrevIntersection?.HeightOffset ?? 0f) + Characteristics.CustomHeightOffset;
            _endHeightOffset = (NextIntersection?.HeightOffset ?? 0f) + Characteristics.CustomHeightOffset;

            if (Characteristics.IsBridge && PrevIntersection == null && NextIntersection != null )
                _startHeightOffset = _endHeightOffset;

            if (Characteristics.IsBridge && PrevIntersection != null && NextIntersection == null )
                _endHeightOffset = _startHeightOffset;

            var hasStartHeightOffset = Mathf.Abs(_startHeightOffset) > .00001f;
            var hasEndHeightOffset = Mathf.Abs(_endHeightOffset) > .00001f;

            if (!hasStartHeightOffset && !hasEndHeightOffset)
                return;

            var heightDiff = System.Math.Abs(_startHeightOffset - _endHeightOffset);
            if (heightDiff < 0.01f)
            {
                OutlineLeft.SetHeight(_startHeightOffset);
                OutlineRight.SetHeight(_startHeightOffset);
                return;
            }

            // used outlineLeft.TotalLength to match the length for lc and its OtherDirection
            var rampLength = Mathf.Min(Elevation.RampLength, OutlineLeft.TotalLength);
            var heights = new float[OutlineLeft.Count];

            var minHeight = Mathf.Min(_startHeightOffset, _endHeightOffset);
            var maxHeight = Mathf.Max(_startHeightOffset, _endHeightOffset);

            var totalLength = 0f;

            if (_endHeightOffset > _startHeightOffset)
            {
                for (var i = OutlineLeft.Count - 1; i >= 0; i--)
                {
                    heights[i] = Mathf.Lerp(maxHeight, minHeight, totalLength / rampLength);
                    totalLength += OutlineLeft.Length[i];
                }
            }
            else
            {
                for (var i = 0; i < OutlineLeft.Count; i++)
                {
                    totalLength += OutlineLeft.Length[i];
                    heights[i] = Mathf.Lerp(maxHeight, minHeight, totalLength / rampLength);
                }
            }

            OutlineLeft.SetHeight(heights);
            OutlineRight.SetHeight(heights);
        }

        public Vector2 GetPointRS0() => OutlineRight[0].ToVector2xz();
        public Vector2 GetPointRS1() => OutlineRight[1].ToVector2xz();
        public Vector2 GetPointRE0() => OutlineRight[OutlineRight.Count - 1].ToVector2xz(); //.Points[OutlineRight.Points.Count - 1];
        public Vector2 GetPointRE1() => OutlineRight[OutlineRight.Count - 2].ToVector2xz(); //.Points[OutlineRight.Points.Count - 2];
        public Vector2 GetPointLS0() => OutlineLeft[0].ToVector2xz();
        public Vector2 GetPointLS1() => OutlineLeft[1].ToVector2xz();
        public Vector2 GetPointLE0() => OutlineLeft[OutlineLeft.Count - 1].ToVector2xz();//.Points[OutlineLeft.Points.Count - 1];
        public Vector2 GetPointLE1() => OutlineLeft[OutlineLeft.Count - 2].ToVector2xz();//.Points[OutlineLeft.Points.Count - 2];
        
    }
    

}