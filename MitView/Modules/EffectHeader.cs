using System.Runtime.InteropServices;

namespace MitView.Modules
{
    [StructLayout(LayoutKind.Explicit)]
    public struct EffectHeader
    {
        [FieldOffset(8)] public uint ActionId;
        [FieldOffset(28)] public ushort AnimationId;
        [FieldOffset(33)] public byte TargetCount;
    }
}
