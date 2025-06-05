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
    private List<CancellationTokenSource> ActiveToggleCanRTVCancellationTokens = new();
    private uint RTVThreshold = 0;
    private bool CanRTV = false;

    public CS2RockTheVoteManager(CS2RockTheVote _plugin, ICS2MapCooldown _cooldownManager, ICS2MapNominate _nominateManager, ICS2MapCache _mapCacheManager, ILogger<CS2RockTheVoteManager> _logger) 
    {
        Plugin = _plugin;
        Logger = _logger;
        CooldownManager = _cooldownManager;
        NominateManager = _nominateManager;
        MapCacheManager = _mapCacheManager;
        MapCacheManager.ReloadActiveMapsList(Path.Combine(_plugin.ModulePath, "../maplist.txt"));

        // This is to stop people RTVing immediately after a new map loads.
        _plugin.RegisterListener<Listeners.OnMapStart>((_) =>
        {
            ActiveToggleCanRTVCancellationTokens.ForEach(ct => ct.Cancel());
            CanRTV = false;

            var cancellationTokenSource = new CancellationTokenSource();
            var ct = cancellationTokenSource.Token;
    
            // Todo change convar `sv_hibernate_when_empty` to false and use _plugin.AddTimer
            Task.Run(async () =>
            {
                Logger.LogWarning("bro we got into the task timer...!");
                await Task.Delay(15 * 1000);
                Logger.LogWarning("ok 15s has passed");
                if (ct.IsCancellationRequested)
                {
                    Logger.LogWarning("Shit, cancellation was requested mate, sorry.");
                    return;
                }
                CanRTV = true;
            }, ct);
            
            ActiveToggleCanRTVCancellationTokens.Add(cancellationTokenSource);
            
        });
        
    }
  
    [ConsoleCommand("css_rtv")]
    public void OnPlayerRTV(CCSPlayerController? player, CommandInfo info) 
    {
        if (player == null || !CanRTV) { Logger.LogWarning($"Yo my g, CanRTV is {CanRTV} right now."); return; }
        
        PlayersWhoRTVd.Add(player);
        if (PlayersWhoRTVd.Count >= RTVThreshold)
        {
            PlayersWhoRTVd.Clear();
            CanRTV = false;
            StartNextMapVote();
        }
        else
        {
            Server.PrintToChatAll($"[RTV] {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has rocked the vote. ({ChatColors.Green}{PlayersWhoRTVd.Count}{ChatColors.Default}/{ChatColors.Green}{RTVThreshold}){ChatColors.Default}.");
        }
        
    }
    
    [ConsoleCommand("css_test")]
    [ConsoleCommand("test")]
    public void OnTEst(CCSPlayerController? player, CommandInfo info) 
    {
        info.ReplyToCommand("Current maps on cooldown:");
        foreach (var kv in CooldownManager.GetMapsOnCooldown()) 
        {
            info.ReplyToCommand($"map: {kv.Key} | on cooldown: {kv.Value}");
        }
        /**
        * TODO:
        Check if RTV works, setup that shit
        Check if nominate works, setup that shit also
        menus for everything
        then we are done.
        *
        **/
    }
    
    
    [ConsoleCommand("css_nominate")]
    public void OnNominate(CCSPlayerController? player, CommandInfo info) 
    {
        if (player == null) { return; }
        
        var mainMenu = new Menu(player, Plugin)
        {
            Title = "Nominate a map",
            ShowDisabledOptionNum = true,
            HasExitButon = true,
            PostSelect = PostSelect.Reset,
            ShowPageCount = true
        };
        
        var subMenu = new Menu(player, Plugin)
        {
            Title = "", // to set later
            HasExitButon = true,
            IsSubMenu = true,
            PrevMenu = mainMenu
        };
        
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
                // todo show menu with maps that were found.
                foreach (var map in mapsFromName) 
                {
                    mainMenu.AddItem($"{map.ActualMapName}", (p, option) =>
                    {
                        //subMenu.AddItem($"{map.Description}", (p, option) => { });
                        //subMenu.Display();
                        // todo show description then an option to vote it or to go back to where you were
                        //p.PrintToChat($"U just clicked on {map.ActualMapName}");
                    });                   
                }
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
             
        foreach (var map in MapCacheManager.GetCachedWorkshopMaps()) 
        {
            // i.e. if there is no cooldown
            if (CooldownManager.GetCooldownInfo(map.MapID) == 0)
            {
                mainMenu.AddItem($"{map.ActualMapName}", (p, option) =>
                {
                    player.PrintToChat("U JUST CLICKED " + map.ActualMapName);
                    // todo show description then an option to vote it or to go back to where you were
                });
            } else 
            {
                mainMenu.AddItem($"{map.ActualMapName}", (p, option) => { }, true);
            }
        }

        mainMenu.Display();
        
    }
    
    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info) 
    {
        // Every time somebody joins or leaves the team, we calculate the number of actively playing players, i.e. on T or CT team
        int activeOnlinePlayers = Utilities.GetPlayers().Where(pl => pl.Team != CsTeam.None || (pl.Team & CsTeam.Spectator) == 0).Count();
        RTVThreshold = activeOnlinePlayers switch
        {   
            (1) => 1, // TODO: remove, this is for debug for local testing
            (<= 3) => 2, // to ensure that when 1 player is online they can't just change it to any shitty map
            _ => (uint)(activeOnlinePlayers * (2/3f))
        };
        
        // If player left, try remove them from players who RTV'd
        if (@event.Disconnect) 
        {
            if (@event.Userid != null) 
            {
                PlayersWhoRTVd.Remove(@event.Userid);
            }
            return HookResult.Continue;
        }

        // Same case if players moves onto a non-playing team. 
        CsTeam switchedTeam = (CsTeam) @event.Team;
        if ( switchedTeam == CsTeam.None || (switchedTeam & CsTeam.Spectator) != 0) 
        {
            if (@event.Userid != null) 
            {
                PlayersWhoRTVd.Remove(@event.Userid);
            }
            return HookResult.Continue;
        }
        
        if (PlayersWhoRTVd.Count() >= RTVThreshold) 
        {
            PlayersWhoRTVd.Clear();
            CanRTV = false;
            StartNextMapVote();
        }
        
        return HookResult.Continue;
    }
 
    private void StartNextMapVote() 
    {
        Logger.LogCritical("YAYYYYYY ASLJDJLASDKJLAS WE STARTED THE NEXT MAP VOTE MATE.");
    }
 
    public void ChangeMap(ulong workshopID) 
    {
        return;
    }
      
}