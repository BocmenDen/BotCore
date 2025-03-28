﻿using BotCore.Interfaces;
using BotCore.OneBot;
using BotCore.Tg;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Types;

namespace BotCore.Demo
{
    [UserTypeName("Telegram")]
    public class UserTg : IUser, IUserTgExtension
    {
        [Key]
        public long Id { get; set; }

        public UserTg()
        {

        }

        public UserTg(long id)
        {
            Id=id;
        }

        public Chat GetTgChat() => new() { Id = Id };
    }
    [PrimaryKey(nameof(NameSource), nameof(IdSource))]
    public class UsersRef : IUser
    {
        [Key]
        public string NameSource { get; set; }
        [Key]
        public long IdSource { get; set; }

        public long Id { get; set; }
        public User? SharedUser { get; set; }

        public UsersRef(string nameSource, long idSource)
        {
            NameSource=nameSource??throw new ArgumentNullException(nameof(nameSource));
            IdSource=idSource;
        }

        public UsersRef(UserLinkInfo linkInfo)
        {
            NameSource=linkInfo.SourceName??throw new ArgumentNullException(nameof(linkInfo), "SourceName");
            IdSource=linkInfo.SourceId;
        }
    }
    public class User : IUser
    {
        [Key]
        public long Id { get; set; }

        public string? KeyPage { get; set; }

        public User()
        {

        }
    }
}
