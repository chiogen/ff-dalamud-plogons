namespace MitView
{

    public unsafe partial class MitView
    {
        internal class Status
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
    }
}
