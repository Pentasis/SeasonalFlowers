using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SeasonalFlowers;

public class SeasonalFlowerOverrideBlock : BlockPlant
{
    FlowerPhenology phen;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        phen = FlowerPhenologyRegistry.Get(Code.Path);
    }

    // WORLD: dynamic season-based textures BEFORE JSON tessellation.
    public override void OnBeforeRender(ICoreClientAPI capi, BlockPos pos, ref MeshData mesh)
    {
        // 1. Determine phase.
        string phase = GetPhase(capi.World.Calendar, pos);

        // 2. Build a texture dictionary overriding vanilla slots
        // but *not* touching JSON tesselation structure.
        var texdict = GetSeasonalTextureDict(capi, phase);

        // 3. Apply the texture dictionary to the block for this render frame
        // This replaces vanilla texture slots without altering shapes.
        if (Textures != null)
        {
            foreach (var kv in texdict)
            {
                if (Textures.ContainsKey(kv.Key))
                {
                    Textures[kv.Key].Baked.Texture = kv.Value;
                }
            }
        }
    }

    // ITEMS / POTTED: always show FLOWER phase
    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        var tex = capi.BlockTextureAtlas.GetOrInsertTexture(
            new AssetLocation($"seasonalflowers:block/{Code.Path}/flower")
        );

        renderinfo.TextureId = tex.AtlasTextureId;
    }

    // --------------------------
    // PHASE & TIME LOGIC
    // --------------------------

    string GetPhase(IGameCalendar cal, BlockPos pos)
    {
        int grow = phen.GrowMonth;
        int flower = phen.FlowerMonth;
        int post = phen.PostFlowerMonth;
        int hiber = phen.HibernateMonth;

        // Hemisphere detection
        try
        {
            var hem = cal.GetHemisphere(pos);
            if (hem == EnumHemisphere.South)
            {
                grow = ShiftMonth(grow);
                flower = ShiftMonth(flower);
                post = ShiftMonth(post);
                hiber = ShiftMonth(hiber);
            }
        }
        catch { }

        double hours = cal.TotalDays * cal.HoursPerDay + cal.HourOfDay;

        // Transition hours (day 3, 01:00)
        double Hg = MonthThreshold(cal, grow);
        double Hf = MonthThreshold(cal, flower);
        double Hp = MonthThreshold(cal, post);
        double Hh = MonthThreshold(cal, hiber);

        // Build ordered cycle
        // We want the LAST transition <= now.
        var list = new List<(double h, string p)>
        {
            (Hg, "grow"),
            (Hf, "flower"),
            (Hp, "postflower"),
            (Hh, "hibernate")
        };

        // Expand +/1 year
        double yearHours = cal.DaysPerYear * cal.HoursPerDay;
        var ex = new List<(double h, string p)>();
        foreach (var t in list)
        {
            ex.Add((t.h - yearHours, t.p));
            ex.Add((t.h, t.p));
            ex.Add((t.h + yearHours, t.p));
        }

        string phase = "hibernate";
        double best = double.NegativeInfinity;

        foreach (var t in ex)
        {
            if (t.h <= hours && t.h > best)
            {
                best = t.h;
                phase = t.p;
            }
        }

        return phase;
    }

    double MonthThreshold(IGameCalendar cal, int month)
    {
        long dayIndex = (month - 1) * cal.DaysPerMonth + 2; // day 3 (0-based index)
        return dayIndex * cal.HoursPerDay + 1.0;            // 01:00
    }

    int ShiftMonth(int month)
    {
        month += 6;
        if (month > 12) month -= 12;
        return month;
    }

    // --------------------------
    // TEXTURE MAPPING
    // --------------------------

    Dictionary<string, int> GetSeasonalTextureDict(ICoreClientAPI capi, string phase)
    {
        // Build dictionary of texture slots → atlasID.

        var dict = new Dictionary<string, int>();

        foreach (var kv in Textures)
        {
            // kv.Key e.g. "north1", "south1", "northTinted1", …
            string slot = kv.Key;

            // final asset: seasonalflowers:block/<codepath>/<phase>/<slot>
            // You can also flatten this, but slot-level files give full control.
            var loc = new AssetLocation($"seasonalflowers:block/{Code.Path}/{phase}/{slot}");

            var tex = capi.BlockTextureAtlas.GetOrInsertTexture(loc);

            dict[slot] = tex.AtlasTextureId;
        }

        return dict;
    }
}
