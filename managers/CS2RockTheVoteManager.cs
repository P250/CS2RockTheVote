using System.Diagnostics.SymbolStore;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CS2RockTheVote.API;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace CS2RockTheVote.managers;

public readonly struct WorkshopMap(ulong _mapID, string _actualMapName, string? _description = null)
{
    public ulong MapID { get; init; } = _mapID;
    public string ActualMapName { get; init; } = _actualMapName;
    public string? Description { get; init; } = _description;
}

public class CS2RockTheVoteManager : ICS2RockTheVote
{

    private readonly List<WorkshopMap> CachedWorkshopMaps = new();
    private readonly List<Task> ActiveCacheTasks = new();
    private readonly ILogger<CS2RockTheVoteManager> Logger;

    public CS2RockTheVoteManager(CS2RockTheVote _plugin, ILogger<CS2RockTheVoteManager> _logger) 
    {
        Logger = _logger;
        _plugin.RegisterAllAttributes(this);
        
        ReloadActiveMapsList(Path.Combine(_plugin.ModulePath, "../maplist.txt"));

    }
    
    [ConsoleCommand("css_currentmap")]
    public void OnCurrentMapCommand(CCSPlayerController? player, CommandInfo info) 
    {
        //info.ReplyToCommand($"Current map: {Server.MapName}");
        foreach (var map in CachedWorkshopMaps) 
        {
            Console.WriteLine($"ID: {map.MapID} Name: {map.ActualMapName} Desc: {map.Description}");
        }
    }

    public void ChangeMap(long workshopID)
    {
        
    }

    public void ReloadActiveMapsList(string filePath)
    {
        CachedWorkshopMaps.Clear();
        ActiveCacheTasks.ForEach((task) => task.Dispose());
        ActiveCacheTasks.Clear();
        
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
            } catch (Exception ex)
            {
                Logger.LogCritical($"Invalid workshopID in maplist: {args[0]}. Refer to the example maplist.txt for correct file format.");
                break;
            }
            if (args.Length == 1) // length 1, workshop id and no description
            {
                // If it's a comment we ignore that line.
                if (args[0].StartsWith("//")) { continue; }
                //CachedWorkshopMaps.Add(new(workshopID, GetActualMapName(workshopID).Result));
                ActiveCacheTasks.Add(Task.Run(async () =>
                {
                    string? mapName = await GetMapNameFromID(workshopID);
                    CachedWorkshopMaps.Add(new(workshopID, (mapName == null) ? "unk" : mapName));
                }));
            }
            else if (args.Length == 2) // length 2, workshop id AND description
            {
                //CachedWorkshopMaps.Add(new(workshopID, GetActualMapName(workshopID).Result, args[1]));
                ActiveCacheTasks.Add(Task.Run(async () =>
                {
                    string? mapName = await GetMapNameFromID(workshopID);
                    CachedWorkshopMaps.Add(new(workshopID, (mapName == null) ? "unk" : mapName, args[1]));
                }));
            }
        }
    }
    
    private async Task<string?> GetMapNameFromID(ulong workshopID) 
    {

        using HttpClient client = new();
        var htmlBody = await client.GetStringAsync($"https://steamcommunity.com/sharedfiles/filedetails/?id={workshopID}");

        HtmlDocument document = new HtmlDocument();
        document.LoadHtml(htmlBody);

        return document.DocumentNode.SelectSingleNode("//div[contains(@class, 'workshopItemTitle')]")?.InnerText;

    } 
    
}