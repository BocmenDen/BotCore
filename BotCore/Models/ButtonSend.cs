namespace BotCore.Models
{
    public record class ButtonSend : CollectionBotParameters
    {
        public string Text { get; }

        public ButtonSend(string text) : this(text, null) { }

        public ButtonSend(string text, CollectionBotParameters? collectionBotParameters) : base(collectionBotParameters)
        {
            Text=text??throw new ArgumentNullException(nameof(text));
        }

        public static implicit operator ButtonSend(string text) => new(text);

        public override string ToString() => Text;
    }
    public readonly struct ButtonSearch(int row, int column, ButtonSend button)
    {
        public readonly int Column = column;
        public readonly int Row = row;
        public readonly ButtonSend Button = button;
    }
}
