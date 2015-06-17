// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.DomainModels;
using Daysim.DomainModels.Actum;
using Daysim.Framework.Core;
using Daysim.Interfaces;
using Ninject;

namespace Daysim.Factories.Actum {
	public class ActumTripWrapperCreator : ITripWrapperCreator
	{
		#region ITripWrapperCreator Members

		public ITripWrapper CreateWrapper(ITrip trip, ITourWrapper tour, IHalfTour halfTour) {
			return new ActumTripWrapper(trip as ActumTrip, tour as ActumTourWrapper, halfTour);
		}


		public ITripWrapper CreateWrapperWithTrip(ITourWrapper t, ITour tour, ITour wrapperTour, int nextTripId, int direction, int sequence, bool isToTourOrigin, IHalfTour halfTour) {
			ITripWrapper tw = CreateWrapper(new ActumTrip {
					Id = nextTripId,
					TourId = wrapperTour.Id,
					HouseholdId = wrapperTour.HouseholdId,
					PersonSequence = wrapperTour.PersonSequence,
					Day = wrapperTour.Day,
					HalfTour = direction,
					Sequence = sequence + 1,
					OriginAddressType = tour.DestinationAddressType,
					OriginParcelId = tour.DestinationParcelId,
					OriginZoneKey = tour.DestinationZoneKey,
					OriginPurpose = tour.DestinationPurpose,
					DestinationAddressType = tour.OriginAddressType,
					DestinationParcelId = tour.OriginParcelId,
					DestinationZoneKey = tour.OriginZoneKey,
					DestinationPurpose = t.IsHomeBasedTour ? Constants.Purpose.NONE_OR_HOME : Constants.Purpose.WORK,
					DepartureTime = 180,
					ArrivalTime = 180,
					PathType = 1,
					ExpansionFactor = t.Household.ExpansionFactor
				}, t as ActumTourWrapper, halfTour);
			tw.IsToTourOrigin = isToTourOrigin;
			return tw;
		}

		public ITripWrapper CreateWrapperWithNextTrip(ITourWrapper t, ITour wrapperTour, ITripWrapper trip, int nextTripId, int intermediateStopPurpose, int destinationPurpose, IHalfTour halfTour)
		{
			ITripWrapper tw = Global.Kernel.Get<TripWrapperFactory>().TripWrapperCreator.CreateWrapper(new ActumTrip {
					Id = nextTripId,
					TourId = wrapperTour.Id,
					HouseholdId = wrapperTour.HouseholdId,
					PersonSequence = wrapperTour.PersonSequence,
					Day = wrapperTour.Day,
					HalfTour = trip.Direction,
					Sequence = trip.Sequence + 1,
					DestinationAddressType = trip.DestinationAddressType,
					DestinationParcelId = trip.DestinationParcelId,
					DestinationZoneKey = trip.DestinationZoneKey,
					OriginAddressType = trip.DestinationAddressType,
					OriginPurpose = intermediateStopPurpose,
					DestinationPurpose = destinationPurpose,
					DepartureTime = 180,
					ArrivalTime = 180,
					PathType = 1,
					ExpansionFactor = t.Household.ExpansionFactor
				}, t, halfTour);
				tw.IsToTourOrigin = trip.IsToTourOrigin;
				return tw;
		}

		#endregion
	}
}
