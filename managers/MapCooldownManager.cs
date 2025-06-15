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

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate nint GetAddonNameDelegate(nint self);
    private static readonly ulong WHITELISTED_ADDON = 3457835230;
    // key: workshopID, value: rounds on cooldown
    private Dictionary<ulong, uint> MapsOnCooldown = new();
    private ICS2MapCache CacheManager;
    private INetworkServerService NetworkServerService = new();
    private WorkshopMap? CurrentLoadedMap = null;
    
    public MapCooldownManager(CS2RockTheVote _plugin, ICS2MapCache _cacheManager, ILogger<CS2RockTheVoteManager> _logger) 
    {
        CacheManager = _cacheManager;
        _plugin.RegisterListener<Listeners.OnMapStart>((mapName) =>
        {
            Server.NextWorldUpdate(() =>
            {
                var addonIds = GetCurrentMountedAddons()?.Split(",");
                if (addonIds == null)
                {
                    _logger.LogCritical("Addon IDS are somehow null... Maybe need an updated vfunc offset?");
                    return;
                }
                _logger.LogWarning("BROOOO WE CHANGED MAP!");
                foreach (string addon in addonIds) 
                {
                    _logger.LogInformation("addon id: " + addon);
                }
                if (addonIds == null) { return; }
                
                foreach (var addonId in addonIds)
                {
                    try 
                    {
                        ulong workshopId = Convert.ToUInt64(addonId);
                        if (workshopId == WHITELISTED_ADDON) { _logger.LogWarning("We skipped yappershq addon"); continue; } // we don't wanna track this one..
                        
                        // decrement all maps on cooldown by one, except the current loaded map
                        foreach (var kv in MapsOnCooldown.Where(kv => kv.Key != workshopId)) 
                        {
                            MapsOnCooldown[kv.Key] = kv.Value - 1;
                            if (MapsOnCooldown[kv.Key] == 0) 
                            {
                                MapsOnCooldown.Remove(kv.Key);
                            }
                        }
                        
                        WorkshopMap? map = CacheManager.GetMapFromWorkshopID(workshopId);
                        if (map != null) 
                        {
                            CurrentLoadedMap = map;
                            _logger.LogWarning($"we added {map.Value.ActualMapName} as currentloadedmap");
                            MapsOnCooldown.Add(map!.Value!.MapID, 5); // this will actually be 4 maps
                            _logger.LogWarning("" + MapsOnCooldown.ContainsKey(map.Value.MapID));
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
    
    public Dictionary<ulong, uint> GetMapsOnCooldown() 
    {
        return this.MapsOnCooldown;
    }
    
}