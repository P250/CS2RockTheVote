namespace CS2RockTheVote.API;

public interface ICS2MapCooldown 
{

    uint GetCooldownInfo(ulong mapID);
    
    void AddMapCooldown(WorkshopMap map);
    
    Dictionary<ulong, uint> GetMapsOnCooldown();
    
    
}