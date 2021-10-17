using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Types;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace OsmVisualizer.Data
{
    public class MapTile : MonoBehaviour
    {

        public AbstractSettingsProvider sp;
        public WGS84Bounds2 bounds;

        public Vector2 Min { get; private set; } 
        public Vector2 Max { get; private set; } 

        public MapData.TilePos pos;

        public bool Initialized { get; private set; }
        public bool VisualisationComplete { get; private set; }

        public bool HasAllNeighbours { get; private set; }

        public Map map;

        public Result RawData;
        
        public readonly Dictionary<MapData.LaneId, LaneCollection> LaneCollections = new Dictionary<MapData.LaneId, LaneCollection>();
        public readonly Dictionary<MapData.LaneId, LaneCollection> DummyLaneCollections = new Dictionary<MapData.LaneId, LaneCollection>();
        public readonly Dictionary<MapData.LaneId, Element> SplitElements = new Dictionary<MapData.LaneId, Element>();
        public readonly ConcurrentDictionary<long, Intersection> Intersections = new ConcurrentDictionary<long, Intersection>();
        public readonly Dictionary<string, WayInterpretation> WayAreas = new Dictionary<string, WayInterpretation>();
        
        public readonly List<long> BridgeNodes = new List<long>();

        public readonly Dictionary<long, Vector2> IntersectionPoints = new Dictionary<long, Vector2>();

        // private bool[] EnabledMeshBuilder;

        private MapTile[] neighbours;

        public string Key { get; private set; }
        
        private IEnumerator Start()
        {
            Key = pos.ToString();
            {
                var tileSize = sp.tileSize;
                var tileSizeHalf = tileSize / 2;
                Min = new Vector2(
                    pos.X * tileSize - tileSizeHalf,
                    pos.Y * tileSize - tileSizeHalf
                );
                Max = new Vector2(
                    pos.X * tileSize + tileSizeHalf,
                    pos.Y * tileSize + tileSizeHalf
                );
            }

            RefreshNeighbours();

            StartCoroutine(Init());

            while(!Initialized || !HasAllNeighbours)
            {
                yield return new WaitForSeconds(.02f);
                RefreshNeighbours();
            }
            
            // Debug.Log($"MapTile ({pos}) Initialized");

            if(!(sp is SettingsPlainLevelProvider))
                while (neighbours.Any(n => !n.Initialized))
                    yield return new WaitForSeconds(.01f);
            
            // Debug.Log($"MapTile ({pos}) neighbours Initialized");
            
            yield return Visualise();
            
            // Debug.Log($"MapTile ({pos}) Visualised");
        }

        public enum InitStep
        {
            None, 
            GatherData,
            Base, 
            Filter,
            SplitOutOfBounds,
            SplitOnIntersection,
            Rails,
            Lanes,
            Intersection,
            Bridges,
            Tunnel,
            Height,
            RemoveBorderLc,
            Building,
            Landuse,
            Waterway,
            Done = int.MaxValue
        }
        
        [NonSerialized]
        public InitStep InitProgress = InitStep.None;

        private void RefreshNeighbours()
        {
            if (HasAllNeighbours) return;
            
            neighbours = map.GetAllNeighbours(pos);
            HasAllNeighbours = neighbours.All(n => n != null);
        }

#if UNITY_EDITOR
        void Update()
        {
            if (!VisualisationComplete) return;
            StartCoroutine(UpdateVisuals());
        }

        private IEnumerator UpdateVisuals()
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            foreach (var v in map.visualizers)
            {
                if(!v.enabled || !v.gameObject.activeSelf) continue;
                stopwatch.Start();
                yield return v.UpdateTile(this, stopwatch);
                stopwatch.Stop();
            }
        }
#endif
        
        private IEnumerator Init()
        {
            var rr = new RequestResult();

            InitProgress = InitStep.GatherData;

            if (sp is SettingsProvider provider)
            {
                var boundsForQuery = bounds;
                var exact = true;
                if (sp.tileSizeBorderWidth > 0)
                {
                    boundsForQuery = new WGS84Bounds2(bounds.Center, sp.tileSizeBorderWidth + sp.tileSize);
                    exact = false;
                }

                yield return Request.Request.Query(
                    rr,
                    provider, 
                    Request.Request.QueryWay,
                    boundsForQuery, 
                    exact
                );
                
                if (rr.hasError)
                {
                    Debug.LogError(rr.error);
                    yield break;
                }
            }
            else if(sp is SettingsPlainLevelProvider levelProvider)
            {
                rr.data = Request.Request.ResultFromFile(levelProvider.mapDirectory, pos);
            }
            else
            {
                Debug.LogError("Unknown AbstractSettingsProvider");
                yield break;
            }
            
            // Debug.Log("Provider start");
            // var totalTime = 0L;
            var stopwatch = new System.Diagnostics.Stopwatch();

            foreach (var dp in map.dataProviderList.DataProviders)
            {
                if (dp == null) continue;

                InitProgress = dp.Step;
                
                stopwatch.Start();
                yield return dp.Convert(rr.data, map.MapData, this, stopwatch);
                stopwatch.Stop();

                // var time = stopwatch.ElapsedMilliseconds;
                // Debug.Log($"{dp.GetType().ToString().PadRight(64,' ')} {(time - totalTime + "").PadLeft(5, ' ')}ms");
                // totalTime = stopwatch.ElapsedMilliseconds;
            }

            InitProgress = InitStep.Done;

            // Debug.Log($"Provider end, total time: {totalTime}ms");

            RawData = rr.data;
            Initialized = true;
        }

        public IEnumerator Visualise()
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            
            foreach (var v in map.visualizers)
            {
                if(!v.enabled || !v.gameObject.activeSelf) continue;
                
                stopwatch.Start();
                yield return v.Create(this, stopwatch);
                stopwatch.Stop();
            }
            
            VisualisationComplete = true;
        }

        public void AddLaneCollection(LaneCollection lc)
        {
            LaneCollections.Add(lc.Id, lc);
            
            // Intersections.TryAdd(lc.Id.StartNode, null);
            // Intersections.TryAdd(lc.Id.EndNode, null);
        }

        public MapTile[] NeighboursForPoints(IEnumerable<Vector2> points)
        {
            // @todo filter neighbours on lane position in this tile
            // eg. if position is in the top right of this tile only use neighbours 3,4,5 [ (1,0),(1,1),(0,1) ]
            return neighbours;
        }

        public MapTile[] Neighbours()
        {
            return neighbours;
        }
        
        public MapTile[] NeighboursForPoint(Vector2 point)
        {
            // @todo filter neighbours on lane position in this tile
            // eg. if position is in the top right of this tile only use neighbours 3,4,5 [ (1,0),(1,1),(0,1) ]
            return neighbours;
        }

        public LaneCollection NeighbourLaneCollection(MapData.LaneId id, MapTile[] neighbours) 
            => map.GetNeighbourLaneCollection(neighbours, id);

        public Intersection NeighbourIntersection(long node, MapTile[] neighbours) 
            => map.GetNeighbourIntersection(neighbours, node);
        
        public bool NeighbourHasIntersection(long node, MapTile[] neighbours) 
            => map.HasNeighbourIntersection(neighbours, node);

        public Element NeighbourSplitElement(MapData.LaneId id, MapTile[] neighbours) 
            => map.GetNeighbourSplitElement(neighbours, id);

        public LaneCollection GetRandomLaneCollection()
        {
            var lcs = LaneCollections.Values.ToList();
            return lcs.Count == 0 ? null : lcs[Random.Range(0, lcs.Count)];
        }
    }
}