using System.Collections.Concurrent;
using System.Globalization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CS2RockTheVote.API;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace CS2RockTheVote.managers;

public class MapCacheManager : ICS2MapCache
{
    private readonly ConcurrentBag<WorkshopMap> CachedWorkshopMaps;
    private readonly List<CancellationTokenSource> ActiveCacheTasks;
    private readonly CS2RockTheVote Plugin;
    private readonly ILogger<MapCacheManager> Logger;
    
    public MapCacheManager(CS2RockTheVote _plugin, ILogger<MapCacheManager> _logger) 
    {
        Plugin = _plugin;
        Logger = _logger;
        CachedWorkshopMaps = new();
        ActiveCacheTasks = new();
    }

    public void ReloadActiveMapsList(string filePath) 
    {
        ActiveCacheTasks.ForEach(ct => ct.Cancel());
        ActiveCacheTasks.Clear();
        CachedWorkshopMaps.Clear();
        
        IEnumerable<string> lines;
        try 
        {
            lines = File.ReadLines(filePath);
        } catch (Exception ex) 
        {
            Logger.LogCritical(ex.Message);
            Logger.LogCritical($"Could not open maplist file with path: {filePath}");
            return;
        }
        foreach (string line in lines) 
        {
            ulong workshopID;
            string[] args = line.Split(':');
            try
            {
                workshopID = Convert.ToUInt64(args[0]);
            } catch (Exception)
            {
                Logger.LogCritical($"Invalid workshopID in maplist: {args[0]}. Refer to the example maplist.txt for correct file format.");
                continue;
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var ct = cancellationTokenSource.Token;

            Task.Run(async () =>
            {
                string? mapName = await GetMapNameFromID(workshopID);
                Logger.LogWarning($"map name for {workshopID} is {mapName}");
                if (mapName == null)
                {
                    Logger.LogCritical($"Could not fetch map name for workshop ID {workshopID}");
                    return;
                }
                if (ct.IsCancellationRequested) 
                {
                    return;
                }
                CachedWorkshopMaps.Add(new(workshopID, mapName, (args.Length > 1) ? args[1] : null));
            }, ct);

            ActiveCacheTasks.Add(cancellationTokenSource);
            
        }
    }

    private async Task<string?> GetMapNameFromID(ulong workshopID) 
    {
        //Logger.LogInformation($"trying to grab name from workshopID " + workshopID);
        //var htmlBody = await HttpClient.GetStringAsync($"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopID}");

        using (var client = new HttpClient()) 
        {
            var response = await client.GetAsync($"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopID}");
            
            // This has never been fired as far I can see, but it's a good fail-safe to have I suppose? 
            // I don't wanna deal with this exponential retry bullshit and import some uncessesary library :)
            if (!response.IsSuccessStatusCode) 
            {
                Logger.LogCritical($"HTTP RESPONSE CODE: {response.StatusCode}");
                await Task.Delay(5000); // wait 5s then try again...
                return await GetMapNameFromID(workshopID);
            }
            
            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(await response.Content.ReadAsStringAsync());
            
            return document.DocumentNode.SelectSingleNode("//div[contains(@class, 'workshopItemTitle')]")?.InnerText;          
        }       
    }

    public WorkshopMap? GetMapFromWorkshopID(ulong workshopID)
    {
        foreach (var map in CachedWorkshopMaps) { if (map.MapID == workshopID) { return map; } }
        return null;
    }

    public IEnumerable<WorkshopMap>? GetMapsFromName(string mapName)
    {
        return CachedWorkshopMaps
        .Where(map => map.ActualMapName.Contains(mapName));
    }
    
    public List<WorkshopMap> GetCachedWorkshopMaps() 
    {
        return CachedWorkshopMaps.ToList();
    }
        
}