using BotCore.Interfaces;
using BotCore.PageRouter.Attributes;
using BotCore.PageRouter.Interfaces;
using Newtonsoft.Json.Linq;

namespace BotCore.Demo.Pages
{
    [Page<string>("DemoPage2")]
    public class DemoPage2<TUser, TContext> : IPage<TUser, TContext>, IPageLoading<TUser>, IPageLoaded<TUser>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        public async Task HandleNewUpdateContext(TContext context)
        {
            await context.Reply(await SearchWikipediaAsync(context.Update.Message!));
        }

        public Task OnNavigate(TContext context)
        {
            return context.Reply("Вы перешли на страницу с поиском краткой информации");
        }

        public void PageLoaded(TUser user)
        {
        }

        public void PageLoading(TUser user)
        {
        }

        private static async Task<string> SearchWikipediaAsync(string query)
        {
            string url = $"https://ru.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(query)}";

            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "CSharpApp/1.0");
            HttpResponseMessage response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return "Не удалось найти информацию.";
            }

            string jsonResponse = await response.Content.ReadAsStringAsync();
            JObject json = JObject.Parse(jsonResponse);
            return json["extract"]?.ToString() ?? "Нет краткого описания.";
        }
    }
}
