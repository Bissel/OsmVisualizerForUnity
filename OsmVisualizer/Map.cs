using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data;
using OsmVisualizer.Data.Request;
using OsmVisualizer.Data.Types;
using OsmVisualizer.Visualisation;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OsmVisualizer
{
    
    [RequireComponent(typeof(AbstractSettingsProvider),typeof(Helper.DataProviderList))]
    public class Map : MonoBehaviour
    {
        public List<Visualizer> visualizers = new List<Visualizer>();
        
        [HideInInspector]
        public AbstractSettingsProvider settingsProvider;
        [HideInInspector]
        public Helper.DataProviderList dataProviderList;
        // [HideInInspector]
        // public Helper.MeshBuilderList meshBuilderList;

        public MapData MapData;
        
        private readonly Dictionary<MapData.TilePos, MapTile> _tiles = new Dictionary<MapData.TilePos, MapTile>();

        private bool _createTiles = false;

        private MapData.TilePos _lastPos = null;

        private readonly Queue<MapData.TilePos> _createQueue = new Queue<MapData.TilePos>();
        // private MapData.TilePos _currentCreatingTile = null;

        public bool IsDone { get; private set; } = false;
        
        // Start is called before the first frame update
        private IEnumerator Start()
        {

            settingsProvider = gameObject.GetComponent<AbstractSettingsProvider>();
            dataProviderList = gameObject.GetComponent<Helper.DataProviderList>();
            
            MapData = new MapData();

            _createQueue.Enqueue(new MapData.TilePos(0, 0));
            
            while (_createQueue.Count > 0 && _tiles.Values.Any(t => !t.Initialized))
                yield return null;
            
            IsDone = true;
        }

        private float _nextStartTimestamp = 0;
        
        // Update is called once per frame
        private void Update()
        {
            // if (_currentCreatingTile != null 
            //     && _tiles.TryGetValue(_currentCreatingTile, out var mt) 
            //     && mt.Initialized)
            // {
            //         _currentCreatingTile = null;
            // }
            
            if (_createQueue.Count > 0 && Time.time > _nextStartTimestamp )
            {
                _nextStartTimestamp = Time.time + .1f;
                // _currentCreatingTile = _createQueue.Dequeue();
                // CreateTile(_currentCreatingTile);
                CreateTile(_createQueue.Dequeue());
            }
            
            var tilePos = GetTilePos(settingsProvider.mapCenter.position);
            if (tilePos.Equals(_lastPos))
                return;

            _lastPos = tilePos;
            CreateTiles(tilePos.X, tilePos.Y);
            UpdateTiles(tilePos.X, tilePos.Y);
        }

        private MapData.TilePos GetTilePos(Vector3 position)
        {
            var tileSize = settingsProvider.tileSize;
            return new MapData.TilePos(
                Mathf.RoundToInt(position.x / tileSize),
                Mathf.RoundToInt(position.z / tileSize)
            );
        }

        private void CreateTiles(int centerX = 0, int centerY = 0)
        {
            if (_createTiles) return;
            
            _createTiles = true;
            var max = settingsProvider.visibleRadiusInTileCount;

            for( var dist = 0; dist <= max; dist++)
            {
                for(var x = -dist; x <= dist; x++)
                {
                    for(var y = -dist; y <= dist; y++)
                    {
                        var pos = new MapData.TilePos(x + centerX, y + centerY);
                        if (_tiles.ContainsKey(pos)) continue;
                        if (_createQueue.Contains(pos)) continue;

                        _createQueue.Enqueue(pos);
                    }
                }
                
                // for (var x = centerX + min; x <= centerX + max; x++)
                // {
                //     for (var y = centerY + min; y <= centerY + max; y++)
                //     {
                //         var pos = new MapData.TilePos(x, y);
                //         if (_tiles.ContainsKey(pos)) continue;
                //         if (_createQueue.Contains(pos)) continue;
                //
                //         _createQueue.Enqueue(pos);
                //     }
                // }
            }

            _createTiles = false;
        }

        private void CreateTile(MapData.TilePos pos)
        {
            var bounds = settingsProvider is SettingsProvider provider 
                ? new WGS84Bounds2(
                    provider.startPosition.WithOffset(
                        pos.ToVector2() * settingsProvider.tileSize
                        + pos.ToVector2().y * new Vector2(60.75f, -4.5f) // @todo hotfix tile-drift
                        ), 
                    settingsProvider.tileSize
                ) 
                : new WGS84Bounds2(new Position2(0,0), settingsProvider.tileSize);
            
            var tile = new GameObject("Tile " + pos.X + "/" + pos.Y);
            
            tile.transform.SetParent(transform);
            tile.transform.Translate( pos.ToVector3() * settingsProvider.tileSize );
            
            var mapTile = tile.AddComponent<MapTile>();
            
            _tiles.Add(pos, mapTile);

            mapTile.pos = pos;
            mapTile.bounds = bounds;
            mapTile.sp = settingsProvider;
            mapTile.map = this;
        }

        private void UpdateTiles(int centerX = 0, int centerY = 0)
        {
            var max = settingsProvider.visibleRadiusBuffer + settingsProvider.visibleRadiusInTileCount;
            var min = -max;

            var activeTiles = new List<MapData.TilePos>();

            for (var x = centerX + min; x <= centerX + max; x++)
            {
                for (var y = centerY + min; y <= centerY + max; y++)
                {
                    activeTiles.Add(new MapData.TilePos(x, y));
                }
            }

            foreach (var kv in _tiles)
            {
                kv.Value.gameObject.SetActive(
                    !kv.Value.Initialized || activeTiles.Contains(kv.Key)
                );
            }
        }
        
        public bool HasNeighbourLaneCollection(MapData.TilePos pos, int x, int y, MapData.LaneId id)
            => GetNeighbourLaneCollection(pos, x, y, id) != null;

        public LaneCollection GetNeighbourLaneCollection(MapData.TilePos pos, int x, int y, MapData.LaneId id)
        {
            var neighbours = GetNeighbours(pos, x, y);

            foreach (var neighbour in neighbours)
            {
                var lc = GetTileLaneCollection(neighbour, id);
                if (lc != null)
                    return lc;
                
            }
            
            return null;
        }
        
        public LaneCollection GetNeighbourLaneCollection(MapTile[] neighbours, MapData.LaneId id)
        {
            return neighbours
                .Select(neighbour => GetTileLaneCollection(neighbour, id))
                .FirstOrDefault(lc => lc != null);
        }
        
        public Intersection GetNeighbourIntersection(MapTile[] neighbours, long node)
        {
            return neighbours
                .Select(neighbour => GetTileNode(neighbour, node))
                .FirstOrDefault(lc => lc != null);
        }
        
        public bool HasNeighbourIntersection(MapTile[] neighbours, long node)
        {
            return neighbours.Any(neighbour => HasTileNode(neighbour, node));
        }
        
        public Element GetNeighbourSplitElement(MapTile[] neighbours, MapData.LaneId id)
        {
            return neighbours
                .Select(neighbour => GetTileSplitElement(neighbour, id))
                .FirstOrDefault(lc => lc != null);
        }

        public MapTile GetNeighbour(MapData.TilePos pos) => _tiles.TryGetValue(pos, out var tile) 
                                                            ? tile 
                                                            : null;

        public MapTile[] GetAllNeighbours(MapData.TilePos pos)
        {
            return new[]
            {
                GetNeighbour(pos.FromOffset(-1, -1)),
                GetNeighbour(pos.FromOffset(-1,  0)),
                GetNeighbour(pos.FromOffset(-1,  1)),
                GetNeighbour(pos.FromOffset( 0, -1)),
                GetNeighbour(pos.FromOffset( 0,  1)),
                GetNeighbour(pos.FromOffset( 1, -1)),
                GetNeighbour(pos.FromOffset( 1,  0)),
                GetNeighbour(pos.FromOffset( 1,  1)),
                GetNeighbour(pos.FromOffset( 1,  1))
            };
        }
        
        public MapTile[] GetNeighbours(MapData.TilePos pos, int x, int y)
        {
            if (x > 1 || x < -1 || y > 1 || y < -1)
                throw new ArgumentException("x and y must be between -1 and 1");

            var neighbours = new MapTile[3];
            
            if (x == 0)
            {
                for (var i = -1; i <= 1; i++)
                {
                    neighbours[i + 1] = GetNeighbour(pos.FromOffset(i, y));
                }
            }
            else if (y == 0)
            {
                for (var i = -1; i <= 1; i++)
                {
                    neighbours[i + 1] = GetNeighbour(pos.FromOffset(x, i));
                }
            }
            else
            {
                neighbours[0] = GetNeighbour(pos.FromOffset(x, y));
                neighbours[1] = GetNeighbour(pos.FromOffset(x, 0));
                neighbours[2] = GetNeighbour(pos.FromOffset(0, y));
            }

            return neighbours;
        }

        private LaneCollection GetTileLaneCollection(MapData.TilePos pos, MapData.LaneId id)
        {
            if (!_tiles.ContainsKey(pos)) return null;

            var tile = _tiles[pos];

            return GetTileLaneCollection(tile, id);
        }
        
        private LaneCollection GetTileLaneCollection(MapTile tile, MapData.LaneId id) 
            => tile == null 
                ? null 
                : tile.DummyLaneCollections.TryGetValue(id, out var lc) 
                    ? lc 
                    : tile.LaneCollections.TryGetValue(id, out lc) 
                        ? lc
                        : null;

        private bool HasTileNode(MapTile tile, long node) => tile != null && tile.Intersections.ContainsKey(node);

        private Intersection GetTileNode(MapTile tile, long node) 
            => tile == null 
                ? null 
                : tile.Intersections.TryGetValue(node, out var inter) 
                    ? inter
                    : null;
        
        private Element GetTileSplitElement(MapTile tile, MapData.LaneId id) 
            => tile == null 
                ? null 
                : tile.SplitElements.TryGetValue(id, out var el) 
                    ? el
                    : null;

        public MapTile GetRandomTile()
        {
            var tiles = _tiles.Values.Where(t => t.gameObject.activeSelf && t.VisualisationComplete).ToList();
            return tiles.Count == 0 ? null : tiles[Random.Range(0, tiles.Count)];
        }

        public MapTile GetTile(Vector3 position)
        {
            var tilePos = GetTilePos(position);
            return !_tiles.ContainsKey(tilePos) ? null : _tiles[tilePos];
        }
    }
}