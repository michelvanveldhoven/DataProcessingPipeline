namespace DataProcessingPipeline;

public static class SemaphoreSlimExtensions
{
    public static async Task<bool> WaitWithCancellation(this SemaphoreSlim semaphore, int? key = 0, CancellationToken cancellationToken = default)
    {
        try
        {
            await semaphore.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return true;
    }
}
