﻿using BotCore.EfDb;
using BotCore.Interfaces;
using BotCore.OneBot;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;

namespace BotCore.Demo
{
    [DB]
    public class DataBase : DbContext, IDBUser<UserTg, Chat>, IDBUser<User, UserLinkInfo>
    {
        public DbSet<UserTg> UsersTg { get; set; } = null!;
        public DbSet<UsersRef> UsersShared { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;

        private static readonly Func<DataBase, long, Task<UserTg?>> findTgUser = EF.CompileAsyncQuery<DataBase, long, UserTg?>((context, id) => context.UsersTg.AsNoTracking().FirstOrDefault(x => x.Id == id));

        public DataBase(DbContextOptions options) : base(options)
        {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            Database.EnsureCreated();
        }

        public async ValueTask<UserTg> CreateUser(Chat chat)
        {
            UserTg user = new() { Id = chat.Id };
            var e = UsersTg.Add(user);
            await SaveChangesAsync();
            e.State = EntityState.Detached;
            return user;
        }

        public async ValueTask<User> CreateUser(UserLinkInfo parameter)
        {
            User user = new();
            var e2 = Users.Add(user);
            UsersRef userRef = new(parameter)
            {
                SharedUser = user
            };
            var e1 = UsersShared.Add(userRef);
            await SaveChangesAsync();
            e1.State = EntityState.Detached;
            e2.State = EntityState.Detached;
            return user;
        }

        public async ValueTask<UserTg?> GetUser(Chat chat)
        {
            var result = await findTgUser(this, chat.Id);
            return result;
        }
        public async ValueTask<User?> GetUser(UserLinkInfo parameter) =>
            (await UsersShared.Include(x => x.SharedUser).AsNoTracking().FirstOrDefaultAsync(x => x.IdSource == parameter.SourceId && x.NameSource == parameter.SourceName))?.SharedUser;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<User>()
                .Property(p => p.Id)
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<UsersRef>()
                .HasOne(x => x.SharedUser)
                .WithMany()
                .HasForeignKey(f => f.Id);
        }
    }
}
