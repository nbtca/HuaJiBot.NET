namespace HuaJiBot.NET.Commands;

public sealed record CommandParameterDescriptor(
    string Name,
    string Description,
    CommandArgumentType ArgumentType,
    Type Type,
    bool IsOptional,
    bool IsRuntimeContext
);

public sealed record CommandDescriptor(
    string Id,
    string Name,
    string? Alias,
    string Description,
    IReadOnlyList<CommandParameterDescriptor> Parameters,
    bool ReadOnly,
    bool AdminOnly,
    bool GroupScoped,
    IReadOnlyList<string> Examples
);