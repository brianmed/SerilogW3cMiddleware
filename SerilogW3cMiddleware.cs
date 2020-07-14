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

        public bool StrictW3C { get; set; } = false;
    }

    public class SerilogW3cMiddleware
    {
        private class LogProperties
        {
            public string RemoteIpAddress { get; set; } = String.Empty;
            public string AuthUser { get; set; } = String.Empty;
            public string Date { get; set; } = String.Empty;
            public string Method { get; set; } = String.Empty;
            public string Path { get; set; } = String.Empty;
            public string Protocol { get; set; } = String.Empty;
            public string StatusCode { get; set; } = String.Empty;
            public string ContentLength { get; set; } = String.Empty;
            public string ElapsedMs { get; set; } = String.Empty;
            public string UserAgent { get; set; } = String.Empty;
            public string Identifier { get; set; } = String.Empty;
        }

        private readonly RequestDelegate Next;
        private readonly SerilogW3cMiddlewareOptions Options;

        public SerilogW3cMiddleware(RequestDelegate next, IOptions<SerilogW3cMiddlewareOptions> options)
        {
            Next = next;

            Options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            if (Options.StrictW3C && Options.DisplayBefore) {
                throw new ArgumentException("Can not set both StrictW3C and DisplayBefore");
            }

            long start = Stopwatch.GetTimestamp();

            LogProperties logProperties = new LogProperties();

            // https://en.wikipedia.org/wiki/Common_Log_Format
            try {
                DateTime now = DateTime.Now;

                logProperties.RemoteIpAddress = context.Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "-";
                logProperties.AuthUser = context.User.Identity.Name ?? "-";
                logProperties.Date = $"{now.ToString("dd/MMM/yyyy:HH:MM:ss ")}{now.ToString("zzz").Replace(":", "")}";
                logProperties.Method = context.Request.Method;
                logProperties.Path = context.Request.Path;
                logProperties.Protocol = context.Request.Protocol;
                logProperties.StatusCode = "000";
                logProperties.ContentLength = "0";
                logProperties.ElapsedMs = "0";
                if (context.Request.Headers.ContainsKey("User-Agent")) {
                    logProperties.UserAgent = context.Request.Headers["User-Agent"].ToString();
                } else {
                    logProperties.UserAgent = "-";
                }
                logProperties.Identifier = $"begin:{context.TraceIdentifier}";

                if (Options.DisplayBefore) {
                    LogIt(logProperties);
                }

                await Next.Invoke(context);

                if (Options.DisplayAfter) {
                    now = DateTime.Now;

                    logProperties.Date = $"{now.ToString("dd/MMM/yyyy:HH:MM:ss ")}{now.ToString("zzz").Replace(":", "")}";
                    logProperties.StatusCode = context.Response.StatusCode.ToString();
                    logProperties.ContentLength = $"{context.Response?.ContentLength ?? -1}";
                    logProperties.ElapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()).ToString();
                    logProperties.Identifier = $"end:{context.TraceIdentifier}";

                    LogIt(logProperties);
                }
            } catch (Exception ex) {
                if (Options.DisplayAfter) {
                    DateTime now = DateTime.Now;

                    logProperties.Date = $"{now.ToString("dd/MMM/yyyy:HH:MM:ss ")}{now.ToString("zzz").Replace(":", "")}";
                    logProperties.StatusCode = "500";
                    logProperties.ContentLength = $"{context.Response?.ContentLength ?? -1}";
                    logProperties.ElapsedMs = GetElapsedMilliseconds(start, Stopwatch.GetTimestamp()).ToString();
                    logProperties.Identifier = $"end:{context.TraceIdentifier}";

                    LogIt(logProperties);
                }

                if (Options.DisplayExceptions) {
                    Log.Error(ex, $"Exception during \"{logProperties.Method} {logProperties.Path} {logProperties.Protocol}\"");
                }

                if (Options.RethrowExceptions) {
                    throw;
                }
            }
        }

        private void LogIt(LogProperties properties)
        {
            string messageTemplateDefault = "{RemoteIpAddress} - {AuthUser} [{Date}] \"{Method} {Path} {Protocol}\" {StatusCode} {ContentLength} {ElapsedMs} \"{UserAgent}\" {Identifier}";
            string messageTemplateStrictW3c = "{RemoteIpAddress} - {AuthUser} [{Date}] \"{Method} {Path} {Protocol}\" {StatusCode} {ContentLength}";

            if (Options.StrictW3C) {
                Log.Information(messageTemplateStrictW3c, properties.RemoteIpAddress, properties.AuthUser, properties.Date, properties.Method, properties.Path, properties.Protocol, properties.StatusCode, properties.ContentLength);
            } else {
                Log.Information(messageTemplateDefault, properties.RemoteIpAddress, properties.AuthUser, properties.Date, properties.Method, properties.Path, properties.Protocol, properties.StatusCode, properties.ContentLength, properties.ElapsedMs, properties.UserAgent, properties.Identifier);
            }
        }

        // Serilog
        public double GetElapsedMilliseconds(long start, long stop)
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
