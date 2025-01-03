﻿using BotCore.FilterRouter.Models;
using BotCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace BotCore.FilterRouter.Extensions
{
    public static class WriterExpressionExtensions
    {
        public static StateCache ChacheOrGetExpression<T, TUser>(this WriterExpression<TUser> writer, ref T expression, string key)
            where T : Expression
            where TUser : IUser
        {
            if (writer.TryGetCacheExpression(key, out Expression? exp))
            {
                expression = (T)exp!;
                return StateCache.Exist;
            }
            else
            {
                writer.TryAddCacheExpression(expression, key);
                return StateCache.Cached;
            }
        }

        public static StateCache ChacheOrGetExpressionAutoKey<T, TUser>(this WriterExpression<TUser> writer,
            ref T expression,
            string key = "",
            [CallerLineNumber] int lineMember = -1,
            [CallerMemberName] string? functionName = null,
            [CallerFilePath] string? file = null
            )
            where T : Expression
            where TUser : IUser
        {
            if (TryGenerateKey(key, lineMember, functionName, file, out var newKey))
                return writer.ChacheOrGetExpression(ref expression, newKey!);
            return writer.ChacheOrGetExpression(ref expression, expression.ToString());
        }

        public static MemberExpression GetBotFunctionsParametr<TUser>(this WriterExpression<TUser> writer)
            where TUser : IUser
        {
            var exp = Expression.Property(writer.ContextParametr, nameof(IUpdateContext<TUser>.BotFunctions));
            writer.ChacheOrGetExpressionAutoKey(ref exp, nameof(TUser));
            return exp;
        }

        public static MemberExpression GetUpdateParametr<TUser>(this WriterExpression<TUser> writer)
            where TUser : IUser
        {
            var exp = Expression.Property(writer.ContextParametr, nameof(IUpdateContext<TUser>.Update));
            writer.ChacheOrGetExpressionAutoKey(ref exp, nameof(TUser));
            return exp;
        }

        public static MemberExpression GetUpdateCommand<TUser>(this WriterExpression<TUser> writer)
            where TUser : IUser
        {
            var exp = Expression.Property(writer.GetUpdateParametr(), nameof(IUpdateContext<TUser>.Update.Command));
            writer.ChacheOrGetExpressionAutoKey(ref exp, nameof(TUser));
            return exp;
        }
        public static MemberExpression GetUpdateType<TUser>(this WriterExpression<TUser> writer)
            where TUser : IUser
        {
            var exp = Expression.Field(writer.GetUpdateParametr(), nameof(IUpdateContext<TUser>.Update.UpdateType));
            writer.ChacheOrGetExpressionAutoKey(ref exp, nameof(TUser));
            return exp;
        }

        public static ParameterExpression CreateFilterResultParametrClass<T, TUser>(this WriterExpression<TUser> writer, Expression value, Expression flag, string? key = null, string? varableName = null)
            where T : class
            where TUser : IUser
        {
            NewExpression newFilterResult = Expression.New(typeof(FilterResult<T>).GetConstructor([typeof(bool), typeof(T)])??
                throw new Exception("Constructor not found"), flag, value);
            ParameterExpression resultExpression = Expression.Parameter(typeof(FilterResult<T>), varableName);

            return WriteFilterResultParametr(writer, key, newFilterResult, resultExpression);
        }

        public static ParameterExpression CreateFilterResultParametrStruct<T, TUser>(this WriterExpression<TUser> writer, Expression value, Expression flag, string? key = null, string? varableName = null)
            where T: struct
            where TUser : IUser
        {
            NewExpression newFilterResult = Expression.New(typeof(FilterResult<T?>).GetConstructor([typeof(bool), typeof(T?)])??
                throw new Exception("Constructor not found"), flag, value);
            ParameterExpression resultExpression = Expression.Parameter(typeof(FilterResult<T?>), varableName);
            return WriteFilterResultParametr(writer, key, newFilterResult, resultExpression);
        }

        private static ParameterExpression WriteFilterResultParametr<TUser>(WriterExpression<TUser> writer, string? key, NewExpression newFilterResult, ParameterExpression resultExpression)
            where TUser : IUser
        {
            StateCache stateCache = StateCache.Cached;
            if (!string.IsNullOrWhiteSpace(key))
                stateCache = writer.ChacheOrGetExpression(ref resultExpression, key);
            if (stateCache == StateCache.Cached)
                writer.WriteBody(Expression.Assign(resultExpression, newFilterResult));
            return resultExpression;
        }

        public static ParameterExpression CreateFilterResultParametrClassAutoKey<T, TUser>(
                this WriterExpression<TUser> writer,
                Expression value,
                Expression flag,
                string? key = null,
                [CallerLineNumber] int lineMember = -1,
                [CallerMemberName] string? functionName = null,
                [CallerFilePath] string? file = null
            )
            where T : class
            where TUser : IUser
        {
            TryGenerateKey(key, lineMember, functionName, file, out var keyResult);
            return writer.CreateFilterResultParametrClass<T, TUser>(value, flag, keyResult
#if DEBUG
                , $"{Path.GetFileName(file)}_{functionName}_{key}"
#endif
                );
        }

        public static ParameterExpression CreateFilterResultParametrStructAutoKey<T, TUser>(
                this WriterExpression<TUser> writer,
                Expression value,
                Expression flag,
                string? key = null,
                [CallerLineNumber] int lineMember = -1,
                [CallerMemberName] string? functionName = null,
                [CallerFilePath] string? file = null
            )
            where T : struct
            where TUser : IUser
        {
            TryGenerateKey(key, lineMember, functionName, file, out var keyResult);
            return writer.CreateFilterResultParametrStruct<T, TUser>(value, flag, keyResult
#if DEBUG
                , $"{Path.GetFileName(file)}_{functionName}_{key}"
#endif
                );
        }

        private static bool TryGenerateKey(string? key, int lineMember, string? functionName, string? file, out string? keyResult)
        {
            keyResult = null;
            if (lineMember != -1 && functionName != null && file != null)
            {
                keyResult = string.Join("->", file, functionName, lineMember, key);
                return true;
            }
            return false;
        }

        public static ParameterExpression GetService<TUser>(this WriterExpression<TUser> writer, Type serviceType)
            where TUser : IUser
        {
            var method = typeof(ServiceProviderServiceExtensions)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(x => x.IsGenericMethod && x.IsPublic && x.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService) && x.GetParameters().Length == 1)
                .First()
                .MakeGenericMethod(serviceType);
            var parametr = Expression.Parameter(serviceType);
            var stateChache = writer.ChacheOrGetExpressionAutoKey(ref parametr, serviceType.Name);
            if (stateChache == StateCache.Exist) return parametr;
            var service = Expression.Call(method, writer.ServiceProvider);
            writer.WriteBody(Expression.Assign(parametr, service));
            return parametr;
        }
    }
    public enum StateCache : byte
    {
        Cached,
        Exist
    }
}
