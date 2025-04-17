using System.Collections.Generic;

namespace BotCoreGenerator.PageRouter.Mirror
{
    internal struct PageInfo
    {
        public string NamePage;
        public string NamePageAddedGeneric;
        public string NameSpacePage;
        public string Modifier;
        public string NameModelProperty;
        public IReadOnlyList<NodeField> Fields;
    }
    internal struct NodeField
    {
        public string Modifier;
        public string Name;
        public string Type;
    }
}
