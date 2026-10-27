namespace PriceHunt.Infrastructure.Simulation;

/// <summary>
/// Settings of the simulated suppliers, read from the Simulation section of appsettings.json.
/// </summary>
public sealed class SimulationOptions
{
    /// <summary>
    /// The name of the configuration section.
    /// </summary>
    public const string SectionName = "Simulation";

    /// <summary>
    /// The shortest time a supplier takes to answer, in milliseconds.
    /// </summary>
    public int MinDelayMs { get; init; } = 500;

    /// <summary>
    /// The longest time a supplier takes to answer, in milliseconds.
    /// </summary>
    public int MaxDelayMs { get; init; } = 5000;

    /// <summary>
    /// The chance, in percent, that the supplier set to fail some of the time returns an error.
    /// </summary>
    public int FailureRatePercent { get; init; } = 30;

    /// <summary>
    /// The lowest price a supplier offers.
    /// </summary>
    public decimal MinPrice { get; init; } = 100m;

    /// <summary>
    /// The highest price a supplier offers.
    /// </summary>
    public decimal MaxPrice { get; init; } = 5000m;

    /// <summary>
    /// The currency code of every price.
    /// </summary>
    public string Currency { get; init; } = "USD";
}
