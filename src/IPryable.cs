using Vintagestory.API.Common;

namespace TranslocatorEngineering
{
  public interface IPryable {
    // float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter);
    void OnCrowbarPried(IPlayer player, BlockSelection blockSel);
  }
}