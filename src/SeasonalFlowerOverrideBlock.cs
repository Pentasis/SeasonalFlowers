using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SeasonalFlowers;

public class SeasonalFlowerOverrideBlock : Block
{
    FlowerPhenology phenology;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        phenology = FlowerPhenologyRegistry.Get(Code.Path);
    }

    // WORLD: seasonal 4-state rendering
    public override void OnBeforeRender(ICoreClientAPI capi, BlockPos pos, ref MeshData mesh)
    {
        base.OnBeforeRender(capi, pos, ref mesh);
        ApplySeasonalTexture_World(capi, pos, ref mesh);
    }

    // ITEM / DROPPED / POT: always show flower texture
    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        ApplyFlowerTexture_Item(capi, ref renderinfo);
    }

    void ApplyFlowerTexture_Item(ICoreClientAPI capi, ref ItemRenderInfo renderinfo)
    {
        var tex = capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation($"seasonalflowers:block/{Code.Path}/flower"));
        renderinfo.TextureId = tex.AtlasTextureId;
    }

    void ApplySeasonalTexture_World(ICoreClientAPI capi, BlockPos pos, ref MeshData mesh)
    {
        var cal = capi.World.Calendar;

        // read base months from phenology (northern hemisphere)
        int g = phenology.GrowMonth;
        int f = phenology.FlowerMonth;
        int p = phenology.PostFlowerMonth;
        int h = phenology.HibernateMonth;

        // detect hemisphere for this position and flip months for southern hemisphere
        try
        {
            var hem = cal.GetHemisphere(pos); // EnumHemisphere
            if (hem == Vintagestory.API.Common.EnumHemisphere.South)
            {
                g = ShiftMonthHalfYear(g);
                f = ShiftMonthHalfYear(f);
                p = ShiftMonthHalfYear(p);
                h = ShiftMonthHalfYear(h);
            }
        }
        catch
        {
            // If GetHemisphere unavailable for some reason, default to northern behavior.
        }

        // Build transition points (ordered) in absolute hours since start of this year
        float hoursPerDay = cal.HoursPerDay;
        int daysPerMonth = cal.DaysPerMonth;
        int daysPerYear = cal.DaysPerYear;

        // Helper: convert month (1..12) and day (3) and hour (1.0) to total hours since Jan 1 0:00
        Func<int, double> monthDay3Hour1_totalHours = (int month) =>
        {
            // day index (0-based) for the 3rd day of that month:
            long dayIndex = (long)(month - 1) * daysPerMonth + (3 - 1);
            double totalHours = dayIndex * (double)hoursPerDay + 1.0; // 01:00
            return totalHours;
        };

        // Create a list of transitions (totalHours, phaseNameAfterTransition)
        var transitions = new List<(double totalHours, string phaseName)>();

        // Order: GrowMonth -> Grow, FlowerMonth -> Flower, PostFlowerMonth -> PostFlower, HibernateMonth -> Hibernate
        transitions.Add((monthDay3Hour1_totalHours(g), "grow"));
        transitions.Add((monthDay3Hour1_totalHours(f), "flower"));
        transitions.Add((monthDay3Hour1_totalHours(p), "postflower"));
        transitions.Add((monthDay3Hour1_totalHours(h), "hibernate"));

        // current time in hours since Jan 1 0:00 (may be fractional, uses TotalDays)
        double currentTotalHours = cal.TotalDays * (double)hoursPerDay + cal.HourOfDay;

        // Because transitions are cyclical, we want the latest transition <= current time considering wrap-around across year boundary.
        // Build an expanded list with year+ and yearcopies so we can easily find the last transition.
        var expanded = new List<(double totalHours, string phaseName)>();
        foreach (var t in transitions)
        {
            expanded.Add((t.totalHours - daysPerYear * hoursPerDay, t.phaseName)); // previous year
            expanded.Add((t.totalHours, t.phaseName));                             // this year
            expanded.Add((t.totalHours + daysPerYear * hoursPerDay, t.phaseName)); // next year
        }

        // find the transition with largest totalHours <= currentTotalHours
        double bestTime = double.NegativeInfinity;
        string currentPhase = "hibernate"; // default
        foreach (var e in expanded)
        {
            if (e.totalHours <= currentTotalHours && e.totalHours > bestTime)
            {
                bestTime = e.totalHours;
                currentPhase = e.phaseName;
            }
        }

        // Set texture based on currentPhase (grow/flower/postflower/hibernate)
        var tex = capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation($"seasonalflowers:block/{Code.Path}/{currentPhase}"));
        mesh.SetTexPos(tex.SubPosition, tex.SubSize);
    }

    // Add 6 months, wrap to 1..12
    int ShiftMonthHalfYear(int month)
    {
        int m = (month + 6);
        while (m > 12) m -= 12;
        while (m < 1) m += 12;
        return m;
    }
}
