using System;

namespace BotCoreGenerator.PageRouter.Mirror
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed partial class GenerateModelMirrorAttribute : Attribute { }
}