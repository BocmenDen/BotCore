using OneBot.Interfaces;
using OneBot.Tg;
using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Types;

namespace OneBots.Demo
{
    public class User : IUser, IUserTgExtension
    {
        [Key]
        public long Id { get; set; }

        public Chat GetTgChat() => new() { Id = Id };
    }
}
