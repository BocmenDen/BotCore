﻿using Microsoft.CodeAnalysis;
using System.IO;
using System.Reflection;
using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace BotCoreGenerator.PageRouter.Mirror
{
    internal static class Utils
    {
        internal static string GetAccessModifier(Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Public:
                    return "public";
                case Accessibility.Internal:
                    return "internal";
                case Accessibility.Private:
                    return "private";
                case Accessibility.Protected:
                    return "protected";
                case Accessibility.ProtectedAndInternal:
                    return "protected internal";
                case Accessibility.ProtectedOrInternal:
                    return "private protected";
                default:
                    return "private";
            }
        }

        internal static string LoadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Resource {resourceName} not found.");
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        internal static bool DirectlyImplementsInterface(INamedTypeSymbol classSymbol, INamedTypeSymbol interfaceSymbol)
            => classSymbol.Interfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, interfaceSymbol));

        internal static PageInfo? GetPageModelInfo(GeneratorSyntaxContext context)
        {
            if (!(context.Node is ClassDeclarationSyntax classSyntax))
                return null;

            // Получаем символ класса
            if (!(context.SemanticModel.GetDeclaredSymbol(classSyntax) is INamedTypeSymbol classSymbol))
                return null;

            // Находим тип атрибута через компиляцию
            var mirrorAttributeType = context.SemanticModel.Compilation
                .GetTypeByMetadataName(Consts.AttributeFullNamespace);
            if (mirrorAttributeType == null)
                return null;

            // Проверяем, применён ли атрибут к классу
            if (!classSymbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, mirrorAttributeType)))
                return null;

            // Считываем информацию о классе
            var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
            var className = classSymbol.Name;
            var classNameAddGenericArgs = className;
            if (classSymbol.IsGenericType)
            {
                var genericArgs = string.Join(", ", classSymbol.TypeArguments.Select(t => t.Name));
                classNameAddGenericArgs += $"<{genericArgs}>";
            }
            var modifier = GetAccessModifier(classSymbol.DeclaredAccessibility);

            // Собираем список полей и свойств
            var fields = classSymbol
                .GetMembers()
                .Where(m => (m is IPropertySymbol prop && !prop.IsStatic && prop.IsPartialDefinition))
                .Cast<IPropertySymbol>()
                .Select(m => new NodeField
                {
                    Name = m.Name,
                    Type = m.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "unknown",
                    Modifier = GetAccessModifier(m.DeclaredAccessibility),
                })
                .ToList();

            return new PageInfo
            {
                NamePage = className,
                NameSpacePage = namespaceName,
                Modifier = modifier,
                Fields = fields,
                NamePageAddedGeneric = classNameAddGenericArgs
            };
        }

        internal static string GenerateNameModel(PageInfo pageInfo) => $"{pageInfo.NamePage}Model";
    }
}
