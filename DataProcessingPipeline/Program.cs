using DataProcessingPipeline;
using DataProcessingPipeline.Background;
using DataProcessingPipeline.Data;
using DataProcessingPipeline.Dependencies;
using Microsoft.Extensions.Logging.Console;
// see https://github.com/mzwierzchlewski/DataProcessing.Channels
var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Logging.AddSimpleConsole(options => 
{
    options.ColorBehavior = LoggerColorBehavior.Enabled;
    options.TimestampFormat = "[HH:mm:ss:ffff] ";
});

builder.Services.AddScoped<IDependency, Dependency>();
builder.Services.AddSingleton<IDataProcessor, BackgroundDataProcessor>();
builder.Services.AddHostedService(provider => (provider.GetRequiredService<IDataProcessor>() as BackgroundDataProcessor)!);

builder.Services.AddHostedService<ProducerWorker>();

//builder.Services.AddSingleton<object>(static sp => { return null; });

var host = builder.Build();
host.Run();
