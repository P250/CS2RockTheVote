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
    void UpdateRTVThreshold();
}