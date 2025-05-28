using CounterStrikeSharp.API.Core;
using CS2RockTheVote.managers;

namespace CS2RockTheVote.API;

public interface ICS2MapNominate 
{


    // Adds a nomination tied to a specific player.
    void AddNomination(CCSPlayerController player, WorkshopMap map);
    
    /// <summary>
    /// Picks the top 6 voted maps, or the top voted maps then then random ones until we have 6 options.
    /// </summary>
    void ConstructVotingList();
    
    
    
}