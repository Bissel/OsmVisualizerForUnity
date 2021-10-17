using System.Collections;
using System.Linq;
using OsmVisualizer.Data;
using TMPro;
using UnityEngine;

namespace OsmVisualizer.Mesh
{
    public class Signs : MeshBuilder
    {
        private readonly GameObject _prefabSignSpeedLimit;
        private readonly GameObject _prefabSignSpeedLimitEnd;
        private readonly GameObject _prefabSignGiveWay;
        private readonly GameObject _prefabSignStop;
        private readonly GameObject _prefabSignPrioritySingle;
        private readonly GameObject _prefabSignPriorityRoad;
        private readonly GameObject _prefabSignPriorityRoadEnd;
        private readonly GameObject _prefabSignOneWayDirectionRight;
        private readonly GameObject _prefabSignOneWayDirectionLeft;
        private readonly GameObject _prefabSignOneWayDirectionThrough;
        private readonly GameObject _prefabSignOneWayNoEntry;
        private readonly GameObject _prefabSignRail;
        
        public class SignCreator : MonoBehaviour
        {
            private GameObject _signParent;
            private AbstractSettingsProvider _settings;

            public void SetParent(MapTile tile)
            {
                _signParent = new GameObject("Signs");
                _signParent.transform.parent = tile.transform;
                _settings = tile.sp;
            }

            public void Destroy()
            {
                Destroy(_signParent);
                Destroy(this);
            }

            public void Update()
            {
                // deactivate _signParent if distance to center > 1 Tile Size
                
                var dist = Vector3.Distance(_signParent.transform.parent.transform.position, _settings.mapCenter.position);
                if(_signParent.activeSelf && dist > _settings.tileSize)
                    _signParent.SetActive(false);
                else if(!_signParent.activeSelf && dist < _settings.tileSize)
                    _signParent.SetActive(true);
            }

            public GameObject CreateSign(GameObject prefab, Vector3 position, Quaternion rotation, string text = null)
            {
                var sign = Instantiate(prefab, position, rotation, _signParent.transform);
                
                if (text != null)
                {
                    var textComp = sign.GetComponentInChildren<TextMeshPro>();
                    textComp.text = text;
                }

                return sign;
            }

            private readonly Vector3 _offsetSR = new Vector3(+.5f, 0, .5f);
            private readonly Vector3 _offsetSL = new Vector3(-.5f, 0, .5f);
            
            private readonly Vector3 _offsetER = new Vector3(+.5f, 0, -.5f);
            private readonly Vector3 _offsetEL = new Vector3(-.5f, 0, -.5f);

            public void CreateSignStart(GameObject prefab, LaneCollection lc, bool left, bool right, int offset = 1, string text = null)
            {
                if (!left && !right)
                    return;
                
                Vector3 a = default, b = default, posR = default, posL = default;
                if (right)
                {
                    a = lc.OutlineRight[0]; //.GetPointRS0().ToVector3xz();
                    b = lc.OutlineRight[1]; //.GetPointRS1().ToVector3xz();
                    posR = (offset == 1 ? b : a);
                           // + lc.GetHeightOffset(offset == 0 ? 0f : Vector3.Distance(a, b), lc.Id.StartNode) * Vector3.up;
                }

                if (left)
                {
                    a = lc.OutlineLeft[0]; //.GetPointLS0().ToVector3xz();
                    b = lc.OutlineLeft[1]; //..GetPointLS1().ToVector3xz();
                    posL = (offset == 1 ? b : a);
                           // + lc.GetHeightOffset(offset == 0 ? 0f : Vector3.Distance(a, b), lc.Id.StartNode) * Vector3.up;
                }
                
                var rotation = Quaternion.AngleAxis(
                    Vector3.SignedAngle(Vector3.forward, b - a, Vector3.up),
                    Vector3.up
                );

                if (right)
                    CreateSign(prefab, posR + rotation * _offsetSR, rotation, text);
                
                if (left)
                    CreateSign(prefab, posL + rotation * _offsetSL, rotation, text);
            }
            
            public void CreateSignEnd(GameObject prefab, LaneCollection lc, bool left, bool right, int offset = 1, string text = null)
            {
                if (!left && !right)
                    return;
                
                Vector3 a = default, b = default, posR = default, posL = default;
                if (right)
                {
                    a = lc.OutlineRight.Get(-1); //.GetPointRE0().ToVector3xz();
                    b = lc.OutlineRight.Get(-2); //.GetPointRE1().ToVector3xz();
                    posR = (offset == 1 ? b : a);
                    //+ lc.GetHeightOffset(lc.ApproxLength - (offset == 0 ? 0f : Vector3.Distance(a, b)), lc.Id.StartNode) * Vector3.up;
                }

                if (left)
                {
                    a = lc.OutlineLeft.Get(-1); //.GetPointLE0().ToVector3xz();
                    b = lc.OutlineLeft.Get(-2); //.GetPointLE1().ToVector3xz();
                    posL = (offset == 1 ? b : a);
                           // + lc.GetHeightOffset(lc.ApproxLength - (offset == 0 ? 0f : Vector3.Distance(a, b)), lc.Id.StartNode) * Vector3.up;
                }
                
                var rotation = Quaternion.AngleAxis(
                    Vector3.SignedAngle(Vector3.forward, a - b, Vector3.up),
                    Vector3.up
                );

                if (right)
                    CreateSign(prefab, posR + rotation * _offsetER, rotation, text);
                
                if (left)
                    CreateSign(prefab, posL + rotation * _offsetEL, rotation, text);
            }
        }
        
        public Signs(
            AbstractSettingsProvider settings, 
            GameObject prefabSignSpeedLimit, GameObject prefabSignSpeedLimitEnd, 
            GameObject prefabSignGiveWay, GameObject prefabSignStop, 
            GameObject prefabSignPrioritySingle, GameObject prefabSignPriorityRoad, GameObject prefabSignPriorityRoadEnd, 
            GameObject prefabSignOneWayDirectionRight, GameObject prefabSignOneWayDirectionLeft, GameObject prefabSignOneWayDirectionThrough, GameObject prefabSignOneWayNoEntry, 
            GameObject prefabSignRail) : base(settings)
        {
            _prefabSignSpeedLimit = prefabSignSpeedLimit;
            _prefabSignSpeedLimitEnd = prefabSignSpeedLimitEnd;
            
            _prefabSignGiveWay = prefabSignGiveWay;
            _prefabSignStop = prefabSignStop;
            
            _prefabSignPrioritySingle = prefabSignPrioritySingle;
            _prefabSignPriorityRoad = prefabSignPriorityRoad;
            _prefabSignPriorityRoadEnd = prefabSignPriorityRoadEnd;
            
            _prefabSignOneWayDirectionRight = prefabSignOneWayDirectionRight;
            _prefabSignOneWayDirectionLeft = prefabSignOneWayDirectionLeft;
            _prefabSignOneWayDirectionThrough = prefabSignOneWayDirectionThrough;
            _prefabSignOneWayNoEntry = prefabSignOneWayNoEntry;
            
            _prefabSignRail = prefabSignRail;
        }

        public override IEnumerator Create(MapData data, MapTile tile)
        {
            var signCreator = tile.gameObject.AddComponent<SignCreator>();
            signCreator.SetParent(tile);
            
            var i = 1;
            foreach (var lc in tile.LaneCollections.Values)
            {
                if(lc == null)
                    continue;
                
                CreateSpeedSigns(lc, signCreator);

                if (i++ % 1000 == 0)
                    yield return null;
            }
            
            foreach (var inter in tile.Intersections.Values)
            {
                if(inter == null || inter.IsRoadRoadConnection)
                    continue;
                
                CreateIntersectionSigns(inter, signCreator);

                if (i++ % 1000 == 0)
                    yield return null;
            }

            yield return null;
        }

        public override IEnumerator Destroy(MapData data, MapTile tile)
        {
            if (tile.gameObject.TryGetComponent<SignCreator>(out var signCreator))
            {
                signCreator.Destroy();
            }
            yield return null;
        }

        private void CreateSpeedSigns(LaneCollection lc, SignCreator sc)
        {
            var speedLimit = Mathf.RoundToInt(lc.Characteristics.SpeedLimit);
            
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
                CreateStartSpeedLimit(lc, speedLimit, sc);
            
            if (speedLimit != nextSpeedLimit && nextSpeedLimit != 0)
                CreateEndSpeedLimit(lc, speedLimit, sc);
        }

        private void CreateStartSpeedLimit(LaneCollection lc, int speedLimit, SignCreator sc)
        {
            // @todo only in city-limits
            if (speedLimit == 50)
                return;
            
            sc.CreateSignStart(_prefabSignSpeedLimit, lc, false, true, 1, "" + speedLimit);
        }
        
        private void CreateEndSpeedLimit(LaneCollection lc, int speedLimit, SignCreator sc)
        {
            // @todo only in city-limits
            if (speedLimit == 50)
                return;
            
            sc.CreateSignEnd(_prefabSignSpeedLimitEnd, lc, false, true, 1, "" + speedLimit);
        }

        // GiveWay, Stop and Priority
        // OneWay Signs
        private void CreateIntersectionSigns(Intersection inter, SignCreator sc)
        {
            CreateIntersectionOneWaySigns(inter, sc);

            CreateIntersectionPrioritySigns(inter, sc);
        }

        private void CreateIntersectionPrioritySigns(Intersection inter, SignCreator sc)
        {
            // @todo road priority -> give way/stop/priority road sign
            if (inter.HasTrafficLights) 
                return;
            
            var maxPriority = -1;
            var minPriority = int.MaxValue;
            
            foreach (var priority in inter.Priorities)
            {
                if (priority > maxPriority)
                    maxPriority = priority;
                if (priority < minPriority)
                    minPriority = priority;
            }

            if (maxPriority == minPriority)
                return;

            var yieldSign = maxPriority - minPriority > 2 ? _prefabSignStop : _prefabSignGiveWay;

            for (var i = 0; i < inter.LanesIn.Count; i++)
            {
                var lc = inter.LanesIn[i];
                if (inter.Priorities[i] == maxPriority)
                {
                    sc.CreateSignEnd(_prefabSignPrioritySingle, lc, false, true, 0);
                }
                else
                {
                    sc.CreateSignEnd(yieldSign, lc, false, true, 0);
                }
            }
        }

        private void CreateIntersectionOneWaySigns(Intersection inter, SignCreator sc)
        {

            inter.LanesOut.ForEach(lc =>
            {
                if (lc.OtherDirection != null)
                    return;

                sc.CreateSignStart(_prefabSignOneWayDirectionLeft, lc, false, true, 0);
                sc.CreateSignStart(_prefabSignOneWayDirectionRight, lc, true, false, 0);
            });

            if (inter.LanesIn.Count > 1)
                inter.LanesIn.ForEach(lc =>
                {
                    if (lc.OtherDirection != null)
                        return;

                    sc.CreateSignEnd(_prefabSignOneWayNoEntry, lc, true, true, 0);
                });
        }

    }
}
