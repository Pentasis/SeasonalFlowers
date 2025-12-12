using Vintagestory.API.Client;
using Vintagestory.GameContent;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using System.Collections.Generic;
using System;

namespace SeasonalFlowers;

public class SeasonalFlowerBlock : BlockPlant
{
    private FlowerPhenology _phen = null!;

    public override void OnLoaded(ICoreAPI coreApi)
    {
        base.OnLoaded(coreApi);
        _phen = FlowerPhenologyRegistry.Get(Code.Path);
    }

    public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
    {
        var capi = api as ICoreClientAPI;
        if (capi == null) return;

        string phase = GetPhase(capi, capi.World.Calendar, pos);

        if (phase == "hibernate")
        {
            ApplyFullOverride(capi, ref sourceMesh, "hibernation");
            return;
        }

        if (phase == "grow" || phase == "wither")
        {
            HidePetals(capi, ref sourceMesh);
        }
    }

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

    private void HidePetals(ICoreClientAPI capi, ref MeshData mesh)
    {
        if (!capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation("seasonalflowers:block/transparent"), out _, out var texPos))
            return;

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

    private void ApplyFullOverride(ICoreClientAPI capi, ref MeshData mesh, string name)
    {
        if (!capi.BlockTextureAtlas.GetOrInsertTexture(new AssetLocation($"seasonalflowers:block/{name}"), out _, out var texPos))
            return;

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