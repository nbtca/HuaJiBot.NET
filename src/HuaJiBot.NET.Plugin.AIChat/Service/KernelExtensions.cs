using HuaJiBot.NET.Bot;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.Core;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

public static class KernelExtensions
{
    public static void AddBotFunctions(this IKernelBuilder builder, BotService service)
    {
#pragma warning disable SKEXP0050
        // https://github.com/microsoft/semantic-kernel/tree/main/dotnet/src/Plugins/Plugins.Core
        builder.Plugins.AddFromType<ConversationSummaryPlugin>(nameof(ConversationSummaryPlugin));
        builder.Plugins.AddFromType<HttpPlugin>(nameof(HttpPlugin));
        builder.Plugins.AddFromType<MathPlugin>(nameof(MathPlugin));
        builder.Plugins.AddFromType<TextPlugin>(nameof(TextPlugin));
        builder.Plugins.AddFromType<TimePlugin>(nameof(TimePlugin));
        builder.Plugins.AddFromType<WaitPlugin>(nameof(WaitPlugin));
#pragma warning restore SKEXP0050

        builder.Plugins.AddFromFunctions(
            "",
            "",
            [KernelFunctionFactory.CreateFromMethod((int i, int j) => i * j, "Multiply")]
        );
    }
}
