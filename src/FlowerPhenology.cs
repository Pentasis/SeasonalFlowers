namespace SeasonalFlowers;

public class FlowerPhenology
{
    // Month numbers 1..12 (northern hemisphere)
    // GrowMonth  = on day 3 at 01:00 switch Hibernate -> Grow
    // FlowerMonth = on day 3 at 01:00 switch Grow -> Flower
    // PostFlowerMonth = on day 3 at 01:00 switch Flower -> PostFlower
    // HibernateMonth = on day 3 at 01:00 switch PostFlower -> Hibernate
    public int GrowMonth;
    public int FlowerMonth;
    public int PostFlowerMonth;
    public int HibernateMonth;

    // If you ever want variation later, keep this, but currently unused.
    public int VariationDays = 0;
}
