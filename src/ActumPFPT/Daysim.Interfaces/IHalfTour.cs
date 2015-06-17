using System;
using System.Collections.Generic;
using System.Linq;
using Daysim.Framework.Core;

namespace Daysim.Interfaces
{
	public interface IHalfTour
	{
		List<ITripWrapper> Trips { get; }

		int SimulatedTrips { get; set; }

		int OneSimulatedTripFlag { get; }

		int TwoSimulatedTripsFlag { get; }

		int ThreeSimulatedTripsFlag { get; }

		int FourSimulatedTripsFlag { get; }

		int FiveSimulatedTripsFlag { get; }

		int FivePlusSimulatedTripsFlag { get; }

		void SetTrips(int direction);
		ITripWrapper CreateNextTrip(ITripWrapper trip, int intermediateStopPurpose, int destinationPurpose);
	}
}