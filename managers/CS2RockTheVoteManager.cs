using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CS2RockTheVote.API;
using CS2ScreenMenuAPI;
using Microsoft.Extensions.Logging;

namespace CS2RockTheVote.managers;

public class CS2RockTheVoteManager : ICS2RockTheVote
{
    private readonly BasePlugin Plugin;
    private readonly ICS2MapCooldown CooldownManager;
    private readonly ICS2MapNominate NominateManager;
    private readonly ICS2MapCache MapCacheManager;
    private readonly ICS2NextMapVote NextMapVoteManager;
    private readonly ILogger<CS2RockTheVoteManager> Logger;
    private List<CCSPlayerController> PlayersWhoRTVd = new();
    private List<CancellationTokenSource> ActiveToggleCanRTVCancellationTokens = new();
    private uint RTVThreshold = 0;
    private bool CanRTV = false;

    public CS2RockTheVoteManager(CS2RockTheVote _plugin, ICS2MapCooldown _cooldownManager, ICS2MapNominate _nominateManager, ICS2MapCache _mapCacheManager, ICS2NextMapVote _mapVoteManager, ILogger<CS2RockTheVoteManager> _logger) 
    {
        Plugin = _plugin;
        Logger = _logger;
        CooldownManager = _cooldownManager;
        NominateManager = _nominateManager;
        MapCacheManager = _mapCacheManager;
        NextMapVoteManager = _mapVoteManager;
        MapCacheManager.ReloadActiveMapsList(Path.Combine(_plugin.ModulePath, "../maplist.txt"));

        // This is to stop people RTVing immediately after a new map loads.
        _plugin.RegisterListener<Listeners.OnMapStart>((_) =>
        {
            ActiveToggleCanRTVCancellationTokens.ForEach(ct => ct.Cancel());
            CanRTV = false;

            var cancellationTokenSource = new CancellationTokenSource();
            var ct = cancellationTokenSource.Token;
    
            // Todo maybe set convar `sv_hibernate_when_empty` to false and use _plugin.AddTimer instead of this.
            Task.Run(async () =>
            {
                await Task.Delay(45 * 1000);
                if (ct.IsCancellationRequested)
                {
                    Logger.LogWarning("Cancellation requested.");
                    return;
                }
                CanRTV = true;
            }, ct);
            
            ActiveToggleCanRTVCancellationTokens.Add(cancellationTokenSource);
            
        });
        
    }
    
    [ConsoleCommand("test")]
    public void OnTest(CCSPlayerController? player, CommandInfo info) 
    {
        NominateManager.Test();
    }


  
    [ConsoleCommand("css_rtv")]
    public void OnPlayerRTV(CCSPlayerController? player, CommandInfo info) 
    {
        if (player == null || !CanRTV) { player?.PrintToChat("[RTV] You can't RTV right now!"); return; }
        
        PlayersWhoRTVd.Add(player);
        Server.PrintToChatAll($"[RTV] {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has rocked the vote. ({ChatColors.Green}{PlayersWhoRTVd.Count}{ChatColors.Default}/{ChatColors.Green}{RTVThreshold}{ChatColors.Default}).");
        if (PlayersWhoRTVd.Count >= RTVThreshold)
        {
            PlayersWhoRTVd.Clear();
            CanRTV = false;
            NextMapVoteManager.StartNextMapVote();
        }
    }
    
    /// <summary>
    /// This function opens the nominate menu and allows players to choose/switch their nomination. 
    /// This command won't run if the `MapThatWonTheVote` variable in NextMapVoteManager.cs is NOT null (as this implies we already have a next map chosen).
    /// This command also won't run when `CanRTV` is false.
    /// </summary>
    [ConsoleCommand("css_nominate")]
    public void OnNominate(CCSPlayerController? player, CommandInfo info) 
    {
        if (player == null || !CanRTV || (NextMapVoteManager.GetMapThatWonVote() != null)) { player?.PrintToChat("[RTV] You can't nominate right now!"); return; };
        
        var mainMenu = new Menu(player, Plugin)
        {
            Title = "Nominate a map",
            ShowDisabledOptionNum = true,
            HasExitButon = true,
            PostSelect = PostSelect.Close,
            ShowPageCount = true
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
                        var subMenu = new Menu(player, Plugin)
                        {
                            Title = "", // to set later
                            HasExitButon = true,
                            IsSubMenu = true,
                            PrevMenu = mainMenu
                        };
                        subMenu.AddItem($"{((map.RawDescription == null) ? "This map has no description set." : map.RawDescription)}", (p, option) => { }, true);
                        
                        subMenu.AddItem($"", (p, option) => { }, true);
                        subMenu.AddItem($"Nominate this map", (p, option) => 
                        { 
                            bool? playerHasNominated = NominateManager.AddNomination(p, map);
                            if (playerHasNominated == null) 
                            {
                                Logger.LogCritical("Critical error when trying to add a nomination.");
                                return;
                            }
                            uint numOfVotes = NominateManager.GetNominationCount(map);
                            if (!playerHasNominated.Value) 
                            {
                                Server.PrintToChatAll($"[RTV] {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has nominated {ChatColors.Green}{map.ActualMapName}{ChatColors.Default}. It now has {ChatColors.Green}{numOfVotes}{ChatColors.Default} vote{((numOfVotes > 1) ? "s" : "")}.");    
                            } else 
                            {
                                Server.PrintToChatAll($"[RTV] {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has changed their nomination to {ChatColors.Green}{map.ActualMapName}{ChatColors.Default}. It now has {ChatColors.Green}{numOfVotes}{ChatColors.Default} vote{((numOfVotes > 1) ? "s" : "")}.");    
                            }
                        });                
                        subMenu.Display();  
                    });                   
                }
            }
            return;
        }
             
        foreach (var map in MapCacheManager.GetCachedWorkshopMaps()) 
        {
            // i.e. if there is no cooldown
            if (CooldownManager.GetCooldownInfo(map.MapID) == 0)
            {
                mainMenu.AddItem($"{map.ActualMapName}", (p, option) =>
                {
                    var subMenu = new Menu(player, Plugin)
                    {
                        Title = map.ActualMapName,
                        HasExitButon = true,
                        IsSubMenu = true,
                        PrevMenu = mainMenu,
                        PostSelect = PostSelect.Close
                    };
                    
                    // Description first
                    if (map.RawDescription == null) { subMenu.AddItem($"This map has no description set.", (p, option) => { }, true); }
                    else 
                    {
                        string[] splitLines = map.RawDescription.Split("\\n");
                        foreach (var splitLine in splitLines) 
                        {
                            subMenu.AddItem($"{splitLine}", (p, option) => { }, true);
                        }
                    }
                    
                    // Then add nominate option
                    subMenu.AddItem($"", (p, option) => { }, true);
                    subMenu.AddItem($"Nominate this map", (p, option) => 
                    { 
                        bool playerHasNominated = NominateManager.AddNomination(p, map);
                        uint numOfVotes = NominateManager.GetNominationCount(map);
                        if (!playerHasNominated)
                        {
                            Server.PrintToChatAll($"[RTV] {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has nominated {ChatColors.Green}{map.ActualMapName}{ChatColors.Default}. It now has {ChatColors.Green}{numOfVotes}{ChatColors.Default} vote{((numOfVotes > 1) ? "s" : "")}.");    
                        } else 
                        {
                            Server.PrintToChatAll($"[RTV] {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has changed their nomination to {ChatColors.Green}{map.ActualMapName}{ChatColors.Default}. It now has {ChatColors.Green}{numOfVotes}{ChatColors.Default} vote{((numOfVotes > 1) ? "s" : "")}.");    
                        }
                    });
                });
            } else 
            {
                mainMenu.AddItem($"{map.ActualMapName}", (p, option) => { }, true);
            }
        }

        mainMenu.Display();
        
    }
    
    /// <summary>
    /// This function listnens for every time a player switches team (which can include disconnects) and updates the RTV threshold based on this. 
    /// It also will start the next map vote if the correct conditions are met.
    /// </summary>
    /// <returns></returns>
    [GameEventHandler]
    public HookResult OnPlayerTeam(EventPlayerTeam @event, GameEventInfo info) 
    {
        // Every time somebody joins or leaves the team, we calculate the number of actively playing players, i.e. on T or CT team
        int activeOnlinePlayers = Utilities.GetPlayers().Where(pl => pl.Team != CsTeam.None || (pl.Team & CsTeam.Spectator) == 0).Count();
        RTVThreshold = activeOnlinePlayers switch
        {   
            (1) => 1, // TODO: remove, this is for debug for local testing
            (<= 3) => 2, // to ensure that when 1 player is online they can't just change it to any shitty map
            _ => (uint)(activeOnlinePlayers * (2/3f)) // seems to balance quite well
        };
        
        // If player left, try remove them from players who RTV'd
        if (@event.Disconnect)
        {
            if (@event.Userid != null) 
            {
                PlayersWhoRTVd.Remove(@event.Userid);
            }
        }

        // Same case if players moves onto a non-playing team. 
        CsTeam switchedTeam = (CsTeam) @event.Team;
        if ( !@event.Disconnect && (switchedTeam == CsTeam.None || (switchedTeam & CsTeam.Spectator) != 0)) 
        {
            if (@event.Userid != null) 
            {
                PlayersWhoRTVd.Remove(@event.Userid);
            }
        } 
        
        if (PlayersWhoRTVd.Count() >= RTVThreshold) 
        {
            PlayersWhoRTVd.Clear();
            CanRTV = false;
            NextMapVoteManager.StartNextMapVote();
        }
        
        return HookResult.Continue;
    }
     
}