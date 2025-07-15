namespace VoronoiLib.Structures
{
    internal class FortuneSiteEvent : FortuneEvent
    {
		public double X { get { return Site.X; } }
		public double Y { get { return Site.Y; } }
		internal FortuneSite Site { get; set; }

        internal FortuneSiteEvent(FortuneSite site)
        {
            Site = site;
        }

        public int CompareTo(FortuneEvent other)
        {
            var c = Y.CompareTo(other.Y);
            return c == 0 ? X.CompareTo(other.X) : c;
        }
     
    }
}