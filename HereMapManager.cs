using II;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading;

public class HereMapManager
{
    private static readonly string apiKey = "RSIb7Xs2Zgghb9Dqf-df_zuiaqX20FS5g_2-By88T5s";
    private static readonly HttpClient client = new HttpClient();
    private static Dictionary<string, string> _memoriePoze = new Dictionary<string, string>();

    // Imagine de rezervă dacă Wikipedia nu găsește nimic
    private const string PlaceholderImage = "ms-appx:///Assets/StoreLogo.png";

    static HereMapManager()
    {
        // Wikipedia cere obligatoriu un User-Agent clar
        if (!client.DefaultRequestHeaders.Contains("User-Agent"))
        {
            client.DefaultRequestHeaders.Add("User-Agent", "SmartTravelApp/1.0 (contact: nica@example.com)");
        }
    }

    public static async Task<List<Attraction>> GetAttractionsFromApi(string city, string category)
    {
        var attractions = new List<Attraction>();

        if (string.IsNullOrWhiteSpace(city)) return attractions;

        try
        {
            string safeCity = Uri.EscapeDataString(city.Trim());
            string geocodeUrl = $"https://geocode.search.hereapi.com/v1/geocode?q={safeCity}&apiKey={apiKey}";

            var response = await client.GetStringAsync(geocodeUrl);
            var json = JObject.Parse(response);

            if (json["items"] == null || !json["items"].Any()) return attractions;

            var firstResult = json["items"][0];

            // Extragere coordonate cu punct, nu virgulă
            var lat = firstResult["position"]["lat"].ToString().Replace(",", ".");
            var lng = firstResult["position"]["lng"].ToString().Replace(",", ".");

            string safeCategory = Uri.EscapeDataString(category);
            string discoverUrl = $"https://discover.search.hereapi.com/v1/discover?at={lat},{lng}&q={safeCategory}&lang=ro-RO&limit=20&apiKey={apiKey}";

            var discoverResponse = await client.GetStringAsync(discoverUrl);
            var discoverJson = JObject.Parse(discoverResponse);

            HashSet<string> numeUtilizate = new HashSet<string>();

            foreach (var item in discoverJson["items"])
            {
                string name = item["title"]?.ToString();
                if (string.IsNullOrEmpty(name) || !numeUtilizate.Add(name.ToLower())) continue;

                string address = item["address"]?["label"]?.ToString() ?? "Adresă indisponibilă";

                // Chemăm Wikipedia pentru poze
                string imageUrl = await GetRealImageFromWikipedia(name, city);

                // Dacă Wikipedia nu a returnat nimic, punem placeholder-ul
                if (string.IsNullOrEmpty(imageUrl)) imageUrl = PlaceholderImage;

                attractions.Add(new Attraction
                {
                    Name = name,
                    City = city,
                    Description = address,
                    ImageUrl = imageUrl
                });

                // Limităm la 12 rezultate pentru rapiditate
                if (attractions.Count >= 12) break;

                // O mică pauză ca să nu fim blocați de Wikipedia pentru spam
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("❌ EROARE API: " + ex.Message);
        }

        return attractions;
    }

    private static async Task<string> GetRealImageFromWikipedia(string attractionName, string city)
    {
        string cacheKey = attractionName + "_" + city;
        if (_memoriePoze.ContainsKey(cacheKey)) return _memoriePoze[cacheKey];

        try
        {
            // Căutăm atracția + orașul pentru precizie
            string query = Uri.EscapeDataString(attractionName + " " + city);
            string wikiUrl = $"https://en.wikipedia.org/w/api.php?action=query&generator=search&gsrsearch={query}&gsrlimit=1&prop=pageimages&format=json&pithumbsize=800";

            // AM MĂRIT TIMPUL LA 10 SECUNDE (era 3)
            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                var response = await client.GetAsync(wikiUrl, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    var json = JObject.Parse(content);

                    if (json["query"]?["pages"] != null)
                    {
                        var pages = json["query"]["pages"] as JObject;
                        var firstPage = pages.Properties().FirstOrDefault()?.Value;

                        if (firstPage?["thumbnail"] != null)
                        {
                            string source = firstPage["thumbnail"]["source"]?.ToString();
                            if (!string.IsNullOrEmpty(source))
                            {
                                _memoriePoze[cacheKey] = source;
                                return source;
                            }
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ Wiki Timeout pentru: {attractionName}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("Eroare Wiki: " + ex.Message);
        }

        _memoriePoze[cacheKey] = null;
        return null;
    }
}