using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CS2RockTheVote.API;
using CS2ScreenMenuAPI;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace CS2RockTheVote.managers;

public class CS2RockTheVoteManager : ICS2RockTheVote
{

    private static readonly HttpClient HttpClient = new();
    private readonly List<WorkshopMap> CachedWorkshopMaps = new();
    private readonly List<Task> ActiveCacheTasks = new();
    private readonly BasePlugin Plugin;
    private readonly ICS2MapCooldown CooldownManager;
    private readonly ICS2MapNominate NominateManager;
    private readonly ILogger<CS2RockTheVoteManager> Logger;

    public CS2RockTheVoteManager(CS2RockTheVote _plugin, ICS2MapCooldown _cooldownManager, ICS2MapNominate _nominateManager, ILogger<CS2RockTheVoteManager> _logger) 
    {
        Plugin = _plugin;
        Logger = _logger;
        CooldownManager = _cooldownManager;
        NominateManager = _nominateManager;
        ReloadActiveMapsList(Path.Combine(_plugin.ModulePath, "../maplist.txt"));
    }
    
    [ConsoleCommand("test")]
    public void OnTest(CCSPlayerController? player, CommandInfo info)
    {
        foreach (var map in CachedWorkshopMaps)
        {
            Logger.LogWarning($"ID: {map.MapID} Name {map.ActualMapName} ");
        }
    }
    
    [ConsoleCommand("css_rtv")]
    public void OnRTV(CCSPlayerController? player, CommandInfo info) 
    {
        if (player == null) { return; }
        /**
        * Todo:
        1. Show menu with all maps that are cached
        2. Gray out the maps which r on cooldown 
        3. Allow user to check descriptions for each map
        4. Show cooldown time left for map
        **/
        var menu = new Menu(player, Plugin)
        {
            Title = "RTV",
            ShowDisabledOptionNum = true,
            HasExitButon = true,
            PostSelect = PostSelect.Reset
        };
        
        foreach (var map in CachedWorkshopMaps) 
        {
            // i.e. if there is no cooldown
            if (CooldownManager.GetCooldownInfo(map.MapID) == 0) 
            {
                menu.AddItem($"{map.ActualMapName}", (p, option) =>
                {
                    player.PrintToChat("U JUST CLICKED " + map.ActualMapName);
                });
            }
        }
        
    }
    
    [ConsoleCommand("css_nominate")]
    public void OnNominate(CCSPlayerController? player, CommandInfo info) 
    {
        /**
        * Todo:
        1. Search map with friendly name provided by player
        2. if multiple matches, display them and tell player to be specific
        3. Add map to voting pool.
        **/
    }

    public void ChangeMap(ulong workshopID) 
    {
        return;
    }

    public void ReloadActiveMapsList(string filePath)
    {
        ActiveCacheTasks.ForEach((task) => task.Dispose());
        ActiveCacheTasks.Clear();
        CachedWorkshopMaps.Clear();

        Logger.LogInformation("We got into the function yay");
        
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
            ActiveCacheTasks.Add(Task.Run(async () =>
            {
                string? mapName = await GetMapNameFromID(workshopID);
                if (args.Length == 1) 
                    CachedWorkshopMaps.Add(new(workshopID, (mapName == null) ? "unk" : mapName));     
                else 
                    CachedWorkshopMaps.Add(new(workshopID, (mapName == null) ? "unk" : mapName, args[1]));
            }));
        }
    }
    
    private async Task<string?> GetMapNameFromID(ulong workshopID) 
    {
        Logger.LogInformation($"we in the main func now mate");
        Logger.LogInformation(HttpClient.Timeout.ToString());
        var htmlBody = await HttpClient.GetStringAsync($"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopID}");

        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(htmlBody);

        return document.DocumentNode.SelectSingleNode("//div[contains(@class, 'workshopItemTitle')]")?.InnerText;

    }

    public IEnumerable<WorkshopMap> GetMapsFromName(string mapName)
    {
        throw new NotImplementedException();
    }

    public WorkshopMap? GetMapFromWorkshopID(ulong workshopID)
    {
        foreach (var map in CachedWorkshopMaps) { if (map.MapID == workshopID) { return map; } }
        return null;
    }
}