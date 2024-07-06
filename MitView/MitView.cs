using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Game.ClientState.Statuses;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using ImGuiNET;
using MitView.Windows;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonPartyList;
using Dalamud.Game.Network;
using Dalamud.Hooking;
using Character = FFXIVClientStructs.FFXIV.Client.Game.Character.Character;
using System.Runtime.InteropServices;
using Dalamud.Game.Gui.FlyText;
using FFXIVClientStructs.Interop;

namespace MitView
{
    public enum ActionEffectType : byte
    {
        Nothing = 0,
        Miss = 1,
        FullResist = 2,
        Damage = 3,
        Heal = 4,
        BlockedDamage = 5,
        ParriedDamage = 6,
        Invulnerable = 7,
        NoEffectText = 8,
        Unknown_0 = 9,
        MpLoss = 10,
        MpGain = 11,
        TpLoss = 12,
        TpGain = 13,
        GpGain = 14,
        ApplyStatusEffectTarget = 15,
        ApplyStatusEffectSource = 16,
        StatusNoEffect = 20,
        Unknown0 = 27,
        Unknown1 = 28,
        Knockback = 33,
        Mount = 40,
        VFX = 59,
        JobGauge = 61,
    };

    public enum PositionalState
    {
        Ignore,
        Success,
        Failure
    }

    public enum AttackType
    {
        Unknown = 0,
        Slashing = 1,
        Piercing = 2,
        Blunt = 3,
        Shot = 4,
        Magical = 5,
        Unique = 6,
        Physical = 7,
        LimitBreak = 8,
    }

    public enum SeDamageType
    {
        None = 0,
        Physical = 60011,
        Magical = 60012,
        Unique = 60013,
    }

    public enum DamageType
    {
        None = 0,
        Physical = 1,
        Magical = 2,
        Unique = 3,
    }

    public enum ActionStep
    {
        None,
        Effect,
        Screenlog,
        Flytext,
    }

    public enum LogType
    {
        FlyText,
        Sound,
        Castbar,
        ScreenLog,
        Effect,
        FlyTextWrite
    }

    public static class DamageTypeExtensions
    {
        public static DamageType ToDamageType(this AttackType type)
        {
            return type switch
            {
                AttackType.Unknown => DamageType.Unique,
                AttackType.Slashing => DamageType.Physical,
                AttackType.Piercing => DamageType.Physical,
                AttackType.Blunt => DamageType.Physical,
                AttackType.Shot => DamageType.Physical,
                AttackType.Magical => DamageType.Magical,
                AttackType.Unique => DamageType.Unique,
                AttackType.Physical => DamageType.Unique,
                AttackType.LimitBreak => DamageType.Unique,
                _ => DamageType.Unique,
            };
        }

        public static DamageType ToDamageType(this SeDamageType type)
        {
            return type switch
            {

                SeDamageType.None => DamageType.None,
                SeDamageType.Physical => DamageType.Physical,
                SeDamageType.Magical => DamageType.Magical,
                SeDamageType.Unique => DamageType.Unique,
                _ => DamageType.None,
            };
        }

        public static SeDamageType ToSeDamageType(this DamageType type)
        {
            return type switch
            {

                DamageType.None => SeDamageType.None,
                DamageType.Physical => SeDamageType.Physical,
                DamageType.Magical => SeDamageType.Magical,
                DamageType.Unique => SeDamageType.Unique,
                _ => SeDamageType.None,
            };
        }

        public static SeDamageType ToSeDamageType(this AttackType type)
        {
            return type.ToDamageType().ToSeDamageType();
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct EffectHeader
    {
        [FieldOffset(8)] public uint ActionId;
        [FieldOffset(28)] public ushort AnimationId;
        [FieldOffset(33)] public byte TargetCount;
    }

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

    public struct EffectEntry
    {
        public ActionEffectType type;
        public byte param0;
        public byte param1;
        public byte param2;
        public byte mult;
        public byte flags;
        public ushort value;

        public byte AttackType => (byte)(param1 & 0xF);

        public override string ToString()
        {
            return
                $"Type: {type}, p0: {param0:D3}, p1: {param1:D3}, p2: {param2:D3} 0x{param2:X2} '{Convert.ToString(param2, 2).PadLeft(8, '0')}', mult: {mult:D3}, flags: {flags:D3} | {Convert.ToString(flags, 2).PadLeft(8, '0')}, value: {value:D6} ATTACK TYPE: {AttackType} DAMAGE TYPE: {((AttackType)AttackType).ToDamageType()}";
        }
    }
    
    public unsafe class MitView : IDalamudPlugin
    {
        public string Name => "MitView";
        private MainWindow MainWindow { get; init; }
        private ConfigWindow ConfigWindow { get; init; }
        public readonly WindowSystem WindowSystem = new("mitview");
        public Configuration Configuration { get; init; }

        private Stopwatch UpdateStopWatch = new Stopwatch();
        
        // Eww, fix all these member variables
        private List<Dalamud.Game.ClientState.Party.IPartyMember?> PartyMembers = new List<Dalamud.Game.ClientState.Party.IPartyMember?>();
        private List<Mitigation> LocalPlayerMitigation = new List<Mitigation>();
        private MitigationAmount LocalPlayerActiveMitigation = new MitigationAmount();
        private int LocalPlayerShields = -1;

        private List<Mitigation> TargetMitigation = new List<Mitigation>();
        private MitigationAmount TargetActiveMitigation = new MitigationAmount();
        private int TargetPlayerShields = -1;

        // What is this tuple even ( TODO: Future self, use a struct ) :(
        private List<Tuple<MitigationAmount, float, float, int, uint>> PartyListActiveMitigationList = new List<Tuple<MitigationAmount,float,float,int,uint>>();

        private uint PlayerID = 0;

        private Dictionary<UInt64, List<EffectEntry>> EnemyList = new Dictionary<UInt64, List<EffectEntry>>();

        private delegate void ReceiveActionEffectDelegate(uint sourceId, Character* sourceCharacter, IntPtr pos, EffectHeader* effectHeader, EffectEntry* effectArray, ulong* effectTail);
        private readonly Hook<ReceiveActionEffectDelegate> ReceiveActionEffectHook;

        public MitView(IDalamudPluginInterface pluginInterface) 
        {
            pluginInterface.Create<Service>();

            Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(Service.PluginInterface);
            MainWindow = new MainWindow();
            ConfigWindow = new ConfigWindow(this);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);

            PlayerID = (uint)(Service.ClientState?.LocalPlayer?.GameObjectId ?? 0); 

            try
            {
                ReceiveActionEffectHook = Service.GameInteropProvider.HookFromSignature<ReceiveActionEffectDelegate>("40 55 53 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70", ReceiveActionEffect);
                debugStrings.Add(ReceiveActionEffectHook.ToString());
            }
            catch (Exception ex) 
            {
                debugStrings.Add(ex.ToString());
                ReceiveActionEffectHook?.Disable();
            }

            ReceiveActionEffectHook?.Enable();


            Service.PluginInterface.UiBuilder.Draw += Draw;
            Service.PluginInterface.UiBuilder.OpenConfigUi += delegate { ConfigWindow.Toggle(); };
            Service.PluginInterface.UiBuilder.OpenMainUi += delegate { MainWindow.Toggle(); };
            Service.Framework.Update += OnFrameworkUpdate;
            Service.GameNetwork.NetworkMessage += OnNetworkMessage;
            UpdateStopWatch.Start();
        }

        private void ReceiveActionEffect(uint sourceId, Character* sourceCharacter, IntPtr pos, EffectHeader* effectHeader, EffectEntry* effectArray, ulong* effectTail) 
        {
            var entryCount = effectHeader->TargetCount switch
            {
                0 => 0,
                1 => 8,
                <= 8 => 64,
                <= 16 => 128,
                <= 24 => 192,
                <= 32 => 256,
                _ => 0
            };

            for (int i = 0; i < entryCount; i++)
            {
              //  if (effectArray[i].type != ActionEffectType.Damage) continue;
                if (effectArray[i].type == ActionEffectType.Nothing ) continue;

                var target = effectTail[i / 8];
                uint dmg = effectArray[i].value;
                if (effectArray[i].mult != 0)
                    dmg += ((uint)ushort.MaxValue + 1) * effectArray[i].mult;

                var dmgType = ((AttackType)effectArray[i].AttackType).ToDamageType();

                if (PlayerID == target)
                {
                    if (EnemyList.TryGetValue(sourceId, out List<EffectEntry> list))
                    {
                        list.Add(effectArray[i]);
                    }
                    else
                    {
                        EnemyList[sourceId] = new List<EffectEntry> { effectArray[i] };
                    }
                   // debugStrings.Add("Incoming damage: " + target + " " + dmg.ToString() + " " + effectArray[i].type + " " + effectArray[i].mult + " " + effectArray[i].param0 + " " + effectArray[i].param1 + " " + effectArray[i].param2 + " " + effectArray[i].flags + " " + dmgType);
                }
                else 
                {

                   // debugStrings.Add("Outgoing damage: " + target + " " + dmg.ToString() + " " + effectArray[i].type + " " + effectArray[i].mult + " " + effectArray[i].param0 + " " + effectArray[i].param1 + " " + effectArray[i].param2 + " " + effectArray[i].flags + " " + dmgType);
                }
                
            }

            ReceiveActionEffectHook.Original( sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTail );
        }

        public void Dispose() 
        {
            Service.PluginInterface.UiBuilder.Draw -= Draw;
            Service.GameNetwork.NetworkMessage -= OnNetworkMessage;
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            ReceiveActionEffectHook?.Disable();
            ReceiveActionEffectHook?.Dispose();
        }

        private void OnFrameworkUpdate(IFramework framework) 
        {
            if (this.UpdateStopWatch.Elapsed >= TimeSpan.FromMilliseconds(100))
            {
                Update();
                UpdateStopWatch.Restart();
            }
        }

        internal class Status // Used for Debug, should probably move it elsewhere ^^
        {
            public Lumina.Text.SeString Name { get; private set; }
            public uint StatusID { get; private set; }
            public float TimeRemaining { get; private set; }

            public Status(Lumina.Text.SeString name,
                uint statusID,
                float timeRemaining)
            {
                Name = name;
                StatusID = statusID;
                TimeRemaining = timeRemaining;
            }
        }

        private List<Status> debugBuffs = new List<Status>();
        private List<string> debugStrings = new List<string>();
        private List<string> NetworkStrings = new List<string>();

        private void UpdatePartyList() 
        {
            PartyMembers.Clear();
            if (Service.PartyList != null)
            {
                for (int i = 0; i < Service.PartyList.Length; ++i)
                {
                    var player = Service.PartyList[i];
                    if (player != null)
                    {
                        PartyMembers.Add(player);
                    }
                }
            }
        }

        private unsafe void GenerateMitigationList(StatusList statusList, ref List<Mitigation> mitigationList) 
        {
            mitigationList.Clear();
            var statusSlots = statusList.Length;
            for (int i = 0; i < statusSlots; ++i)
            {
                if (statusList[i].GameData.Name.ToString().Length != 0)
                {
                    Status status = new Status(statusList[i].GameData.Name, statusList[i].StatusId, statusList[i].RemainingTime);
                    debugBuffs.Add(status);

                    if (MitigationDictionary.TryGetMitigation(statusList[i].StatusId, out Mitigation? mit))
                    {
                        mitigationList.Add(mit);
                    }
                }
            }
        }

        private unsafe List<Mitigation> GenerateMitigationList(StatusList statusList)
        {
            var list = new List<Mitigation>();
            GenerateMitigationList(statusList, ref list);
            return list;
        }
        private void UpdateTargetMits()
        {
            TargetActiveMitigation.Reset();

            if (Service.TargetManager.Target is Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
            {
                GenerateMitigationList(target.StatusList, ref TargetMitigation);
                if (TargetMitigation.Count() > 0)
                {
                    TargetActiveMitigation = Mitigation.CalculateMitigation(TargetMitigation);
                }
                // Can't calculate shield when not in party because there is no yellow line to guess the shields from.
            }
            else 
            { 
                TargetMitigation.Clear();
                TargetPlayerShields = -1;
            }
        }

        private void UpdateLocalPlayerMits() 
        {
            LocalPlayerActiveMitigation.Reset();
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer != null)
            {
                GenerateMitigationList(localPlayer.StatusList, ref LocalPlayerMitigation);

                if (LocalPlayerMitigation.Count() > 0)
                {
                    LocalPlayerActiveMitigation = Mitigation.CalculateMitigation(LocalPlayerMitigation);
                }

                List<Mitigation> mitsAppliedToTarget = TargetMitigation.Where(a => (a.Flags & 0x01) == 1).ToList();
                if (mitsAppliedToTarget.Count > 0)
                {
                    Mitigation.CalculateMitigation(mitsAppliedToTarget, ref LocalPlayerActiveMitigation);
                }

                // Can't calculate shield when not in party because there is no yellow line to guess the shields from.
            }
            else 
            {
                LocalPlayerMitigation.Clear();
                LocalPlayerShields = -1;
            }
        }

        private unsafe void RearrangePartyMemberListToIngameList() 
        {
            var partyList = (AddonPartyList*)Service.GameGui.GetAddonByName("_PartyList", 1);
            if (partyList != null)
            {
                var orderedPartyMemberList = new List<Dalamud.Game.ClientState.Party.IPartyMember?>();
                for(int i = 0; i < PartyMembers.Count; ++i)
                {
                    var nameInpartyList = partyList->PartyMembers[i].Name->NodeText.ToString();
                    bool found = false;
                    foreach (var member in PartyMembers) 
                    {
                        var name = member.Name.ToString();
                        int maxIterations = 10;
                        bool isFirstIteration = true;
                        while( --maxIterations >= 0 )
                        {
                          //  debugStrings.Add(name);
                            if (nameInpartyList.Contains(name)) 
                            {
                                orderedPartyMemberList.Add(member);
                                found = true;
                                break;
                            }                      

                            if (name.Count() >= 15)
                            {
                                if (isFirstIteration) // Replace the last 3 characters of the name with dots
                                {
                                    var index = name.Count() - 3;
                                    name = name.Remove(index, 3).Insert(index, "...");
                                    isFirstIteration = false;
                                }
                                else // Remove 1 character to the left of the 3 dots
                                {
                                    name = name.Remove(name.Count() - 4, 1);
                                }
                                
                            }
                            else 
                            {
                                break; // Didn't find name
                            }
                        }

                        if (found) break;
                    }
                    if (!found)
                    {
                        orderedPartyMemberList.Add(null);
                    }
                }
                PartyMembers = orderedPartyMemberList;
            }
        }

        private unsafe void UpdatePartyListMits()
        {
            PartyListActiveMitigationList.Clear();
            if (PartyMembers.Count() == 0) return;

            RearrangePartyMemberListToIngameList();
            var partyList = (AddonPartyList*)Service.GameGui.GetAddonByName("_PartyList", 1);
            if (partyList != null)
            {
                foreach (var (member, i) in PartyMembers.Select((member, i) => (member, i)))
                {
                    float xPosition = partyList->PartyMembers[i].NameAndBarsContainer->ScreenX;
                    float yPosition = partyList->PartyMembers[i].NameAndBarsContainer->ScreenY;
                    var memberMits = new List<Mitigation>();
                    if (member != null)
                    {
                        GenerateMitigationList(member.Statuses, ref memberMits);
                        MitigationAmount memberMitAmount = memberMits.Count() > 0 ? Mitigation.CalculateMitigation(memberMits) : new MitigationAmount();
                        if (TargetMitigation.Count() > 0)
                        {
                            List<Mitigation> mitsAppliedToTarget = TargetMitigation.Where(a => (a.Flags & 0x01) == 1).ToList();
                            if (mitsAppliedToTarget.Count() > 0)
                            {
                                Mitigation.CalculateMitigation(mitsAppliedToTarget, ref memberMitAmount);
                            }
                        }
                        var memberStruct = (FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember*)member.Address;
                        int shield = (int)MathF.Floor(memberStruct->MaxHP * (memberStruct->DamageShield / 100.0f));
                        uint objectId = memberStruct->EntityId;
                        //   debugStrings.Add(memberStruct->DamageShield.ToString() + " " + memberStruct->Unk_Struct_208__0 + " " + memberStruct->Unk_Struct_208__4 + " " + memberStruct->Unk_Struct_208__8 + " " + memberStruct->Unk_Struct_208__C + " " + memberStruct->Unk_Struct_208__10 + " " + memberStruct->Unk_Struct_208__14 + " " + memberStruct->Flags  );

                        if (Configuration.ShowEffectiveHP) 
                        {
                            shield += (int)memberStruct->CurrentHP;
                        }

                        PartyListActiveMitigationList.Add(new(memberMitAmount, xPosition, yPosition, shield, objectId));
                    }
                    else
                    {
                        PartyListActiveMitigationList.Add(new(new MitigationAmount(), xPosition, yPosition, 0, 0));
                    }
                }
            }
        }

        private void Update() 
        {
            debugBuffs.Clear();
            while (debugStrings.Count() >= 15) 
            {
                debugStrings.RemoveAt(0);
            }
            UpdatePartyList();
            UpdateTargetMits();
            UpdateLocalPlayerMits();
            UpdatePartyListMits();
        }

        private void OnNetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            NetworkStrings.Add(opCode.ToString() + " " + sourceActorId.ToString() + " " + targetActorId.ToString() + " " + direction.ToString() + " " + DateTime.Now + "." + DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond % 1000);
            
            if (NetworkStrings.Count == 15) 
            {
                NetworkStrings.RemoveAt(0);
            }
        }

        private void DrawMitigationWindow(int id, float x, float y, MitigationAmount mit, int shield = -1, bool separateLine = false) 
        {
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(2.5f, 0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(x, y));
            ImGui.Begin("#localmit" + id, ImGuiWindowFlags.NoInputs | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoMouseInputs | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoFocusOnAppearing);
            ImGui.SetWindowFontScale(Math.Clamp(Configuration.FontSize, 0.5f, 2f));

            if (separateLine)
            {
                ImGui.TextUnformatted($"P{mit.PhysicalMitAmount}%");
                ImGui.TextUnformatted($"M{mit.MagicalMitAmount}%");
                if (shield != -1)
                {
                    if (Configuration.ShowEffectiveHP)
                    {
                        ImGui.TextUnformatted($"HP{shield}");
                    }
                    else
                    {
                        ImGui.TextUnformatted($"S {shield}");
                    }
                }
            }
            else
            {
                if (Configuration.ShowEffectiveHP)
                {
                    if (shield != -1)
                    {
                        ImGui.TextUnformatted($"P{mit.PhysicalMitAmount}%/M{mit.MagicalMitAmount}%/HP{shield}");
                    }
                    else 
                    {
                        ImGui.TextUnformatted($"P{mit.PhysicalMitAmount}%/M{mit.MagicalMitAmount}%");
                    }
                }
                else
                {
                    if (shield != -1)
                    {
                        ImGui.TextUnformatted($"P{mit.PhysicalMitAmount}%/M{mit.MagicalMitAmount}%/S{shield}");
                    }
                    else
                    {
                        ImGui.TextUnformatted($"P{mit.PhysicalMitAmount}%/M{mit.MagicalMitAmount}%");
                    }
                }
            }

            ImGui.End();
            ImGui.PopStyleVar(2);
        }

        private void Draw() 
        {
            WindowSystem.Draw();
            DrawMitigationWindow(0, 5f, 50f, LocalPlayerActiveMitigation, LocalPlayerShields);
            DrawMitigationWindow(1, 5f, 75f, TargetActiveMitigation, TargetPlayerShields);
            for (int i = 0; i < PartyListActiveMitigationList.Count; ++i) 
            {
                var mitAmount = PartyListActiveMitigationList[i].Item1;
                var xPosition = PartyListActiveMitigationList[i].Item2;
                var yPosition = PartyListActiveMitigationList[i].Item3;
                var shield = PartyListActiveMitigationList[i].Item4;
                var objectId = PartyListActiveMitigationList[i].Item5;
                if (EnemyList.TryGetValue(objectId, out List<EffectEntry> list)) 
                {
                    var last = list.Last();
                    uint dmg = last.value;
                    if (last.mult != 0)
                        dmg += ((uint)ushort.MaxValue + 1) * last.mult;
                }
                DrawMitigationWindow(i + 2, Math.Max( 0f, xPosition + Configuration.XOffset ), Math.Max( 0f, yPosition + Configuration.YOffset ), mitAmount, shield, true);
            }

            if (Configuration.ShowDebug)
            {
                ImGui.Text("Buffs:");
                foreach (var buff in debugBuffs)
                {
                    ImGui.Text($"{buff.Name} {buff.StatusID} {buff.TimeRemaining}");
                }

                ImGui.Text("String:");
                foreach (var s in debugStrings)
                {
                    ImGui.Text(s);
                }

                ImGui.Text( "EnemyList:" );
                foreach (var (key, value) in EnemyList) 
                {
                    ImGui.Text($"Enemy ID: {key}");
                    foreach (var i in value) 
                    {
                        ImGui.Text($"{i}");
                    }
                }

                /*
                ImGui.Text("Network: ");
                foreach (var s in NetworkStrings)
                {
                    ImGui.Text(s);
                }
                */
            }
        }
    }
}
