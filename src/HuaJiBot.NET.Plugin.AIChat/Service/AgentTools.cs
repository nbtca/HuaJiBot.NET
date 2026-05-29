using HuaJiBot.NET.Agent;
using HuaJiBot.NET.Plugin.AIChat.Plugins;
using Microsoft.Extensions.AI;

namespace HuaJiBot.NET.Plugin.AIChat.Service;

public static class AgentTools
{
    public static AIFunction[] CreateBotFunctions(
        IReadOnlyDictionary<string, IEnumerable<AgentFunctionInfo>> extraFunctions
    )
    {
        var tools = new List<AIFunction>();

        var dateTimeUtils = new DateTimeUtils();
        tools.Add(AIFunctionFactory.Create(dateTimeUtils.GetCurrentDateTime));
        tools.Add(AIFunctionFactory.Create(dateTimeUtils.GetCurrentDate));
        tools.Add(AIFunctionFactory.Create(dateTimeUtils.GetCurrentTime));
        tools.Add(AIFunctionFactory.Create(dateTimeUtils.GetDayOfWeek));
        tools.Add(AIFunctionFactory.Create(dateTimeUtils.CalculateTimeDifference));

        foreach (var (pluginName, functions) in extraFunctions)
        {
            foreach (var func in functions)
            {
                tools.Add(AIFunctionFactory.Create(func.Function, func.Name, func.Description));
            }
        }

        return tools.ToArray();
    }
}
