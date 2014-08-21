using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Owin;
using Raven.Client;
using Raven.Client.Embedded;

namespace OwinKatanaRaven
{
    public class Startup
    {
        private const string Chars = "abcdefghijklmnoprstqwzABCDEFGHIJKLMNOPRSTQWZ01234567890";
        private static readonly IDocumentStore DocumentStore = new EmbeddableDocumentStore { DataDirectory = "~/App_Data" };

        public void Configuration(IAppBuilder app)
        {
            app.Use(async (ctx, next) =>
            {
                var actions = new Dictionary<string, Func<IAsyncDocumentSession, Task>>
                {
                    {"GET", async session =>
                    {
                        var url = await session.LoadAsync<Url>(ctx.Request.Path.Value.Substring(1));
                        if (url != null)
                        {
                            ctx.Response.StatusCode = (int) HttpStatusCode.MovedPermanently;
                            ctx.Response.Headers["Location"] = url.Location;
                        }
                        else ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    }},
                    {"POST", async session =>
                    {
                        var random = new Random();
                        var hash = string.Join("", Enumerable.Range(0, 7).Select(_ => Chars[random.Next(Chars.Length)]));
                        await session.StoreAsync(new Url() {Id = hash, Location = ctx.Request.Query["path"]});
                        await session.SaveChangesAsync();
                        ctx.Response.StatusCode =(int) HttpStatusCode.Created;
                        await ctx.Response.WriteAsync(hash);
                    }}
                };
                DocumentStore.Initialize();
                using (var session = DocumentStore.OpenAsyncSession())
                    await actions[ctx.Request.Method](session);
            });
        }
    }

    public class Url
    {
        public string Id { get; set; }
        public string Location { get; set; }
    }
}