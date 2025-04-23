using HuaJiBot.NET.Plugin.AIChat.Plugins;
using Microsoft.SemanticKernel;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

public static class KernelExtensions
{
    public static void AddBotFunctions(this IKernelBuilder builder)
    {
        builder.Plugins.AddFromType<DateTimeUtils>(nameof(DateTimeUtils));
    }
}
