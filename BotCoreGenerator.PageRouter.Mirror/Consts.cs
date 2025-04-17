namespace BotCoreGenerator.PageRouter.Mirror
{
    internal static class Consts
    {
        internal const string NameVarTmp = "obj";

        internal const string Namespace = "BotCoreGenerator.PageRouter.Mirror.Models";
        internal const string AttributeFullNamespace = "BotCoreGenerator.PageRouter.Mirror.GenerateModelMirrorAttribute";
        internal const string InterfaceBindStorageModel = "BotCore.PageRouter.Interfaces.IBindStorageModel";
        internal const string MethodNameBindStorageModel = "BindStorageModel";
        internal const string ModelStorageModel = "BotCore.PageRouter.Models.StorageModel";

        internal const string NameSaveModelMethod = "SaveStorage";

        internal static readonly string AttributeCode = Utils.LoadEmbeddedResource($"{AttributeFullNamespace}.cs");
    }
}
