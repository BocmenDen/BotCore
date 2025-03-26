using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;

namespace BotCoreGenerator.PageRouter.Mirror
{
    [Generator]
    public class MirrorGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //if (!System.Diagnostics.Debugger.IsAttached)
            //{
            //    System.Diagnostics.Debugger.Launch();
            //}

            context.RegisterPostInitializationOutput(ctx =>
            {
                ctx.AddSource("GenerateModelMirrorAttribute.g.cs", SourceText.From(Consts.AttributeCode, Encoding.UTF8));
            });
            var pageModels = context.SyntaxProvider.CreateSyntaxProvider(
                        predicate: (node, _) => node is ClassDeclarationSyntax,
                        transform: (ctx, _) => Utils.GetPageModelInfo(ctx)
                    ).Where(info => info != null);

            context.RegisterSourceOutput(pageModels, Build);
        }

        private void Build(SourceProductionContext context, PageInfo? pageInfoN)
        {
            if (pageInfoN == null) return;
            var pageInfo = pageInfoN.Value;
            HelpBuilderCode modelCode = new HelpBuilderCode();
            var modelName = Utils.GenerateNameModel(pageInfo);
            modelCode.AppendLine($"namespace {Consts.Namespace}");
            modelCode.OpenScope();
            {
                modelCode.AppendLine($"public class {modelName}");
                modelCode.OpenScope();
                {
                    foreach (var field in pageInfo.Fields)
                    {
                        modelCode.AppendLine($"public {field.Type} {field.Name} {{ get; set; }} = null!;");
                    }
                }
                modelCode.CloseScope();
            }
            modelCode.CloseScope();
            context.AddSource($"{modelName}.g.cs", modelCode);
            var modelFullName = $"{Consts.Namespace}.{modelName}";

            HelpBuilderCode pageCode = new HelpBuilderCode();
            pageCode.AppendLine($"namespace {pageInfo.NameSpacePage}");
            pageCode.OpenScope();
            {
                var interfaceFullName = $"{Consts.InterfaceBindStorageModel}<{modelFullName}>";
                var storageModelFullName = $"{Consts.ModelStorageModel}<{modelFullName}>";
                pageCode.AppendLine($"{pageInfo.Modifier} partial class {pageInfo.NamePageAddedGeneric}: {interfaceFullName}");
                pageCode.OpenScope();
                {
                    pageCode.AppendLine($"protected {storageModelFullName} {Consts.NameModel};");
                    pageCode.AppendLine($"void {interfaceFullName}.{Consts.MethodNameBindStorageModel}({storageModelFullName} {Consts.NameVarTmp})");
                    pageCode.OpenScope();
                    {
                        pageCode.AppendLine($"this.{Consts.NameModel} = {Consts.NameVarTmp};");
                    }
                    pageCode.CloseScope();
                    foreach (var field in pageInfo.Fields)
                    {
                        pageCode.AppendLine($"{field.Modifier} partial {field.Type} {field.Name}");
                        pageCode.OpenScope();
                        {
                            pageCode.AppendLine($"get => this.{Consts.NameModel}.Value.{field.Name};");
                            pageCode.AppendLine($"set => this.{Consts.NameModel}.Value.{field.Name} = value;");
                        }
                        pageCode.CloseScope();
                    }
                }
                pageCode.CloseScope();
            }
            pageCode.CloseScope();
            context.AddSource($"{pageInfo.NamePage}.g.cs", pageCode);
        }
    }
}
