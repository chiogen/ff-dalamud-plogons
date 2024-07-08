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
using FFXIVClientStructs.Interop;
using MitView.Enums;
using MitView.Modules;

namespace MitView
{

    public unsafe partial class MitView : IDalamudPlugin
    {
        // I think this is required by Dalamud, even tho it is unused.
        public string Name => "MitView";

        private MainWindow MainWindow { get; init; }
        private ConfigWindow ConfigWindow { get; init; }
        public readonly WindowSystem WindowSystem = new("mitview");
        public Configuration Configuration { get; init; }

        private readonly Stopwatch updateStopWatch = new();

        // Eww, fix all these member variables
        private List<Dalamud.Game.ClientState.Party.IPartyMember?> partyMembers = [];
        private List<Mitigation> localPlayerMitigation = [];
        private MitigationAmount localPlayerActiveMitigation = new();
        private int localPlayerShields = -1;

        private List<Mitigation> targetMitigation = [];
        private MitigationAmount targetActiveMitigation = new();
        private int targetPlayerShields = -1;

        // What is this tuple even ( TODO: Future self, use a struct ) :(
        private readonly List<Tuple<MitigationAmount, float, float, int, uint>> partyListActiveMitigationList = [];

        private readonly uint playerID = 0;

        private readonly Dictionary<ulong, List<EffectEntry>> enemyList = [];

        private delegate void ReceiveActionEffectDelegate(uint sourceId, Character* sourceCharacter, IntPtr pos, EffectHeader* effectHeader, EffectEntry* effectArray, ulong* effectTail);
        private readonly Hook<ReceiveActionEffectDelegate> receiveActionEffectHook;

        public MitView(IDalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(Service.PluginInterface);
            MainWindow = new MainWindow();
            ConfigWindow = new ConfigWindow(this);

            WindowSystem.AddWindow(MainWindow);
            WindowSystem.AddWindow(ConfigWindow);

            playerID = (uint)(Service.ClientState?.LocalPlayer?.GameObjectId ?? 0);

            try
            {
                receiveActionEffectHook = Service.GameInteropProvider.HookFromSignature<ReceiveActionEffectDelegate>("40 55 53 57 41 54 41 55 41 56 41 57 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 45 70", ReceiveActionEffect);
                debugStrings.Add(receiveActionEffectHook.ToString());
            }
            catch (Exception ex)
            {
                debugStrings.Add(ex.ToString());
                receiveActionEffectHook?.Disable();
            }

            receiveActionEffectHook?.Enable();


            Service.PluginInterface.UiBuilder.Draw += Draw;
            Service.PluginInterface.UiBuilder.OpenConfigUi += delegate { ConfigWindow.Toggle(); };
            Service.PluginInterface.UiBuilder.OpenMainUi += delegate { MainWindow.Toggle(); };
            Service.Framework.Update += OnFrameworkUpdate;
            Service.GameNetwork.NetworkMessage += OnNetworkMessage;
            updateStopWatch.Start();
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
                if (effectArray[i].type == ActionEffectType.Nothing) continue;

                var target = effectTail[i / 8];
                uint dmg = effectArray[i].value;
                if (effectArray[i].mult != 0)
                    dmg += ((uint)ushort.MaxValue + 1) * effectArray[i].mult;

                var dmgType = ((AttackType)effectArray[i].AttackType).ToDamageType();

                if (playerID == target)
                {
                    if (enemyList.TryGetValue(sourceId, out List<EffectEntry> list))
                    {
                        list.Add(effectArray[i]);
                    }
                    else
                    {
                        enemyList[sourceId] = new List<EffectEntry> { effectArray[i] };
                    }
                    // debugStrings.Add("Incoming damage: " + target + " " + dmg.ToString() + " " + effectArray[i].type + " " + effectArray[i].mult + " " + effectArray[i].param0 + " " + effectArray[i].param1 + " " + effectArray[i].param2 + " " + effectArray[i].flags + " " + dmgType);
                }
                else
                {

                    // debugStrings.Add("Outgoing damage: " + target + " " + dmg.ToString() + " " + effectArray[i].type + " " + effectArray[i].mult + " " + effectArray[i].param0 + " " + effectArray[i].param1 + " " + effectArray[i].param2 + " " + effectArray[i].flags + " " + dmgType);
                }

            }

            receiveActionEffectHook.Original(sourceId, sourceCharacter, pos, effectHeader, effectArray, effectTail);
        }

        public void Dispose()
        {
            Service.PluginInterface.UiBuilder.Draw -= Draw;
            Service.GameNetwork.NetworkMessage -= OnNetworkMessage;
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            receiveActionEffectHook?.Disable();
            receiveActionEffectHook?.Dispose();
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            if (this.updateStopWatch.Elapsed >= TimeSpan.FromMilliseconds(100))
            {
                Update();
                updateStopWatch.Restart();
            }
        }

        private readonly List<Status> debugBuffs = [];
        private readonly List<string> debugStrings = [];
        private readonly List<string> networkStrings = [];

        private unsafe List<Mitigation> GenerateMitigationList(StatusList statusList)
        {
            var list = new List<Mitigation>();
            GenerateMitigationList(statusList, ref list);
            return list;
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

        private void Update()
        {
            debugBuffs.Clear();

            while (debugStrings.Count >= 15)
                debugStrings.RemoveAt(0);

            UpdatePartyList();
            UpdateTargetMits();
            UpdateLocalPlayerMits();
            UpdatePartyListMits();
        }
        private void UpdatePartyList()
        {
            partyMembers.Clear();
            if (Service.PartyList != null)
            {
                for (int i = 0; i < Service.PartyList.Length; ++i)
                {
                    var player = Service.PartyList[i];
                    if (player != null)
                    {
                        partyMembers.Add(player);
                    }
                }
            }
        }
        private void UpdateTargetMits()
        {
            targetActiveMitigation.Reset();

            if (Service.TargetManager.Target is Dalamud.Game.ClientState.Objects.Types.IBattleChara target)
            {
                GenerateMitigationList(target.StatusList, ref targetMitigation);
                if (targetMitigation.Count() > 0)
                {
                    targetActiveMitigation = Mitigation.CalculateMitigation(targetMitigation);
                }
                // Can't calculate shield when not in party because there is no yellow line to guess the shields from.
            }
            else
            {
                targetMitigation.Clear();
                targetPlayerShields = -1;
            }
        }
        private unsafe void UpdatePartyListMits()
        {
            partyListActiveMitigationList.Clear();
            if (partyMembers.Count() == 0) return;

            RearrangePartyMemberListToIngameList();
            var partyList = (AddonPartyList*)Service.GameGui.GetAddonByName("_PartyList", 1);
            if (partyList != null)
            {
                foreach (var (member, i) in partyMembers.Select((member, i) => (member, i)))
                {
                    float xPosition = partyList->PartyMembers[i].NameAndBarsContainer->ScreenX;
                    float yPosition = partyList->PartyMembers[i].NameAndBarsContainer->ScreenY;
                    var memberMits = new List<Mitigation>();
                    if (member != null)
                    {
                        GenerateMitigationList(member.Statuses, ref memberMits);
                        MitigationAmount memberMitAmount = memberMits.Count() > 0 ? Mitigation.CalculateMitigation(memberMits) : new MitigationAmount();
                        if (targetMitigation.Count() > 0)
                        {
                            List<Mitigation> mitsAppliedToTarget = targetMitigation.Where(a => (a.Flags & 0x01) == 1).ToList();
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

                        partyListActiveMitigationList.Add(new(memberMitAmount, xPosition, yPosition, shield, objectId));
                    }
                    else
                    {
                        partyListActiveMitigationList.Add(new(new MitigationAmount(), xPosition, yPosition, 0, 0));
                    }
                }
            }
        }
        private void UpdateLocalPlayerMits()
        {
            localPlayerActiveMitigation.Reset();
            var localPlayer = Service.ClientState.LocalPlayer;
            if (localPlayer != null)
            {
                GenerateMitigationList(localPlayer.StatusList, ref localPlayerMitigation);

                if (localPlayerMitigation.Count() > 0)
                {
                    localPlayerActiveMitigation = Mitigation.CalculateMitigation(localPlayerMitigation);
                }

                List<Mitigation> mitsAppliedToTarget = targetMitigation.Where(a => (a.Flags & 0x01) == 1).ToList();
                if (mitsAppliedToTarget.Count > 0)
                {
                    Mitigation.CalculateMitigation(mitsAppliedToTarget, ref localPlayerActiveMitigation);
                }

                // Can't calculate shield when not in party because there is no yellow line to guess the shields from.
            }
            else
            {
                localPlayerMitigation.Clear();
                localPlayerShields = -1;
            }
        }
        private unsafe void RearrangePartyMemberListToIngameList()
        {
            var partyList = (AddonPartyList*)Service.GameGui.GetAddonByName("_PartyList", 1);
            if (partyList != null)
            {
                var orderedPartyMemberList = new List<Dalamud.Game.ClientState.Party.IPartyMember?>();
                for (int i = 0; i < partyMembers.Count; ++i)
                {
                    var nameInpartyList = partyList->PartyMembers[i].Name->NodeText.ToString();
                    bool found = false;
                    foreach (var member in partyMembers)
                    {
                        var name = member.Name.ToString();
                        int maxIterations = 10;
                        bool isFirstIteration = true;
                        while (--maxIterations >= 0)
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
                partyMembers = orderedPartyMemberList;
            }
        }


        private void OnNetworkMessage(nint dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            networkStrings.Add(opCode.ToString() + " " + sourceActorId.ToString() + " " + targetActorId.ToString() + " " + direction.ToString() + " " + DateTime.Now + "." + (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond % 1000));

            if (networkStrings.Count == 15)
                networkStrings.RemoveAt(0);
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
                        ImGui.TextUnformatted($"HP{shield}");
                    else
                        ImGui.TextUnformatted($"S {shield}");
                }
            }
            else
            {
                if (Configuration.ShowEffectiveHP)
                {
                    if (shield != -1)
                        ImGui.TextUnformatted($"P{mit.PhysicalMitAmount}%/M{mit.MagicalMitAmount}%/HP{shield}");
                    else
                        ImGui.TextUnformatted($"P{mit.PhysicalMitAmount}%/M{mit.MagicalMitAmount}%");
                }
                else
                {
                    if (shield != -1)
                        ImGui.TextUnformatted($"P{mit.PhysicalMitAmount}%/M{mit.MagicalMitAmount}%/S{shield}");
                    else
                        ImGui.TextUnformatted($"P{mit.PhysicalMitAmount}%/M{mit.MagicalMitAmount}%");
                }
            }

            ImGui.End();
            ImGui.PopStyleVar(2);
        }

        private void Draw()
        {
            WindowSystem.Draw();
            DrawMitigationWindow(0, 5f, 50f, localPlayerActiveMitigation, localPlayerShields);
            DrawMitigationWindow(1, 5f, 75f, targetActiveMitigation, targetPlayerShields);
            for (int i = 0; i < partyListActiveMitigationList.Count; ++i)
            {
                var mitAmount = partyListActiveMitigationList[i].Item1;
                var xPosition = partyListActiveMitigationList[i].Item2;
                var yPosition = partyListActiveMitigationList[i].Item3;
                var shield = partyListActiveMitigationList[i].Item4;
                var objectId = partyListActiveMitigationList[i].Item5;
                if (enemyList.TryGetValue(objectId, out List<EffectEntry> list))
                {
                    var last = list.Last();
                    uint dmg = last.value;
                    if (last.mult != 0)
                        dmg += ((uint)ushort.MaxValue + 1) * last.mult;
                }
                DrawMitigationWindow(i + 2, Math.Max(0f, xPosition + Configuration.XOffset), Math.Max(0f, yPosition + Configuration.YOffset), mitAmount, shield, true);
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

                ImGui.Text("EnemyList:");
                foreach (var (key, value) in enemyList)
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
