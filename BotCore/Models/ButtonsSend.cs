namespace BotCore.Models
{
    public record class ButtonsSend : CollectionBotParameters
    {
        public IReadOnlyList<IReadOnlyList<ButtonSend>> Buttons { get; }

        public ButtonsSend(IEnumerable<IEnumerable<ButtonSend>> buttons)
            => Buttons = buttons.Select(x => x.ToArray()).ToArray();

        public ButtonsSend(IReadOnlyList<IReadOnlyList<ButtonSend>> buttons)
        {
            Buttons=buttons??throw new ArgumentNullException(nameof(buttons));
        }
    }
}
