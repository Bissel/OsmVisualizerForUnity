using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OsmVisualizer.Data.Request
{
    public static class Cache
    {
        private static List<string> _cachedKeys;

        private const string CacheDirectory = "./osm_cache";
        private const string DirectorySeparator = "/";

        private static bool _enabled;
        
        private static string GetPath(string key) => CacheDirectory + DirectorySeparator + key + ".json";

        public static string GetCached(string key)
        {
            if (!IsEnabled() || !HasCache(key))
                return null;

            var sr = new StreamReader(GetPath(key), Encoding.UTF8);
            return sr.ReadToEnd();
        }
        
        public static void WriteCache(string key, string data)
        {
            if(!IsEnabled())
                return;
            
            _cachedKeys.Add(key);
            var sw = new StreamWriter(GetPath(key), false, Encoding.UTF8);
            sw.Write(data);
            sw.Close();
        }

        public static bool HasCache(string key)
        {
            GenerateCacheKeys();

            return _cachedKeys.Contains(key);
        }

        public static bool IsEnabled() => _enabled;

        // load cached filenames;
        private static void GenerateCacheKeys()
        {
            if (_cachedKeys != null)
                return;

            if (!Directory.Exists(CacheDirectory))
            {
                try
                {
                    Debug.Log("Cache Directory dont exist");
                    Directory.CreateDirectory(CacheDirectory);
                }
                catch(IOException e)
                {
                    Debug.LogError(e);
                    _cachedKeys = new List<string>();
                    return;
                }
            }
            
            _cachedKeys = new List<string>();
            foreach (var fileName in Directory.GetFiles(CacheDirectory))
            {
                _cachedKeys.Add(Path.GetFileNameWithoutExtension(fileName));
            }
            _enabled = true;
        }

        public static void ClearCache(string key = null)
        {
            if (key == null)
            {
                foreach (var k in _cachedKeys)
                {
                    File.Delete(GetPath(k));
                }

                _cachedKeys = null;
            }
            else
            {
                if (HasCache(key))
                {
                    File.Delete(GetPath(key));
                    _cachedKeys.Remove(key);
                }
            }
        }
    }
}