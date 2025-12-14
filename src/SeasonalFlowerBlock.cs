using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System;
using System.Collections.Generic;
using Vintagestory.GameContent;

namespace SeasonalFlowers;

public class SeasonalFlowerBlock : BlockPlant
{
    // Stores the seasonal growth cycle information for this specific flower block.
    private FlowerPhenology _phen = null!;

    // Called when the block is loaded by the game.
    // This method initializes the phenology for the flower by retrieving it from the central registry.
    public override void OnLoaded(ICoreAPI coreApi)
    {
        base.OnLoaded(coreApi); // This call populates the base Block.RandomTickDensity from JSON attributes.
        // Retrieve the flower's phenology data based on its block code.
        _phen = FlowerPhenologyRegistry.Get(Code.Path);
    }

    // Called by the server on a separate thread to determine if this block needs a game tick.
    // This is where the seasonal phase change is initiated.
    public override bool ShouldReceiveServerGameTicks(IWorldAccessor world, BlockPos pos, Random offThreadRandom, out object? extra)
    {
        extra = null; // Initialize extra to null, indicating no block change by default.

        // Use the base Block.RandomTickDensity property, which is populated from JSON attributes.
        // This value controls the frequency of the internal check.
        //if (offThreadRandom.NextDouble() > this.RandomTickDensity) return false;

        world.Logger.Debug($"[SeasonalFlowers] ShouldReceiveServerGameTicks: Checking block at {pos}");

        // Get the current phase from the block's variant attributes (e.g., "flower-daisy-flowering" -> "flowering").
        string currentPhase = Variant["phase"];
        // Determine the correct phase based on the current in-game calendar and the flower's phenology.
        string correctPhase = GetPhase(world.Calendar, pos);

        world.Logger.Debug($"[SeasonalFlowers] ShouldReceiveServerGameTicks: Block {Code.Path} at {pos}. Current phase: {currentPhase}, Correct phase: {correctPhase}");

        // If the current phase does not match the correct phase for the season,
        // prepare to swap the block.
        if (currentPhase != correctPhase)
        {
            // Get the block instance for the new phase variant.
            // CodeWithVariant constructs a new AssetLocation for the target block (e.g., "flower-daisy-hibernating").
            AssetLocation nextBlockCode = CodeWithVariant("phase", correctPhase);
            Block nextBlock = world.GetBlock(nextBlockCode);
            
            world.Logger.Debug($"[SeasonalFlowers] ShouldReceiveServerGameTicks: Phase mismatch for {Code.Path} at {pos}. Attempting to swap to {nextBlockCode.Path}");

            if (nextBlock != null)
            {
                extra = nextBlock; // Pass the new block as extra data to OnServerGameTick.
                world.Logger.Debug($"[SeasonalFlowers] ShouldReceiveServerGameTicks: Found next block {nextBlock.Code.Path}. Returning true.");
                return true; // Indicate that OnServerGameTick should be called on the main thread.
            }
            else
            {
                world.Logger.Warning($"[SeasonalFlowers] ShouldReceiveServerGameTicks: Failed to find next block for {Code.Path} with variant phase={correctPhase}. AssetLocation: {nextBlockCode.Path}");
            }
        }
        else
        {
            world.Logger.Debug($"[SeasonalFlowers] ShouldReceiveServerGameTicks: Phase matches for {Code.Path} at {pos}. No change needed.");
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
            world.Logger.Debug($"[SeasonalFlowers] OnServerGameTick: Swapping block at {pos} from {Code.Path} to {nextBlock.Code.Path}");
            // Exchange the current block with the new phase variant.
            // This updates the block in the world and triggers client-side re-rendering.
            world.BlockAccessor.ExchangeBlock(nextBlock.Id, pos);
        }
        else
        {
            world.Logger.Warning($"[SeasonalFlowers] OnServerGameTick: Extra object was not a Block for block at {pos}. No swap performed.");
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