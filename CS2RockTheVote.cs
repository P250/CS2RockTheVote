using CounterStrikeSharp.API.Core;
using CS2RockTheVote.API;
using CS2RockTheVote.managers;
using Microsoft.Extensions.DependencyInjection;

namespace CS2RockTheVote;

public class CS2RockTheVote(IServiceProvider _provider) : BasePlugin
{
    public override string ModuleName => "CS2RockTheVote";
    public override string ModuleVersion => "v1.0.0";
    public override string ModuleAuthor => "Discord - johnnyboy2329";
    public override string ModuleDescription => "Classic RTV plugin with menu support.";

    public override void Load(bool hotReload)
    {
        //RegisterAllAttributes((CS2RockTheVoteManager) _service.GetService(typeof(ICS2RockTheVote))!);
        RegisterAllAttributes(_provider.CreateScope().ServiceProvider.GetService<ICS2RockTheVote>()!);
    }
    
    public override void Unload(bool hotReload)
    {
        
    }
}