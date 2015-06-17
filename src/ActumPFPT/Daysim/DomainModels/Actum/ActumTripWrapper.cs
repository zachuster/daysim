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
using Daysim.Framework.Core;
using Daysim.Interfaces;

namespace Daysim.DomainModels.Actum {
	public class ActumTripWrapper : TripWrapper {
		public ActumTripWrapper(ActumTrip trip, TourWrapper tour, IHalfTour halfTour)
			: base(trip, tour, halfTour) {
		}

		public int BikePTCombination {
			get { return ((ActumTrip) _trip).BikePTCombination; }
		}

		public int EscortedDestinationPurpose {
			get { return ((ActumTrip) _trip).EscortedDestinationPurpose; }
		}

		// flags, choice model properties, etc.

		public bool IsBusinessDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.BUSINESS; }
		}

		public bool IsBusinessOriginPurpose {
			get { return OriginPurpose == Constants.Purpose.BUSINESS; }
		}

		// wrapper methods

		public override void SetDriverOrPassenger(List<ITripWrapper> trips) {
			switch (Mode) {
				case Constants.Mode.WALK:
				case Constants.Mode.BIKE:
				case Constants.Mode.TRANSIT:
				case Constants.Mode.SCHOOL_BUS:
				case Constants.Mode.OTHER:
					_trip.DriverType = Constants.DriverType.NOT_APPLICABLE;

					return;
				case Constants.Mode.SOV:
				case Constants.Mode.HOVDRIVER:
					_trip.DriverType = Constants.DriverType.DRIVER;

					return;
				case Constants.Mode.HOVPASSENGER:
						_trip.DriverType = Constants.DriverType.PASSENGER;

					return;
			}
		}

		public override void SetTripValueOfTime() {
			var costDivisor =
				_trip.Mode == Constants.Mode.HOVDRIVER && (Tour.DestinationPurpose == Constants.Purpose.WORK || Tour.DestinationPurpose == Constants.Purpose.BUSINESS)
					? Global.Configuration.Coefficients_HOV2CostDivisor_Work
					: _trip.Mode == Constants.Mode.HOVDRIVER && Tour.DestinationPurpose != Constants.Purpose.WORK && Tour.DestinationPurpose != Constants.Purpose.BUSINESS
					  	? Global.Configuration.Coefficients_HOV2CostDivisor_Other
					  	: _trip.Mode == Constants.Mode.HOVPASSENGER && (Tour.DestinationPurpose == Constants.Purpose.WORK || Tour.DestinationPurpose == Constants.Purpose.BUSINESS)
					  	  	? Global.Configuration.Coefficients_HOV3CostDivisor_Work
					  	  	: _trip.Mode == Constants.Mode.HOVPASSENGER && Tour.DestinationPurpose != Constants.Purpose.WORK && Tour.DestinationPurpose != Constants.Purpose.BUSINESS
					  	  	  	? Global.Configuration.Coefficients_HOV3CostDivisor_Other
					  	  	  	: 1.0;

			_trip.ValueOfTime = (Tour.TimeCoefficient * 60) / (Tour.CostCoefficient / costDivisor);
		}


	}
}
