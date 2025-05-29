using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CS2RockTheVote.API;
using CS2RockTheVote.misc;
using CS2ScreenMenuAPI;
using Microsoft.Extensions.Logging;

namespace CS2RockTheVote.managers;

public class CS2RockTheVoteManager : ICS2RockTheVote
{
    private readonly BasePlugin Plugin;
    private readonly ICS2MapCooldown CooldownManager;
    private readonly ICS2MapNominate NominateManager;
    private readonly ICS2MapCache MapCacheManager;
    private readonly ILogger<CS2RockTheVoteManager> Logger;
    private List<CCSPlayerController> PlayersWhoRTVd = new();
    
    // todo we need to scale it based on how many players are online
    private uint RTVThreshold = 0;

    public CS2RockTheVoteManager(CS2RockTheVote _plugin, ICS2MapCooldown _cooldownManager, ICS2MapNominate _nominateManager, ICS2MapCache _mapCacheManager, ILogger<CS2RockTheVoteManager> _logger) 
    {
        Plugin = _plugin;
        Logger = _logger;
        CooldownManager = _cooldownManager;
        NominateManager = _nominateManager;
        MapCacheManager = _mapCacheManager;
        MapCacheManager.ReloadActiveMapsList(Path.Combine(_plugin.ModulePath, "../maplist.txt"));
    }
  
    [ConsoleCommand("css_rtv")]
    public void OnPlayerRTV(CCSPlayerController? player, CommandInfo info) 
    {
        if (player == null) { return; }
        PlayersWhoRTVd.Add(player);

        if (PlayersWhoRTVd.Count == RTVThreshold)
        {
            // start next map vote bober kurwa
            PlayersWhoRTVd.Clear();
        }
        else
        {
            Server.PrintToChatAll($"[RTV] {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has rocked the vote. ({ChatColors.Green}{PlayersWhoRTVd.Count}{ChatColors.Default}/{ChatColors.Green}{RTVThreshold}){ChatColors.Default}.");
        }
        
    }
    
    [ConsoleCommand("css_nominate")]
    public void OnRTV(CCSPlayerController? player, CommandInfo info) 
    {
        if (player == null) { return; }
        
        if (info.ArgCount > 1) 
        {
            string nominatedMapName = info.GetArg(2);
            var mapsFromName = MapCacheManager.GetMapsFromName(nominatedMapName);
            if (mapsFromName == null) 
            {
                // todo print no map found  
                 
            } else if (mapsFromName.Count() == 0) 
            {
                // todo print no map found
                
            } else if (mapsFromName.Count() > 1) 
            {
                // todo print which maps were found 
            
            } else 
            {
                // otherwise we found just one map, so add it as a nomination now.
                NominateManager.AddNomination(player, mapsFromName.First());
                this.UpdateRTVThreshold();
                // todo print that player nominated map
            }
            return;
        }
        
        /**
        * Todo:
        1. Show menu with all maps that are cached
        2. Gray out the maps which r on cooldown 
        3. Allow user to check descriptions for each map
        4. Show cooldown time left for map
        **/
        
        var menu = new Menu(player, Plugin)
        {
            Title = "Nominate a map",
            ShowDisabledOptionNum = true,
            HasExitButon = true,
            PostSelect = PostSelect.Reset,
            ShowPageCount = true
        };
        
        foreach (var map in MapCacheManager.GetCachedWorkshopMaps()) 
        {
            // i.e. if there is no cooldown
            if (CooldownManager.GetCooldownInfo(map.MapID) == 0)
            {
                menu.AddItem($"{map.ActualMapName}", (p, option) =>
                {
                    player.PrintToChat("U JUST CLICKED " + map.ActualMapName);
                    // todo show description then an option to vote it or to go back to where you were
                });
            } else 
            {
                menu.AddItem($"{map.ActualMapName}", (p, option) => { }, true);
            }
        }

        menu.Display();
        
    }
    
    public void UpdateRTVThreshold() 
    {
        // todo calculate RTV threshold based on how many players are on server
    }
    
    public void ChangeMap(ulong workshopID) 
    {
        return;
    }
      
}