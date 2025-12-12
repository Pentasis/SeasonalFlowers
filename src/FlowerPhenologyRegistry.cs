using System.Collections.Generic;

namespace SeasonalFlowers;

public static class FlowerPhenologyRegistry
{
    static readonly Dictionary<string, FlowerPhenology> Data = new Dictionary<string, FlowerPhenology>()
    {
        // key: block.Code.Path => value: months (Grow, Flower, Wither, Hibernate)
        // Replace keys with exact vanilla block code paths if necessary.

        // Catmint (Nepeta)
        { "flower-catmint", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 5, WitherMonth = 9,  HibernateMonth = 11 } },

        // Cornflower
        { "flower-cornflower", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 6, WitherMonth = 8,  HibernateMonth = 10 } },

        // Cow parsley
        { "flower-cowparsley", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 4, WitherMonth = 6,  HibernateMonth = 9 } },

        // Dwarf furze (Ulex minor)
        { "flower-dwarffurze", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 4, WitherMonth = 7,  HibernateMonth = 10 } },

        // Edelweiss
        { "flower-edelweiss", new FlowerPhenology { GrowMonth = 4, FlowerMonth = 7, WitherMonth = 9,  HibernateMonth = 11 } },

        // Forget-me-not
        { "flower-forgetmenot", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 4, WitherMonth = 6,  HibernateMonth = 10 } },

        // Golden poppy
        { "flower-goldenpoppy", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 4, WitherMonth = 7,  HibernateMonth = 11 } },

        // Heather
        { "flower-heather", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 8, WitherMonth = 10, HibernateMonth = 12 } },

        // Lily-of-the-Valley
        { "flower-lilyofthevalley", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 5, WitherMonth = 6,  HibernateMonth = 9 } },

        // Lupine
        { "flower-lupine", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 6, WitherMonth = 8,  HibernateMonth = 10 } },

        // Orange mallow
        { "flower-orange-mallow", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 5, WitherMonth = 8,  HibernateMonth = 10 } },

        // Redtop grass
        { "flower-redtop", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 6, WitherMonth = 8,  HibernateMonth = 11 } },

        // Wild daisy
        { "flower-wilddaisy", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 4, WitherMonth = 9,  HibernateMonth = 12 } },

        // Woad
        { "flower-woad", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 5, WitherMonth = 7,  HibernateMonth = 10 } },
    };

    public static FlowerPhenology Get(string code)
    {
        if (Data.TryGetValue(code, out var val)) return val;

        // fallback: gentle default
        return new FlowerPhenology
        {
            GrowMonth = 3,
            FlowerMonth = 5,
            WitherMonth = 8,
            HibernateMonth = 11
        };
    }
}