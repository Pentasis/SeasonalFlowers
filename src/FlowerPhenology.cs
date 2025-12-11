namespace SeasonalFlowers;

public class FlowerPhenology
{
    // fractions of the year: 0..1 (0 = Jan 1 start)
    public float GrowStartRel;   // when green/growing stage begins
    public float BloomStartRel;  // when bloom stage begins
    public float BloomEndRel;    // when bloom ends and hibernation begins

    // Variation in days (positive integer). We'll convert to fraction at runtime.
    public int VariationDays;
}
