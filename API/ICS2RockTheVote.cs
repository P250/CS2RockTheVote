namespace CS2RockTheVote.API;

public readonly struct WorkshopMap(ulong _mapID, string _actualMapName, string? _rawDescription = null)
{
    public ulong MapID { get; init; } = _mapID;
    public string ActualMapName { get; init; } = _actualMapName;
    public string? RawDescription { get; init; } = _rawDescription;
}

public interface ICS2RockTheVote 
{
    
}