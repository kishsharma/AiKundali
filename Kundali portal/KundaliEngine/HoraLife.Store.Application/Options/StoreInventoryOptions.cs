namespace HoraLife.Store.Application.Options;

public sealed class StoreInventoryOptions
{
    public const string SectionName = "StoreInventory";

    /// <summary>
    /// Pending reserved orders older than this are auto-released.
    /// </summary>
    public int ReservationExpiryHours { get; set; } = 24;

    /// <summary>
    /// How often the background sweeper runs.
    /// </summary>
    public int ExpirySweepMinutes { get; set; } = 15;
}
