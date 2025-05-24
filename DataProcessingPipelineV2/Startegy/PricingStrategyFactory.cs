using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataProcessingPipelineV2.Startegy;

public interface IPricingStrategyFactory
{
    IPricingStrategy GetPricingStrategy(string strategyType);
}

internal class PricingStrategyFactory : IPricingStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PricingStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPricingStrategy GetPricingStrategy(string strategyType)
    {
        return strategyType switch
        {
            "Discount" => _serviceProvider.GetRequiredService<DiscountPricingStrategy>(),
            "Surge" => _serviceProvider.GetRequiredService<SurgePricingStrategy>(),
            "Seasonal" => _serviceProvider.GetRequiredService<SeasonalPricingStrategy>(),
            _ => throw new NotImplementedException($"Strategy {strategyType} not implemented.")
        };
    }
}
