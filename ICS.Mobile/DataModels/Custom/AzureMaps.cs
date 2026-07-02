//public string AzureMapsAddrSearchHost { set; get; } = "https://atlas.microsoft.com/search/address/json";
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using System.Web;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Threading.Tasks;
using System.Reflection;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using static ICS.Portal.Data.Custom.AzureRouteResults;
using System.IO;
using System.Text.Json;



namespace ICS.Portal.Data.Custom;

public class AzureMaps
{

    public string AzureMapsAddrSearchHost { set; get; } = "https://atlas.microsoft.com/search/address/json";
    public string AzureMapsAddrSearchURI { set; get; } = "{AzureMapsHost}?subscription-key={AzureMapsSubscriptionKey}&api-version=1.0&language=en-US&query=";
    public string AzureMapsStaticImageHost { set; get; } = "https://atlas.microsoft.com/map/static/png";

    private string AzureMapsSubscriptionKey { set; get; } = "";
    private string AzureMapsClientId { set; get; } = "fce03aab-b348-4082-b9d2-da59080bfc37";
    private string AzureMapsAadTenant { set; get; } = "3d25794e-bfef-4f4c-a5fa-7e6a025ec3fb";
    private string AzureMapsAadAppId { set; get; } = "253026d3-1bf7-4ed4-9926-6f1fecdb142b";
    private string AzureMapsAppKey { set; get; } = "";

    public string JsonReturn { set; get; }

    public AzureMapsAddressSearchResults SearchResults { set; get; }

    private HttpClient httpClient;
    private bool keyok = false;
    private bool lastSearchOk = false;
    private bool lastStaticMapOk = false;

    public AzureMaps(string azureMapsSubscriptionKey = "")
    {
        if (!string.IsNullOrEmpty(azureMapsSubscriptionKey))
        {
            _ = SetMapsKey(azureMapsSubscriptionKey);
        }
        httpClient = new HttpClient();
    }

    public bool IsInitialized
    {
        get
        {
            if (string.IsNullOrEmpty(AzureMapsSubscriptionKey))
                keyok = false;
            return keyok;
        }
    }

    public bool WasSearchSuccessful
    {
        get { return lastSearchOk; }
    }

    public bool WasStaticMapImageSuccessful
    {
        get { return lastStaticMapOk; }
    }

    public bool SetMapsKey(string azureMapsSubscriptKey, string azureMapsAddrSearchHost = "")
    {
        if (!string.IsNullOrEmpty(azureMapsSubscriptKey))
            AzureMapsSubscriptionKey = azureMapsSubscriptKey;

        if (!string.IsNullOrEmpty(azureMapsAddrSearchHost))
            AzureMapsAddrSearchHost = azureMapsAddrSearchHost;

        if (!string.IsNullOrEmpty(AzureMapsAddrSearchHost) && !string.IsNullOrEmpty(AzureMapsSubscriptionKey))
        {
            AzureMapsAddrSearchURI = AzureMapsAddrSearchURI.Replace("{AzureMapsHost}", AzureMapsAddrSearchHost).Replace("{AzureMapsSubscriptionKey}", AzureMapsSubscriptionKey);
            keyok = true;
        }
        return keyok;
    }

    ~AzureMaps()
    {
        httpClient = null;
        SearchResults = null;
    }


    public async Task<AzureRouteResults.Summary?> GetDrivingDistanceAndTimeSummary(double originLat, double originLon, double destLat, double destLon)
    {
        if (!IsInitialized || originLat == 0 || originLon == 0 || destLat == 0 || destLon == 0) { return null; }

        string subscriptionKey = AzureMapsSubscriptionKey;
        string baseUrl = "https://atlas.microsoft.com/route/directions/json";

        string query = $"{originLat.ToString("F5", CultureInfo.InvariantCulture)},{originLon.ToString("F5", CultureInfo.InvariantCulture)}:{destLat.ToString("F5", CultureInfo.InvariantCulture)},{destLon.ToString("F5", CultureInfo.InvariantCulture)}";
        string requestUrl = $"{baseUrl}?subscription-key={subscriptionKey}&api-version=1.0&report=effectiveSettings&query={query}";

        double distance = 0;
        int travelTimeSecs = 0;

        using (HttpClient client = new HttpClient())
        {
            HttpResponseMessage response = await client.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                string jsonResult = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(jsonResult))
                {
                    AzureRouteResults.Root? routeInfo = DeserializeJsonResult(jsonResult);
                    // routes[0].summary.lengthInMeters // distance
                    // routes[0].summary.travelTimeInSeconds // estimated travel time
                    if (routeInfo is not null)
                    {
                        return routeInfo.Routes[0].Summary;
                    }


                }
            }
        }
        return null;
    }


    public AzureRouteResults.Root DeserializeJsonResult(string jsonResult)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        // Deserialize the JSON string into AzureRouteResults.Root class
        AzureRouteResults.Root routeResponse = System.Text.Json.JsonSerializer.Deserialize<AzureRouteResults.Root>(jsonResult, options);

        return routeResponse;
    }

    public async Task<AzureMapsAddressSearchResults> SearchAddressesAndGeocode(string AddrLine = "")
    {
        if (!IsInitialized || string.IsNullOrEmpty(AddrLine)) { return null; }

        try
        {
            lastSearchOk = false;
            string searchUrl = string.Concat(AzureMapsAddrSearchURI, HttpUtility.UrlEncode(AddrLine, Encoding.UTF8)); // Reiff Pl, Reading, PA 19606";
            var request = new HttpRequestMessage(HttpMethod.Get, new System.Uri(searchUrl));
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                JsonReturn = await response.Content.ReadAsStringAsync();
                if (!string.IsNullOrEmpty(JsonReturn))
                {
                    SearchResults = JsonConvert.DeserializeObject<AzureMapsAddressSearchResults>(JsonReturn,
                    new JsonSerializerSettings
                    {
                        MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
                        DateParseHandling = DateParseHandling.None,
                        Converters = { new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal } },
                    });

                    lastSearchOk = true;
                }
            }
            response = null; request = null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GeoCode Error to Azure Maps:   {ex.Message}");
            JsonReturn = "";

        }

        return SearchResults;
    }

    public async Task<byte[]> GetStaticMapImage(double latitude, double longitude, int zoom = 7, int width = 600, int height = 600)
    {
        if (!keyok) { return null; }

        try
        {
            lastStaticMapOk = false;
            string staticMapUrl = $"{AzureMapsStaticImageHost}?subscription-key={AzureMapsSubscriptionKey}&api-version=1.0&center={longitude.ToString("F5", CultureInfo.InvariantCulture)},{latitude.ToString("F5", CultureInfo.InvariantCulture)}&zoom={zoom}&width={width}&height={height}";
            var request = new HttpRequestMessage(HttpMethod.Get, new System.Uri(staticMapUrl));
            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                lastStaticMapOk = true;
                return await response.Content.ReadAsByteArrayAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Static Map Error to Azure Maps: {ex.Message}");
        }
        return null;
    }

    public async Task<byte[]> GetStaticMapWithMarkers(List<MapPoint> points, double centerLat, double centerLon, string itemPrefix = "Step ", int width = 800, int height = 800, int zoom = 14)
    {

        List<MapPoint> clusteredPoints = new();

        if (points is null && points.Count == 0)
            return null;

        try
        {

            // Constructing the pins parameter in the required format
            var pins = string.Join("&pins=", points.Select(p =>
                $"default|la-10%204|al.6|ls12|co{p.Color.Trim('#')}|lc{p.Color.Trim('#')}||'{HttpUtility.UrlEncode(p.Label)}'{p.Longitude.ToString("F5", CultureInfo.InvariantCulture)}%20{p.Latitude.ToString("F5", CultureInfo.InvariantCulture)}"));

            string staticMapUrl = $"{AzureMapsStaticImageHost}?api-version=2022-08-01&subscription-key={AzureMapsSubscriptionKey}&style=main&layer=basic&zoom={zoom}&height={height}&width={width}&center={centerLon.ToString("F5", CultureInfo.InvariantCulture)},{centerLat.ToString("F5", CultureInfo.InvariantCulture)}&pins={pins}";


            //var pins = string.Join("|", points.Select(p => $"'{HttpUtility.UrlEncode(p.Label)}'{p.Longitude.ToString(CultureInfo.InvariantCulture)}%20{p.Latitude.ToString(CultureInfo.InvariantCulture)}"));
            //string staticMapUrl = $"{AzureMapsStaticImageHost}?api-version=2022-08-01&subscription-key={AzureMapsSubscriptionKey}&style=main&layer=basic&zoom={zoom}&height={height}&width={width}&center={centerLon.ToString(CultureInfo.InvariantCulture)},{centerLat.ToString(CultureInfo.InvariantCulture)}&pins=default|la15+50|al0.66|lc003C62|co41D42A||{pins}";

            var request = new HttpRequestMessage(HttpMethod.Get, new System.Uri(staticMapUrl));

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Static Map Error to Azure Maps: {ex.Message}");
        }
        return null;
    }

    public async Task<byte[]> GetStaticMapWithMarkersAndClusters(List<MapPoint> points, double centerLat, double centerLon, string itemPrefix = "Step ", int width = 800, int height = 800, double clusterThresholdKm = 0.05, int zoom = 14)
    {

        List<MapPoint> clusteredPoints = new();

        if (points is null && points.Count == 0)
            return null;

        try
        {

            // get zoom level based on points and max distance
            if (points is not null && points.Count > 1)
            {


                clusteredPoints = points.Select(p => new MapPoint(p)).ToList();

                // Re-Label the clustered items as Step1, Step2, Step3...
                int ii = 0;
                clusteredPoints.ForEach(x => x.Label = ($"{++ii}"));

                // build the clustered points...
                clusteredPoints = ClusterPoints(clusteredPoints, clusterThresholdKm, itemPrefix);

                // get the zoom level based on the clustered points
                zoom = DetermineZoomLevel(centerLat, centerLon, clusteredPoints, width, height);
                var zoom2 = DetermineZoomLevel(clusteredPoints, width, height);

                // set zoom to the integer average of zoom and zoom2
                zoom = (int)Math.Round((zoom + zoom2) / 2.0);
                if (zoom == 20) zoom = 18;
                //zoom = zoom2;


            }
            else
            {
                clusteredPoints = points;
                //clusteredPoints = points.Select(p => new MapPoint(p)).ToList();

            }


            // Constructing the pins parameter in the required format
            var pins = string.Join("&pins=", clusteredPoints.Select(p =>
                $"default|la-10%204|al.6|ls12|co{p.Color.Trim('#')}|lc{p.Color.Trim('#')}||'{HttpUtility.UrlEncode(p.Label)}'{p.Longitude.ToString("F5", CultureInfo.InvariantCulture)}%20{p.Latitude.ToString("F5", CultureInfo.InvariantCulture)}"));

            string staticMapUrl = $"{AzureMapsStaticImageHost}?api-version=2022-08-01&subscription-key={AzureMapsSubscriptionKey}&style=main&layer=basic&zoom={zoom}&height={height}&width={width}&center={centerLon.ToString("F5", CultureInfo.InvariantCulture)},{centerLat.ToString("F5", CultureInfo.InvariantCulture)}&pins={pins}";


            //var pins = string.Join("|", points.Select(p => $"'{HttpUtility.UrlEncode(p.Label)}'{p.Longitude.ToString(CultureInfo.InvariantCulture)}%20{p.Latitude.ToString(CultureInfo.InvariantCulture)}"));
            //string staticMapUrl = $"{AzureMapsStaticImageHost}?api-version=2022-08-01&subscription-key={AzureMapsSubscriptionKey}&style=main&layer=basic&zoom={zoom}&height={height}&width={width}&center={centerLon.ToString(CultureInfo.InvariantCulture)},{centerLat.ToString(CultureInfo.InvariantCulture)}&pins=default|la15+50|al0.66|lc003C62|co41D42A||{pins}";

            var request = new HttpRequestMessage(HttpMethod.Get, new System.Uri(staticMapUrl));

            var response = await httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var img = await response.Content.ReadAsByteArrayAsync();
                return img;
                //return AddLegendToMap(img, clusteredPoints, itemPrefix, null);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Static Map Error to Azure Maps: {ex.Message}");
        }
        return null;
    }


    public static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double R = 6371.0; // Radius of the Earth in kilometers
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);
        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c; // Distance in kilometers
    }

    public static double ToRadians(double angle)
    {
        return angle * Math.PI / 180.0;
    }

    public static (MapPoint FurthestPoint, double MaxDistance) GetFurthestPoint(double centerLat, double centerLon, List<MapPoint> points)
    {
        double maxDistance = 0.0;
        MapPoint furthestPoint = null;

        foreach (var point in points)
        {
            double distance = HaversineDistance(centerLat, centerLon, point.Latitude, point.Longitude);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                furthestPoint = point;
            }
        }

        return (furthestPoint, maxDistance);
    }

    public static int DetermineZoomLevel(double centerLat, double centerLon, List<MapPoint> points, double mapWidthInPixels, double mapHeightInPixels)
    {
        var zoomLevels = new[]
        {
            36000.0, // Zoom 0 (approximation in kilometers)
            18000.0, // Zoom 1
            9000.0,  // Zoom 2
            4500.0,  // Zoom 3
            2250.0,  // Zoom 4
            1125.0,  // Zoom 5
            563.0,   // Zoom 6
            282.0,   // Zoom 7
            141.0,   // Zoom 8
            70.8,    // Zoom 9
            35.4,    // Zoom 10
            17.7,    // Zoom 11
            8.9,     // Zoom 12
            4.4,     // Zoom 13
            2.2,     // Zoom 14
            1.1,     // Zoom 15
            0.55,    // Zoom 16
            0.28,    // Zoom 17
            0.14,    // Zoom 18
            0.07,    // Zoom 19
            0.035    // Zoom 20
        };

        var (furthestPoint, maxDistance) = GetFurthestPoint(centerLat, centerLon, points);

        // Approximate the map size in kilometers
        double mapSizeKm = (maxDistance / 0.391) * (Math.Max(mapWidthInPixels, mapHeightInPixels) / 256.0);

        for (int i = zoomLevels.Length - 1; i >= 0; i--)
        {
            if (mapSizeKm <= zoomLevels[i])
            {
                return i;
            }
        }

        return 0; // Default to the widest zoom if no match
    }

    public static int DetermineZoomLevel(List<MapPoint> points, int mapWidthInPixels, int mapHeightInPixels)
    {
        var boundingBox = GetBoundingBox(points);

        // Calculate the width and height of the bounding box in km
        double widthKm = HaversineDistance(boundingBox.MinLat, boundingBox.MinLon, boundingBox.MinLat, boundingBox.MaxLon);
        double heightKm = HaversineDistance(boundingBox.MinLat, boundingBox.MinLon, boundingBox.MaxLat, boundingBox.MinLon);

        // Adjust map width and height to take into account the map's aspect ratio
        double mapAspectRatio = mapWidthInPixels / mapHeightInPixels;
        double mapKm = Math.Max(widthKm / mapAspectRatio, heightKm); // Use the larger dimension to maintain aspect ratio

        // Approximate zoom levels in terms of visible area size in kilometers
        var zoomLevels = new[]
            {
            36000.0, // Zoom 0
            18000.0, // Zoom 1
            9000.0,  // Zoom 2
            4500.0,  // Zoom 3
            2250.0,  // Zoom 4
            1125.0,  // Zoom 5
            563.0,   // Zoom 6
            282.0,   // Zoom 7
            141.0,   // Zoom 8
            70.8,    // Zoom 9
            35.4,    // Zoom 10
            17.7,    // Zoom 11
            8.9,     // Zoom 12
            4.4,     // Zoom 13
            2.2,     // Zoom 14
            1.1,     // Zoom 15
            0.55,    // Zoom 16
            0.28,    // Zoom 17
            0.14,    // Zoom 18
            0.07,    // Zoom 19
            0.035    // Zoom 20
        };

        for (int i = zoomLevels.Length - 1; i >= 0; i--)
        {
            if (mapKm <= zoomLevels[i])
            {
                return i;
            }
        }

        return 0; // Default to the widest zoom if no match
    }

    public static (double MinLat, double MaxLat, double MinLon, double MaxLon) GetBoundingBox(List<MapPoint> points)
    {
        double minLat = points.Min(p => p.Latitude);
        double maxLat = points.Max(p => p.Latitude);
        double minLon = points.Min(p => p.Longitude);
        double maxLon = points.Max(p => p.Longitude);

        return (minLat, maxLat, minLon, maxLon);
    }

    public static List<MapPoint> ClusterPoints(List<MapPoint> points, double thresholdKm, string itemPrefix = "Step ")
    {
        var clusters = new List<List<MapPoint>>();
        var remainingPoints = new HashSet<MapPoint>(points);

        while (remainingPoints.Count > 0)
        {
            var currentCluster = new List<MapPoint>();
            var currentPoint = remainingPoints.FirstOrDefault();

            remainingPoints.Remove(currentPoint);
            currentCluster.Add(currentPoint);

            var pointsToCluster = new List<MapPoint>(remainingPoints);
            foreach (var point in pointsToCluster)
            {
                if (HaversineDistance(currentPoint.Latitude, currentPoint.Longitude, point.Latitude, point.Longitude) <= thresholdKm)
                {
                    currentCluster.Add(point);
                    remainingPoints.Remove(point);
                }
            }

            clusters.Add(currentCluster);
        }

        return clusters.Select(cluster =>
        {
            var averageLat = cluster.Average(p => p.Latitude);
            var averageLon = cluster.Average(p => p.Longitude);
            var memberLabels = string.Concat(itemPrefix, string.Join($", {itemPrefix}", cluster.Select(p => p.Label)));
            string combinedLabel = "";
            //HttpUtility.UrlEncode(itemPrefix)
            string firstItem = cluster.FirstOrDefault().Label;
            string lastItem = cluster.LastOrDefault().Label;
            if (firstItem != lastItem)
            {
                combinedLabel = $"{itemPrefix}{firstItem}-{lastItem.Replace(itemPrefix, "")}";
            }
            else
            {
                combinedLabel = $"{itemPrefix}{firstItem}";
            }



            return new MapPoint
            {
                Latitude = averageLat,
                Longitude = averageLon,
                Label = combinedLabel,
                Members = memberLabels,
                Color = cluster.FirstOrDefault().Color // Choose a representative color
            };
        }).ToList();
    }

    public static string LatLongFormat(double lat, double lon)
    {
        var typeLon = lon > 0 ? "E" : "W";
        lon = Math.Abs(lon);

        var typeLat = lat > 0 ? "N" : "S";
        lat = Math.Abs(lat);

        return $"{lat:F4}° {typeLat} {lon:F4}° {typeLon}";
    }
}


public class MapPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Color { get; set; }
    public string Label { get; set; }

    public string Members { get; set; }

    public MapPoint() { }

    // Copy constructor
    public MapPoint(MapPoint other)
    {
        Latitude = other.Latitude;
        Longitude = other.Longitude;
        Color = other.Color;
        Label = other.Label;
        Members = other.Members;
    }
}


public class AzureMapsAddressSearchResults
{
    [JsonProperty("summary")]
    public Summaries? Summary { get; set; }

    [JsonProperty("results")]
    public Result[]? Results { get; set; }

    public partial class Result
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("id")]
        public string? Id { get; set; }

        [JsonProperty("score")]
        public double? Score { get; set; }

        [JsonProperty("matchConfidence")]
        public MatchConfidence? MatchConfidence { get; set; }

        [JsonProperty("address")]
        public Address? Address { get; set; }

        [JsonProperty("position")]
        public Position? Position { get; set; }

        [JsonProperty("viewport")]
        public Viewport? Viewport { get; set; }

        [JsonProperty("entryPoints")]
        public EntryPoint[]? EntryPoints { get; set; }
    }

    public partial class Address
    {
        [JsonProperty("streetNumber")]
        public string? StreetNumber { get; set; }

        [JsonProperty("streetName")]
        public string? StreetName { get; set; }

        [JsonProperty("municipality")]
        public string? Municipality { get; set; }

        [JsonProperty("countrySecondarySubdivision")]
        public string? CountrySecondarySubdivision { get; set; }

        [JsonProperty("countrySubdivision")]
        public string? CountrySubdivision { get; set; }

        [JsonProperty("countrySubdivisionName")]
        public string? CountrySubdivisionName { get; set; }

        [JsonProperty("postalCode")]
        public string? PostalCode { get; set; }

        [JsonProperty("extendedPostalCode")]
        public string? ExtendedPostalCode { get; set; }

        [JsonProperty("countryCode")]
        public string? CountryCode { get; set; }

        [JsonProperty("country")]
        public string? Country { get; set; }

        [JsonProperty("countryCodeISO3")]
        public string? CountryCodeIso3 { get; set; }

        [JsonProperty("freeformAddress")]
        public string? FreeformAddress { get; set; }

        [JsonProperty("localName")]
        public string? LocalName { get; set; }
    }

    public partial class EntryPoint
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("position")]
        public Position? Position { get; set; }
    }

    public partial class Position
    {
        [JsonProperty("lat")]
        public double? Lat { get; set; }

        [JsonProperty("lon")]
        public double? Lon { get; set; }
    }

    public partial class MatchConfidence
    {
        [JsonProperty("score")]
        public long? Score { get; set; }
    }

    public partial class Viewport
    {
        [JsonProperty("topLeftPoint")]
        public Position? TopLeftPoint { get; set; }

        [JsonProperty("btmRightPoint")]
        public Position? BtmRightPoint { get; set; }
    }

    public partial class Summaries
    {
        [JsonProperty("query")]
        public string? Query { get; set; }

        [JsonProperty("queryType")]
        public string? QueryType { get; set; }

        [JsonProperty("queryTime")]
        public long? QueryTime { get; set; }

        [JsonProperty("numResults")]
        public long? NumResults { get; set; }

        [JsonProperty("offset")]
        public long? Offset { get; set; }

        [JsonProperty("totalResults")]
        public long? TotalResults { get; set; }

        [JsonProperty("fuzzyLevel")]
        public long? FuzzyLevel { get; set; }
    }
}

public class AzureRouteResults
{
    // Root myDeserializedClass = JsonSerializer.Deserialize<AzureRouteResults>(myJsonResponse);
    public class EffectiveSetting
    {
        [JsonProperty("key")]
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonProperty("value")]
        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }

    public class Leg
    {
        [JsonProperty("summary")]
        [JsonPropertyName("summary")]
        public Summary? Summary { get; set; }

        [JsonProperty("points")]
        [JsonPropertyName("points")]
        public List<Point>? Points { get; set; }
    }

    public class Point
    {
        [JsonProperty("latitude")]
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    public class Report
    {
        [JsonProperty("effectiveSettings")]
        [JsonPropertyName("effectiveSettings")]
        public List<EffectiveSetting>? EffectiveSettings { get; set; }
    }

    public class Root
    {
        [JsonProperty("formatVersion")]
        [JsonPropertyName("formatVersion")]
        public string? FormatVersion { get; set; }

        [JsonProperty("report")]
        [JsonPropertyName("report")]
        public Report? Report { get; set; }

        [JsonProperty("routes")]
        [JsonPropertyName("routes")]
        public List<Route>? Routes { get; set; }
    }

    public class Route
    {
        [JsonProperty("summary")]
        [JsonPropertyName("summary")]
        public Summary? Summary { get; set; }

        [JsonProperty("legs")]
        [JsonPropertyName("legs")]
        public List<Leg>? Legs { get; set; }

        [JsonProperty("sections")]
        [JsonPropertyName("sections")]
        public List<Section>? Sections { get; set; }
    }

    public class Section
    {
        [JsonProperty("startPointIndex")]
        [JsonPropertyName("startPointIndex")]
        public int StartPointIndex { get; set; }

        [JsonProperty("endPointIndex")]
        [JsonPropertyName("endPointIndex")]
        public int EndPointIndex { get; set; }

        [JsonProperty("sectionType")]
        [JsonPropertyName("sectionType")]
        public string? SectionType { get; set; }

        [JsonProperty("travelMode")]
        [JsonPropertyName("travelMode")]
        public string? TravelMode { get; set; }
    }

    public class Summary
    {
        [JsonProperty("lengthInMeters")]
        [JsonPropertyName("lengthInMeters")]
        public int LengthInMeters { get; set; }

        [JsonProperty("travelTimeInSeconds")]
        [JsonPropertyName("travelTimeInSeconds")]
        public int TravelTimeInSeconds { get; set; }

        [JsonProperty("trafficDelayInSeconds")]
        [JsonPropertyName("trafficDelayInSeconds")]
        public int TrafficDelayInSeconds { get; set; }

        [JsonProperty("trafficLengthInMeters")]
        [JsonPropertyName("trafficLengthInMeters")]
        public int TrafficLengthInMeters { get; set; }

        [JsonProperty("departureTime")]
        [JsonPropertyName("departureTime")]
        public DateTime DepartureTime { get; set; }

        [JsonProperty("arrivalTime")]
        [JsonPropertyName("arrivalTime")]
        public DateTime ArrivalTime { get; set; }
    }




}

