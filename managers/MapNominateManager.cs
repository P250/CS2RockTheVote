using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CS2RockTheVote.API;
using CS2ScreenMenuAPI;

namespace CS2RockTheVote.managers;

public class MapNominateManager : ICS2MapNominate
{

    private readonly Dictionary<ulong, WorkshopMap> PlayerMapVotes = new();
    private readonly Dictionary<WorkshopMap, uint> MapVoteCounts = new();
    private readonly CS2RockTheVote Plugin;
    private readonly ICS2MapCooldown CooldownManager;
    private readonly ICS2MapCache MapCacheManager;
    private readonly ICS2NextMapVote NextMapVote;
    private uint RoundsPlayed = 0;
    private static readonly uint MAX_ROUNDS = 10;
    private static bool StartNextMapVote = false;
    private static readonly Random RANDOM = new();

    public MapNominateManager(CS2RockTheVote _plugin, ICS2NextMapVote _nextMapVote, ICS2MapCooldown _cooldownManager, ICS2MapCache _mapCacheManager) 
    {
        Plugin = _plugin;
        CooldownManager = _cooldownManager;
        MapCacheManager = _mapCacheManager;
        NextMapVote = _nextMapVote;
        
        // Reset state on map change
        _plugin.RegisterListener<Listeners.OnMapStart>((mapName) =>
        {
            RoundsPlayed = 0;
            PlayerMapVotes.Clear();
            MapVoteCounts.Clear();
        });
    }
    
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info) 
    {
        RoundsPlayed++;
        if (!NextMapVote.IsNextMapVoteActive() && RoundsPlayed >= MAX_ROUNDS - 3)
        {
            StartNextMapVote = true;
            // start change map vote, and respect which maps r on cooldown
            NextMapVote.StartNextMapVote();
        }
        return HookResult.Continue;
    }

    public void AddNomination(CCSPlayerController player, WorkshopMap map)
    {
        PlayerMapVotes[player.SteamID] = map;
        // todo we want to return true if they already had a nomination.
    }

    //  
    public IEnumerable<WorkshopMap> ConstructVotingList()
    {
        List<WorkshopMap> SortedTop6MapVotes = MapVoteCounts
            .OrderByDescending(kv => kv.Value)
            .Take(6)
            .Select(kv => kv.Key)
            .ToList();


        // todo specify max size in config and ensure we can always construct a voting list
        int size = SortedTop6MapVotes.Count;
        if (size < 6)
        {
            while (6 - size > 0) 
            {
                var randomMap = MapCacheManager.GetCachedWorkshopMaps()[(int)RANDOM.NextInt64(MapCacheManager.GetCachedWorkshopMaps().Count)];
                if (SortedTop6MapVotes.Contains(randomMap) || CooldownManager.GetCooldownInfo(randomMap.MapID) != 0) { continue; }

                SortedTop6MapVotes.Add(randomMap);
                size++;
            }
        }
        
        return SortedTop6MapVotes;

    }

}