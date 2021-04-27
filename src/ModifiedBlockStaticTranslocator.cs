using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TranslocatorEngineering {
  public class ModifiedBlockStaticTranslocator : BlockStaticTranslocator, IPryable {
    public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
      // api.Logger.Notification("YYY: block.OnBlockBroken on " + api.Side);
      base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
    }
    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1) {
      // api.Logger.Notification("YYY: block.GetDrops on " + api.Side);
      var list = new List<ItemStack>();
      int scrapDropQty = 0;
      list.Add(new ItemStack(api.World.GetItem(new AssetLocation("translocatorengineering:coalescencecrystalshard")), api.World.Rand.Next(5, 6)));
      int metalPartsQty = api.World.Rand.Next(2, 4);
      list.Add(new ItemStack(api.World.GetBlock(new AssetLocation("game:metal-parts")), metalPartsQty));
      scrapDropQty += 4 - metalPartsQty;
      if (api.World.Rand.NextDouble() < 0.8) {
       list.Add(new ItemStack(api.World.GetItem(new AssetLocation("translocatorengineering:gatearray")), 1));
      }
      else {
         scrapDropQty += 1;
      }
      if (api.World.Rand.NextDouble() < 0.8) {
       list.Add(new ItemStack(api.World.GetItem(new AssetLocation("translocatorengineering:particulationcomponent")), 1));
      }
      else {
         scrapDropQty += 1;
      }
      list.Add(new ItemStack(api.World.GetItem(new AssetLocation("translocatorengineering:powercore")), 1));
      list.Add(new ItemStack(api.World.GetBlock(new AssetLocation("game:glassslab-plain-down-free")), 1));
      if (scrapDropQty > 0) {
        list.Add(new ItemStack(api.World.GetBlock(new AssetLocation("game:metal-scraps")), scrapDropQty));
      }
      //
      var blockEntity = api.World.BlockAccessor.GetBlockEntity(pos) as ModifiedBlockEntityStaticTranslocator;
      if (blockEntity != null) {
        list.AddRange(blockEntity.GetDrops());
      }
      return list.ToArray();
    }
    public void OnCrowbarPried(IPlayer player, BlockSelection blockSel) {
      // break block!
      // api.Logger.Notification("XXXYYY: block.OnCrowbarPried on " + api.Side);
      api.World.BlockAccessor.BreakBlock(blockSel.Position, player);
    }
  }
}
