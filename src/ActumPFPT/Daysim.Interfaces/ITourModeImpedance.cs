using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface ITourModeImpedance 
	{
		int AdjacentMinutesBefore { get; set; }

		int MaxMinutesBefore { get; set; }

		int TotalMinutesBefore { get; set; }

		int AdjacentMinutesAfter { get; set; }

		int MaxMinutesAfter { get; set; }

		int TotalMinutesAfter { get; set; }

		double GeneralizedTimeFromOrigin { get; set; }

		double GeneralizedTimeFromDestination { get; set; }
	}
}
