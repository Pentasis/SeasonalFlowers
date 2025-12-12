namespace SeasonalFlowers;

public class FlowerPhenology
{
    // Month numbers 1..12 (Northern Hemisphere)
    // GrowMonth  = on day 3 at 01:00 switch Hibernate -> Grow
    // FlowerMonth = on day 3 at 01:00 switch Grow -> Flower
    // WitherMonth = on day 3 at 01:00 switch Flower -> Wither
    // HibernateMonth = on day 3 at 01:00 switch Wither -> Hibernate
    public int GrowMonth;
    public int FlowerMonth;
    public int WitherMonth;
    public int HibernateMonth;

    // If you ever want variation later, keep this, but currently unused.
    public int VariationDays = 0;
}
