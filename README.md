# Standalone Kestrel Server

![dotnet](https://img.shields.io/badge/dotnet-9.0-blue)
![dotnet](https://img.shields.io/badge/dotnet-8.0-yellow)
![latest](https://img.shields.io/github/v/release/Sugavanas/StandaloneKestrelServer)
![latest-pre](https://img.shields.io/github/v/release/Sugavanas/StandaloneKestrelServer?color=yellow&include_prereleases)
[![HttpServerTests](https://img.shields.io/github/actions/workflow/status/Sugavanas/StandaloneKestrelServer/ServerTests.yml?branch=main&label=Main%20Branch%20Tests)](https://github.com/Sugavanas/StandaloneKestrelServer/actions/workflows/ServerTests.yml?query=branch%3Amain)
[![HttpServerTests-dev](https://img.shields.io/github/actions/workflow/status/Sugavanas/StandaloneKestrelServer/ServerTests.yml?branch=dev&label=Dev%20Branch%20Tests)](https://github.com/Sugavanas/StandaloneKestrelServer/actions/workflows/ServerTests.yml?query=branch%3Adev)

## Usage

```c#
Host.CreateDefaultBuilder(args)
    .UseStandaloneKestrelServer(options =>
    {
        options.KestrelServerOptions.ListenLocalhost(8080);
        options.ConfigureRequestPipeline(builder =>
        {
            builder.Use(next =>
                async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                    await next(context);
                });
        });
    }).Build().Run();
```

### Minimal API Example

```c#
var builder = WebApplication.CreateBuilder(args);

builder.UseStandaloneKestrelServer(options =>
{
  options.KestrelServerOptions.ListenLocalhost(8080);
  options.ConfigureRequestPipeline(builder =>
        {
            builder.Use(next =>
                async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                    await next(context);
                });
        });
});

var app = builder.Build();
app.Run();
```

See [SampleServer/Program.cs](https://github.com/Sugavanas/StandaloneKestrelServer/blob/main/tests/SampleServer/Program.cs)
for example setup.

## License

[MIT License](https://github.com/Sugavanas/StandaloneKestrelServer/blob/main/LICENSE.md)

