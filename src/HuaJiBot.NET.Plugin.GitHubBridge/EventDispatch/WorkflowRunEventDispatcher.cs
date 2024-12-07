using HuaJiBot.NET.Plugin.GitHubBridge.Types.WorkflowRunEventBody;

namespace HuaJiBot.NET.Plugin.GitHubBridge.EventDispatch;

internal static class WorkflowRunEventDispatcher
{
    public static async Task DispatchWorkflowRunEventAsync(
        this PluginMain plugin,
        WorkflowRunEventBody body
    ) { }
}
