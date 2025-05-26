using System.Runtime.CompilerServices;

namespace CS2RockTheVote.API;

public interface ICS2RockTheVote 
{

    void ChangeMap(long workshopID);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="filePath">Should be in the plugin folder...</param>
    void ReloadActiveMapsList(string filePath);
    
}