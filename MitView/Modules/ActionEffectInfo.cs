using System;
using Dalamud.Game.Gui.FlyText;
using MitView.Enums;

namespace MitView.Modules
{
    public struct ActionEffectInfo
    {
        public ActionStep step;
        public ulong tick;

        public uint actionId;
        public ActionEffectType type;
        public DamageType damageType;
        public FlyTextKind kind;
        public uint sourceId;
        public ulong targetId;
        public uint value;
        public PositionalState positionalState;

        public bool Equals(ActionEffectInfo other) => step == other.step && tick == other.tick && actionId == other.actionId && type == other.type && damageType == other.damageType && kind == other.kind && sourceId == other.sourceId && targetId == other.targetId && value == other.value && positionalState == other.positionalState;
        public override bool Equals(object obj) => obj is ActionEffectInfo other && Equals(other);
        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add((int)step);
            hashCode.Add(tick);
            hashCode.Add(actionId);
            hashCode.Add((int)type);
            hashCode.Add(damageType);
            hashCode.Add((int)kind);
            hashCode.Add(sourceId);
            hashCode.Add(targetId);
            hashCode.Add(value);
            hashCode.Add((int)positionalState);
            return hashCode.ToHashCode();
        }

        public override string ToString() => $"{nameof(step)}: {step}, {nameof(tick)}: {tick}, {nameof(actionId)}: {actionId}, {nameof(type)}: {type}, {nameof(damageType)}: {damageType}, {nameof(kind)}: {kind}, {nameof(sourceId)}: {sourceId}, {nameof(targetId)}: {targetId}, {nameof(value)}: {value}, {nameof(positionalState)}: {positionalState}";
    }
}
