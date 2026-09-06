using HuaJiBot.NET.Bot;

namespace HuaJiBot.NET.Interfaces;

public interface IAdapterService
{
    Task SetupServiceAsync();
    void Reconnect();
    MemberType GetMemberType(string robotId, string targetGroup, string userId);
    string GetNick(string robotId, string userId);
    void SetGroupName(string? robotId, string targetGroup, string groupName);
    string GetPluginDataPath();
}
