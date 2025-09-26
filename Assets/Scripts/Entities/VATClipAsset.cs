using UnityEngine;

[CreateAssetMenu(menuName = "DOTS/VAT Clip", fileName = "NewVATClip" )]
public class VATClipAsset : ScriptableObject
{
    public Mesh Mesh;
    public Texture2DArray PositionTexture;
    public Texture2DArray NormalTexture;
    public int FrameCount;
    public float FrameRate;
    public int VertexCount;
    public int VertsPerRow;
    public int SliceHeight;
    public float Duration;
}
