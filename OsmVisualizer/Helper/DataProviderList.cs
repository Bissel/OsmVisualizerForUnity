using System;
using System.Collections.Generic;
using System.Linq;
using OsmVisualizer.Data.Provider;
using UnityEngine;

namespace OsmVisualizer.Helper
{
    [RequireComponent(typeof(Map))]
    public class DataProviderList : MonoBehaviour
    {
        [Serializable]
        public class SpeedLimits
        {
            public string tag;
            public int speed;
        }
        
        private AbstractSettingsProvider _settings;

        public float streetLaneWidthForSpeedLimitLess50 = 2.75f;
        public float streetLaneWidthForSpeedLimitLess70 = 3.25f;
        public float streetLaneWidthForSpeedLimitLess100 = 3.50f;
        public float streetLaneWidthForSpeedLimitOver100 = 3.75f;

        public float streetLaneWidthScaler = 1.0f;
        public float streetLaneWidthForSpeedScaler = 1.0f;

        public int defaultSpeedLimit = 50;
        public SpeedLimits[] speedLimits = 
        {
            new SpeedLimits { tag = "motorway", speed = 120 },
            new SpeedLimits { tag = "motorway_link", speed = 120 },
            new SpeedLimits { tag = "trunk", speed = 70 },
            new SpeedLimits { tag = "trunk_link", speed = 70 },
            new SpeedLimits { tag = "residential", speed = 30 },
            new SpeedLimits { tag = "unclassified", speed = 30 },
            new SpeedLimits { tag = "living_street", speed = 30 },
        };
        
        // ReSharper disable once MemberCanBePrivate.Global
        protected AbstractSettingsProvider Settings() => _settings ??= gameObject.GetComponent<Map>().settingsProvider;

        public List<Provider> DataProviders { get; protected set; }

        private Dictionary<long, Vector2> _customPositions; 
        private List<CustomTags.CustomTag> _customTags; 
        private Dictionary<long, List<long>> _customMerge;
        private List<CustomRemove.Lane> _customRemove;
        private Dictionary<string, int> _speedLimits;

        public void Start()
        {
            var cPos = GetComponent<CustomNodePosition>();
            _customPositions = new Dictionary<long, Vector2>();
            if(cPos)
                cPos.nodePositions.ForEach(n =>
                {
                    _customPositions.Add(n.node, n.position);
                });
            
            _customTags = (GetComponent<CustomTags>()?.customTags ?? new List<CustomTags.CustomTag>()).Where(t => t.use).ToList();
            _customRemove = (GetComponent<CustomRemove>()?.customRemove ?? new List<CustomRemove.Lane>()).ToList();
            var cMerge = GetComponent<CustomMerge>();
            _customMerge = new Dictionary<long, List<long>>();
            if (cMerge)
                cMerge.customMergers.ForEach(m =>
                {
                    _customMerge.Add(m.intersection, m.otherIntersections);
                });

            _speedLimits = new Dictionary<string, int>();
            foreach (var speedLimit in speedLimits)
            {
                _speedLimits.Add(speedLimit.tag, speedLimit.speed);
            }
            
            CreateList();
        }

        protected virtual void CreateList()
        {
            DataProviders = new List<Provider>
            {
                GetBasicConverter(),
                new SplitOutOfTileBounds(Settings()),
                new SplitRoadsOnIntersection(Settings()),
                new GenerateRails(Settings()),
                GetConvertToLanes(),
                GetIntersectionConverter(),
                new SetHeight(Settings()),
                new ConvertToBuildings(Settings()),
                new ConvertToLanduse(Settings()),
                new ConvertToWaterway(Settings()),
            };
        }

        protected BasicConverter GetBasicConverter()
        {
            return new BasicConverter(Settings(), _customTags, _customRemove, _customPositions);
        }

        protected GenerateIntersections GetIntersectionConverter(bool useIntersectionMerging = false)
        {
            return new GenerateIntersections(Settings(), _customMerge, useIntersectionMerging);
        }
        
        protected ConvertToLanes GetConvertToLanes(float subdivideDistance = float.NaN)
        {
            return new ConvertToLanes(
                Settings(), subdivideDistance,
                streetLaneWidthForSpeedLimitLess50, streetLaneWidthForSpeedLimitLess70,
                streetLaneWidthForSpeedLimitLess100, streetLaneWidthForSpeedLimitOver100,
                streetLaneWidthScaler, streetLaneWidthForSpeedScaler,
                _speedLimits, defaultSpeedLimit
            );
        }
    }
}