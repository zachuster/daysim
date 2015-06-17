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

namespace Daysim {
	public sealed class HTripModeTime {
		public const int TOTAL_TRIP_MODE_TIMES = DayPeriod.H_SMALL_DAY_PERIOD_TOTAL_TRIP_TIMES * 7;

		private HTripModeTime(int index, int mode, MinuteSpan departurePeriod) {
			Index = index;
            Mode = mode;
			DeparturePeriod = departurePeriod;
		}

		public HTripModeTime(int mode, int departureTime) {
			FindModeAndPeriod(mode, departureTime);
		}

		public int Index { get; private set; }

		public int Mode { get; private set; }

        public MinuteSpan DeparturePeriod { get; private set; }

		public static HTripModeTime[] ModeTimes { get; private set; }

        public bool Available;

        public PathTypeModel ModeLOS;

        public MinuteSpan FeasibleWindow;

		private void FindModeAndPeriod(int mode, int departureTime) {
			foreach (var period in DayPeriod.HSmallDayPeriods.Where(period => departureTime.IsBetween(period.Start, period.End))) {
				DeparturePeriod = period;
			}
            Mode = Math.Min(mode,7);

			foreach (var modeTime in ModeTimes.Where(modeTime => modeTime.DeparturePeriod == DeparturePeriod && modeTime.Mode == Mode )) {
				Index = modeTime.Index;

				break;
			}
		}

		public int GetRandomDepartureTime(TripWrapper trip) {
			if (trip == null) {
				throw new ArgumentNullException("trip");
			}

			var timeWindow = trip.Tour.ParentTour == null ? trip.Tour.PersonDay.TimeWindow : trip.Tour.ParentTour.TimeWindow;
			var departureTime = timeWindow.GetAvailableMinute(trip.Household.RandomUtility, DeparturePeriod.Start, DeparturePeriod.End);

			//if (departureTime == Constants.DEFAULT_VALUE) {
			//	throw new InvalidDepartureTimeException();
			//}

			return departureTime;
		}
        public int GetTripMode(TripWrapper trip)
        {
            if (trip == null)
            {
                throw new ArgumentNullException("trip");
            }

            var mode = (Mode == 7) ? Constants.Mode.SCHOOL_BUS : Mode;

            return mode;
        }

		public static void InitializeTripModeTimes() {
			if (ModeTimes != null) {
				return;
			}

			ModeTimes = new HTripModeTime[TOTAL_TRIP_MODE_TIMES];

			var alternativeIndex = 0;

			for (var mode = 1; mode <= 7; mode++) {
                foreach (var minuteSpan in DayPeriod.HSmallDayPeriods)
                {
                    var modeTime = new HTripModeTime(alternativeIndex, mode, minuteSpan);

                    ModeTimes[alternativeIndex++] = modeTime;
                }
			}
		}

        public static void SetModeTimeImpedances(TripWrapper trip, int earliestIn, int latestIn)
        {

            var tour = trip.Tour;
            foreach (var modeTime in ModeTimes)
            {

                var alternativeIndex = modeTime.Index;
                var period = modeTime.DeparturePeriod;
                var mode = modeTime.Mode;
                var earliest = earliestIn;
                var latest = latestIn;

                // set mode LOS and mode availability
                if (period.End < earliest || period.Start > latest
                    || mode > tour.Mode
                    || (trip.OriginPurpose == Constants.Purpose.CHANGE_MODE && mode != Constants.Mode.TRANSIT)
                    || (trip.DestinationPurpose == Constants.Purpose.CHANGE_MODE && mode != Constants.Mode.SOV))
                {
                    modeTime.Available = false;
                }
                else
                {
                    var pathMode = (mode >= Constants.Mode.SCHOOL_BUS - 1) ? Constants.Mode.HOV3 : mode;

                    var pathTypeModels =
                                PathTypeModel.Run(
                                trip.Household.RandomUtility,
                                trip.IsHalfTourFromOrigin ? trip.DestinationParcel : trip.OriginParcel,
                                trip.IsHalfTourFromOrigin ? trip.OriginParcel : trip.DestinationParcel,
                                period.Middle,
                                0,
                                tour.DestinationPurpose,
                                tour.CostCoefficient,
                                tour.TimeCoefficient,
                                tour.Person.IsDrivingAge,
                                tour.Household.VehiclesAvailable,
                                tour.Person.TransitFareDiscountFraction,
                                true,
                                pathMode);

                    var pathTypeModel = pathTypeModels.First(x => x.Mode == pathMode);

                    modeTime.Available = pathTypeModel.Available;
                    modeTime.ModeLOS = pathTypeModel;

                    //adjust the window for the travel time and recheck availability
                    if (pathTypeModel.Available)
                    {
                        if (trip.IsHalfTourFromOrigin)
                        {
                            earliest = earliest + (int)(pathTypeModel.PathTime + 0.5);
                        }
                        else
                        {
                            latest = latest - (int)(pathTypeModel.PathTime + 0.5);
                        }
                        if (period.End < earliest || period.Start > latest)
                        {
                            modeTime.Available = false;
                        }

                    }
                    
                }
                modeTime.FeasibleWindow = new MinuteSpan(earliest, latest);
            }
        }

		
         public bool Equals(HTripModeTime other) {
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

			return obj is HTripModeTime && Equals((HTripModeTime) obj);
		}

		public override int GetHashCode() {
			return Index;
		}
	}
}