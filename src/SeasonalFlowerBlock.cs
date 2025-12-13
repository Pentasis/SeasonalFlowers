using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System;
using System.Collections.Generic;
using Vintagestory.GameContent; // BlockPlant is in this namespace

namespace SeasonalFlowers;

// Represents a flower block that changes its appearance based on the current season.
// This class now handles server-side block swapping to reflect seasonal changes.
public class SeasonalFlowerBlock : BlockPlant
{
    // Stores the seasonal growth cycle information for this specific flower block.
    private FlowerPhenology _phen = null!;

    // Called when the block is loaded by the game.
    // This method initializes the phenology for the flower by retrieving it from the central registry.
    public override void OnLoaded(ICoreAPI coreApi)
    {
        base.OnLoaded(coreApi);
        // Retrieve the flower's phenology data based on its block code.
        _phen = FlowerPhenologyRegistry.Get(Code.Path);
    }

    // Called by the server on a separate thread to determine if this block needs a game tick.
    // This is where the seasonal phase change is initiated.
    public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object? extra)
    {
        extra = null; // Initialize extra to null, indicating no block change by default.

        // Reduce the frequency of checks to improve performance.
        // Only a small percentage of blocks will be checked on each tick.
        if (offThreadRandom.NextDouble() > 0.05) return false;

        // Get the current phase from the block's variant attributes (e.g., "flower-daisy-flowering" -> "flowering").
        string currentPhase = Variant["phase"];
        // Determine the correct phase based on the current in-game calendar and the flower's phenology.
        string correctPhase = GetPhase(world.Calendar, pos);

        // If the current phase does not match the correct phase for the season,
        // prepare to swap the block.
        if (currentPhase != correctPhase)
        {
            // Get the block instance for the new phase variant.
            // CodeWithVariant constructs a new AssetLocation for the target block (e.g., "flower-daisy-hibernating").
            Block nextBlock = world.GetBlock(CodeWithVariant("phase", correctPhase));
            if (nextBlock != null)
            {
                extra = nextBlock; // Pass the new block as extra data to OnServerGameTick.
                return true; // Indicate that OnServerGameTick should be called on the main thread.
            }
        }

        return false; // No change needed or next block not found.
    }

    // Called by the server on the main thread if ShouldReceiveServerGameTicks returned true.
    // This method performs the actual block swap.
    public override void OnServerGameTick(IWorldAccessor world, BlockPos pos, object? extra = null)
    {
        // If extra contains a Block, it means a phase change is required.
        if (extra is Block nextBlock)
        {
            // Exchange the current block with the new phase variant.
            // This updates the block in the world and triggers client-side re-rendering.
            world.BlockAccessor.ExchangeBlock(nextBlock.Id, pos);
        }
    }

    // Determines the current seasonal phase of the flower (e.g., "grow", "flower", "wither", "hibernate")
    // based on the current date in the in-game calendar.
    private string GetPhase(IGameCalendar cal, BlockPos pos)
    {
        // Retrieve phenology data for the current flower.
        int g = _phen.GrowMonth;
        int f = _phen.FlowerMonth;
        int p = _phen.WitherMonth;
        int h = _phen.HibernateMonth;

        // Adjust months for Southern Hemisphere if applicable.
        if (cal.GetHemisphere(pos) == EnumHemisphere.South)
        {
            g = ShiftMonth(g);
            f = ShiftMonth(f);
            p = ShiftMonth(p);
            h = ShiftMonth(h);
        }

        // Calculate total hours passed in the year.
        double hours = cal.TotalDays * cal.HoursPerDay + cal.HourOfDay;

        // Calculate threshold hours for each phase transition.
        double hg = Threshold(cal, g);
        double hf = Threshold(cal, f);
        double hp = Threshold(cal, p);
        double hh = Threshold(cal, h);

        // Define the phase transition points.
        var phases = new List<(double t, string name)> {
            (hg, "grow"),
            (hf, "flower"),
            (hp, "wither"),
            (hh, "hibernate")
        };

        // Expand phase transition points across multiple years to handle wrap-around
        // (e.g., if current time is near year start, consider previous year's transitions).
        double yearHours = cal.DaysPerYear * cal.HoursPerDay;
        var expanded = new List<(double t, string name)>();

        foreach (var ph in phases)
        {
            expanded.Add((ph.t - yearHours, ph.name)); // Previous year's equivalent
            expanded.Add((ph.t, ph.name));             // Current year's transition
            expanded.Add((ph.t + yearHours, ph.name)); // Next year's equivalent
        }

        // Find the most recent phase transition that has occurred relative to 'hours'.
        double best = double.NegativeInfinity;
        string phaseName = "hibernate"; // Default to hibernate if no other phase is matched.

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

    // Calculates the total hours passed in the year up to a specific point in a given month.
    // This is used as a threshold for switching between seasonal phases.
    private double Threshold(IGameCalendar cal, int month)
    {
        // Calculates days into the year, adding 2 days as a small offset into the month.
        long daysIntoYear = (month - 1) * cal.DaysPerMonth + 2;
        // Converts days to hours and adds a small offset (1 hour) to ensure transition is past exact start.
        return daysIntoYear * cal.HoursPerDay + 1.0;
    }

    // Shifts a given month by 6 months to adjust for the Southern Hemisphere's seasons.
    private int ShiftMonth(int month)
    {
        month += 6; // Advance month by 6.
        if (month > 12) month -= 12; // Wrap around if month exceeds 12.
        return month;
    }
}