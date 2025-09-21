using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace HuaJiBot.NET.SourceGenerator;

internal static class Helper
{
    public static IEnumerable<INamedTypeSymbol> FilterTypesInNamespace(
        SemanticModel semanticModel,
        string namespaceFull
    )
    {
        var namespaceSymbol = semanticModel.Compilation.GlobalNamespace;
        foreach (var part in namespaceFull.Split('.'))
        {
            namespaceSymbol = namespaceSymbol
                .GetNamespaceMembers()
                .FirstOrDefault(ns => ns.Name == part);
            if (namespaceSymbol == null)
                yield break;
        }
        foreach (var type in namespaceSymbol.GetTypeMembers())
        {
            yield return type;
        }
    }

    public static bool ContainsBaseType(
        INamedTypeSymbol currentTypeSymbol,
        INamedTypeSymbol targetTypeSymbol
    )
    {
        var baseType = currentTypeSymbol;
        while (baseType.BaseType is { } baseNext)
        {
            if (baseNext.Equals(targetTypeSymbol, SymbolEqualityComparer.Default))
                return true;
            baseType = baseNext;
        }

        return false;
    }
}
