using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CS2RockTheVote.API;
using CS2RockTheVote.managers;
using Microsoft.Extensions.DependencyInjection;

namespace CS2RockTheVote.misc;

public class RockTheVoteServiceProvider : IPluginServiceCollection<CS2RockTheVote>
{
    public void ConfigureServices(IServiceCollection service)
    {
        service.AddSingleton<ICS2MapCooldown, MapCooldownManager>();
        service.AddSingleton<ICS2MapNominate, MapNominateManager>();
        service.AddSingleton<ICS2RockTheVote, CS2RockTheVoteManager>();
    }
}