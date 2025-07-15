namespace VoronoiLib.Structures
{
	internal class FortuneCircleEvent : FortuneEvent
    {
		internal VPoint Lowest { get; set; }
		internal double YCenter { get; set; }
		internal RBTreeNode<BeachSection> ToDelete { get; set; }

        internal FortuneCircleEvent(VPoint lowest, double yCenter, RBTreeNode<BeachSection> toDelete)
        {
            Lowest = lowest;
            YCenter = yCenter;
            ToDelete = toDelete;
        }

        public int CompareTo(FortuneEvent other)
        {
            var c = Y.CompareTo(other.Y);
            return c == 0 ? X.CompareTo(other.X) : c;
        }

		public double X { get { return Lowest.X; } }
		public double Y { get { return Lowest.Y; } }
    }
}
