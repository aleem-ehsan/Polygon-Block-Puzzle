using System.Collections.Generic;

namespace VoronoiLib.Structures
{
    public class FortuneSite
    {
		public int Id { get; set; }
		public double X { get; set; }
		public double Y { get; set; }

        public List<VEdge> Cell { get; private set; }

        public List<FortuneSite> Neighbors { get; private set; }

        public FortuneSite(int id, double x, double y)
        {
			Id = id;
            X = x;
            Y = y;
            Cell = new List<VEdge>();
            Neighbors = new List<FortuneSite>();
        }

		public override string ToString()
		{
			return string.Format("[{0},{1}]", X, Y);
		}
	}
}
