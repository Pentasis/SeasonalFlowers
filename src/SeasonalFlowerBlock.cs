using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System;
using System.Collections.Generic;
using Vintagestory.GameContent;

namespace SeasonalFlowers;

public class SeasonalFlowerBlock : BlockPlant
{
    private FlowerPhenology _phen = null!;

    public override void OnLoaded(ICoreAPI coreApi)
    {
        base.OnLoaded(coreApi);
        _phen = FlowerPhenologyRegistry.Get(Code.Path);
    }

    public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object? extra)
    {
        extra = null;
        // _phen is guaranteed to be non-null after OnLoaded, so this check is redundant.
        // if (_phen == null) return false; 

        // Reduce the frequency of checks
        if (offThreadRandom.NextDouble() > 0.05) return false;

        string currentPhase = Variant["phase"];
        string correctPhase = GetPhase(world.Calendar, pos);

        if (currentPhase != correctPhase)
        {
            Block nextBlock = world.GetBlock(CodeWithVariant("phase", correctPhase));
            if (nextBlock != null)
            {
                extra = nextBlock;
                return true;
            }
        }

        return false;
    }

    public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object? extra = null)
    {
        if (extra is Block nextBlock)
        {
            world.BlockAccessor.ExchangeBlock(nextBlock.Id, pos);
        }
    }

    private string GetPhase(IGameCalendar cal, BlockPos pos)
    {
        int g = _phen.GrowMonth;
        int f = _phen.FlowerMonth;
        int p = _phen.WitherMonth;
        int h = _phen.HibernateMonth;

        // This logic assumes the hemisphere is available on the server, which it should be.
        if (cal.GetHemisphere(pos) == EnumHemisphere.South)
        {
            g = ShiftMonth(g);
            f = ShiftMonth(f);
            p = ShiftMonth(p);
            h = ShiftMonth(h);
        }

        double hours = cal.TotalDays * cal.HoursPerDay + cal.HourOfDay;

        double hg = Threshold(cal, g);
        double hf = Threshold(cal, f);
        double hp = Threshold(cal, p);
        double hh = Threshold(cal, h);

        var phases = new List<(double t, string name)> {
            (hg, "grow"),
            (hf, "flower"),
            (hp, "wither"),
            (hh, "hibernate")
        };

        double yearHours = cal.DaysPerYear * cal.HoursPerDay;
        var expanded = new List<(double t, string name)>();

        foreach (var ph in phases)
        {
            expanded.Add((ph.t - yearHours, ph.name));
            expanded.Add((ph.t, ph.name));
            expanded.Add((ph.t + yearHours, ph.name));
        }

        double best = double.NegativeInfinity;
        string phaseName = "hibernate";

        foreach (var ph in expanded)
        {
            if (ph.t <= hours && ph.t > best)
            {
                best = ph.t;
                phaseName = ph.name;
            }
        }

        return phaseName;
    }

    private double Threshold(IGameCalendar cal, int month)
    {
        long day = (month - 1) * cal.DaysPerMonth + 2;
        return day * cal.HoursPerDay + 1.0;
    }

    private int ShiftMonth(int month)
    {
        month += 6;
        if (month > 12) month -= 12;
        return month;
    }
}