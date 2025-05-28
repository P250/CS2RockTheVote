using CounterStrikeSharp.API.Core;
using CS2RockTheVote.API;
using Microsoft.Extensions.Logging;

namespace CS2RockTheVote.managers;

public class MapCooldownManager : ICS2MapCooldown
{

    // key: workshopID, value: rounds on cooldown
    private Dictionary<ulong, uint> MapsOnCooldown = new();
    
    public MapCooldownManager(CS2RockTheVote _plugin, ILogger<CS2RockTheVoteManager> _logger) 
    {
        _plugin.RegisterListener<Listeners.OnMapStart>((mapName) =>
        {
            foreach (var workshopId in MapsOnCooldown.Keys)
            {
                MapsOnCooldown[workshopId] -= 1;
                if (MapsOnCooldown[workshopId] == 0) 
                {
                    MapsOnCooldown.Remove(workshopId);
                }
            }
        });
    }

    public void AddMapCooldown(WorkshopMap map)
    {
        MapsOnCooldown.TryAdd(map.MapID, 5);
    }

    public uint GetCooldownInfo(ulong mapID)
    {
        return MapsOnCooldown.GetValueOrDefault(mapID);
    }
}