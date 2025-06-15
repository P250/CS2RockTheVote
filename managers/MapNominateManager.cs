using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CS2RockTheVote.API;
using Microsoft.Extensions.Logging;
namespace CS2RockTheVote.managers;

public class MapNominateManager : ICS2MapNominate
{

    private readonly Dictionary<ulong, WorkshopMap> PlayerMapVotes = new();
    private readonly Dictionary<WorkshopMap, uint> MapVoteCounts = new();
    private readonly CS2RockTheVote Plugin;
    private readonly ICS2MapCooldown CooldownManager;
    private readonly ICS2MapCache MapCacheManager;
    private readonly Random RANDOM = new();

    public MapNominateManager(CS2RockTheVote _plugin, ICS2MapCooldown _cooldownManager, ICS2MapCache _mapCacheManager) 
    {
        Plugin = _plugin;
        CooldownManager = _cooldownManager;
        MapCacheManager = _mapCacheManager;
        
        // Reset state on map change
        _plugin.RegisterListener<Listeners.OnMapStart>((mapName) =>
        {
            PlayerMapVotes.Clear();
            MapVoteCounts.Clear();
        });
    }
    
    public bool AddNomination(CCSPlayerController player, WorkshopMap map)
    {

        // First we check if they have an existing vote.
        bool prevEntryExists = PlayerMapVotes.TryGetValue(player.SteamID, out WorkshopMap prevVotedMap);
        if (prevEntryExists) 
        {
            MapVoteCounts[prevVotedMap] -= 1;
        }

        // Then we set their current vote
        PlayerMapVotes[player.SteamID] = map;
        bool noExistingVotes = MapVoteCounts.TryAdd(map, 1);
        if (!noExistingVotes) // i.e. if there ARE existing votes... :P 
        {
            MapVoteCounts[map] += 1;
        }

        return prevEntryExists;
        
    }

    public IEnumerable<WorkshopMap> ConstructVotingList()
    {
        List<WorkshopMap> SortedTop6MapVotes = MapVoteCounts
            .OrderByDescending(kv => kv.Value) // Taking the highest votes first
            .Take(6) // If there are less than 6 it will take as many as it can
            .Where(kv => kv.Value > 0) // Ensure we only pick maps with votes!
            .Select(kv => kv.Key) // Then take the maps themselves only
            .ToList(); 

        // todo specify max size in config and ensure we can always construct a voting list
        int size = SortedTop6MapVotes.Count;
        while (size < 6) 
        {
            var randomMap = MapCacheManager.GetCachedWorkshopMaps()[(int)RANDOM.NextInt64(MapCacheManager.GetCachedWorkshopMaps().Count)];
            if (SortedTop6MapVotes.Contains(randomMap) || CooldownManager.GetCooldownInfo(randomMap.MapID) != 0) { continue; }

            SortedTop6MapVotes.Add(randomMap);
            size++;
        }
        
        return SortedTop6MapVotes;

    }
    
    public uint GetNominationCount(WorkshopMap map) 
    {
        return MapVoteCounts[map];
    }


    // todo put into api maybe
    private void ChangeMap() 
    {
        
    }
    
    public void Test() 
    {
        foreach (var kv in PlayerMapVotes) 
        {
            Console.WriteLine($"{kv.Key} voted {kv.Value.ActualMapName}");
        }
        
        foreach (var kv in MapVoteCounts) 
        {
            Console.WriteLine($"{kv.Key.ActualMapName} has {kv.Value} votes.");
        }
    }

}