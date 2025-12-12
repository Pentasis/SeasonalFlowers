using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using System;

namespace SeasonalFlowers;

// Represents a flower block that changes its appearance based on the current season.
// This class overrides the default plant block behaviour to implement seasonal texture changes.
public class SeasonalFlowerBlock : BlockPlant
{
    // Holds the seasonal growth cycle information for this specific flower block.
    private FlowerPhenology _phen = null!;

    // Pre-loaded texture atlas positions for seasonal textures
    private TextureAtlasPosition _transparentTexPos = null!;
    private TextureAtlasPosition _hibernationTexPos = null!;

    // Called when the block is loaded by the game. This method initializes the phenology
    // for the flower by retrieving it from the central registry.
    public override void OnLoaded(ICoreAPI coreApi)
    {
        base.OnLoaded(coreApi);
        _phen = FlowerPhenologyRegistry.Get(Code.Path);

        // Pre-load textures on the main thread
        if (coreApi is ICoreClientAPI capi)
        {
            capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation("seasonalflowers:block/transparent"), out _, out _transparentTexPos);
            capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation("seasonalflowers:block/hibernation"), out _, out _hibernationTexPos);
        }
    }

    // This method is called by the engine when it's generating the visual mesh for a chunk.
    // It allows for modification of the block's mesh before it's sent to the graphics card.
    // Here, it's used to apply seasonal textures to the flower.
    public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
    {
        var capi = api as ICoreClientAPI;
        if (capi == null) return;

        string phase = GetPhase(capi, capi.World.Calendar, pos);

        if (phase == "hibernate")
        {
            ApplyFullOverride(ref sourceMesh, _hibernationTexPos);
            return;
        }

        if (phase == "grow" || phase == "wither")
        {
            HidePetals(ref sourceMesh, _transparentTexPos);
        }
        
        // if (phase == "flower") {} ==> Vanilla Textures
    }

    // Determines the current seasonal phase of the flower (e.g., "grow", "flower", "wither", "hibernate")
    // based on the current date in the in-game calendar.
    private string GetPhase(ICoreClientAPI capi, IGameCalendar cal, BlockPos pos)
    {
        int g = _phen.GrowMonth;
        int f = _phen.FlowerMonth;
        int p = _phen.WitherMonth;
        int h = _phen.HibernateMonth;

        try
        {
            if (cal.GetHemisphere(pos) == EnumHemisphere.South)
            {
                g = ShiftMonth(g);
                f = ShiftMonth(f);
                p = ShiftMonth(p);
                h = ShiftMonth(h);
            }
        }
        catch (Exception e)
        {
            capi.Logger.Error("[SeasonalFlowers] Failed to get hemisphere for flower at {0}, defaulting to North. Exception: {1}", pos, e);
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

    // Calculates the total hours passed in the year up to the beginning of a given month.
    // This is used as a threshold for switching between seasonal phases.
    private double Threshold(IGameCalendar cal, int month)
    {
        long day = (month - 1) * cal.DaysPerMonth + 2;
        return day * cal.HoursPerDay + 1.0;
    }

    // Shifts a given month by 6 months to adjust for the Southern Hemisphere's seasons.
    private int ShiftMonth(int month)
    {
        month += 6;
        if (month > 12) month -= 12;
        return month;
    }

    // Modifies the flower's mesh to hide the petals. It does this by replacing the petal
    // textures with a transparent texture.
    private void HidePetals(ref MeshData mesh, TextureAtlasPosition texPos)
    {
        if (texPos == null) return;

        for (int i = 0; i < mesh.GetVerticesCount(); i++)
        {
            var textureId = mesh.TextureIndices[i / 4];
            foreach (var kv in Textures)
            {
                if (kv.Value.Baked.TextureSubId == mesh.TextureIds[textureId])
                {
                    if (kv.Key.ToLowerInvariant().Contains("petal"))
                    {
                        var uv = new Vec2f(mesh.Uv[i * 2], mesh.Uv[i * 2 + 1]);
                        var newUv = new Vec2f(
                            texPos.x1 + uv.X * (texPos.x2 - texPos.x1),
                            texPos.y1 + uv.Y * (texPos.y2 - texPos.y1)
                        );
                        mesh.Uv[i * 2] = newUv.X;
                        mesh.Uv[i * 2 + 1] = newUv.Y;
                    }
                }
            }
        }
    }

    // Modifies the flower's mesh to replace all its textures with a single, specified texture.
    // This is used for the "hibernate" phase.
    private void ApplyFullOverride(ref MeshData mesh, TextureAtlasPosition texPos)
    {
        if (texPos == null) return;

        for (int i = 0; i < mesh.GetVerticesCount(); i++)
        {
            var uv = new Vec2f(mesh.Uv[i * 2], mesh.Uv[i * 2 + 1]);
            var newUv = new Vec2f(
                texPos.x1 + uv.X * (texPos.x2 - texPos.x1),
                texPos.y1 + uv.Y * (texPos.y2 - texPos.y1)
            );
            mesh.Uv[i * 2] = newUv.X;
            mesh.Uv[i * 2 + 1] = newUv.Y;
        }
    }
}