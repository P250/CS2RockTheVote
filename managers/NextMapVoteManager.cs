using CounterStrikeSharp.API;
using CS2RockTheVote.API;
using CS2ScreenMenuAPI;

namespace CS2RockTheVote.managers;

public class NextMapVoteManager 
{

    private CS2RockTheVote Plugin;
    private ICS2MapNominate NominateManager;
    private bool NextMapVoteActive = false;
    
    public NextMapVoteManager(CS2RockTheVote _plugin, ICS2MapNominate _nominateManager) 
    {
        Plugin = _plugin;
        NominateManager = _nominateManager;
    }
    
    public void StartNextMapVote() 
    {
        NextMapVoteActive = true;
        foreach (var player in Utilities.GetPlayers()) 
        {
            var menu = new Menu(player, Plugin)
            {
                Title = "Next map vote",
                HasExitButon = false,
                PostSelect = PostSelect.Close        
            };
            
            foreach (var map in NominateManager.ConstructVotingList()) 
            {
                menu.AddItem(map.ActualMapName, (pl, option) =>
                {
                    
                });
            }
            
        }
    }
    
    public bool IsNextMapVoteActive() 
    {
        return this.NextMapVoteActive;
    }
    
    
}