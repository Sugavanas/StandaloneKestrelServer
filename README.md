# Standalone Kestrel Server 

![dotnet](https://img.shields.io/badge/dotnet-5.0-blue) 
[![HttpServerTests](https://img.shields.io/github/workflow/status/Sugavanas/StandaloneKestrelServer/HttpServerTests?label=Tests)](https://github.com/Sugavanas/StandaloneKestrelServer/actions/workflows/ServerTests.yml)

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

See [SampleServer/Program.cs](https://github.com/Sugavanas/StandaloneKestrelServer/blob/main/tests/SampleServer/Program.cs) for example setup.

## License

[MIT License](https://github.com/Sugavanas/StandaloneKestrelServer/blob/main/LICENSE.md)

