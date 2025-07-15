using System;

namespace VoronoiLib.Structures
{
	internal interface FortuneEvent : IComparable<FortuneEvent>
    {
        double X { get; }
        double Y { get; }
    }
}
