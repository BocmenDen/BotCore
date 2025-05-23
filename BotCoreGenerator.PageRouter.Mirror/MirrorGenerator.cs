﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            GenerateModel(pageInfo, out HelpBuilderCode modelCode, out string modelName);
            context.AddSource($"{modelName}.g.cs", modelCode);
            var modelFullName = $"{Consts.Namespace}.{modelName}";
            HelpBuilderCode pageCode = BindModelToPage(pageInfo, modelFullName);
            context.AddSource($"{pageInfo.NamePage}.g.cs", pageCode);
        }

        private static HelpBuilderCode BindModelToPage(PageInfo pageInfo, string modelFullName)
        {
            HelpBuilderCode pageCode = new HelpBuilderCode();
            pageCode.AppendLine($"namespace {pageInfo.NameSpacePage}");
            pageCode.OpenScope();
            {
                var interfaceFullName = $"{Consts.InterfaceBindStorageModel}<{modelFullName}>";
                var storageModelFullName = $"{Consts.ModelStorageModel}<{modelFullName}>";

                pageCode.AppendLine($"{pageInfo.Modifier} partial class {pageInfo.NamePageAddedGeneric}: {interfaceFullName}");
                pageCode.OpenScope();
                {
                    var nameTempVarableIsSave = $"__{pageInfo.NamePage}IsSaveModel";
                    pageCode.AppendLine($"private bool {nameTempVarableIsSave} = false;");
                    pageCode.AppendLine($"protected {storageModelFullName} {pageInfo.NameModelProperty};");
                    pageCode.AppendLine($"void {interfaceFullName}.{Consts.MethodNameBindStorageModel}({storageModelFullName} {Consts.NameVarTmp})");
                    pageCode.OpenScope();
                    {
                        pageCode.AppendLine($"this.{pageInfo.NameModelProperty} = {Consts.NameVarTmp};");
                    }
                    pageCode.CloseScope();

                    pageCode.AppendLine($"protected async {typeof(Task).FullName} {Consts.NameSaveModelMethod}()");
                    pageCode.OpenScope();
                    {
                        pageCode.AppendLine($"if({nameTempVarableIsSave})");
                        pageCode.OpenScope();
                        {
                            pageCode.AppendLine($"await {pageInfo.NameModelProperty}.Save();");
                            pageCode.AppendLine($"this.{nameTempVarableIsSave} = false;");
                        }
                        pageCode.CloseScope();
                    }
                    pageCode.CloseScope();

                    foreach (var field in pageInfo.Fields)
                    {
                        pageCode.AppendLine($"{field.Modifier} partial {field.Type} {field.Name}");
                        pageCode.OpenScope();
                        {
                            pageCode.AppendLine($"get => this.{pageInfo.NameModelProperty}.Value.{field.Name};");
                            pageCode.AppendLine("set");
                            pageCode.OpenScope();
                            {
                                pageCode.AppendLine($"this.{pageInfo.NameModelProperty}.Value.{field.Name} = value;");
                                pageCode.AppendLine($"this.{nameTempVarableIsSave} = true;");
                            }
                            pageCode.CloseScope();
                        }
                        pageCode.CloseScope();
                    }
                }
                pageCode.CloseScope();
            }
            pageCode.CloseScope();
            return pageCode;
        }

        private static void GenerateModel(PageInfo pageInfo, out HelpBuilderCode modelCode, out string modelName)
        {
            modelCode = new HelpBuilderCode();
            modelName = Utils.GenerateNameModel(pageInfo);
            modelCode.AppendLine($"namespace {Consts.Namespace}");
            modelCode.OpenScope();
            {
                modelCode.AppendLine($"public class {modelName}");
                modelCode.OpenScope();
                {
                    foreach (var field in pageInfo.Fields)
                    {
                        modelCode.AppendLine($"public {field.Type} {field.Name} {{ get; set; }} = default!;");
                    }
                }
                modelCode.CloseScope();
            }
            modelCode.CloseScope();
        }
    }
}