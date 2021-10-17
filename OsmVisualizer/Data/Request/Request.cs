using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using OsmVisualizer.Data.Types;
using UnityEngine;
using UnityEngine.Networking;

namespace OsmVisualizer.Data.Request
{
    public class RequestResult
    {
        public Result data;
        public RequestError error;
        public bool hasError => error != null;
    }

    public class RequestError
    {
        public readonly string error;
        public readonly string type;
        public readonly string context;

        public RequestError(string error, string type, string context = null)
        {
            this.error = error;
            this.type = type;
            this.context = context;
        }

        public override string ToString() => $"Error({type}{(context == null ? "" : " with " + context)}): {error}";
    }
    
    public static class Request
    {
        public const string QueryWay = "way;";
        public const string QueryRelation = "relation;";
        public const string QueryRoad = "way[\"highway\"];";
        public const string QueryTrafficSigns = "node[\"traffic_sign\"];way[\"traffic_sign\"];relation[\"traffic_sign\"];";

        public static string GetUri(SettingsProvider settings, string query, WGS84Bounds2 bounds, bool exact = true)
        {
            return string.Format(NumberFormatInfo.InvariantInfo,
                exact
                    ? "{1}?data=[out:json][timeout:{2}][bbox:{0}];{3}out geom({0});"
                    : "{1}?data=[out:json][timeout:{2}][bbox:{0}];{3}out geom;"
                ,
                bounds.ToMinMaxString(),
                settings.GetOverpassUri(), 
                settings.requestTimeout, 
                query
            );
        }

        public static Result ResultFromFile(string mapFolder, MapData.TilePos tileId)
        {
            // var sr = new StreamReader($"{mapFolder}/{tileId.X},{tileId.Y}.json", Encoding.UTF8);
            // return ConvertJsonToResult(sr.ReadToEnd());
            return ConvertJsonToResult(Resources.Load<TextAsset>($"{mapFolder}/{tileId.X},{tileId.Y}").ToString());
        }

        private static Result ConvertJsonToResult(string json) => JsonConvert.DeserializeObject<Result>(json);

        public static IEnumerator Query(RequestResult result, SettingsProvider settings, string query, WGS84Bounds2 bounds, bool exact = true )
        {
            var cacheKey = bounds.ToMinMaxString();
            var useCache = settings.useCache;
            if (useCache && Cache.HasCache(cacheKey))
            {
                // Debug.Log(cacheKey + " From Cache");
                result.data = ConvertJsonToResult(Cache.GetCached(cacheKey));
            }
            else
            {
                var uri = GetUri(settings, query, bounds, exact);
                
                Debug.Log(uri);

                using var webRequest = UnityWebRequest.Get(uri);
            
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    result.error = new RequestError(webRequest.error, "WebRequest", $"[{bounds.ToMinMaxString()}] {query}");
                }
                else
                {
                    while (!webRequest.downloadHandler.isDone)
                        yield return null;
                    
                    try
                    {
                        var data = webRequest.downloadHandler.text;
                        result.data = ConvertJsonToResult(data);
                        Cache.WriteCache(cacheKey, data);
                    }
                    catch(System.Exception e)
                    {
                        result.error = new RequestError(e.Message, "JsonConvert", $"[{bounds.ToMinMaxString()}] {query}");
                    }
                }
            }
        }
    }
}
