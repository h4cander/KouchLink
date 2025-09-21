namespace KouchLink.Common.Data
{
    public class JoystickData
    {
        public Axes axes { get; set; }
        public Buttons buttons { get; set; }
    }

    public class Axes
    {
        public short lx { get; set; }
        public short ly { get; set; }
        public short rx { get; set; }
        public short ry { get; set; }
    }

    public class Buttons
    {
        public bool A { get; set; }
        public bool B { get; set; }
        public bool X { get; set; }
        public bool Y { get; set; }
    }
}
