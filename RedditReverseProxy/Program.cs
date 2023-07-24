using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using RedditReverseProxy.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .ConfigureHttpClient((context, handler) =>
    {
        handler.AutomaticDecompression = 
        System.Net.DecompressionMethods.GZip 
        | System.Net.DecompressionMethods.Deflate 
        | System.Net.DecompressionMethods.Brotli;
    });

var app = builder.Build();

app.UseRouting();
app.UseMiddleware<CustomResponseModifier>();

app.MapReverseProxy();
app.Run();
