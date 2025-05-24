namespace DataProcessingPipelineV2.Startegy;

public interface IPricingStrategy
{
    decimal CalculatePrice(decimal basePrice);
}

public class DiscountPricingStrategy : IPricingStrategy
{
    public decimal CalculatePrice(decimal basePrice)
    {
        return basePrice * 0.9m; // 10% discount
    }
}

public class SurgePricingStrategy : IPricingStrategy
{
    public decimal CalculatePrice(decimal basePrice)
    {
        return basePrice * 1.5m; // 50% surge
    }
}

public class SeasonalPricingStrategy : IPricingStrategy
{
    public decimal CalculatePrice(decimal basePrice)
    {
        return basePrice * 0.8m; // 20% seasonal discount
    }
}
