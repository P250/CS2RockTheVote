using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using CS2RockTheVote.API;
using CS2ScreenMenuAPI;

namespace CS2RockTheVote.managers;

public class NextMapVoteManager : ICS2NextMapVote
{

    private CS2RockTheVote Plugin;
    private ICS2MapNominate NominateManager;
    private bool NextMapVoteActive = false;
    private static uint NEXT_MAPVOTE_VOTE_TIME = 20;
    private readonly Dictionary<WorkshopMap, uint> MapVoteCount = new();
    private WorkshopMap? MapThatWonTheVote = null; // if this is null then we should be able to rtv/nominate, otherwise we should disable this
    private uint RoundsPlayed = 0;
    private static readonly uint MAX_ROUNDS = 5;
    
    public NextMapVoteManager(CS2RockTheVote _plugin, ICS2MapNominate _nominateManager)
    {
        Plugin = _plugin;
        NominateManager = _nominateManager;
    }
    
    [ConsoleCommand("css_timeleft")]
    public void OnPlayerTimeleft(CCSPlayerController? player, CommandInfo info) 
    {
        if (player == null) { return; }
        uint timeleft = MAX_ROUNDS - RoundsPlayed;
        if (timeleft <= 0)
        {
            player.PrintToChat($"[RTV] This is the last round.");
        }
        else
        {
            player.PrintToChat($"[RTV] Map will change after {timeleft} round{((timeleft == 1) ? "" : "s")}.");
        }
    }
    
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info) 
    {
        // I believe reason 16 is that warmup has ended. This will stop /timeleft showing 9 rounds instead of 10 on warmup ending.
        if (@event.Reason == 16) { return HookResult.Continue; }
        
        RoundsPlayed++;
        
        // If next map isn't null that means RTV was successful and we can switch map now that we've played 10 rounds.
        if (RoundsPlayed >= MAX_ROUNDS && GetMapThatWonVote() != null) {
            Console.WriteLine("We wanna switch map now mate!!!");    
            // todo switch map
            return HookResult.Continue;
        }
        
        // Otherwise we haven't reached 10 rounds yet but we are close, so let's do a next map vote!
        
        // todo we can probably simplify this logic, I'll probs rewrite this whole function.
        if (!IsNextMapVoteActive() && (RoundsPlayed >= MAX_ROUNDS - 3) && GetMapThatWonVote() == null)
        {
            // todo schedule a map vote to start on the next round start + 2 seconds :> 
            Task.Run(async () =>
            {
                await Task.Delay(8000);
                await Server.NextFrameAsync(() =>
                {
                    StartNextMapVote();
                });
            });
        }
        return HookResult.Continue;
    }
    
    public void StartNextMapVote() 
    {
        if (NextMapVoteActive) { return; }
        NextMapVoteActive = true;
        List<Menu> activeMenus = new();
        IEnumerable<WorkshopMap> votingList = NominateManager.ConstructVotingList();
        foreach (var player in Utilities.GetPlayers()) 
        {
            var menu = new Menu(player, Plugin)
            {
                Title = $"Next map vote - {NEXT_MAPVOTE_VOTE_TIME}s",
                HasExitButon = false,
                PostSelect = PostSelect.Close      
            };
            
            foreach (var map in votingList) 
            {
                Console.WriteLine(map.ActualMapName);
                MapVoteCount.Add(map, 0);
                menu.AddItem(map.ActualMapName, (pl, option) =>
                {
                    MapVoteCount[map] += 1;
                    Server.PrintToChatAll($"[RTV] {ChatColors.Green}{player.PlayerName}{ChatColors.Default} has voted for {ChatColors.Green}{map.ActualMapName}{ChatColors.Default}. It now has {ChatColors.Green}{MapVoteCount[map]}{ChatColors.Default} vote{(MapVoteCount[map] > 1 ? "s" : "")}.");
                    activeMenus.Remove(menu);
                });
            }
            
            activeMenus.Add(menu);
            menu.Display();
            
        }

        // Start decrementing the counter
        Task.Run(async () =>
        {
            uint timeleft = NEXT_MAPVOTE_VOTE_TIME;
            while (timeleft > 0)
            {
                await Server.NextFrameAsync(() =>
                {
                    timeleft--;
                    foreach (var menu in activeMenus)
                    {
                        menu.Title = $"Next map vote - {timeleft}s";
                        menu.Refresh();
                    }
                });        
                await Task.Delay(1000);
            }
            Server.NextFrame(() =>
            {
                activeMenus.ForEach(menu => menu.Dispose());
                float fraction = ResetEverything();
                
                string message = (MAX_ROUNDS - RoundsPlayed) switch 
                {
                    (0) => "at the end of this round",
                    (1) => "next round",
                    _ => $"in {MAX_ROUNDS - RoundsPlayed} rounds"
                };
                
                Server.PrintToChatAll($"[RTV] {ChatColors.Green}{MapThatWonTheVote?.ActualMapName}{ChatColors.Default} won the map with {(int)(fraction * 100)}% of total votes. Map will change {ChatColors.Green}{message}{ChatColors.Default}.");
            });
        });
        
    }
    
    public bool IsNextMapVoteActive() 
    {
        return this.NextMapVoteActive;
    }
    
    public WorkshopMap? GetMapThatWonVote() 
    {
        return MapThatWonTheVote;
    }
    
    private long ResetEverything() 
    {
        MapThatWonTheVote = MapVoteCount.OrderByDescending(kv => kv.Value).First().Key; // won't be null
        long totalVotes = MapVoteCount.Values.Sum(_ => _);
        long winnerVotes = MapVoteCount[MapThatWonTheVote.Value];
        
        MapVoteCount.Clear();
        NextMapVoteActive = false;

        return (winnerVotes / totalVotes);
    }
    
    
}