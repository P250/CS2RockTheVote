using CounterStrikeSharp.API.Core;
using CS2RockTheVote.API;
using CS2RockTheVote.managers;
using Microsoft.Extensions.DependencyInjection;

namespace CS2RockTheVote.misc;

public class RockTheVoteServiceProvider : IPluginServiceCollection<CS2RockTheVote>
{
    public void ConfigureServices(IServiceCollection service)
    {
        service.AddSingleton<ICS2RockTheVote, CS2RockTheVoteManager>();
    }
}