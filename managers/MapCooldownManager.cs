using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CS2RockTheVote.API;
using CS2RockTheVote.misc;
using Microsoft.Extensions.Logging;

namespace CS2RockTheVote.managers;

public class MapCooldownManager : ICS2MapCooldown
{

    // key: workshopID, value: rounds on cooldown
    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate nint GetAddonNameDelegate(nint self);
    
    private Dictionary<ulong, uint> MapsOnCooldown = new();
    private MapCacheManager CacheManager;
    private INetworkServerService NetworkServerService = new();
    private WorkshopMap? CurrentLoadedMap = null;
    
    public MapCooldownManager(CS2RockTheVote _plugin, MapCacheManager _cacheManager, ILogger<CS2RockTheVoteManager> _logger) 
    {
        CacheManager = _cacheManager;
        _plugin.RegisterListener<Listeners.OnMapStart>((mapName) =>
        {
            Server.NextWorldUpdate(() =>
            {
                var addonIds = GetCurrentMountedAddons()?.Split(",");
                if (addonIds == null) { return; }
                
                foreach (var addonId in addonIds) 
                {
                    try 
                    {
                        ulong workshopId = Convert.ToUInt64(addonId);
                        WorkshopMap? map = CacheManager.GetMapFromWorkshopID(workshopId);
                        if (map != null) 
                        {
                            CurrentLoadedMap = map;
                            break;
                        }
                    } catch (Exception) 
                    {
                        _logger.LogCritical("Could not parse current mounted addons in MapCooldownManager constructor.");
                        break;
                    }
                }
            });
        });
    }
    
    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info) 
    {
        
        return HookResult.Continue;
    }
    
    public void AddMapCooldown(WorkshopMap map)
    {
        MapsOnCooldown.TryAdd(map.MapID, 5);
    }

    public uint GetCooldownInfo(ulong mapID)
    {
        return MapsOnCooldown.GetValueOrDefault(mapID);
    }
    
    private string? GetCurrentMountedAddons() 
    {
        nint pInterface = NetworkServerService.GetIGameServer();
        nint pVtable = Marshal.ReadIntPtr(pInterface);
        nint pGetMapNameFunc = Marshal.ReadIntPtr(pVtable + (25 * nint.Size));

        var vfunc = Marshal.GetDelegateForFunctionPointer<GetAddonNameDelegate>(pGetMapNameFunc);
        nint pAddonName = vfunc(pInterface);
        
        return Marshal.PtrToStringAnsi(pAddonName);
    }
    
    private string? GetCurrentMountedMapName() 
    {
        nint pInterface = NetworkServerService.GetIGameServer();
        nint pVtable = Marshal.ReadIntPtr(pInterface);
        nint pGetMapNameFunc = Marshal.ReadIntPtr(pVtable + (24 * nint.Size));

        var vfunc = Marshal.GetDelegateForFunctionPointer<GetAddonNameDelegate>(pGetMapNameFunc);
        nint pAddonName = vfunc(pInterface);
        
        return Marshal.PtrToStringAnsi(pAddonName);
    }
    
}