namespace SeasonalFlowers;

// Defines the seasonal growth cycle for a flower.
// This class holds the months (numbers 1-12) for each major phase of the flower's life cycle
// based on the Northern Hemisphere.
public class FlowerPhenology
{
    // The month when the flower starts to grow from its hibernating state.
    public int GrowMonth;

    // The month when the flower is in full bloom.
    public int FlowerMonth;

    // The month when the flower begins to wither after blooming.
    public int WitherMonth;

    // The month when the flower enters hibernation for the winter.
    public int HibernateMonth;

    // An unused property that could be used in the future to add random variation
    // to the timing of the growth phases.
    public int VariationDays = 0;
}