﻿namespace BotCore.Interfaces
{
    public interface IFactory<DB>
    {
        public DB Create();
    }
}