namespace CS2RockTheVote.API;

public interface ICS2MapCache 
{

    void ReloadActiveMapsList(string filePath);
    IEnumerable<WorkshopMap>? GetMapsFromName(string mapName);
    WorkshopMap? GetMapFromWorkshopID(ulong workshopID);
    List<WorkshopMap> GetCachedWorkshopMaps();
    
}