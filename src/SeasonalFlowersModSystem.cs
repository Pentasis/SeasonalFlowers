using Vintagestory.API.Common;

namespace SeasonalFlowers;

public class SeasonalFlowersModSystem : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("SeasonalFlowerBlock", typeof(SeasonalFlowerBlock));
    }
}