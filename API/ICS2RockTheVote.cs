using System.Runtime.CompilerServices;

namespace CS2RockTheVote.API;

public readonly struct WorkshopMap(ulong _mapID, string _actualMapName, string? _description = null)
{
    public ulong MapID { get; init; } = _mapID;
    public string ActualMapName { get; init; } = _actualMapName;
    public string? Description { get; init; } = _description;
}

public interface ICS2RockTheVote 
{

    void ChangeMap(ulong workshopID);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath">Should be in the plugin folder...</param>
    void ReloadActiveMapsList(string filePath);
    
    // Does partial matches and gets as many as possible
    IEnumerable<WorkshopMap> GetMapsFromName(string mapName);
    WorkshopMap? GetMapFromWorkshopID(ulong workshopID);
    
}