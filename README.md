# SerilogW3cMiddleware

Serilog W3C Logging Middleware for .Net Core MVC

> dotnet add package BrianMed.AspNetCore.SerilogW3cMiddleware

### Usage

> In Program.cs

```csharp
public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        CreateHostBuilder(args).Build().Run();
    }
}
```

> In startup.cs

```csharp
using BrianMed.AspNetCore.SerilogW3cMiddleware;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSerilogW3cMiddleware(options => {
            options.DisplayBefore = true;
            options.DisplayAfter = true;
            options.DisplayExceptions = true;
        });    
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSerilogW3cMiddleware();    
    }
}
```

```bash
[20:46:13 INF] ::1 - - 30/Nov/2019:20:11:13 -0600 "GET / HTTP/1.1" begin:0HLRM12GN5SLP:00000001
[20:46:13 INF] ::1 - - 30/Nov/2019:20:11:13 -0600 "GET / HTTP/1.1" 200 -1 31.867737 end:0HLRM12GN5SLP:00000001
```
