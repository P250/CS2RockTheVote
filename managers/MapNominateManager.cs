using CounterStrikeSharp.API.Core;
using CS2RockTheVote.API;

namespace CS2RockTheVote.managers;

public class MapNominateManager : ICS2MapNominate
{

    private readonly Dictionary<ulong, WorkshopMap> PlayerMapVotes = new();
    private readonly Dictionary<WorkshopMap, uint> MapVoteCounts = new();

    public MapNominateManager() 
    {
        
    }

    public void AddNomination(CCSPlayerController player, WorkshopMap map)
    {
        PlayerMapVotes[player.SteamID] = map;
    }

    //  
    public IEnumerable<WorkshopMap>? ConstructVotingList()
    {
        var SortedTop6MapVotes = MapVoteCounts
            .OrderByDescending(kv => kv.Value)
            .Take(6)
            .Select(kv => kv.Key);

        return SortedTop6MapVotes;

    }
    
}