using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TranslocatorEngineering {
  [HarmonyPatch(typeof(BlockEntityStaticTranslocator))]
  [HarmonyPatch("DoRepair")]
  public class Patch_BlockEntityStaticTranslocator_DoRepair {
    static void Prefix(BlockEntityStaticTranslocator __instance) {
      (__instance as ModifiedBlockEntityStaticTranslocator)?.OnDoRepair();
    }
  }
  public class ModifiedBlockEntityStaticTranslocator : BlockEntityStaticTranslocator {
    // easy access to base class's privates
    static Type BaseType = typeof(BlockEntityStaticTranslocator);
    public bool CanTeleport { get { return this.XXX_GetFieldValue<bool>(BaseType, "canTeleport"); } set { this.XXX_SetFieldValue<bool>(BaseType, "canTeleport", value); } }
    public int RepairState { get { return this.XXX_GetFieldValue<int>(BaseType, "repairState"); } set { this.XXX_SetFieldValue<int>(BaseType, "repairState", value); } }
    public bool FindNextChunk { get { return this.XXX_GetFieldValue<bool>(BaseType, "findNextChunk"); } set { this.XXX_SetFieldValue<bool>(BaseType, "findNextChunk", value); } }
    // extra properties
    int gearsAdded = 0;
    bool wasPlaced = false;
    double lastDestinationAssignmentTimestamp = 0;
    public override void OnBlockBroken() {
      // Api.Logger.Notification("XXXYYY: BlockEntity.OnBlockBroken on " + Api.Side);
      base.OnBlockBroken();
      if (Api.Side == EnumAppSide.Client) { return; }
      // unlink paired translocator
      if (this.tpLocation != null) {
        Api.ModLoader.GetModSystem<TranslocatorEngineeringMod>().SetDestinationOrQueue(this.tpLocation, null); // unlink linked translocator
      }
    }
    public ItemStack[] GetDrops() {
      // Api.Logger.Notification("XXXYYY: BlockEntity GetDrops");
      var list = new List<ItemStack>();
      list.Add(new ItemStack(Api.World.GetBlock(new AssetLocation("game:metal-parts")), 2));
      for (var i = 0; i < gearsAdded; i += 1) {
        list.Add(new ItemStack(Api.World.GetItem(new AssetLocation("game:gear-temporal")), 1)); // gears don't stack
      }
      return list.ToArray();
    }
    // called by Patch_BlockEntityStaticTranslocator_DoRepair
    public void OnDoRepair() {
      if (FullyRepaired) { return; }
      if (RepairState == 1) { return; } // metal parts are being added
      gearsAdded += 1;
    }
    public override void Initialize(ICoreAPI api) {
      base.Initialize(api);
      if (api.Side == EnumAppSide.Server) {
        var queuedAssignment = api.ModLoader.GetModSystem<TranslocatorEngineeringMod>().PullQueuedDestinationAssignment(this.Pos);
        if (queuedAssignment != null) {
          // api.Logger.Notification($"XXX: ModifiedBlockEntityStaticTranslocator.Initialize: queuedAssignment! {queuedAssignment.dstPos?.ToString()} ({queuedAssignment.timestamp})");
          SetDestination(queuedAssignment.dstPos, queuedAssignment.timestamp);
        }
      }
    }
    public void SetDestination(BlockPos dstPos, double timestamp) { // also called by TranslocatorEngineeringMod.SetDestinationOrQueue
      // if this assignment is old news, ignore it (consider player visits A, then B (linking A to B), then visits A's previous dst before returning to A)
      if (timestamp < lastDestinationAssignmentTimestamp) {
        // Api.Logger.Notification($"XXX: ModifiedBlockEntityStaticTranslocator.SetDestination: old news! {timestamp} < {lastDestinationAssignmentTimestamp} for {dstPos?.ToString()}");
        return;
      }
      lastDestinationAssignmentTimestamp = timestamp;
      // if this assignment is a link (not an unlink), and we are already linked, unlink the current destination
      if (dstPos != null && this.tpLocation != null) {
        // Api.Logger.Notification($"XXX: ModifiedBlockEntityStaticTranslocator.SetDestination: linking already linked! unlink current destination {this.tpLocation?.ToString()} @ preversed timestamp {timestamp}");
        Api.ModLoader.GetModSystem<TranslocatorEngineeringMod>().SetDestinationOrQueue(this.tpLocation, null, timestamp); // same timestamp!
      }
      // n.b. Api is null?!?!?!?!?!?!?!?
      // Api.Logger.Notification($"XXX: ModifiedBlockEntityStaticTranslocator.SetDestination: finishing up by setting tpLocation and CanTeleport: {this.tpLocation?.ToString()} => {dstPos?.ToString()}");
      this.tpLocation = dstPos;
      CanTeleport = dstPos != null;
    }
    public void Link(BlockPos otherPos, BlockPos otherDstPos, double otherDstTimestamp) {
      if (otherPos.Equals(Pos)) { return; } // can't link to self!
      // Api.Logger.Notification($"XXX: ModifiedBlockEntityStaticTranslocator.Link: start: otherPos {otherPos?.ToString()} otherDstPos {otherDstPos?.ToString()} otherDstTimestamp {otherDstTimestamp}");
      SetDestination(otherPos, Api.World.Calendar.TotalDays);
      Api.ModLoader.GetModSystem<TranslocatorEngineeringMod>().SetDestinationOrQueue(otherPos, Pos); // link stored translocator
      // if "other" translocator had a destination when the linker was syncronized, null out its destination (but using the old timestamp)
      // ... this fixes the awkward situation in which you sync A, teleport from A to B, then link C right next to B, and C still appears linked until A is chunkloaded
      if (otherDstPos != null) {
        // Api.Logger.Notification($"XXX: ModifiedBlockEntityStaticTranslocator.Link: fixup for otherDstPos @ {otherDstTimestamp}");
        Api.ModLoader.GetModSystem<TranslocatorEngineeringMod>().SetDestinationOrQueue(otherDstPos, null, otherDstTimestamp);
      }
    }
    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve) {
      base.FromTreeAttributes(tree, worldAccessForResolve);
      wasPlaced = tree.GetBool("wasPlaced", false);
      var defaultGearsAdded = 0;
      if (!wasPlaced) {
        if (RepairState == 2 || RepairState == 3) { // 2: non-clockmaker has added 1 gear; 3: non-clockmaker has added 2 gears OR clockmaker has added 1
          defaultGearsAdded = 1; // gotta round down to avoid cheesing
        }
        else if (RepairState == 4) { // 4: fully fixed, either via 2 gears for the clockmaker or 3 for non-clockmakers
          defaultGearsAdded = 2; // gotta round down to avoid cheesing
        }
      }
      gearsAdded = tree.GetInt("gearsAdded", defaultGearsAdded);
      lastDestinationAssignmentTimestamp = tree.GetDouble("lastDestinationAssignmentTimestamp", 0);
    }
    public override void ToTreeAttributes(ITreeAttribute tree) {
      base.ToTreeAttributes(tree);
      tree.SetInt("gearsAdded", gearsAdded);
      tree.SetBool("wasPlaced", wasPlaced);
      tree.SetDouble("lastDestinationAssignmentTimestamp", lastDestinationAssignmentTimestamp);
    }
    public override void OnBlockPlaced(ItemStack byItemStack = null) {
      // confusingly, OnBlockPlaced gets called with null when the Translocator is repaired with Metal Parts!
      if (byItemStack == null) { return; } // repairing with Metal Parts is not being "placed"
      wasPlaced = true;
      FindNextChunk = false; // disable automatically searching for target
      // when "placed", translocator is fully repaired (metal parts and 2 gears, as if repaired by clockmaker)
      RepairState = 4;
      gearsAdded = 2;
      setupGameTickers();
    }
    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc) {
      if (FullyRepaired && wasPlaced && !CanTeleport) {
        dsc.AppendLine("Unlinked.");
      }
      else {
        base.GetBlockInfo(forPlayer, dsc);
      }
    }
  }
}
