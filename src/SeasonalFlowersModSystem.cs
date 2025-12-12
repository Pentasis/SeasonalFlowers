using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;

namespace SeasonalFlowers;

public class SeasonalFlowersModSystem : ModSystem
{
    private ICoreClientAPI _capi = null!;
    private int _lastCheckDay = -1;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("SeasonalFlowerBlock", typeof(SeasonalFlowerBlock));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        base.StartClientSide(api);
        _capi = api;
        api.Event.RegisterGameTickListener(OnGameTick, 5000);
    }

    private void OnGameTick(float dt)
    {
        if (_capi.World == null) return;

        var calendar = _capi.World.Calendar;
        int currentDay = calendar.DayOfYear;

        if (currentDay == _lastCheckDay) return;

        if (calendar.HourOfDay >= 22)
        {
            _lastCheckDay = currentDay;
            RedrawAllSeasonalFlowers();
        }
    }

    private void RedrawAllSeasonalFlowers()
    {
        var blockAccessor = _capi.World.BlockAccessor;
        int chunkSize = GlobalConstants.ChunkSize;

        int chunkMapSizeX = blockAccessor.MapSizeX / chunkSize;
        int chunkMapSizeZ = blockAccessor.MapSizeZ / chunkSize;

        foreach (long chunkIndex3d in _capi.World.LoadedChunkIndices)
        {
            int chunkX = (int)(chunkIndex3d % chunkMapSizeX);
            int chunkY = (int)(chunkIndex3d / ((long)chunkMapSizeX * chunkMapSizeZ));
            int chunkZ = (int)((chunkIndex3d / chunkMapSizeX) % chunkMapSizeZ);
            
            var chunk = blockAccessor.GetChunk(chunkIndex3d);

            if (chunk == null || chunk.Disposed) continue;

            int chunkWorldX = chunkX * chunkSize;
            int chunkWorldY = chunkY * chunkSize;
            int chunkWorldZ = chunkZ * chunkSize;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        BlockPos blockPos = new BlockPos(
                            chunkWorldX + x,
                            chunkWorldY + y,
                            chunkWorldZ + z
                        );
                        
                        var block = blockAccessor.GetBlock(blockPos);

                        if (block is SeasonalFlowerBlock)
                        {
                            blockAccessor.MarkBlockDirty(blockPos);
                        }
                    }
                }
            }
        }
    }
}