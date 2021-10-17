using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data;
using OsmVisualizer.Visualisation.Components.Signs;
using UnityEngine;

namespace OsmVisualizer.Visualisation.Components
{
    public class Sings : VisualizerComponent
    {
        public GameObject signPrefab;

        private readonly Vector3 offsetSr = new Vector3(+.5f, 0, .5f);
        private readonly Vector3 offsetSl = new Vector3(-.5f, 0, .5f);
        
        private readonly Vector3 offsetEr = new Vector3(+.5f, 0, -.5f);
        private readonly Vector3 offsetEl = new Vector3(-.5f, 0, -.5f);
        
        //
        // public void Update()
        // {
        //     // deactivate _signParent if distance to center > 1 Tile Size
        //     
        //     var dist = Vector3.Distance(_signParent.transform.parent.transform.position, _settings.mapCenter.position);
        //     if(_signParent.activeSelf && dist > _settings.tileSize)
        //         _signParent.SetActive(false);
        //     else if(!_signParent.activeSelf && dist < _settings.tileSize)
        //         _signParent.SetActive(true);
        // }

        
        protected override IEnumerator Create(MapTile tile, Creator creator, System.Diagnostics.Stopwatch stopwatch)
        {
            if(Visualizer.mode == Mode.MODE_2D)
                yield break;

            var startTime = stopwatch.ElapsedMilliseconds;

            var signs = new Dictionary<SignPos, List<Sign>>();
            
            foreach (var lc in tile.LaneCollections.Values)
            {
                if(lc == null)
                    continue;
                
                CreateSpeedSigns(lc, signs);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
            
            foreach (var inter in tile.Intersections.Values)
            {
                if(inter == null || inter.IsRoadRoadConnection)
                    continue;
                
                CreateIntersectionSigns(inter, signs);
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }

            foreach (var kv in signs)
            {
                var lc = kv.Key.Lc;
                var pos = kv.Key;

                if (pos.PosY == SignSimplePos.S)
                {
                    CreateSignStart(signPrefab, creator, lc, pos.PosX).Init(kv.Value, pos);
                }
                else
                {
                    var hasStopSign = kv.Value.Count(s => s.Type == Sign.SignType.Stop || s.Type == Sign.SignType.GiveWay) > 0;
                    CreateSignEnd(signPrefab, creator, lc, pos.PosX, hasStopSign).Init(kv.Value, pos);
                }
                
                if (stopwatch.ElapsedMilliseconds - startTime <= tile.sp.maxFrameTime) 
                    continue;
                
                stopwatch.Stop();
                yield return null;
                stopwatch.Start();
                startTime = stopwatch.ElapsedMilliseconds;
            }
        }
        
        private void CreateSpeedSigns(LaneCollection lc, Dictionary<SignPos, List<Sign>> signs)
        {
            if (lc.ApproxLength < 20f && !(lc.PrevIntersection?.IsRoadRoadConnection ?? true) && !(lc.NextIntersection?.IsRoadRoadConnection ?? true))
                return;
            
            var speedLimit = lc.Characteristics.SpeedLimit;
            
            var prevSpeedLimit = lc.PrevIntersection == null || lc.PrevIntersection.TotalLaneCollectionCount == 0
                ? 0
                : lc.PrevIntersection.IsRoadRoadConnection
                    ? lc.PrevIntersection.GetRoadRoadCollection(lc)?.Characteristics.SpeedLimit ?? 0
                    : Mathf.RoundToInt((float) lc.PrevIntersection.LanesIn.Average(l => l.Characteristics.SpeedLimit));
            
            var nextSpeedLimit = lc.NextIntersection == null || lc.NextIntersection.TotalLaneCollectionCount == 0
                ? 0
                : lc.NextIntersection.IsRoadRoadConnection
                    ? lc.NextIntersection.GetRoadRoadCollection(lc)?.Characteristics.SpeedLimit ?? 0
                    : Mathf.RoundToInt((float) lc.NextIntersection.LanesOut.Average(l => l.Characteristics.SpeedLimit));

            if (speedLimit == 0)
                return;
            
            if (speedLimit != prevSpeedLimit && prevSpeedLimit != 0)
                CreateStartSpeedLimit(lc, speedLimit, signs);
            
            if (speedLimit != nextSpeedLimit && nextSpeedLimit != 0)
                CreateEndSpeedLimit(lc, speedLimit, signs);
        }

        private void CreateStartSpeedLimit(LaneCollection lc, int speedLimit, Dictionary<SignPos, List<Sign>> signs)
        {
            // @todo only in city-limits
            if (speedLimit == 50)
                return;
            
            AddSign(signs, lc, SignSimplePos.R, SignSimplePos.S, new Sign(Sign.SignType.SpeedLimit, speedLimit));
        }
        
        private void CreateEndSpeedLimit(LaneCollection lc, int speedLimit, Dictionary<SignPos, List<Sign>> signs)
        {
            // @todo only in city-limits
            if (speedLimit == 50)
                return;
            
            AddSign(signs, lc, SignSimplePos.R, SignSimplePos.E, new Sign(Sign.SignType.SpeedLimitEnd, speedLimit));
        }

        // GiveWay, Stop and Priority
        // OneWay Signs
        private void CreateIntersectionSigns(Intersection inter, Dictionary<SignPos, List<Sign>> signs)
        {
            CreateIntersectionOneWaySigns(inter, signs);

            CreateIntersectionPrioritySigns(inter, signs);
        }

        private void CreateIntersectionPrioritySigns(Intersection inter, Dictionary<SignPos, List<Sign>> signs)
        {
            foreach (var lc in inter.LanesIn)
            {
                var p = inter.GetPriorityRelative(lc);

                switch (p)
                {
                    case Intersection.Priority.RightOfWay:
                        AddSign(signs, lc, SignSimplePos.R, SignSimplePos.E, new Sign(Sign.SignType.PrioritySingle));
                        break;
                    
                    case Intersection.Priority.GiveWay:
                        AddSign(signs, lc, SignSimplePos.R, SignSimplePos.E, new Sign(Sign.SignType.GiveWay));
                        break;
                    
                    case Intersection.Priority.Stop:
                        AddSign(signs, lc, SignSimplePos.R, SignSimplePos.E, new Sign(Sign.SignType.Stop));
                        break;
                        
                    case Intersection.Priority.RightBeforeLeft:
                    default:
                        break;
                }
            }
        }

        private void CreateIntersectionOneWaySigns(Intersection inter, Dictionary<SignPos, List<Sign>> signs)
        {

            inter.LanesOut.ForEach(lc =>
            {
                if (lc.OtherDirection != null || (lc.OutlineRight.TotalLength + lc.OutlineLeft.TotalLength) * .5f < 2f)
                    return;

                AddSign(signs, lc, SignSimplePos.Both, SignSimplePos.S, new Sign(Sign.SignType.OneWay));
            });

            if (inter.LanesIn.Count > 1)
                inter.LanesIn.ForEach(lc =>
                {
                    if (lc.OtherDirection != null)
                        return;

                    AddSign(signs, lc, SignSimplePos.Both, SignSimplePos.E, new Sign(Sign.SignType.OneWay));
                });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signs"></param>
        /// <param name="lc"></param>
        /// <param name="posX">-1 Left | +1 Right | 0 Both </param>
        /// <param name="posY">-1 Start | +1 End | 0 Both</param>
        /// <param name="sign"></param>
        private static void AddSign(Dictionary<SignPos, List<Sign>> signs, LaneCollection lc, int posX, int posY, Sign sign)
        {
            if (posX == SignSimplePos.Both)
            {
                AddSign(signs, lc, SignSimplePos.L, posY, sign);
                AddSign(signs, lc, SignSimplePos.R, posY, sign);
                return;
            }
            if (posY == SignSimplePos.Both)
            {
                AddSign(signs, lc, posX, SignSimplePos.S, sign);
                AddSign(signs, lc, posX, SignSimplePos.E, sign);
                return;
            }

            var key = new SignPos(lc, posX, posY);
            if(!signs.ContainsKey(key))
                signs.Add(key, new List<Sign>());
            
            signs[key].Add(sign);
        }

        
        private SignBuilder CreateSign(GameObject prefab, Creator creator, Vector3 position, Quaternion rotation)
        {
            return Instantiate(prefab, position, rotation, creator.GetTransform()).GetComponent<SignBuilder>();
        }
        
        private SignBuilder CreateSignStart(GameObject prefab, Creator creator, LaneCollection lc, int posX, bool useFirst = false)
        {
            Vector3 pos, forward;

            var i = useFirst ? 0 : 1;
            
            if (posX == SignSimplePos.L)
            {
                pos = lc.OutlineLeft[i];
                forward = lc.OutlineLeft.Forward[i];
                var rotation = Rotation(forward);
                return CreateSign(prefab, creator, pos + rotation * offsetSl, rotation);
            } 
            
            if (posX == SignSimplePos.R)
            {
                pos = lc.OutlineRight[i];
                forward = lc.OutlineRight.Forward[i];
                var rotation = Rotation(forward);
                return CreateSign(prefab, creator, pos + rotation * offsetSr, rotation);
            }
            
            return null;
        }
        
        private SignBuilder CreateSignEnd(GameObject prefab, Creator creator, LaneCollection lc, int posX, bool useLast = false)
        {
            Vector3 pos, forward;
            
            var i = useLast ? -1 : -2;
            
            if (posX == SignSimplePos.L)
            {
                pos = lc.OutlineLeft.Get(i);
                forward = lc.OutlineLeft.Forward[lc.OutlineLeft.Forward.Count + i];
                var rotation = Rotation(forward);
                return CreateSign(prefab, creator, pos + rotation * offsetEl, rotation);
            } 
            
            if (posX == SignSimplePos.R)
            {
                pos = lc.OutlineRight.Get(i);
                forward = lc.OutlineRight.Forward[lc.OutlineRight.Forward.Count + i];
                var rotation = Rotation(forward);
                return CreateSign(prefab, creator, pos + rotation * offsetEr, rotation);
            }
            
            return null;
        }

        private static Quaternion Rotation(Vector3 forward) => Quaternion.AngleAxis(
            Vector3.SignedAngle(Vector3.forward, forward, Vector3.up),
            Vector3.up
        );

    }
}