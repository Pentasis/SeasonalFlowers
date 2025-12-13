using System.Collections.Generic;

namespace SeasonalFlowers;

// A static registry that holds the seasonal growth data (phenology) for all the flowers
// modified by this mod. This class provides a central place to define and retrieve
// the seasonal cycles for different flower types.
public static class FlowerPhenologyRegistry
{
    // The core data of the registry, a dictionary that maps flower block codes
    // to their corresponding FlowerPhenology data.
    static readonly Dictionary<string, FlowerPhenology> Data = new Dictionary<string, FlowerPhenology>()
    {
        // The keys in this dictionary are the block codes of the vanilla flowers.
        // The values are FlowerPhenology objects that define the seasonal cycle for that flower.
        { "flower-catmint", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 5, WitherMonth = 9,  HibernateMonth = 11 } },
        { "flower-cornflower", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 6, WitherMonth = 8,  HibernateMonth = 10 } },
        { "flower-cowparsley", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 4, WitherMonth = 6,  HibernateMonth = 9 } },
        { "flower-dwarffurze", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 4, WitherMonth = 7,  HibernateMonth = 10 } },
        { "flower-edelweiss", new FlowerPhenology { GrowMonth = 4, FlowerMonth = 7, WitherMonth = 9,  HibernateMonth = 11 } },
        { "flower-forgetmenot", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 4, WitherMonth = 6,  HibernateMonth = 10 } },
        { "flower-goldenpoppy", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 4, WitherMonth = 7,  HibernateMonth = 11 } },
        // I assume Erica carnea (winter heather) so we have some colour in winter:
        { "flower-heather", new FlowerPhenology { GrowMonth = 9, FlowerMonth = 11, WitherMonth = 4, HibernateMonth = 5 } },
        { "flower-lilyofthevalley", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 5, WitherMonth = 6,  HibernateMonth = 9 } },
        { "flower-lupine", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 6, WitherMonth = 8,  HibernateMonth = 10 } },
        { "flower-orange-mallow", new FlowerPhenology { GrowMonth = 3, FlowerMonth = 5, WitherMonth = 8,  HibernateMonth = 10 } },
        { "flower-redtop", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 6, WitherMonth = 8,  HibernateMonth = 11 } },
        { "flower-wilddaisy", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 4, WitherMonth = 9,  HibernateMonth = 12 } },
        { "flower-woad", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 5, WitherMonth = 7,  HibernateMonth = 10 } },
        { "flower-horsetail", new FlowerPhenology { GrowMonth = 2, FlowerMonth = 5, WitherMonth = 7,  HibernateMonth = 10 } }
    };

    // Retrieves the FlowerPhenology for a given flower block code.
    public static FlowerPhenology Get(string code)
    {
        if (Data.TryGetValue(code, out var val)) return val;

        // Fallback if the flower code is not in our registry.
        return new FlowerPhenology
        {
            GrowMonth = 3,
            FlowerMonth = 5,
            WitherMonth = 8,
            HibernateMonth = 11
        };
    }
}