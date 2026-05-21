namespace TiffinOS.API.Services.Interfaces
{
    public interface IDeliveryEngineService
    {
        // Generates daily_packing_summary rows for tomorrow
        // Called the night before at prep_schedule_time
        Task GeneratePackingSummaryAsync(Guid tenantId);

        // Generates delivery_schedule rows for today
        // Called morning of delivery at dispatch_schedule_time
        Task GenerateDispatchListAsync(Guid tenantId);

        // Manual trigger — admin can regenerate if cron failed
        Task<int> RegenerateForDateAsync(Guid tenantId, DateOnly date);

        Task GenerateDriverPayoutsAsync(Guid tenantId);
    }
}
