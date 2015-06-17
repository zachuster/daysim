// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Linq;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ChoiceModels;
using Daysim.Interfaces;

namespace Daysim {
	public sealed class HTourModeTime {
		public const int TOTAL_TOUR_MODE_TIMES = DayPeriod.H_BIG_DAY_PERIOD_TOTAL_TOUR_TIME_COMBINATIONS * Constants.Mode.SCHOOL_BUS;

		private HTourModeTime(int index, int mode, MinuteSpan arrivalPeriod, MinuteSpan departurePeriod, int periodCombination) {
			Index = index;
			Mode = mode;
			ArrivalPeriod = arrivalPeriod;
			DeparturePeriod = departurePeriod;
			PeriodCombinationIndex = periodCombination;
		}

		public HTourModeTime(int mode, int arrivalTime, int departureTime) {
			FindIndexForModeAndPeriods(mode, arrivalTime, departureTime);
		}

		public int Index { get; private set; }

		public int Mode { get; private set; }

		public int PeriodCombinationIndex { get; private set; }

		public MinuteSpan ArrivalPeriod { get; private set; }

		public MinuteSpan DeparturePeriod { get; private set; }

		public double TravelTimeToDestination { get; private set; }

		public double TravelTimeFromDestination { get; private set; }

		public double GeneralizedTimeToDestination { get; private set; }

		public double GeneralizedTimeFromDestination { get; private set; }

		public bool ModeAvailableToDestination { get; private set; }

		public bool ModeAvailableFromDestination { get; private set; }

		public IMinuteSpan LongestFeasibleWindow { get; private set; }


		public static HTourModeTime[] ModeTimes { get; private set; }

		private void FindIndexForModeAndPeriods(int mode, int arrivalTime, int departureTime) {
			foreach (var period in DayPeriod.HBigDayPeriods) {
				if (arrivalTime.IsBetween(period.Start, period.End)) {
					ArrivalPeriod = period;
				}

				if (departureTime.IsBetween(period.Start, period.End)) {
					DeparturePeriod = period;
				}

			}
			Mode = mode;

			foreach (var modeTime in ModeTimes.Where(modeTime => modeTime.ArrivalPeriod == ArrivalPeriod && modeTime.DeparturePeriod == DeparturePeriod && modeTime.Mode == Mode)) {
				Index = modeTime.Index;
				/*
				TravelTimeToDestination = modeTime.TravelTimeToDestination;
				TravelTimeFromDestination = modeTime.TravelTimeFromDestination;
				GeneralizedTimeToDestination = modeTime.GeneralizedTimeToDestination;
				GeneralizedTimeFromDestination = modeTime.GeneralizedTimeFromDestination;
				ModeAvailableToDestination = modeTime.ModeAvailableToDestination;
				ModeAvailableFromDestination = modeTime.ModeAvailableFromDestination;
				LongestFeasibleWindow = modeTime.LongestFeasibleWindow;
				*/
				break;
			}
		}

		public int GetRandomDepartureTime(IHouseholdDayWrapper householdDay, ITourWrapper tour) {
			if (tour == null) {
				throw new ArgumentNullException("trip");
			}

			var timeWindow = tour.GetRelevantTimeWindow(householdDay);

			var departureTime = timeWindow.GetAvailableMinute(tour.Household.RandomUtility, DeparturePeriod.Start, DeparturePeriod.End);

			//if (departureTime == Constants.DEFAULT_VALUE) {
			//	throw new InvalidDepartureTimeException();
			//}

			return departureTime;
		}

		public IMinuteSpan GetRandomDestinationTimes(TimeWindow timeWindow, TourWrapper tour) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			return timeWindow.GetMinuteSpan(tour.Household.RandomUtility, ArrivalPeriod.Start, ArrivalPeriod.End, DeparturePeriod.Start, DeparturePeriod.End);
		}

		public bool SubtourIsWithinTour(TourWrapper subtour) {
			if (subtour == null) {
				throw new ArgumentNullException("subtour");
			}

			var tour = subtour.ParentTour;

			return ArrivalPeriod.Start >= tour.DestinationArrivalTime && DeparturePeriod.End <= tour.DestinationDepartureTime;
		}

		public static void InitializeTourModeTimes() {
			if (ModeTimes != null) {
				return;
			}

			ModeTimes = new HTourModeTime[TOTAL_TOUR_MODE_TIMES];

			var alternativeIndex = 0;
			var periodCombinationIndex = -1;

			for (var arrivalPeriodIndex = 0; arrivalPeriodIndex < DayPeriod.H_BIG_DAY_PERIOD_TOTAL_TOUR_TIMES; arrivalPeriodIndex++) {
				var arrivalPeriod = DayPeriod.HBigDayPeriods[arrivalPeriodIndex];
				for (var departurePeriodIndex = arrivalPeriodIndex; departurePeriodIndex < DayPeriod.H_BIG_DAY_PERIOD_TOTAL_TOUR_TIMES; departurePeriodIndex++) {
					var departurePeriod = DayPeriod.HBigDayPeriods[departurePeriodIndex];
					periodCombinationIndex++;

					for (var mode = Constants.Mode.WALK; mode <= Constants.Mode.SCHOOL_BUS; mode++) {
						var modeTimes = new HTourModeTime(alternativeIndex, mode, arrivalPeriod, departurePeriod, periodCombinationIndex);

						ModeTimes[alternativeIndex++] = modeTimes;
					}
				}
			}
		}

		public static void SetModeTimeImpedances(IHouseholdDayWrapper householdDay, ITourWrapper tour,
			int constrainedMode, int constrainedArrivalTime, int constrainedDepartureTime ) {

			if (householdDay.Household.Id == 80059 && tour.Person.Sequence == 2 && tour.Sequence == 2
				&& constrainedMode == 5 && constrainedArrivalTime == 354 && constrainedDepartureTime == 361) {
				bool testBreak = true;
			}


			var timeWindow = tour.GetRelevantTimeWindow(householdDay);

			foreach (var modeTimes in ModeTimes) {
				modeTimes.LongestFeasibleWindow = null;
				if (  (constrainedMode <=0 || constrainedMode == modeTimes.Mode)
					&& (constrainedArrivalTime <=0 || constrainedArrivalTime.IsBetween(modeTimes.ArrivalPeriod.Start,modeTimes.ArrivalPeriod.End))
					&& (constrainedDepartureTime <= 0 || constrainedDepartureTime.IsBetween(modeTimes.DeparturePeriod.Start,modeTimes.DeparturePeriod.End))) {

					SetImpedanceAndWindow(timeWindow, tour, modeTimes);
				}
			}
		}

		public static void SetImpedanceAndWindow(ITimeWindow timeWindow, ITourWrapper tour, HTourModeTime modeTimes) {
			
			var alternativeIndex = modeTimes.Index;
			var arrivalPeriod = modeTimes.ArrivalPeriod;
			var departurePeriod = modeTimes.DeparturePeriod;
			var mode = modeTimes.Mode;

			var arrivalPeriodAvailableMinutes = timeWindow.TotalAvailableMinutes(arrivalPeriod.Start, arrivalPeriod.End);
			var departurePeriodAvailableMinutes = timeWindow.TotalAvailableMinutes(departurePeriod.Start, departurePeriod.End);


			// set round trip mode LOS and mode availability
			if (arrivalPeriodAvailableMinutes <= 0 || departurePeriodAvailableMinutes <= 0
			 || (mode == Constants.Mode.PARK_AND_RIDE && tour.DestinationPurpose != Constants.Purpose.WORK)
			 || (mode == Constants.Mode.SCHOOL_BUS && tour.DestinationPurpose != Constants.Purpose.SCHOOL)) {
				modeTimes.ModeAvailableToDestination = false;
				modeTimes.ModeAvailableFromDestination = false;
			}
			//ACTUM must also use round trip path type to preserve the tour-based nonlinear gamma utility functions
			//else if (mode == Constants.Mode.PARK_AND_RIDE) {
			else if (mode == Constants.Mode.PARK_AND_RIDE || Global.Configuration.PathImpedance_UtilityForm_Auto == 1 || Global.Configuration.PathImpedance_UtilityForm_Transit == 1) {
				// park and ride has to use round-trip path type, approximate each half 

				var pathTypeModels =
								PathTypeModel.Run(
								tour.Household.RandomUtility,
								tour.OriginParcel,
								tour.DestinationParcel,
								arrivalPeriod.Middle,
								departurePeriod.Middle,
								tour.DestinationPurpose,
								tour.CostCoefficient,
								tour.TimeCoefficient,
								tour.Person.IsDrivingAge,
								tour.Household.VehiclesAvailable,
								tour.Person.TransitFareDiscountFraction,
								false,
								mode);

				var pathTypeModel = pathTypeModels.First(x => x.Mode == mode);

				modeTimes.ModeAvailableToDestination = pathTypeModel.Available;
				modeTimes.ModeAvailableFromDestination = pathTypeModel.Available;

				if (pathTypeModel.Available) {
					modeTimes.TravelTimeToDestination = pathTypeModel.PathTime / 2.0;
					modeTimes.GeneralizedTimeToDestination = pathTypeModel.GeneralizedTimeLogsum / 2.0;
					modeTimes.TravelTimeFromDestination = pathTypeModel.PathTime / 2.0;
					modeTimes.GeneralizedTimeFromDestination = pathTypeModel.GeneralizedTimeLogsum / 2.0;
				}
			}
			else {
				// get times for each half tour separately, using HOV3 for school bus
				var pathMode = (mode == Constants.Mode.SCHOOL_BUS) ? Constants.Mode.HOV3 : mode;

				//if (tour.Household.Id == 80205 && tour.PersonDay.HouseholdDay.AttemptedSimulations == 9 && (pathMode == 1 || pathMode == 5)) {
				if (tour.Person.IsDrivingAge == true && tour.Household.VehiclesAvailable > 0 && pathMode == 3) {
					bool testbreak = true;
				}
				var pathTypeModels =
								PathTypeModel.Run(
								tour.Household.RandomUtility,
								tour.OriginParcel,
								tour.DestinationParcel,
								arrivalPeriod.Middle,
								0,
								tour.DestinationPurpose,
								tour.CostCoefficient,
								tour.TimeCoefficient,
								tour.Person.IsDrivingAge,
								tour.Household.VehiclesAvailable,
								tour.Person.TransitFareDiscountFraction,
								false,
								pathMode);

				var pathTypeModel = pathTypeModels.First(x => x.Mode == pathMode);

				modeTimes.ModeAvailableToDestination = pathTypeModel.Available;

				if (pathTypeModel.Available) {
					modeTimes.TravelTimeToDestination = pathTypeModel.PathTime;
					modeTimes.GeneralizedTimeToDestination = pathTypeModel.GeneralizedTimeLogsum;
				}

				pathTypeModels =
							PathTypeModel.Run(
							tour.Household.RandomUtility,
							tour.DestinationParcel,
							tour.OriginParcel,
							departurePeriod.Middle,
							0,
							tour.DestinationPurpose,
							tour.CostCoefficient,
							tour.TimeCoefficient,
							tour.Person.IsDrivingAge,
							tour.Household.VehiclesAvailable,
							tour.Person.TransitFareDiscountFraction,
							false,
							pathMode);

				pathTypeModel = pathTypeModels.First(x => x.Mode == pathMode);

				modeTimes.ModeAvailableFromDestination = pathTypeModel.Available;

				if (pathTypeModel.Available) {
					modeTimes.TravelTimeFromDestination = pathTypeModel.PathTime;
					modeTimes.GeneralizedTimeFromDestination = pathTypeModel.GeneralizedTimeLogsum;
				}
			}
            if (tour.Household.Id == 2138 && tour.Person.Sequence == 1 && tour.Sequence == 1)
            {
                bool testbreak = true;
            }
			if (modeTimes.ModeAvailableToDestination && modeTimes.ModeAvailableFromDestination) {
				modeTimes.LongestFeasibleWindow = timeWindow.LongestAvailableFeasibleWindow(arrivalPeriod.End, departurePeriod.Start,
				modeTimes.TravelTimeToDestination, modeTimes.TravelTimeFromDestination, Constants.Time.MINIMUM_ACTIVITY_DURATION);
			}
		}


		public bool Equals(TourTime other) {
			if (ReferenceEquals(null, other)) {
				return false;
			}

			if (ReferenceEquals(this, other)) {
				return true;
			}

			return other.Index == Index;
		}

		public override bool Equals(object obj) {
			if (ReferenceEquals(null, obj)) {
				return false;
			}

			if (ReferenceEquals(this, obj)) {
				return true;
			}

			return obj is HTourModeTime && Equals((HTourModeTime) obj);
		}

		public override int GetHashCode() {
			return Index;
		}
	}
}