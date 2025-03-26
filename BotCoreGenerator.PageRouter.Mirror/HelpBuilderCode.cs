using System.Text;

namespace BotCoreGenerator.PageRouter.Mirror
{
    public class HelpBuilderCode
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();
        private int _currentPosTab = 0;

        public void AppendLine(string line)
        {
            if (_currentPosTab >= 0)
            {
                _stringBuilder.Append(new string('\t', _currentPosTab));
                _stringBuilder.AppendLine(line);
            }
        }
        public void AppendLine() => _stringBuilder.AppendLine();

        public void Tab() => _currentPosTab++;
        public void UnTab()
        {
            if (_currentPosTab != 0) _currentPosTab--;
        }

        public void OpenScope()
        {
            AppendLine("{");
            Tab();
        }
        public void CloseScope()
        {
            UnTab();
            AppendLine("}");
        }

        public override string ToString() => _stringBuilder.ToString();

        public static implicit operator string(HelpBuilderCode source) => source._stringBuilder.ToString();
    }
}
