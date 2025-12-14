using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;

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
        

        // Scan around each player instead of trying to process all chunks
        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
            if (player?.Entity?.Pos == null) continue;
            
            var playerPos = player.Entity.Pos.AsBlockPos;
            int scanRadius = 128;
            
            BlockPos minPos = playerPos.AddCopy(-scanRadius, -64, -scanRadius);
            BlockPos maxPos = playerPos.AddCopy(scanRadius, 64, scanRadius);

            blockAccessor.WalkBlocks(minPos, maxPos, (block, x, y, z) =>
            {
                if (block.Code.Path.Contains("flower"))
                {
                    
                    if (block.Code.Path.StartsWith("flower-"))
                    {
                        BlockPos pos = new BlockPos(x, y, z);
                        
                        var phen = FlowerPhenologyRegistry.Get(block.Code.Path);
                        int currentMonth = _sapi.World.Calendar.Month;
                        string correctPhase = "flowering";
                        
                        if (currentMonth >= phen.HibernateMonth || currentMonth < phen.GrowMonth)
                            correctPhase = "hibernating";
                        else if (currentMonth >= phen.GrowMonth && currentMonth < phen.FlowerMonth)
                            correctPhase = "growing";
                        else if (currentMonth >= phen.FlowerMonth && currentMonth < phen.WitherMonth)
                            correctPhase = "flowering";
                        else if (currentMonth >= phen.WitherMonth && currentMonth < phen.HibernateMonth)
                            correctPhase = "withering";
                        
                        // Build the target block code by manually adding phase variant
                        string targetPath = block.Code.Path + "-" + correctPhase;
                        AssetLocation targetCode = new AssetLocation("seasonalflowers", targetPath);
                        Block next = _sapi.World.GetBlock(targetCode);
                        
                        // If not found in mod domain, try game domain
                        if (next == null)
                        {
                            targetCode = new AssetLocation(block.Code.Domain, targetPath);
                            next = _sapi.World.GetBlock(targetCode);
                        }
                        
                        if (next != null && next.Id != block.Id)
                        {
                            blockAccessor.ExchangeBlock(next.Id, pos);
                        }
                    }
                }
            });
        }
    }
}