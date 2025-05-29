using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
namespace CS2RockTheVote.misc;

public class INetworkServerService : NativeObject
{

    private readonly VirtualFunctionWithReturn<nint, nint> GetIGameServerVFunc;

    public INetworkServerService() : base(NativeAPI.GetValveInterface(0, "NetworkServerService_001")) 
    {
        int offset = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? 23 : 24;
        GetIGameServerVFunc = new(this.Handle, offset);
    }
    
    public nint GetIGameServer() 
    {
        return GetIGameServerVFunc.Invoke(this.Handle);
    }
    
}
