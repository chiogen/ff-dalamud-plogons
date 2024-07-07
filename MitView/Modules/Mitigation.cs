using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MitView
{
    internal class MitigationAmount
    {
        public float PhysicalMitAmount { get; set; } = 0;
        public float MagicalMitAmount { get; set; } = 0;

        public void Reset() { MagicalMitAmount = PhysicalMitAmount = 0; }
    }

    internal class Mitigation
    {
        public uint Id { get; private set; }
        public MitigationAmount MitAmount { get; private set; } = new MitigationAmount();
        public uint Flags { get; private set; }

        public Mitigation(uint id, uint physicalMitAmount, uint magicalMitAmount, uint flags)
        {
            Id = id;
            MitAmount.PhysicalMitAmount = physicalMitAmount;
            MitAmount.MagicalMitAmount = magicalMitAmount;
            Flags = flags;
        }

        public Mitigation(uint id, uint mitAmount, uint flags)
        {
            Id = id;
            MitAmount.PhysicalMitAmount = mitAmount;
            MitAmount.MagicalMitAmount = mitAmount;
            Flags = flags;
        }

        public static void CalculateMitigation(IEnumerable<Mitigation> mits, ref MitigationAmount m) // m must not contain 0s
        {
            foreach (var mit in mits)
            {
                m = CalculateMitigation(m, mit.MitAmount);
            }
        }
        public static MitigationAmount CalculateMitigation(IEnumerable<Mitigation> mits)
        {
            MitigationAmount m = new MitigationAmount();
            m.PhysicalMitAmount = mits.First().MitAmount.PhysicalMitAmount;
            m.MagicalMitAmount = mits.First().MitAmount.MagicalMitAmount;
            foreach (var mit in mits.Skip(1))
            {
                m = CalculateMitigation(m, mit.MitAmount);
            }
            return m;
        }

        public static void CalculateMitigation(IEnumerable<MitigationAmount> mits, ref MitigationAmount m) // m must not contain 0s
        {
            foreach (var mit in mits)
            {
                m = CalculateMitigation(m, mit);
            }
        }

        public static MitigationAmount CalculateMitigation(IEnumerable<MitigationAmount> mits)
        {
            MitigationAmount m = new MitigationAmount();
            m = mits.First();
            foreach (var mit in mits.Skip(1))
            {
                m = CalculateMitigation(m, mit);
            }
            return m;
        }
        public static MitigationAmount CalculateMitigation(MitigationAmount l, MitigationAmount r)
        {
            MitigationAmount m = new MitigationAmount();

            m.PhysicalMitAmount = (1 - l.PhysicalMitAmount / 100) * (1 - r.PhysicalMitAmount / 100);
            m.MagicalMitAmount = (1 - l.MagicalMitAmount / 100) * (1 - r.MagicalMitAmount / 100);
            m.PhysicalMitAmount = MathF.Round((1 - m.PhysicalMitAmount) * 100);
            m.MagicalMitAmount = MathF.Round((1 - m.MagicalMitAmount) * 100);
            return m;
        }
    }
    internal class MitigationDictionary
    {
        private static readonly Dictionary<uint, Mitigation> MitigationIds = new()
        {
            {   74, new Mitigation(  74, 30, 0) }, // Sentinel
            {   89, new Mitigation(  89, 30, 0) }, // Vengeance
            {  194, new Mitigation( 194, 20, 0) }, // Shield Wall (tank LB1)
            {  195, new Mitigation( 195, 40, 0) }, // Stronghold (tank LB2)
            {  196, new Mitigation( 196, 80, 0) }, // Last Bastion (pld tank LB3)
            {  299, new Mitigation( 299, 10, 0) }, // Sacred Soil
            {  317, new Mitigation( 317, 0,  5, 0) }, // Fey Illumination
            {  746, new Mitigation( 746, 0, 20, 0) }, // Dark Mind
            {  747, new Mitigation( 747, 30, 0) }, // Shadow Wall
            {  849, new Mitigation( 849, 10, 0) }, // Collective Unconscious
            {  860, new Mitigation( 860, 10, 1) }, // Dismantle
            {  863, new Mitigation( 863, 80, 0) }, // Land Waker (war tank LB3)
            {  864, new Mitigation( 864, 80, 0) }, // Dark Force (drk tank LB3)
            { 1174, new Mitigation(1174, 10, 0) }, // Intervention
            { 1176, new Mitigation(1176, 15, 0) }, // Passage of Arms
            { 1179, new Mitigation(1179, 20, 0) }, // Riddle of Earth
            { 1191, new Mitigation(1191, 20, 0) }, // Rampart
            { 1193, new Mitigation(1193, 10, 1) }, // Reprisal
            { 1195, new Mitigation(1195, 10, 5, 1) }, // Feint
            { 1203, new Mitigation(1203, 5, 10, 1) }, // Addle
            { 1232, new Mitigation(1232, 10, 0) }, // Third Eye
            { 1826, new Mitigation(1826, 10, 0) }, // Shield Samba
            { 1832, new Mitigation(1832, 10, 0) }, // Camouflage
            { 1834, new Mitigation(1834, 30, 0) }, // Nebula
            { 1839, new Mitigation(1839, 0, 10, 0) }, // Heart of Light
            { 1858, new Mitigation(1858, 10, 0)}, // Nascent Flash
            { 1873, new Mitigation(1873, 10, 0) }, // Temperance
            { 1894, new Mitigation(1894, 0, 10, 0) }, // Dark Missionary
            { 1931, new Mitigation(1931, 80, 0) }, // Gunmetal Soul (gnb tank LB3)
            { 1934, new Mitigation(1934, 10, 0) }, // Troubadour
            { 1951, new Mitigation(1951, 10, 0) }, // Tactician
            { 2618, new Mitigation(2618, 10, 0) }, // Kerachole
            { 2619, new Mitigation(2619, 10, 0) }, // Taurochole
            { 2674, new Mitigation(2674, 15, 0) }, // Holy Sheltron
            { 2675, new Mitigation(2675, 10, 0) }, // Knight's Resolve
            { 2678, new Mitigation(2678, 10, 0) }, // BloodWhetting
            { 2679, new Mitigation(2679, 10, 0) }, // Stem the Flow ( first 4 seconds of bloodwhetting )
            { 2682, new Mitigation(2682, 10, 0 ) }, // Oblation
            { 2683, new Mitigation(2683, 15, 0) }, // Heart of Corundum
            { 2707, new Mitigation(2707, 0, 10, 0) }, // Magic Barrier
            { 2708, new Mitigation(2708, 15, 0) }, // Aquaveil
            { 2711, new Mitigation(2711, 10, 0) }, // Desperate Measures (Expedient)
            { 3003, new Mitigation(3003, 10, 0) }, // Holos
        };

        public static bool IsMitigation(uint id)
        {
            return MitigationIds.ContainsKey(id);
        }

        public static bool TryGetMitigation(uint id, out Mitigation mit)
        {
            return MitigationIds.TryGetValue(id, out mit);
        }
    }
}
