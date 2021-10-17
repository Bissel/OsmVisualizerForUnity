using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using OsmVisualizer.Data.Characteristics;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Data.Utils;
using UnityEngine;

namespace OsmVisualizer.Data
{
    public class Intersection
    {
        public enum Priority
        {
            RightBeforeLeft,
            Stop,
            GiveWay,
            RightOfWay
        }
        
        public readonly long Node;
        public Vector2 Center { get; private set; }
        public float Radius { get; private set; }
        
        public List<LaneCollection> LanesIn { get; private set; }
        public List<LaneCollection> LanesOut { get; private set; }

        public List<Vector2> Points { get; private set; }
        public Area Area { get; private set; }
        public List<Vector2> OutlinePoints { get; private set; }
        public List<float> Angles { get; private set; }

        public List<int> Priorities { get; private set; }
        public List<Priority> PrioritiesRelative { get; private set; }

        private List<int> _orientations;
        public int TotalLaneCollectionCount { get; private set; }

        private readonly MapTile tile;

        public SimpleBounds Bounds;
        public Elevation Elevation;
        public bool ElevationRamp;
        
        public string Surface { get; private set; }
        public bool IsBridge { get; private set; }
        public bool IsTunnel { get; private set; }
        public bool IsLit { get; private set; }
        
        public bool HasTrafficLights { get; private set; }
        
        public bool IsRoadRoadConnection { get; private set; }

        public float HeightOffset { get; private set; }

        public MapData.LaneType mainLaneType = MapData.LaneType.HIGHWAY;
        public bool hasSingleLaneType;

        public class RoadMapping
        {
            public readonly LaneCollection Lc;
            public Vector2 ForwardRotation;
            public readonly Vector2 Point;
            public readonly bool IsIn;
            
            public RoadMapping(LaneCollection lc, Vector2 forwardRotation, Vector2 point, bool isIn = false)
            {
                Lc = lc;
                ForwardRotation = forwardRotation;
                Point = point;
                IsIn = isIn;
            }
        }

        private List<RoadMapping> _roadMapping; 

        public Intersection(
            long node, 
            Vector2 pos, 
            List<LaneCollection> lanesIn, 
            List<LaneCollection> lanesOut, 
            List<Vector2> points, 
            List<int> orientations, 
            List<float> angles, 
            MapTile tile,
            bool hasTrafficLights
        )
        {
            Node = node;
            Center = pos;
            Angles = angles;
            Points = points;
            HasTrafficLights = hasTrafficLights;
            SetLanes(lanesIn ?? new List<LaneCollection>(), lanesOut ?? new List<LaneCollection>());

            HeightOffset = 0f;

            _orientations = orientations;
            this.tile = tile;

            _roadMapping = new List<RoadMapping>(LanesIn.Count + LanesOut.Count);

            SetAttributes();

            CalcRadius(Points);

            if (_orientations.Count == 0 || _orientations[0] < 0)
                return;

            for (var i = 0; i < _orientations.Count; i++)
            {
                var index = _orientations[i];
                if (index < 0)
                    continue;

                var point = i < Points.Count ? Points[i] : Vector2.zero;

                if (index < LanesIn.Count)
                {
                    var lc = LanesIn[index];
                    _roadMapping.Add(new RoadMapping(lc, (lc.GetPointLE0() - lc.GetPointLE1()).normalized, point, true));
                }
                else
                {
                    var lc = LanesOut[index - LanesIn.Count];
                    _roadMapping.Add(new RoadMapping(lc, (lc.GetPointLS1() - lc.GetPointLS0()).normalized, point));
                }
            }

        }

        private void SetRoadPriorities()
        {
            Priorities = new List<int>(LanesIn.Count);
            PrioritiesRelative = new List<Priority>(LanesIn.Count);
            
            LanesIn.ForEach(lc =>
            {
                if (lc.Characteristics is RailWayCharacteristics)
                {
                    Priorities.Add(10);
                    return;
                }

                switch (lc.Characteristics.Type)
                {
                    case "motorway":  Priorities.Add(5); break;
                    case "trunk":     Priorities.Add(4); break;
                    case "primary":   Priorities.Add(3); break;
                    case "secondary": Priorities.Add(2); break;
                    case "tertiary":  Priorities.Add(1); break;
                    
                    case "living_street": Priorities.Add(-1); break;
                    
                    default: Priorities.Add(0); break;
                }
            });
            
            var maxPriority = -1;
            var minPriority = int.MaxValue;
            
            foreach (var priority in Priorities)
            {
                if (priority > maxPriority)
                    maxPriority = priority;
                if (priority < minPriority)
                    minPriority = priority;
            }

            if (maxPriority == minPriority)
            {
                for (var i = 0; i < LanesIn.Count; i++)
                    PrioritiesRelative.Add(Priority.RightBeforeLeft);
                
                return;
            }

            var yieldSign = maxPriority - minPriority > 2 ? Priority.Stop : Priority.GiveWay;

            for (var i = 0; i < LanesIn.Count; i++)
                PrioritiesRelative.Add(Priorities[i] == maxPriority ? Priority.RightOfWay : yieldSign);
        }

        public bool IsHigherPriority(LaneCollection lc)
        {
            var index = LanesIn.IndexOf(lc);
            if (index < 0)
                return false;

            return Priorities[index] == Priorities.Max();
        }
        
        private float _roadWidth = float.NaN; 
        private float _roadWidthNoScale = float.NaN; 
        public float RoadWidth() => !float.IsNaN(_roadWidth) 
            ? _roadWidth
            : _roadWidth = Mathf.Max(
                LanesIn?.Count > 0 ? LanesIn.Max(lc => lc.Characteristics.Width + (lc.OtherDirection?.Characteristics.Width ?? 0f)) : 0f,
                LanesOut?.Count > 0 ? LanesOut.Max(lc => lc.Characteristics.Width + (lc.OtherDirection?.Characteristics.Width ?? 0f)) : 0f
            ) * .5f;
        
        public float RoadWidthNoScale() => !float.IsNaN(_roadWidthNoScale)
            ? _roadWidthNoScale
            :  _roadWidthNoScale = Mathf.Max(
                LanesIn?.Count > 0 ? LanesIn.Max(lc => lc.Characteristics.Width + (lc.OtherDirection?.Characteristics.Width ?? 0f) / lc.Characteristics.WidthScaling) : 0f,
                LanesOut?.Count > 0 ? LanesOut.Max(lc => lc.Characteristics.Width + (lc.OtherDirection?.Characteristics.Width ?? 0f) / lc.Characteristics.WidthScaling) : 0f
            ) * .5f;

        private float _maxAngleMultiplier = float.NaN;
        
        public float MaxAngleMultiplier()
        {
            return float.IsNaN(_maxAngleMultiplier)
                ? _maxAngleMultiplier = LanesIn.Aggregate(0f, (current, lc) => Mathf.Max(current, AngleMultiplier(lc)))
                : _maxAngleMultiplier;
        }

        public void SetLaneIntersectionBounds()
        {
            var pointsCount = Points.Count;
            if (pointsCount == 0)
                return;

            var offsetMin = RoadWidth() * 1.15f;
            var offset = Mathf.Max(RoadWidth() * .5f, 1.5f);
            
            if (IsRoadRoadConnection && LanesIn.Count > 0 && LanesOut.Count > 0)
            {
                if(LanesIn.Count == 1 && LanesOut.Count == 1)
                {
                    var lIn = LanesIn[0];
                    var lOut = LanesOut[0];
                    
                    offset = Mathf.Max( offsetMin,System.Math.Abs(lIn.Lanes.Length - lOut.Lanes.Length) * RoadWidth());
                }
                else if(LanesIn.Count > 1 && LanesOut.Count > 1)
                {
                    var lIn0 = LanesIn[0];
                    var lIn1 = LanesIn[1];
            
                    var lOut0 = lIn1.OtherDirection;
                    var lOut1 = lIn0.OtherDirection;
            
                    var perLaneOffset = RoadWidth() * 2f / (lIn0.Lanes.Length + lOut1.Lanes.Length);
            
                    var angleMultiplier = Vector2.Angle(
                        lIn0.OutlineRight.Forward[lIn0.OutlineRight.Forward.Count - 1],
                        lIn1.OutlineRight.Forward[lIn1.OutlineRight.Forward.Count - 1]
                    ) < 60f ? 3f : 1f;
                    
                    offset = Mathf.Max( offsetMin,(
                        System.Math.Abs(lIn0.Lanes.Length - lOut0.Lanes.Length)
                        + System.Math.Abs(lIn1.Lanes.Length - lOut1.Lanes.Length)
                    ) * perLaneOffset) * angleMultiplier;
                } 
            }
            else
            {
                offset += RoadWidth();
            }
            
            for (var i = 0; i < TotalLaneCollectionCount; i++)
            {
                var lc = GetNthLaneCollection(i);
                if(lc == null)
                    continue;

                var o = OrientationNumberByCount(i); 
                if (o < 0)
                    continue;
            
                var lcOtherDir = lc.OtherDirection != null;
                
                if (!IsNthLaneCollectionDirIn(i))
                {
                    if(lcOtherDir)
                        continue;
            
                    var lcOffset = offset * AngleMultiplier(lc);
                    
                    var pointIndex = 0;
                    var length = 0f;
                    var olCnt = System.Math.Min(
                        System.Math.Min(lc.OutlineRight.Count, lc.OutlineLeft.Count),
                        System.Math.Min(
                            System.Math.Min(lc.OutlineRight.Length.Count - 1, lc.OutlineLeft.Length.Count - 1), 
                            System.Math.Min(lc.OutlineRight.Forward.Count, lc.OutlineLeft.Forward.Count)
                        )
                    );
                    
                    for (; pointIndex < olCnt - 1 && length < lcOffset; pointIndex++)
                    {
                        length += .5f * (lc.OutlineLeft.Length[pointIndex + 1] + lc.OutlineRight.Length[pointIndex + 1]);
                    }
            
                    lcOffset -= length;
                    
                    var fwR = lc.OutlineRight.Forward[pointIndex].ToVector2xz();
                    var fwL = lc.OutlineLeft.Forward[pointIndex].ToVector2xz();
                    
                    var r0 = lc.OutlineRight[pointIndex].ToVector2xz();
                    var l0 = lc.OutlineLeft[pointIndex].ToVector2xz();
                    
                    var right = r0 + fwR * lcOffset;
                    var left = l0 + fwL * lcOffset;
                    
                    lc.SetS(left, right);
                    continue;
                }
            
                {
                    var lcOffset = offset * AngleMultiplier(lc);
                    
                    var pointIndex = 0;
                    var length = 0f;
                    
                    var olR = lc.OutlineRight;
                    var olL = lcOtherDir ? lc.OtherDirection.OutlineRight : lc.OutlineLeft;
                    
                    var olCnt = System.Math.Min(
                        System.Math.Min(olR.Count, olL.Count),
                        System.Math.Min(
                            System.Math.Min(olR.Length.Count - 1, olL.Length.Count - 1),
                            System.Math.Min(olR.Forward.Count, olL.Forward.Count)
                        )
                    );
                    
                    for (; pointIndex < olCnt - 1 && length < lcOffset; pointIndex++)
                    {
                        length += .5f * (
                            olR.Length[olR.Length.Count - pointIndex - 1] 
                            + olL.Length[lcOtherDir ? pointIndex + 1 : olL.Length.Count - pointIndex - 1]
                        );
                    }
            
                    lcOffset -= length;
                    
                    var fwR = olR.Forward[olR.Forward.Count - pointIndex - 1].ToVector2xz();
                    var fwL = (lcOtherDir 
                            ? -olL.Forward[pointIndex] 
                            : olL.Forward[olL.Forward.Count - pointIndex - 1]
                        ).ToVector2xz();
                    
                    var r0 = olR.Get(-pointIndex - 1).ToVector2xz();
                    var l0 = (lcOtherDir ? olL[pointIndex] : olL.Get(-pointIndex - 1)).ToVector2xz();
            
                    var right = r0 - fwR.normalized * lcOffset;
                    var left = l0 - fwL.normalized * lcOffset;
            
                    if (lcOtherDir)
                    {
                        var center = right + (left - right).normalized * lc.Characteristics.Width;
            
                        lc.OtherDirection.SetS(center, left);
                        lc.SetE(center, right);
                    }
                    else
                    {
                        lc.SetE(left, right);
                    }
            
                }
            
                var l = new List<LaneCollection>();
                var t = new List<LaneCollection>();
                var r = new List<LaneCollection>();
            
                var angleCurrent = Angles[i];
                for (var j = 0; j < LanesOut.Count; j++)
                {
                    if(!lc.Characteristics.SameSimpleType(LanesOut[j].Characteristics))
                        continue;
                    
                    var angle = (360 + Angles[j + LanesIn.Count] - angleCurrent) % 360 - 180;
                    if (angle < 135f && angle > 45f)
                    {
                        l.Add(LanesOut[j]);
                    }
                    else if (angle <= 45f && angle >= -45f)
                    {
                        t.Add(LanesOut[j]);
                    }
                    else if (angle < -45f && angle > -135f)
                    {
                        r.Add(LanesOut[j]);
                    }
                }
            
                lc.SetNextLanes(l, t, r);
            }

            var areaPoints = new List<Vector2>(TotalLaneCollectionCount * 2);
            OutlinePoints = new List<Vector2>(TotalLaneCollectionCount * 2);

            var roadMapping = new List<RoadMapping>();
            
            foreach (var index in _orientations)
            {
                var rot = Vector2.zero; // _roadMapping[index].ForwardRotation;
                
                var lc = GetNthLaneCollection(index);
                if (lc == null)
                    continue;


                if (index < LanesIn.Count)
                {
                    roadMapping.Add(new RoadMapping(
                        lc, 
                        rot, 
                        lc.GetPointLE0(),
                        true
                    ));
                    roadMapping.Add(new RoadMapping(
                        null, 
                        rot, 
                        lc.GetPointRE0()
                    ));
                }
                else
                {
                    roadMapping.Add(new RoadMapping(
                        lc, 
                        rot, 
                        lc.GetPointRS0()
                    ));
                    
                    if(lc.OtherDirection == null)
                        roadMapping.Add(new RoadMapping(
                            null, 
                            rot, 
                            lc.GetPointLS0()
                        ));
                }

                _roadMapping = roadMapping;
            }

            AddAdditionalPointForLaneLaneConnection();
            
            _roadMapping.ForEach(rm =>
            {
                if(!areaPoints.Contains(rm.Point))
                    areaPoints.Add(rm.Point);
                
                // if(!OutlinePoints.Contains(rm.Point))
                //     OutlinePoints.Add(rm.Point);
            });
            
            Area = new Area(areaPoints, HeightOffset);
            CalcRadius(areaPoints);
        }

        private float AngleMultiplier(LaneCollection lc)
        {
            // return 1f;

            AngleToNeighbors(lc, out var prev, out var next);
            var angle = Mathf.Min(Mathf.Abs(prev), Mathf.Abs(next));
            
            // return angle < 20f && angle > .1f ? 4f : 1f;

            var w = lc.Characteristics.Width * .5f;

            return angle < .5f || angle > 80f ? 1f
                : angle > 45f ? 1.5f
                    : 2f
                // : angle > 30f ? 2f
                // : angle > 25f ? 3f
                // : angle > 20f ? 6.5f
                // : angle > 15f ? 8f // / w
                // : 10f // / w
                ;
        }
        
        private void AngleToNeighbors(LaneCollection lc, out float prev, out float next)
        {
            prev = 180f;
            next = 180f;
            
            var index = _roadMapping.FindIndex(rm => rm.Lc == lc);

            var currRm = _roadMapping[index];
            var curr = (currRm.IsIn ? 1f : -1f) * currRm.ForwardRotation;

            var rmCnt = _roadMapping.Count;

            for (var i = 1; i < rmCnt; i++)
            {
                var rm = _roadMapping[(index + i) % rmCnt];
                if (rm.Lc == null)
                    break;
                
                if(rm.Lc == lc.OtherDirection) continue;

                next = Vector2.SignedAngle(curr, (rm.IsIn ? 1f : -1f) * rm.ForwardRotation);
                
                break;
            }
            
            for (var i = 1; i < rmCnt; i++)
            {
                var rm = _roadMapping[(index - i + rmCnt) % rmCnt];
                if (rm.Lc == null)
                    break;
                
                if(rm.Lc == lc.OtherDirection) continue;

                prev = Vector2.SignedAngle(curr, (rm.IsIn ? 1f : -1f) * rm.ForwardRotation);
                
                break;
            }
        }

        public List<RoadMapping> GetRoadMapping()
        {
            return _roadMapping;
        }

        private void AddAdditionalPointForLaneLaneConnection()
        {
            if (!IsRoadRoadConnection || LanesIn.Count == 0 || LanesOut.Count == 0)
                return;

            var i = LanesIn[0];

            if (LanesOut[0] == i.OtherDirection && LanesOut.Count == 1)
                return;

            var o = LanesOut[0] == i.OtherDirection ? LanesOut[1] : LanesOut[0];

            var inLine = i.OutlineRight;
            var outLine = o.OutlineRight;

            var angle = Vector3.SignedAngle(
                inLine.Forward[inLine.Forward.Count - 1],
                outLine.Forward[0],
                Vector3.up
            );
            

            if (Mathf.Abs(angle) < 25f)
                return;

            var find = angle > 0 ? (i.OtherDirection ?? i) : o;
            var rmIndex = -1;
            for (var index = 0; index < _roadMapping.Count; index++)
            {
                if (_roadMapping[index].Lc == find)
                    rmIndex = index;
            }

            if (rmIndex < 0)
                return;

            Vector3 forward;
            Vector3 point;
            var multiply = RoadWidth() * 1.25f; // 2.5f;

            if (angle > 0f)
            {
                if (i.OtherDirection == null)
                {
                    forward = -i.OutlineLeft.Forward[i.OutlineLeft.Forward.Count - 1];
                    point = i.OutlineLeft.Get(-1) + -forward * multiply;
                }
                else
                {
                    var lc = i.OtherDirection;
                    forward = -lc.OutlineRight.Forward[0];
                    point = lc.OutlineRight[0] + forward * multiply;
                }
            }
            else
            {
                forward = -o.OutlineRight.Forward[0];
                point = o.OutlineRight[0] + forward * multiply;
            }

            _roadMapping.Insert(rmIndex, new RoadMapping(null, forward.ToVector2xz(), point.ToVector2xz()));

            var lastMapping = _roadMapping[(rmIndex - 1 + _roadMapping.Count) % _roadMapping.Count];
            lastMapping.ForwardRotation = (point.ToVector2xz() - lastMapping.Point).normalized;
        }

        private void SetLanes(List<LaneCollection> colIn, List<LaneCollection> colOut, bool alert = true)
        {
            LanesIn = colIn;
            LanesOut = colOut;
            TotalLaneCollectionCount = colIn.Count + colOut.Count;
            SetToLaneCollectionsPrev(colOut, alert);
            SetToLaneCollectionsNext(colIn, alert);
            IsRoadRoadConnection = IsNodeRoadRoadConnection(LanesIn, LanesOut);
            SetRoadPriorities();
            _roadWidth = float.NaN;

            SetLaneType();
        }

        private void SetLaneType()
        {
            mainLaneType = MapData.LaneType.NONE;
            hasSingleLaneType = true;
            if(TotalLaneCollectionCount == 0)
                return;

            var laneTypes = new Dictionary<MapData.LaneType, int>();

            foreach (var lc in LanesIn)
            {
                if (!laneTypes.ContainsKey(lc.Id.Type))
                    laneTypes.Add(lc.Id.Type, 1);
                else
                    laneTypes[lc.Id.Type] += 1;
            }
            foreach (var lc in LanesOut)
            {
                if (!laneTypes.ContainsKey(lc.Id.Type))
                    laneTypes.Add(lc.Id.Type, 1);
                else
                    laneTypes[lc.Id.Type] += 1;
            }

            if (laneTypes.Count > 1)
            {
                mainLaneType = laneTypes.OrderBy(kv => kv.Value).Last().Key;
                hasSingleLaneType = false;
            }
            else if (laneTypes.Count == 1)
            {
                mainLaneType = laneTypes.First().Key;
            }
        }
        
        private void SetToLaneCollectionsPrev(List<LaneCollection> collections, bool alert = true)
        {
            foreach (var lc in collections)
            {
                #if (UNITY_EDITOR)
                if(alert && lc.PrevIntersection != null)
                    Debug.LogError($"Has already a 'PrevIntersection' (lc: {lc.Id}, node: {Node})");
                #endif // SHIPPING
                lc.PrevIntersection = this;
            }
        }        
        
        private void SetToLaneCollectionsNext(List<LaneCollection> collections, bool alert = true)
        {
            foreach (var lc in collections)
            {
                #if (UNITY_EDITOR)
                if(alert && lc.NextIntersection != null)
                    Debug.LogError($"Has already a 'PrevIntersection' (lc: {lc.Id}, node: {Node})");
                #endif // SHIPPING
                lc.NextIntersection = this;
            }
        }

        public LaneCollection GetNthLaneCollection(int n)
        {
            if (n < 0 || n >= TotalLaneCollectionCount)
                return null;

            return n < LanesIn.Count
                ? LanesIn[n]
                : LanesOut[n - LanesIn.Count];
        }

        public int GetLaneCollectionIndex(LaneCollection lc)
        {
            var index = LanesIn.IndexOf(lc);
            if (index >= 0)
                return index;

            index = LanesOut.IndexOf(lc);
            if (index >= 0)
                return index + LanesIn.Count;

            return -1;
        }

        public bool IsNthLaneCollectionDirIn(int n) => n < LanesIn.Count;

        public int OrientationNumberByCount(int index) => _orientations.FindIndex(x => x == index);

        public int IndexByOrientation(int orientation) => _orientations[orientation];

        public LaneCollection CollectionByOrientation(int orientation) => GetNthLaneCollection(IndexByOrientation(orientation));

        private void SetAttributes()
        {
            var surfaces = new Dictionary<string, int>();
            var bridge = false;
            var tunnel = false;
            var lit = false;

            foreach (var lc in LanesIn)
            {
                bridge = bridge || lc.Characteristics.IsBridge;
                tunnel = tunnel || lc.Characteristics.IsTunnel;
                lit    = lit || lc.Characteristics.IsLit;
                
                var s = lc.Characteristics.Surface;
                
                if(s == null) continue;
                
                if (!surfaces.ContainsKey(s))
                    surfaces.Add(s, 0);

                surfaces[s]++;
            }
            foreach (var lc in LanesOut)
            {
                bridge = bridge || lc.Characteristics.IsBridge;
                tunnel = tunnel || lc.Characteristics.IsTunnel;
                lit    = lit || lc.Characteristics.IsLit;
                
                var s = lc.Characteristics.Surface;
                
                if(s == null) continue;
                
                if (!surfaces.ContainsKey(s))
                    surfaces.Add(s, 0);

                surfaces[s]++;
            }

            Surface = surfaces.Count == 0 
                ? null
                : surfaces.OrderByDescending( a => a.Value).First().Key;
            
            IsBridge = bridge;
            IsTunnel = tunnel;
            IsLit = lit;
        }

        public static bool IsNodeRoadRoadConnection(List<LaneCollection> ins, List<LaneCollection> outs)
        {
            return (ins.Count + outs.Count == 2)
                   || (ins.Count == 2 && outs.Count < 3
                                      && outs.Contains(ins[0].OtherDirection) 
                                      && outs.Contains(ins[1].OtherDirection)
                   ); 
        }
        
        public LaneCollection GetRoadRoadCollection(LaneCollection lc)
        {
            if (!IsRoadRoadConnection)
                return null;

            if (LanesIn.Contains(lc))
            {
                if (LanesOut.Count == 1)
                    return LanesOut[0];

                var index = LanesOut.IndexOf(lc.OtherDirection);

                return index < 0 || LanesOut.Count == 0 
                    ? null
                    : LanesOut[(index + 1) % LanesOut.Count];
            }
            else
            {
                if (LanesIn.Count == 1)
                    return LanesIn[0];
                
                var index = LanesIn.IndexOf(lc.OtherDirection);

                return index < 0 || LanesIn.Count == 0 
                    ? null 
                    : LanesIn[(index + 1) % LanesIn.Count];
            }
        }

        private void CalcRadius(IReadOnlyCollection<Vector2> points)
        {
            Radius = points.Max(p => Vector2.Distance(Center, p));
            Bounds = new SimpleBounds(points);
        }

        public void SetHeightOffset()
        {
            if (Elevation == null)
                return;

            if (Elevation is Bridge bridge)
            {
                HeightOffset = bridge.BaseHeight + .021f;
            } 
            else if (Elevation is Tunnel tunnel)
            {
                HeightOffset = tunnel.BaseHeight + .021f;
            }

            Area.SetHeight(HeightOffset);
        }

        private List<long> _merges;

        public void Merge(Intersection other, LaneCollection mergePoint)
        {
            var otherLcIndex = other.LanesIn.IndexOf(mergePoint);
            if (otherLcIndex < 0)
                return;
            
            _merges ??= new List<long>();
            
            _merges.Add(other.Node);
            
            var div = _merges.Count + 1;
            Center = Center / div * _merges.Count + other.Center / div;
            
            var hasOtherDir = mergePoint.OtherDirection != null;
            
            var otherIndex = other.OrientationNumberByCount(otherLcIndex);
            var totalIn = LanesIn.Count - (hasOtherDir ? 1 : 0) + other.LanesIn.Count - 1;
            
            var roadMapping = new List<RoadMapping>();

            var thisIndex = 0;
            for (; thisIndex < _roadMapping.Count; thisIndex++)
            {
                var mapping = _roadMapping[thisIndex];
                if (mapping.Lc == mergePoint)
                {
                    break;
                }

                roadMapping.Add(mapping);
            }

            for (var i = 0; i < other._roadMapping.Count; i++)
            {
                var index = (otherIndex + i) % other._roadMapping.Count;
                
                var mapping = other._roadMapping[index];
                if (mapping.Lc == mergePoint)
                {
                    roadMapping.Add(new RoadMapping(null, mapping.ForwardRotation, mapping.Point));
                    continue;
                }
                
                if (hasOtherDir && mapping.Lc == mergePoint.OtherDirection)
                {
                    break;
                }

                roadMapping.Add(mapping);
            }
            
            for (; thisIndex < _roadMapping.Count; thisIndex++)
            {
                var mapping = _roadMapping[thisIndex];
                
                if(mapping.Lc == mergePoint || hasOtherDir && mapping.Lc == mergePoint.OtherDirection)
                {
                    if (!hasOtherDir && mapping.Lc == mergePoint || hasOtherDir && mapping.Lc == mergePoint.OtherDirection)
                    {
                        roadMapping.Add(new RoadMapping(null, mapping.ForwardRotation, mapping.Point));
                    }
                    
                    continue;
                }
                
                roadMapping.Add(mapping);
            }

            SetRoadMapping(roadMapping, totalIn);

            HasTrafficLights = HasTrafficLights || other.HasTrafficLights;
            IsBridge = IsBridge || other.IsBridge;
            IsTunnel = IsTunnel || other.IsTunnel;
            IsLit = IsLit || other.IsLit;
        }
        
        private void SetRoadMapping(List<RoadMapping> roadMapping, int totalIn)
        {
            var newLanesIn = new List<LaneCollection>();
            var newLanesOut = new List<LaneCollection>();
            var newPoints = new List<Vector2>();
            var newAngles = new List<float>();
            var newOrientations = new List<int>();

            var inCnt = 0;
            var outCnt = totalIn;
            
            foreach (var mapping in roadMapping)
            {
                var angle = Vector2.SignedAngle(mapping.ForwardRotation, Vector2.up);
                newAngles.Add(angle > 0 ? angle : 360f + angle);
                newPoints.Add(mapping.Point);
                
                if(mapping.Lc == null)
                    continue;
                
                if (mapping.IsIn)
                {
                    newLanesIn.Add(mapping.Lc);
                    newOrientations.Add(inCnt++);
                }
                else
                {
                    newLanesOut.Add(mapping.Lc);
                    newOrientations.Add(outCnt++);
                }
            }
            
            Points = newPoints;
            _orientations = newOrientations;
            Angles = newAngles;

            _roadMapping = roadMapping;
            _roadWidth = float.NaN;
            _maxAngleMultiplier = float.NaN;
            
            SetLanes(newLanesIn, newLanesOut, false);
        }

        public List<LaneCollection> RemoveInternal()
        {
            var lanes = LanesOut.Where(lc => LanesIn.Contains(lc)).ToList();

            if (lanes.Count == 0)
                return lanes;
            
            foreach (var lc in lanes)
            {
                LanesIn.Remove(lc);
                LanesOut.Remove(lc);
            }

            var rmCnt = _roadMapping.Count;
            for (var i = 0; i < rmCnt; i++)
            {
                var rm = _roadMapping[i];
                if(rm != null && lanes.Contains(rm.Lc))
                    _roadMapping[i] = null;
            }
            
            _roadMapping.RemoveAll(rm => rm == null);
            
            SetRoadMapping(_roadMapping, LanesIn.Count);

            return lanes;
        }

        public void SetElevation(Elevation elevation, bool isRamp = false)
        {
            Elevation = elevation;
            ElevationRamp = isRamp;
        }

        public int GetPriority(LaneCollection lc)
        {
            var index = GetLaneCollectionIndex(lc);
            return index >= 0 && index < Priorities.Count
                ? Priorities[index]
                : -1;
        }

        public Priority GetPriorityRelative(LaneCollection lc)
        {
            var index = GetLaneCollectionIndex(lc);
            return index >= 0 && index < PrioritiesRelative.Count
                ? PrioritiesRelative[index]
                : Priority.RightBeforeLeft;
        }
    }
}