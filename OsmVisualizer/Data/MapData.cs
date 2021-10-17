using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace OsmVisualizer.Data
{
    public class MapData
    {
        public class WayKey
        {
            public readonly string LaneId;
            public readonly string TileId;
            public readonly bool ForwardDirection;

            public WayKey(string laneId, string tileId, bool forwardDirection = true)
            {
                LaneId = laneId;
                TileId = tileId;
                ForwardDirection = forwardDirection;
            }

            public override int GetHashCode()
            {
                return $"{TileId}-{LaneId}-{(ForwardDirection ? "f" : "b")}".GetHashCode();
            }
        }

        public enum LaneType
        {
            NONE,
            HIGHWAY,
            RAILWAY,
            PATHWAY
        }

        [Serializable]
        public class LaneId
        {
            public readonly long StartNode;
            public readonly long EndNode;
            public readonly LaneType Type;
            private bool Alt;
            
            public void SetAsAlternative()
            {
                Alt = true;
            }

            public LaneId(IReadOnlyList<long> nodes, LaneType type = LaneType.HIGHWAY)
            {
                StartNode = nodes[0];
                EndNode = nodes[nodes.Count - 1];
                Type = type;
            }

            [JsonConstructor]
            public LaneId(long startNode, long endNode, LaneType type = LaneType.HIGHWAY)
            {
                StartNode = startNode;
                EndNode = endNode;
                Type = type;
            }

            public LaneId GetReverseId() => new LaneId(EndNode, StartNode, Type);

            public LaneId GetOtherType(LaneType type) => new LaneId(StartNode, EndNode, type);

            public override int GetHashCode()
            {
                return $"{Type}-{StartNode}-{EndNode}-{(Alt ? "T" : "F")}".GetHashCode();
            }

            public override string ToString()
            {
                return $"[{Type}|{StartNode}|{EndNode}]";
            }

            public override bool Equals(object obj)
            {
                return obj is LaneId id && GetHashCode() == id.GetHashCode();
            }
        }

        [Serializable]
        public class TilePos
        {
            public readonly int X;
            public readonly int Y;
            
            public TilePos(int x, int y)
            {
                X = x;
                Y = y;
            }

            public TilePos FromOffset(int x, int y)
            {
                return new TilePos(X + x, Y + y);
            }

            public override string ToString() => $"{X}/{Y}";

            public override int GetHashCode() => ToString().GetHashCode();

            public override bool Equals(object obj)
            {
                return obj is TilePos pos && GetHashCode() == pos.GetHashCode();
            }

            public Vector2 ToVector2() => new Vector2(X, Y);

            public Vector3 ToVector3() => new Vector3(X, 0f, Y);
        }
        
        public Dictionary<string, Dictionary<WayKey, LaneCollection>> WayIdToLaneCollection = new Dictionary<string, Dictionary<WayKey, LaneCollection>>();

        public readonly ConcurrentDictionary<LaneId, LaneCollection> edges = new ConcurrentDictionary<LaneId, LaneCollection>();
        public readonly ConcurrentDictionary<long, Intersection> nodes = new ConcurrentDictionary<long, Intersection>();
        public readonly ConcurrentDictionary<long, List<LaneId>> nodesToEdges = new ConcurrentDictionary<long, List<LaneId>>();
        public readonly ConcurrentDictionary<string, Bridge> Bridges = new ConcurrentDictionary<string, Bridge>();
        
        public bool TryClaimEdge(LaneId id)
        {
            // if (!edges.TryAdd(id, null)) 
            //     return false;
            //
            // nodesToEdges.TryAdd(id.StartNode, new List<LaneId>());
            // nodesToEdges.TryAdd(id.EndNode, new List<LaneId>());
            // nodesToEdges[id.StartNode].Add(id);
            // nodesToEdges[id.EndNode].Add(id);

            return true;
        }

    }
}
