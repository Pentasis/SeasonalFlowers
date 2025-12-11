using Vintagestory.API.Common;

namespace SeasonalFlowers;

public class SeasonalFlowersModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("SeasonalFlowerOverride", typeof(SeasonalFlowerOverrideBlock));
    }

    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);

        foreach (var block in api.World.Blocks)
        {
            if (block?.Code == null) continue;

            if (block.Code.Path.StartsWith("flower-"))
            {
                block.BlockClass = "SeasonalFlowerOverride";
            }
        }
    }
}