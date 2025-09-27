using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

/// <summary>
/// Shared VAT playback parameters baked from authoring data.
/// </summary>
public struct VATAnimationSettings : IComponentData
{
    public int FrameCount;
    public float FrameRate;
    public float Speed;
    public float Offset;
}

/// <summary>
/// Runtime state used to initialize per-instance shader data.
/// </summary>
public struct VATAnimationState : IComponentData
{
    public byte Initialized;
    public float ManualTime;
}

[MaterialProperty("_AnimStartTime")]
public struct VATAnimStartTimeProperty : IComponentData
{
    public float Value;
}

[MaterialProperty("_ManualTime")]
public struct VATAnimManualTimeProperty : IComponentData
{
    public float Value;
}

[MaterialProperty("_AnimSpeed")]
public struct VATAnimSpeedProperty : IComponentData
{
    public float Value;
}

[MaterialProperty("_AnimOffset")]
public struct VATAnimOffsetProperty : IComponentData
{
    public float Value;
}

[MaterialProperty("_Frames")]
public struct VATFrameCountProperty : IComponentData
{
    public float Value;
}

[MaterialProperty("_FPS")]
public struct VATFrameRateProperty : IComponentData
{
    public float Value;
}

[MaterialProperty("_VertsPerRow")]
public struct VATVertsPerRowProperty : IComponentData
{
    public float Value;
}

[MaterialProperty("_SliceHeight")]
public struct VATSliceHeightProperty : IComponentData
{
    public float Value;
}

[MaterialProperty("_EntitiesTime")]
public struct VATTimeProperty : IComponentData
{
    public float Value;
}
