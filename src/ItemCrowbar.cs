using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace TranslocatorEngineering {
  public class ItemCrowbar : Item {
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
      var foo = api.Side;
      if (blockSel != null && byEntity.Controls.Sneak) {
        var block = api.World.BlockAccessor.GetBlock(blockSel.Position);
        IPlayer player = byEntity.World.PlayerByUid((byEntity as EntityPlayer).PlayerUID) as IPlayer;
        handling = EnumHandHandling.PreventDefaultAction;
        // api.Logger.Notification("XXXYYY: Crowbar calling block.OnCrowbarPried on " + api.Side);
        (block as IPryable)?.OnCrowbarPried(player, blockSel);
      }
      base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
    }
  }
}
