var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DataProcessingPipeline>("dataprocessingpipeline")
    .WithReplicas(2);

builder.AddProject<Projects.DataProcessingPipelineV2>("dataprocessingpipelinev2");

builder.Build().Run();
