using BotCore.Interfaces;
using BotCore.Tg;
using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Types;

namespace BotCore.Demo
{
    public class User : IUser, IUserTgExtension
    {
        [Key]
        public long Id { get; set; }

        public Chat GetTgChat() => new() { Id = Id };
    }
}
