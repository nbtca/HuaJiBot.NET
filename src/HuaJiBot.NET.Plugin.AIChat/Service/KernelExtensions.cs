using HuaJiBot.NET.Agent;
using HuaJiBot.NET.Bot;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Core;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

public static class KernelExtensions
{
    public static void AddBotFunctions(
        this IKernelBuilder builder,
        IReadOnlyDictionary<string, IEnumerable<AgentFunctionInfo>> extraFunctions
    )
    {
#pragma warning disable SKEXP0050
        // https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Plugins/Plugins.Core
        builder.Plugins.AddFromType<ConversationSummaryPlugin>(nameof(ConversationSummaryPlugin));
        builder.Plugins.AddFromType<HttpPlugin>(nameof(HttpPlugin));
        builder.Plugins.AddFromType<TextPlugin>(nameof(TextPlugin));
        builder.Plugins.AddFromType<TimePlugin>(nameof(TimePlugin));
#pragma warning restore SKEXP0050

        foreach (var (pluginName, functions) in extraFunctions)
        {
            builder.Plugins.AddFromFunctions(
                pluginName,
                from x in functions
                select KernelFunctionFactory.CreateFromMethod(x.Function, x.Name, x.Description)
            );
        }
    }
}
