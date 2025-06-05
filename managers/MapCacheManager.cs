using CS2RockTheVote.API;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace CS2RockTheVote.managers;

public class MapCacheManager : ICS2MapCache
{
    private readonly List<WorkshopMap> CachedWorkshopMaps;
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
            Logger.LogWarning(line);
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
                if (mapName == null) 
                {
                    Logger.LogCritical($"Could not fetch map name for workshop ID {workshopID}");
                    return;
                }
                if (ct.IsCancellationRequested) 
                {
                    return;
                }
                CachedWorkshopMaps.Add(new(workshopID, mapName));
            }, ct);

            ActiveCacheTasks.Add(cancellationTokenSource);
            
        }
    }

    private async Task<string?> GetMapNameFromID(ulong workshopID) 
    {
        Logger.LogInformation($"trying to grab name from workshopID " + workshopID);
        //var htmlBody = await HttpClient.GetStringAsync($"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopID}");

        using (var client = new HttpClient()) 
        {
            var htmlBody = await client.GetStringAsync($"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopID}");

            HtmlDocument document = new HtmlDocument();
            document.LoadHtml(htmlBody);

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
        return CachedWorkshopMaps;
    }
     
}