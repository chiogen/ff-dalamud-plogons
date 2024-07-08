namespace MitView.Modules
{
    internal class MitigationAmount
    {
        public float PhysicalMitAmount { get; set; } = 0;
        public float MagicalMitAmount { get; set; } = 0;

        public void Reset() { MagicalMitAmount = PhysicalMitAmount = 0; }
    }
}
