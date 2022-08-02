namespace YKEnroll.Lib;

/// <summary>
///     This class keeps the results from the KeyCollector.
///     CurrentValue and NewValue is zeroed as soon as they've
///     been submitted by the KeyCollector.
/// </summary>
public class KeyCollectorResult
{
    public bool UseDefault { get; set; } = true;
    public bool Cancelled { get; set; } = true;
    public byte[] CurrentValue { get; set; } = Array.Empty<byte>();
    public byte[] NewValue { get; set; } = Array.Empty<byte>();
}
