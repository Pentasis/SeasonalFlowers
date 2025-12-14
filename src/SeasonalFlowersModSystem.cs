using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;

namespace SeasonalFlowers;

public class SeasonalFlowersModSystem : ModSystem
{
    private ICoreServerAPI _sapi = null!;
    private int _lastCheckedMonth = -1;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("SeasonalFlowerBlock", typeof(SeasonalFlowerBlock));
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        _sapi = api;

        // Check once every in-game hour (3000 ms real-time is fine too)
        _sapi.Event.RegisterGameTickListener(OnServerTick, 3000);
    }

    private void OnServerTick(float dt)
    {
        var cal = _sapi.World.Calendar;

        int currentMonth = cal.Month;

        // Only act when month changes
        if (currentMonth == _lastCheckedMonth) return;
        _lastCheckedMonth = currentMonth;

        UpdateSeasonalFlowers();
    }
    
    private void UpdateSeasonalFlowers()
    {
        var world = _sapi.World;
        var blockAccessor = world.BlockAccessor;

        // We only need to run this scan once for the entire server, not per player.
        // We'll scan all loaded chunks.

        foreach (var kvp in _sapi.WorldManager.AllLoadedChunks)
        {
            long chunkIndex3d = kvp.Key;
            int chunkSize = GlobalConstants.ChunkSize;
            
            // Convert 3D index to chunk coordinates
            Vec3i chunkPos = new Vec3i();
            MapUtil.PosInt3d(chunkIndex3d, _sapi.WorldManager.MapSizeX / chunkSize, _sapi.WorldManager.MapSizeZ / chunkSize, chunkPos);

            BlockPos minPos = new BlockPos(
                chunkPos.X * chunkSize,
                chunkPos.Y * chunkSize,
                chunkPos.Z * chunkSize
            );

            BlockPos maxPos = minPos.AddCopy(chunkSize - 1, chunkSize - 1, chunkSize - 1);

            blockAccessor.WalkBlocks(minPos, maxPos, (block, x, y, z) =>
            {
                if (block is not SeasonalFlowerBlock flower) return;

                BlockPos pos = new BlockPos(x, y, z);

                string currentPhase = block.Variant["phase"];
                string correctPhase = flower.GetCorrectPhase(_sapi.World.Calendar, pos);

                if (currentPhase == correctPhase) return;

                Block next = _sapi.World.GetBlock(block.CodeWithVariant("phase", correctPhase));
                if (next != null)
                {
                    blockAccessor.ExchangeBlock(next.Id, pos);
                }
            });
        }

    }
}