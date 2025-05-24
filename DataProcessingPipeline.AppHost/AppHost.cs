var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DataProcessingPipeline>("dataprocessingpipeline");

builder.Build().Run();
