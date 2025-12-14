using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SeasonalFlowers;

public class SeasonalFlowersModSystem : ModSystem
{
    private ICoreServerAPI sapi;
    private int lastCheckedMonth = -1;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("SeasonalFlowerBlock", typeof(SeasonalFlowerBlock));
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        sapi = api;

        // Check once every in-game hour (1000 ms real-time is fine too)
        sapi.Event.RegisterGameTickListener(OnServerTick, 1000);
    }

    private void OnServerTick(float dt)
    {
        var cal = sapi.World.Calendar;

        int currentMonth = cal.Month;

        // Only act when month changes
        if (currentMonth == lastCheckedMonth) return;
        lastCheckedMonth = currentMonth;

        UpdateSeasonalFlowers();
    }
    
    private void UpdateSeasonalFlowers()
    {
        var world = sapi.World;
        var blockAccessor = world.BlockAccessor;

        foreach (var player in sapi.World.AllOnlinePlayers)
        {
            var chunkPos = player.Entity.ServerPos.AsBlockPos;

            // Radius: only loaded chunks around players
            int radius = 8;

            blockAccessor.WalkBlocks(
                chunkPos.AddCopy(-radius * 16, -8, -radius * 16),
                chunkPos.AddCopy(radius * 16, 8, radius * 16),
                (pos, block) =>
                {
                    if (block is SeasonalFlowerBlock flower)
                    {
                        string correctPhase = flower.GetCorrectPhase(world.Calendar, pos);
                        string currentPhase = block.Variant["phase"];

                        if (currentPhase != correctPhase)
                        {
                            Block next = world.GetBlock(block.CodeWithVariant("phase", correctPhase));
                            if (next != null)
                            {
                                blockAccessor.ExchangeBlock(next.Id, pos);
                            }
                        }
                    }
                }
            );
        }
    }
}
