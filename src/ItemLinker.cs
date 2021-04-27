using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace TranslocatorEngineering {
  public class ItemLinker : Item {
    private static readonly int MAX_DISTANCE = 8000;
    private BlockPos GetStoredSrcPos(ItemStack itemStack) {
      if (!itemStack.Attributes.HasAttribute("srcPos")) { return null; }
      return SerializerUtil.Deserialize<BlockPos>(itemStack.Attributes.GetBytes("srcPos"), null);
    }
    private void SetStoredSrcPos(ItemStack itemStack, BlockPos blockPos) {
      if (blockPos == null) {
        itemStack.Attributes.RemoveAttribute("srcPos");
      }
      else {
        itemStack.Attributes.SetBytes("srcPos", SerializerUtil.Serialize<BlockPos>(blockPos));
      }
    }
    private BlockPos GetStoredDstPos(ItemStack itemStack) {
      if (!itemStack.Attributes.HasAttribute("dstPos")) { return null; }
      return SerializerUtil.Deserialize<BlockPos>(itemStack.Attributes.GetBytes("dstPos"), null);
    }
    private void SetStoredDstPos(ItemStack itemStack, BlockPos blockPos) {
      if (blockPos == null) {
        itemStack.Attributes.RemoveAttribute("dstPos");
      }
      else {
        itemStack.Attributes.SetBytes("dstPos", SerializerUtil.Serialize<BlockPos>(blockPos));
      }
    }
    private double GetStoredTimestamp(ItemStack itemStack) {
      return itemStack.Attributes.GetDouble("timestamp");
    }
    private void SetStoredTimestamp(ItemStack itemStack, double value) {
      itemStack.Attributes.SetDouble("timestamp", value);
    }
    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling) {
      ICoreClientAPI capi = api as ICoreClientAPI;
      ItemStack itemStack = slot.Itemstack;
      BlockPos storedSrcPos = GetStoredSrcPos(itemStack);

      if (byEntity.Controls.Sneak) {
        if (storedSrcPos != null) {
          SetStoredSrcPos(itemStack, null);
          SetStoredDstPos(itemStack, null);
          handling = EnumHandHandling.PreventDefault;
          capi?.TriggerIngameError(null, null, Lang.Get("translocatorengineering:ingameerror-linker-cleared"));
          byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/effect/latch"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
        }
        return;
      }

      if (blockSel?.Position == null) { return; }

      var blockEntity = api.World.BlockAccessor.GetBlockEntity(blockSel.Position);
      var blockEntityTranslocator = blockEntity as ModifiedBlockEntityStaticTranslocator;
      if (blockEntityTranslocator == null) { return; }
      if (!blockEntityTranslocator.FullyRepaired) { return; } // can't link an unrepaired translocator

      // if we already have a stored position, ask blockEntityTranslocator to link the translocator at the stored position
      if (storedSrcPos != null && !storedSrcPos.Equals(blockSel.Position)) {
        // check distance
        var distance = byEntity.Pos.AsBlockPos.DistanceTo(storedSrcPos);
        if (distance > MAX_DISTANCE) {
          capi?.TriggerIngameError(null, null, Lang.Get("translocatorengineering:ingameerror-linker-out-of-range"));
        }
        else {
          blockEntityTranslocator.Link(storedSrcPos, GetStoredDstPos(itemStack), GetStoredTimestamp(itemStack));
          capi?.TriggerIngameError(null, null, Lang.Get("translocatorengineering:ingameerror-linker-linked"));
          if (api.Side == EnumAppSide.Server) {
            byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/block/teleporter"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
          }
          SetStoredSrcPos(itemStack, null);
        }
      }
      else {
        capi?.TriggerIngameError(null, null, Lang.Get("translocatorengineering:ingameerror-linker-synced"));
        if (api.Side == EnumAppSide.Server) {
          byEntity.World.PlaySoundAt(new AssetLocation("game:sounds/tool/padlock"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z, null, true, 32f, 1f);
        }
        SetStoredSrcPos(itemStack, blockSel.Position);
        SetStoredDstPos(itemStack, blockEntityTranslocator.tpLocation); // can be null
        SetStoredTimestamp(itemStack, byEntity.World.Calendar.TotalDays);
      }

      handling = EnumHandHandling.PreventDefault;
    }
    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot) {
      var translocatorItemStacks = new ItemStack[] { new ItemStack(api.World.GetBlock(new AssetLocation("game:statictranslocator-normal-north")), 1) };
      WorldInteraction[] interactions;
      if (GetStoredSrcPos(inSlot.Itemstack) != null) {
        interactions = new WorldInteraction[] {
          new WorldInteraction
          {
            ActionLangCode = "Link", // "heldhelp-linker-link",
            MouseButton = (EnumMouseButton)2,
            Itemstacks = translocatorItemStacks
          },
          new WorldInteraction
          {
            ActionLangCode = "Desynchronize", // "heldhelp-linker-clear",
            HotKeyCode = "sneak",
            MouseButton = (EnumMouseButton)2
          }
        };
      }
      else {
        interactions = new WorldInteraction[] {
          new WorldInteraction
          {
            ActionLangCode = "Synchronize", // "heldhelp-linker-sync",
            MouseButton = (EnumMouseButton)2,
            Itemstacks = translocatorItemStacks
          }
        };
      }
      return ArrayExtensions.Append<WorldInteraction>(interactions, base.GetHeldInteractionHelp(inSlot));
    }
    #region rendering
    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo) {
      var storedSrcPos = GetStoredSrcPos(itemstack);
      if (storedSrcPos == null) {
        renderinfo.ModelRef = meshrefs[0]; // no lights, no screen
      }
      else {
        var distance = capi.World.Player.Entity.Pos.AsBlockPos.DistanceTo(storedSrcPos);
        var lights = (int)GameMath.Clamp(Math.Ceiling(8 - (8 * distance / MAX_DISTANCE)), 0, 8);
        if (lights == 0) {
          renderinfo.ModelRef = meshrefs[capi.World.Rand.NextDouble() < 0.25 ? 1 : 0];
        }
        else {
          renderinfo.ModelRef = meshrefs[1 + lights];
        }
      }
      // renderinfo.ModelRef = meshrefs[(int)((float)capi.World.ElapsedMilliseconds / 1000f) % 10];
    }
    MeshRef[] meshrefs;
    public override void OnLoaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        OnLoadedClientSide(api as ICoreClientAPI);
      }
    }
    private void OnLoadedClientSide(ICoreClientAPI capi) {
      meshrefs = new MeshRef[10];
      string key = Code.ToString() + "-meshes";
      var shape = capi.Assets.TryGet("translocatorengineering:shapes/item/linker.json").ToObject<Shape>().Clone();
      meshrefs[0] = tesselateAndUpload(this, shape, capi);
      shape.GetElementByName("Screen").Faces["up"].Glow = 120; meshrefs[1] = tesselateAndUpload(this, shape, capi);
      shapeElementAdjustUpFaceGlowAndTexture(shape, "Light1", 255, "#fire-red"); meshrefs[2] = tesselateAndUpload(this, shape, capi);
      shapeElementAdjustUpFaceGlowAndTexture(shape, "Light2", 255, "#fire-red"); meshrefs[3] = tesselateAndUpload(this, shape, capi);
      shapeElementAdjustUpFaceGlowAndTexture(shape, "Light3", 255, "#fire-red"); meshrefs[4] = tesselateAndUpload(this, shape, capi);
      shapeElementAdjustUpFaceGlowAndTexture(shape, "Light4", 255, "#fire-red"); meshrefs[5] = tesselateAndUpload(this, shape, capi);
      shapeElementAdjustUpFaceGlowAndTexture(shape, "Light5", 255, "#fire-red"); meshrefs[6] = tesselateAndUpload(this, shape, capi);
      shapeElementAdjustUpFaceGlowAndTexture(shape, "Light6", 255, "#fire-red"); meshrefs[7] = tesselateAndUpload(this, shape, capi);
      shapeElementAdjustUpFaceGlowAndTexture(shape, "Light7", 255, "#fire-red"); meshrefs[8] = tesselateAndUpload(this, shape, capi);
      shapeElementAdjustUpFaceGlowAndTexture(shape, "Light8", 255, "#fire-red"); meshrefs[9] = tesselateAndUpload(this, shape, capi);
    }
    private static MeshRef tesselateAndUpload(CollectibleObject collectible, Shape shape, ICoreClientAPI capi) {
      capi.Tesselator.TesselateShape(collectible, shape, out MeshData meshData, new Vec3f(0, 0, 0));
      return capi.Render.UploadMesh(meshData);
    }
    private static void shapeElementAdjustUpFaceGlowAndTexture(Shape shape, string name, int glow, string newTexture) {
      var upFace = shape.GetElementByName(name).Faces["up"];
      upFace.Glow = glow;
      if (newTexture != null) {
        upFace.Texture = newTexture;
      }
    }
    public override void OnUnloaded(ICoreAPI api) {
      if (api.Side == EnumAppSide.Client) {
        for (var meshIndex = 0; meshIndex < meshrefs.Length; meshIndex += 1) {
          meshrefs[meshIndex]?.Dispose();
          meshrefs[meshIndex] = null;
        }
      }
    }
    #endregion
  }
}
