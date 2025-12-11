using System.Collections.Generic;

namespace SeasonalFlowers;

public static class FlowerPhenologyRegistry
{
    static Dictionary<string, FlowerPhenology> data = new Dictionary<string, FlowerPhenology>()
    {
        // fill in from the block below
        { "flower-poppy", new FlowerPhenology { GrowStartRel = 0.2083f, BloomStartRel = 0.375f, BloomEndRel = 0.7083f, VariationDays = 7 } },
    };

    public static FlowerPhenology Get(string code)
    {
        if (data.TryGetValue(code, out var val)) return val;

        // sensible default (spring growth, midsummer bloom)
        return new FlowerPhenology {
            GrowStartRel = 0.20f,
            BloomStartRel = 0.35f,
            BloomEndRel = 0.70f,
            VariationDays = 7
        };
    }
}

/*
   catmint (Nepeta)         : bloom start 0.375 (mid-May)  — bloom end 0.7083 (mid-Sep) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:3]{index=3}
   cornflower (Centaurea)  : bloom start 0.375 (mid-May)  — bloom end 0.5417 (mid-Jul) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:4]{index=4}
   cow parsley (Anthriscus): bloom start 0.2917 (mid-Apr) — bloom end 0.4583 (mid-Jun) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:5]{index=5}
   dwarf furze (Ulex minor) : bloom start 0.5417 (mid-Jul)  — bloom end 0.7917 (mid-Oct) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:6]{index=6}
   
   edelweiss (Leontopodium): bloom start 0.5417 (mid-Jul)  — bloom end 0.7083 (mid-Sep) — grow start 0.2917 (mid-Apr). :contentReference[oaicite:7]{index=7}
   forget-me-not (Myosotis): bloom start 0.2917 (mid-Apr) — bloom end 0.375 (mid-May) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:8]{index=8}
   golden poppy (Eschsch.) : bloom start 0.2083 (mid-Mar)  — bloom end 0.7083 (mid-Sep) — grow start 0.125 (mid-Feb). :contentReference[oaicite:9]{index=9}
   heather (Calluna)       : bloom start 0.4583 (mid-Jun)  — bloom end 0.7917 (mid-Oct) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:10]{index=10}
   
   lily-of-the-valley     : bloom start 0.375 (mid-May)  — bloom end 0.4583 (mid-Jun) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:11]{index=11}
   lupine (Lupinus)        : bloom start 0.375 (mid-May)  — bloom end 0.4583 (mid-Jun) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:12]{index=12}
   orange mallow (Lavatera): bloom start 0.5417 (mid-Jul)  — bloom end 0.7083 (mid-Sep) — grow start 0.375 (mid-May). :contentReference[oaicite:13]{index=13}
   redtop grass (Agrostis)  : bloom start 0.4583 (mid-Jun)  — bloom end 0.5417 (mid-Jul) — grow start 0.2917 (mid-Apr). :contentReference[oaicite:14]{index=14}
   
   wild daisy (Bellis)     : bloom start 0.4583 (mid-Jun)  — bloom end 0.625 (mid-Aug) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:15]{index=15}
   woad (Isatis tinctoria) : bloom start 0.375 (mid-May)  — bloom end 0.4583 (mid-Jun) — grow start 0.2083 (mid-Mar). :contentReference[oaicite:16]{index=16}
*/