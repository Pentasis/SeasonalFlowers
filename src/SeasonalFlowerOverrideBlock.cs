using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace SeasonalFlowers;

public class SeasonalFlowerOverrideBlock
{
    FlowerPhenology phenology;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        phenology = FlowerPhenologyRegistry.Get(Code.Path);
    }

    // WORLD: seasonal
    public override void OnBeforeRender(ICoreClientAPI capi, BlockPos pos, ref MeshData mesh)
    {
        base.OnBeforeRender(capi, pos, ref mesh);
        ApplySeasonalTexture_World(capi, pos, ref mesh);
    }

    // ITEM / DROPPED / POT: always bloom
    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
        ApplyBloomTexture_Item(capi, ref renderinfo);
    }

    void ApplySeasonalTexture_World(ICoreClientAPI capi, BlockPos pos, ref MeshData mesh)
    {
        var cal = capi.World.Calendar;

        // relYear: fraction in 0..1
        float relYear = cal.YearRel; // API provides YearRel (0..1)
        // If calendar supports hemisphere queries, query it and flip half-year for southern hemisphere
        try
        {
            var hem = cal.GetHemisphere(pos); // returns EnumHemisphere
            if (hem == Vintagestory.API.Common.EnumHemisphere.South)
            {
                relYear = (relYear + 0.5f) % 1f;
            }
        }
        catch
        {
            // If GetHemisphere not available, optional fallback: rely on default (northern) or provide config.
        }

        // per-position variation: convert VariationDays -> fraction
        float variationRel = (float)phenology.VariationDays / Math.Max(1, cal.DaysPerYear);

        // add deterministic per-position pseudo-random shift within variationRel
        var rand = new Random(pos.GetHashCode());
        float shiftRel = (float)(rand.NextDouble() * 2.0 - 1.0) * variationRel;

        float relShifted = (relYear + shiftRel + 1f) % 1f;

        string phase;
        if (relShifted < phenology.GrowStartRel)
            phase = "hibernate";
        else if (relShifted < phenology.BloomStartRel)
            phase = "grow";
        else if (relShifted < phenology.BloomEndRel)
            phase = "bloom";
        else
            phase = "hibernate";

        var tex = capi.BlockTextureAtlas.GetOrInsertTexture(
            new AssetLocation($"seasonalflowers:block/{Code.Path}/{phase}")
        );

        mesh.SetTexPos(tex.SubPosition, tex.SubSize);
    }

    void ApplyBloomTexture_Item(ICoreClientAPI capi, ref ItemRenderInfo renderinfo)
    {
        var tex = capi.BlockTextureAtlas.GetOrInsertTexture(
            new AssetLocation($"seasonalflowers:block/{Code.Path}/bloom")
        );

        renderinfo.TextureId = tex.AtlasTextureId;
    }
}
