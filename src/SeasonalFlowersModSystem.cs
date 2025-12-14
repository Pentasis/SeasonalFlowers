using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.MathTools;
using Vintagestory.API.Config;
using System.Linq;

namespace SeasonalFlowers;

public class SeasonalFlowersModSystem : ModSystem
{
    private ICoreServerAPI _sapi = null!;
    private int _lastCheckedMonth = -1;

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        api.RegisterBlockClass("SeasonalFlowerBlock", typeof(SeasonalFlowerBlock));
        api.Logger.Debug("[SeasonalFlowers] Registered SeasonalFlowerBlock class");
    }
    
    public override void StartServerSide(ICoreServerAPI api)
    {
        _sapi = api;

        // Debug: List flower blocks to see what's available
        api.Logger.Debug("[SeasonalFlowers] Checking available flower blocks...");
        var allBlocks = api.World.Blocks.Where(b => b?.Code?.Path?.Contains("flower") == true);
        var flowerBlocks = allBlocks.Take(20).ToList();
        api.Logger.Debug("[SeasonalFlowers] Found {0} total flower blocks, showing first 20:", allBlocks.Count());
        
        foreach (var block in flowerBlocks)
        {
            api.Logger.Debug("[SeasonalFlowers] Available: {0}", block.Code);
        }
        
        // Check specifically for seasonal variants
        var seasonalBlocks = api.World.Blocks.Where(b => b?.Code?.Path?.Contains("flowering") == true || 
                                                         b?.Code?.Path?.Contains("growing") == true ||
                                                         b?.Code?.Path?.Contains("withering") == true ||
                                                         b?.Code?.Path?.Contains("hibernating") == true).Take(10);
        api.Logger.Debug("[SeasonalFlowers] Seasonal variants found: {0}", seasonalBlocks.Count());
        foreach (var block in seasonalBlocks)
        {
            api.Logger.Debug("[SeasonalFlowers] Seasonal: {0}", block.Code);
        }

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

        _sapi.Logger.Debug("[SeasonalFlowers] Month changed to {0}, updating flowers", currentMonth);
        UpdateSeasonalFlowers();
    }
    
    private void UpdateSeasonalFlowers()
    {
        var world = _sapi.World;
        var blockAccessor = world.BlockAccessor;
        
        int flowersFound = 0;
        int flowersUpdated = 0;
        int playersScanned = 0;

        // Scan around each player instead of trying to process all chunks
        foreach (var player in _sapi.World.AllOnlinePlayers)
        {
            if (player?.Entity?.Pos == null) continue;
            
            playersScanned++;
            var playerPos = player.Entity.Pos.AsBlockPos;
            int scanRadius = 128;
            
            BlockPos minPos = playerPos.AddCopy(-scanRadius, -64, -scanRadius);
            BlockPos maxPos = playerPos.AddCopy(scanRadius, 64, scanRadius);
            
            if (playersScanned == 1)
            {
                _sapi.Logger.Debug("[SeasonalFlowers] Scanning around player at {0}", playerPos);
            }

            blockAccessor.WalkBlocks(minPos, maxPos, (block, x, y, z) =>
            {
                if (block.Code.Path.Contains("flower"))
                {
                    if (flowersFound < 5)
                    {
                        _sapi.Logger.Debug("[SeasonalFlowers] Found flower: {0}", block.Code.Path);
                    }
                    flowersFound++;
                    
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
                            flowersUpdated++;
                            _sapi.Logger.Debug("[SeasonalFlowers] Updating {0} to {1}", block.Code, targetCode);
                            blockAccessor.ExchangeBlock(next.Id, pos);
                        }
                        else if (flowersFound <= 10)
                        {
                            _sapi.Logger.Debug("[SeasonalFlowers] No seasonal variant found for {0} -> {1}", block.Code, targetCode);
                        }
                    }
                }
            });
        }
        
        _sapi.Logger.Debug("[SeasonalFlowers] Scan complete: {0} players, {1} flowers found, {2} updated", playersScanned, flowersFound, flowersUpdated);
    }
}