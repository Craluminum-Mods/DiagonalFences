using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DiagonalFences;

/// <summary>
/// The fence's own textures, falling back to the arm shape's texture list for codes the fence
/// doesn't have. A fence variant only knows the textures of its own shape: vanilla's unconnected
/// wattle fence has no "wattle" texture, although every other wattle shape uses it.
/// </summary>
public class ArmTextureSource : ITexPositionSource
{
    private readonly ICoreClientAPI capi;
    private readonly Block fence;
    private readonly Shape armShape;
    private readonly ITexPositionSource fenceTextures;

    public ArmTextureSource(ICoreClientAPI capi, Block fence, Shape armShape)
    {
        this.capi = capi;
        this.fence = fence;
        this.armShape = armShape;
        fenceTextures = capi.Tesselator.GetTextureSource(fence);
    }

    public Size2i AtlasSize => fenceTextures.AtlasSize;

    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            bool fenceHasTexture = fence.Textures.ContainsKey(textureCode);
            if (fenceHasTexture) return fenceTextures[textureCode];

            bool armHasTexture = armShape.Textures?.ContainsKey(textureCode) == true;
            if (!armHasTexture) return fenceTextures[textureCode];

            // Already in the atlas when another variant of the fence uses it, then this just finds it
            AssetLocation texturePath = armShape.Textures[textureCode];
            capi.BlockTextureAtlas.GetOrInsertTexture(texturePath, out _, out TextureAtlasPosition position);
            return position;
        }
    }
}
