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
    private TextureAtlasPosition? _transparentTexPos;
    private TextureAtlasPosition? _hibernationTexPos;

    // Called when the block is loaded by the game. This method initializes the phenology
    // for the flower by retrieving it from the central registry.
    public override void OnLoaded(ICoreAPI coreApi)
    {
        base.OnLoaded(coreApi);
        _phen = FlowerPhenologyRegistry.Get(Code.Path);

        // Pre-load textures on the main thread
        if (coreApi is ICoreClientAPI capi)
        {
            if (!capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation("seasonalflowers:block/transparent"), out _, out _transparentTexPos))
            {
                capi.Logger.Error("[SeasonalFlowers] Failed to load texture: seasonalflowers:block/transparent");
            }
            if (!capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation("seasonalflowers:block/hibernation"), out _, out _hibernationTexPos))
            {
                capi.Logger.Error("[SeasonalFlowers] Failed to load texture: seasonalflowers:block/hibernation");
            }
        }
    }

    // This method is called by the engine when it's generating the visual mesh for a chunk.
    // It allows for modification of the block's mesh before it's sent to the graphics card.
    // Here, it's used to apply seasonal textures to the flower.
    public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
    {
        var capi = api as ICoreClientAPI;
        if (capi == null) return;

        // Clone the sourceMesh to avoid modifying the cached mesh directly
        MeshData mesh = sourceMesh.Clone();

        string phase = GetPhase(capi, capi.World.Calendar, pos);

        if (phase == "hibernate")
        {
            if (_hibernationTexPos != null)
            {
                ApplyFullOverride(ref mesh, _hibernationTexPos);
            }
        }
        else if (phase == "grow" || phase == "wither")
        {
            if (_transparentTexPos != null)
            {
                HidePetals(ref mesh, _transparentTexPos);
            }
        }
        // if (phase == "flower") {} ==> Vanilla Textures (no modifications needed)

        // Assign the modified mesh back to sourceMesh
        sourceMesh = mesh;
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
        var capi = api as ICoreClientAPI;
        if (capi == null) return;

        for (int i = 0; i < mesh.GetVerticesCount(); i++)
        {
            // Get the texture ID used by the current face of the mesh
            // mesh.TextureIndices[i / 4] gives the index into mesh.TextureIds
            // mesh.TextureIds[index] gives the actual texture sub ID
            int currentMeshTextureSubId = mesh.TextureIds[mesh.TextureIndices[i / 4]];

            // Find the CompositeTexture in the Block's Textures dictionary that matches this sub ID
            CompositeTexture? matchedCompositeTexture = null;
            string matchedKey = "N/A"; // For logging
            foreach (var kv in Textures)
            {
                if (kv.Value.Baked.TextureSubId == currentMeshTextureSubId)
                {
                    matchedCompositeTexture = kv.Value;
                    matchedKey = kv.Key;
                    break; // Found the matching texture definition
                }
            }

            if (matchedCompositeTexture != null)
            {
                capi.Logger.Debug($"[SeasonalFlowers] HidePetals: BlockCode={Code.Path}, Matched Texture Key={matchedKey}, Matched Path={matchedCompositeTexture.Base.Path}, Mesh TextureSubId={currentMeshTextureSubId}");

                // Now check if the path of this matched texture contains "petal"
                if (matchedCompositeTexture.Base.Path.Contains("petal"))
                {
                    capi.Logger.Debug($"[SeasonalFlowers] HidePetals: Path contains 'petal' for BlockCode={Code.Path}, Key={matchedKey}. Applying transparency.");
                    var uv = new Vec2f(mesh.Uv[i * 2], mesh.Uv[i * 2 + 1]);
                    var newUv = new Vec2f(
                        texPos.x1 + uv.X * (texPos.x2 - texPos.x1),
                        texPos.y1 + uv.Y * (texPos.y2 - texPos.y1)
                    );
                    mesh.Uv[i * 2] = newUv.X;
                    mesh.Uv[i * 2 + 1] = newUv.Y;
                }
                else
                {
                    capi.Logger.Debug($"[SeasonalFlowers] HidePetals: Path does NOT contain 'petal' for BlockCode={Code.Path}, Key={matchedKey}. Path: {matchedCompositeTexture.Base.Path}");
                }
            }
            else
            {
                capi.Logger.Debug($"[SeasonalFlowers] HidePetals: BlockCode={Code.Path}, No matching CompositeTexture found in Block.Textures for mesh TextureSubId={currentMeshTextureSubId}");
            }
        }
    }

    // Modifies the flower's mesh to replace all its textures with a single, specified texture.
    // This is used for the "hibernate" phase.
    private void ApplyFullOverride(ref MeshData mesh, TextureAtlasPosition texPos)
    {
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