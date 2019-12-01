# SerilogW3cMiddleware

Serilog W3C Logging Middleware for .Net Core MVC

## NuGet [![NuGet](https://img.shields.io/nuget/v/BrianMed.AspNetCore.SerilogW3cMiddleware.svg)](https://www.nuget.org/packages/BrianMed.AspNetCore.SerilogW3cMiddleware)

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
            options.StrictW3c = false;
        });    
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseSerilogW3cMiddleware();    
    }
}
```

> Output

```bash
[13:39:40 INF] ::1 - - [01/Dec/2019:13:12:40 -0600] "GET / HTTP/1.1" 000 0 0 begin:0HLRMIOQHF8TL:00000001
[13:39:40 INF] ::1 - - [01/Dec/2019:13:12:40 -0600] "GET / HTTP/1.1" 200 -1 35.549611 end:0HLRMIOQHF8TL:00000001
```
