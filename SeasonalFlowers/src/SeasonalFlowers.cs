namespace SeasonalFlowers;

public class SeasonalFlowers : ModSystem {
    public override void Start(ICoreAPI api) {
        this.api = api;

        api.RegisterBlockClass("BlockFlower", typeof(BlockFlower));
    }
}
