using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Serilog;

namespace BrianMed.AspNetCore.SerilogW3cMiddleware
{
    public class SerilogW3cMiddlewareOptions
    {
        public bool DisplayBefore { get; set; } = true;

        public bool DisplayAfter { get; set; } = true;

        public bool DisplayExceptions { get; set; } = true;

        public bool RethrowExceptions { get; set; } = true;
    }

    public class SerilogW3cMiddleware
    {
        private readonly RequestDelegate Next;
        private readonly SerilogW3cMiddlewareOptions Options;

        public SerilogW3cMiddleware(RequestDelegate next, IOptions<SerilogW3cMiddlewareOptions> options)
        {
            Next = next;

            Options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
                long start = Stopwatch.GetTimestamp();

                // https://en.wikipedia.org/wiki/Common_Log_Format
                try {
                    DateTime now = DateTime.Now;

                    if (Options.DisplayBefore) {
                        Log.Information($"{context.Request.HttpContext.Connection.RemoteIpAddress} - {context.User.Identity.Name ?? "-"} {now.ToString("dd/MMM/yyyy:HH:MM:ss ")}{now.ToString("zzz").Replace(":", "")} \"{context.Request.Method} {context.Request.Path} {context.Request.Protocol}\" begin:{context.TraceIdentifier}");
                    }

                    await Next.Invoke(context);
                    double elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

                    if (Options.DisplayAfter) {
                        now = DateTime.Now;
                        Log.Information($"{context.Request.HttpContext.Connection.RemoteIpAddress} - {context.User.Identity.Name ?? "-"} {now.ToString("dd/MMM/yyyy:HH:MM:ss ")}{now.ToString("zzz").Replace(":", "")} \"{context.Request.Method} {context.Request.Path} {context.Request.Protocol}\" {context.Response.StatusCode} {context.Response?.ContentLength ?? -1} {elapsedMs} end:{context.TraceIdentifier}");
                    }
                } catch (Exception ex) {
                    double elapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp());

                    if (Options.DisplayAfter) {
                        DateTime now = DateTime.Now;
                        Log.Information($"{context.Request.HttpContext.Connection.RemoteIpAddress} - {context.User.Identity.Name ?? "-"} {now.ToString("dd/MMM/yyyy:HH:MM:ss ")}{now.ToString("zzz").Replace(":", "")} \"{context.Request.Method} {context.Request.Path} {context.Request.Protocol}\" 500 {context.Response?.ContentLength ?? -1} {elapsedMs} end:{context.TraceIdentifier} {ex.Message.Replace("\n", "|")}");
                    }

                    if (Options.DisplayExceptions) {
                        Log.Debug(ex.ToString());
                    }

                    if (Options.RethrowExceptions) {
                        throw;
                    }
                }
        }

        // Serilog
        static double GetElapsedMilliseconds(long start, long stop)
        {
            return (stop - start) * 1000 / (double)Stopwatch.Frequency;
        }
    }

    // https://adamstorr.azurewebsites.net/blog/aspnetcore-exploring-custom-middleware
    public static class Extensions
    {
        public static IServiceCollection AddSerilogW3cMiddleware(this IServiceCollection service, Action<SerilogW3cMiddlewareOptions> options = default)
        {
            options = options ?? (opts => {});

            service.Configure(options);

            return service;
        }

        public static IApplicationBuilder UseSerilogW3cMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SerilogW3cMiddleware>();
        }
    }
}
