using DataProcessingPipelineV2.Startegy;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServicesExtensions
{
    public static void AddPricingStrategies(this IServiceCollection services)
    {
        // Register the interface
        services.AddTransient<IPricingStrategy, DiscountPricingStrategy>();
        services.AddTransient<IPricingStrategy, SurgePricingStrategy>();
        services.AddTransient<IPricingStrategy, SeasonalPricingStrategy>();
        // Register concrete strategies
        services.AddTransient<DiscountPricingStrategy>();
        services.AddTransient<SurgePricingStrategy>();
        services.AddTransient<SeasonalPricingStrategy>();

        // Register factory
        services.AddSingleton<IPricingStrategyFactory, PricingStrategyFactory>();

        // Today we don't need a factory use keyed services instead sourcegenerated code??
        services.AddKeyedScoped<IPricingStrategy, DiscountPricingStrategy>("Discount");
        services.AddKeyedScoped<IPricingStrategy, SurgePricingStrategy>("Surge");
        services.AddKeyedScoped<IPricingStrategy, SeasonalPricingStrategy>("Seasonal");
    }
}
