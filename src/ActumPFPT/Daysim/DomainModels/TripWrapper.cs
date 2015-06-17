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
using Daysim.ChoiceModels;
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;
using Daysim.ModelRunners;
using Ninject;

namespace Daysim.DomainModels {
	public class TripWrapper : Daysim.Framework.Sampling.ITrip, ITripWrapper {
		protected readonly ITrip _trip;

		public TripWrapper(ITrip trip, ITourWrapper tour, IHalfTour halfTour) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			//	_exporter = ChoiceModelFactory.ExporterFactory.GetExporter<Trip>(Global.Configuration.OutputTripPath, Global.Configuration.OutputTripDelimiter);

			_trip = trip;

			Household = tour.Household;
			Person = tour.Person;
			PersonDay = tour.PersonDay;
			Tour = tour;
			HalfTour = halfTour;

			SetParcelRelationships(trip);

			IsHalfTourFromOrigin = Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION;
		}


		// relations

		public IHouseholdWrapper Household { get; private set; }

		public IPersonWrapper Person { get; private set; }

		public IPersonDayWrapper PersonDay { get; private set; }

		public ITourWrapper Tour { get; private set; }

		public IHalfTour HalfTour { get; private set; }

		public ICondensedParcel OriginParcel { get; set; }

		IParcel Daysim.Framework.Sampling.ITrip.OriginParcel {
			get { return OriginParcel; }
		}

		public ICondensedParcel DestinationParcel { get; set; }


		// domain model properies

		public int Id {
			get { return _trip.Id; }
		}

		public int Day {
			get { return _trip.Day; }
		}

		public int Direction {
			get { return _trip.HalfTour; }
		}

		public int Sequence {
			get { return _trip.Sequence; }
			private set { _trip.Sequence = value; }
		}

		public int DestinationPurpose {
			get { return _trip.DestinationPurpose; }
			set { _trip.DestinationPurpose = value; }
		}

		public int OriginPurpose {
			get { return _trip.OriginPurpose; }
			set { _trip.OriginPurpose = value; }
		}

		public int DestinationAddressType {
			get { return _trip.DestinationAddressType; }
			set { _trip.DestinationAddressType = value; }
		}

		public int OriginParcelId {
			get { return _trip.OriginParcelId; }
			set { _trip.OriginParcelId = value; }
		}

		public int OriginZoneKey {
			get { return _trip.OriginZoneKey; }
			set { _trip.OriginZoneKey = value; }
		}

		public int DestinationParcelId {
			get { return _trip.DestinationParcelId; }
			set { _trip.DestinationParcelId = value; }
		}

		public int DestinationZoneKey {
			get { return _trip.DestinationZoneKey; }
			set { _trip.DestinationZoneKey = value; }
		}

		public int Mode {
			get { return _trip.Mode; }
			set { _trip.Mode = value; }
		}

		public int DriverType {
			get { return _trip.DriverType; }
			set { _trip.DriverType = value; }
		}

		public int PathType {
			get { return _trip.PathType; }
			set { _trip.PathType = value; }
		}

		public int DepartureTime {
			get { return _trip.DepartureTime.ToMinutesAfter3AM(); }
			set { _trip.DepartureTime = value.ToMinutesAfterMidnight(); }
		}

		public int ArrivalTime {
			get { return _trip.ArrivalTime.ToMinutesAfter3AM(); }
			private set { _trip.ArrivalTime = value.ToMinutesAfterMidnight(); }
		}

		public int ActivityEndTime {
			get { return _trip.ActivityEndTime.ToMinutesAfter3AM(); }
			private set { _trip.ActivityEndTime = value.ToMinutesAfterMidnight(); }
		}

		public int EarliestDepartureTime { get; set; }
		public int LatestDepartureTime { get; set; }
		public int ArrivalTimeLimit { get; set; }

		public double ValueOfTime {
			get { return _trip.ValueOfTime; }
			private set { _trip.ValueOfTime = value; }
		}

		// flags, choice model properties, etc.

		public bool IsHalfTourFromOrigin { get; private set; }

		public bool IsToTourOrigin { get; set; }

		public bool IsNoneOrHomePurposeByOrigin {
			get {
				var purpose = IsHalfTourFromOrigin ? DestinationPurpose : _trip.OriginPurpose;

				return purpose == Constants.Purpose.NONE_OR_HOME;
			}
		}

		public bool IsWorkPurposeByOrigin {
			get {
				var purpose = IsHalfTourFromOrigin ? DestinationPurpose : _trip.OriginPurpose;

				return purpose == Constants.Purpose.WORK;
			}
		}

		public bool IsEscortPurposeByOrigin {
			get {
				var purpose = IsHalfTourFromOrigin ? DestinationPurpose : _trip.OriginPurpose;

				return purpose == Constants.Purpose.ESCORT;
			}
		}

		public bool IsNoneOrHomePurposeByDestination {
			get {
				var purpose = IsHalfTourFromOrigin ? _trip.OriginPurpose : DestinationPurpose;

				return purpose == Constants.Purpose.NONE_OR_HOME;
			}
		}

		public bool IsWorkPurposeByDestination {
			get {
				var purpose = IsHalfTourFromOrigin ? _trip.OriginPurpose : DestinationPurpose;

				return purpose == Constants.Purpose.WORK;
			}
		}

		public bool IsEscortPurposeByDestination {
			get {
				var purpose = IsHalfTourFromOrigin ? _trip.OriginPurpose : DestinationPurpose;

				return purpose == Constants.Purpose.ESCORT;
			}
		}

		public bool IsWorkDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.WORK; }
		}

		public bool IsSchoolDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.SCHOOL; }
		}

		public bool IsEscortDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.ESCORT; }
		}

		public bool IsPersonalBusinessDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.PERSONAL_BUSINESS; }
		}

		public bool IsShoppingDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.SHOPPING; }
		}

		public bool IsMealDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.MEAL; }
		}

		public bool IsSocialDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.SOCIAL; }
		}

		public bool IsRecreationDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.RECREATION; }
		}

		public bool IsMedicalDestinationPurpose {
			get { return DestinationPurpose == Constants.Purpose.MEDICAL; }
		}

		public bool IsPersonalBusinessOrMedicalDestinationPurpose {
			get { return IsPersonalBusinessDestinationPurpose || IsMedicalDestinationPurpose; }
		}

		public bool IsWorkOrSchoolDestinationPurpose {
			get { return IsWorkDestinationPurpose || IsSchoolDestinationPurpose; }
		}

		public bool IsPersonalReasonsDestinationPurpose {
			get { return IsMealDestinationPurpose || IsPersonalBusinessDestinationPurpose || IsShoppingDestinationPurpose || IsSocialDestinationPurpose; }
		}

		public bool IsSchoolOriginPurpose {
			get { return _trip.OriginPurpose == Constants.Purpose.SCHOOL; }
		}

		public bool IsEscortOriginPurpose {
			get { return _trip.OriginPurpose == Constants.Purpose.ESCORT; }
		}

		public bool IsShoppingOriginPurpose {
			get { return _trip.OriginPurpose == Constants.Purpose.SHOPPING; }
		}

		public bool IsPersonalBusinessOriginPurpose {
			get { return _trip.OriginPurpose == Constants.Purpose.PERSONAL_BUSINESS; }
		}

		public bool IsMealOriginPurpose {
			get { return _trip.OriginPurpose == Constants.Purpose.MEAL; }
		}

		public bool IsSocialOriginPurpose {
			get { return _trip.OriginPurpose == Constants.Purpose.SOCIAL; }
		}

		public bool UsesSovOrHovModes {
			get { return IsSovMode || IsHov2Mode || IsHov3Mode; }
		}

		public bool IsWalkMode {
			get { return Mode == Constants.Mode.WALK; }
		}

		public bool IsBikeMode {
			get { return Mode == Constants.Mode.BIKE; }
		}

		public bool IsSovMode {
			get { return Mode == Constants.Mode.SOV; }
		}

		public bool IsHov2Mode {
			get { return Mode == Constants.Mode.HOV2; }
		}

		public bool IsHov3Mode {
			get { return Mode == Constants.Mode.HOV3; }
		}

		public bool IsTransitMode {
			get { return Mode == Constants.Mode.TRANSIT; }
		}

		public bool IsBeforeMandatoryDestination {
			get { return Direction == 1 && (Tour.IsWorkPurpose || Tour.IsSchoolPurpose); }
		}

		public ITripWrapper PreviousTrip {
			get {
				var index = Sequence - 1;
				var previousTripIndex = index - 1;

				return HalfTour.Trips[previousTripIndex];
			}
		}

		public ITripWrapper NextTrip {
			get {
				var index = Sequence - 1;
				var nextTripIndex = index + 1;

				return nextTripIndex < HalfTour.Trips.Count ? HalfTour.Trips[nextTripIndex] : null;
			}
		}

		public int StartTime {
			get {
				if (IsHalfTourFromOrigin && Sequence == 1) {
					return Tour.DestinationArrivalTime;
				}

				if (!IsHalfTourFromOrigin && Sequence == 1) {
					return Tour.DestinationDepartureTime;
				}

				return PreviousTrip.ArrivalTime; // arrival time of prior trip to prior stop location
			}
		}

		public bool IsMissingData { get; set; }


		// wrapper methods

		private void SetParcelRelationships(ITrip trip) {
			CondensedParcel originParcel;

			if (trip.OriginParcelId != Constants.DEFAULT_VALUE && ChoiceModelFactory.Parcels.TryGetValue(trip.OriginParcelId, out originParcel)) {
				OriginParcel = originParcel;
			}

			CondensedParcel destinationParcel;

			if (trip.DestinationParcelId != Constants.DEFAULT_VALUE && ChoiceModelFactory.Parcels.TryGetValue(trip.DestinationParcelId, out destinationParcel)) {
				DestinationParcel = destinationParcel;
			}
		}

		public virtual void SetDriverOrPassenger(List<ITripWrapper> trips) {
			switch (Mode) {
				case Constants.Mode.WALK:
				case Constants.Mode.BIKE:
				case Constants.Mode.TRANSIT:
				case Constants.Mode.SCHOOL_BUS:
				case Constants.Mode.OTHER:
					_trip.DriverType = Constants.DriverType.NOT_APPLICABLE;

					return;
				case Constants.Mode.SOV:
					_trip.DriverType = Constants.DriverType.DRIVER;

					return;
				case Constants.Mode.HOV2:
				case Constants.Mode.HOV3:
					if (Person.IsChildUnder16 || Household.VehiclesAvailable == 0 || (!Tour.IsHov2Mode && !Tour.IsHov3Mode)) {
						_trip.DriverType = Constants.DriverType.PASSENGER;
					}
					else {
						if (trips.Any(t => ((TripWrapper) t).IsWalkMode || ((TripWrapper) t).IsBikeMode)) {
							_trip.DriverType = Constants.DriverType.PASSENGER;
						}
						else if (trips.Any(t => ((TripWrapper) t).IsSovMode)) {
							_trip.DriverType = Constants.DriverType.DRIVER;
						}
						else if (trips.Any(t => ((TripWrapper) t).IsHov2Mode)) {
							var randomNumber = Household.RandomUtility.Uniform01();

							_trip.DriverType = randomNumber > .799 ? Constants.DriverType.PASSENGER : Constants.DriverType.DRIVER;
						}
						else {
							// HOV3 mode
							var randomNumber = Household.RandomUtility.Uniform01();

							_trip.DriverType = randomNumber > .492 ? Constants.DriverType.PASSENGER : Constants.DriverType.DRIVER;
						}
					}

					return;
			}
		}

		public void UpdateTripValues() {
			if (Global.Configuration.IsInEstimationMode || ((Global.Configuration.ShouldRunTourTripModels || Global.Configuration.ShouldRunSubtourTripModels) && !Global.Configuration.ShouldRunIntermediateStopLocationModel) || ((Global.Configuration.ShouldRunTourTripModels || Global.Configuration.ShouldRunSubtourTripModels) && !Global.Configuration.ShouldRunTripModeModel) || ((Global.Configuration.ShouldRunTourTripModels || Global.Configuration.ShouldRunSubtourTripModels) && !Global.Configuration.ShouldRunTripTimeModel)) {
				return;
			}

			var modeImpedance = GetTripModeImpedance(DepartureTime, true);

			_trip.TravelTime = modeImpedance.TravelTime;
			_trip.TravelCost = modeImpedance.TravelCost;
			_trip.TravelDistance = modeImpedance.TravelDistance;
			_trip.PathType = modeImpedance.PathType;

			var duration = (int) _trip.TravelTime;

			if (duration == Constants.DEFAULT_VALUE && Global.Configuration.ReportInvalidPersonDays) {
				Global.PrintFile.WriteDurationIsInvalidWarning("TripWrapper", "UpdateTripValues", PersonDay.Id, _trip.TravelTime, _trip.TravelCost, _trip.TravelDistance);
				if (!Global.Configuration.IsInEstimationMode) {
					PersonDay.IsValid = false;
				}
				return;
			}

			ArrivalTime = IsHalfTourFromOrigin ? Math.Max(1, DepartureTime - duration) : Math.Min(Constants.Time.MINUTES_IN_A_DAY, DepartureTime + duration);

			var timeWindow = Tour.IsHomeBasedTour ? Tour.PersonDay.TimeWindow : Tour.ParentTour.TimeWindow;

			if (!Global.Configuration.AllowTripArrivalTimeOverlaps && timeWindow.IsBusy(ArrivalTime)) {
				// move entire trip up to 15 minutes later or earlier depending on half tour direction.  
				// Find the smallest shift that will make the arrival time a non-busy minute while still leaving 
				// a gap between the departure time and the arrival time at the trip origin (non-0 activity duration)
				const int moveLimit = 15;

				if (IsHalfTourFromOrigin) {
					int originArrivalTime = Sequence == 1 ? Tour.DestinationDepartureTime : PreviousTrip.ArrivalTime;
					int moveLater = 0;
					do {
						moveLater++;
					} while (moveLater <= moveLimit && DepartureTime + moveLater < originArrivalTime && timeWindow.IsBusy(ArrivalTime + moveLater));

					if (!timeWindow.IsBusy(ArrivalTime + moveLater)) {
						ArrivalTime += moveLater;
						DepartureTime += moveLater;
						if (Sequence == 1) Tour.DestinationArrivalTime += moveLater;
						if (Global.Configuration.ReportInvalidPersonDays) Global.PrintFile.WriteLine("Tour {0}. Arrival time moved later by {1} minutes, New departure time {2}, Origin arrival {3}", Tour.Id, moveLater, DepartureTime, originArrivalTime);
					}
				}
				else {
					int originArrivalTime = Sequence == 1 ? Tour.DestinationArrivalTime : PreviousTrip.ArrivalTime;
					int moveEarlier = 0;
					do {
						moveEarlier++;
					} while (moveEarlier <= moveLimit && DepartureTime - moveEarlier > originArrivalTime && timeWindow.IsBusy(ArrivalTime - moveEarlier));

					if (!timeWindow.IsBusy(ArrivalTime - moveEarlier)) {
						ArrivalTime -= moveEarlier;
						DepartureTime -= moveEarlier;
						if (Sequence == 1) Tour.DestinationDepartureTime -= moveEarlier;
						if (Global.Configuration.ReportInvalidPersonDays) Global.PrintFile.WriteLine("Tour {0}. Arrival time moved earlier by {1} minutes, New departure time {2}, Origin arrival {3}", Tour.Id, moveEarlier, DepartureTime, originArrivalTime);
					}
				}
			}

			//check again after possible adjustment
			if (!Global.Configuration.AllowTripArrivalTimeOverlaps && timeWindow.IsBusy(ArrivalTime)) {
				if (Global.Configuration.ReportInvalidPersonDays) Global.PrintFile.WriteLine("Arrival time is busy for {0}.", Tour.Id);
				if (!Global.Configuration.IsInEstimationMode) {
					PersonDay.IsValid = false;
				}
			}
		}

		public void HUpdateTripValues() {

			//new version for household models - assumes that mode and departure time have been set

			//time windows also reset in estimation mode  - this just resets for one window
			var timeWindow = Tour.IsHomeBasedTour ? Tour.PersonDay.TimeWindow : Tour.ParentTour.TimeWindow;

			if (!Global.Configuration.IsInEstimationMode) {

				//some variables reset only in application mode
				var time = new HTripTime(DepartureTime);
				HTripTime.SetTimeImpedanceAndWindow(this, time);

				if (!time.Available) {
					if (!Global.Configuration.IsInEstimationMode) {
						PersonDay.IsValid = false;
					}
					return;
				}

				var modeImpedance = time.ModeLOS;

				_trip.TravelTime = modeImpedance.PathTime;
				_trip.TravelCost = modeImpedance.PathCost;
				_trip.TravelDistance = modeImpedance.PathDistance;
				_trip.PathType = modeImpedance.PathType;

				var duration = (int) (_trip.TravelTime + 0.5);

				if (duration == Constants.DEFAULT_VALUE && Global.Configuration.ReportInvalidPersonDays) {
					Global.PrintFile.WriteDurationIsInvalidWarning("TripWrapper", "UpdateTripValues", PersonDay.Id,
						_trip.TravelTime, _trip.TravelCost, _trip.TravelDistance);
					if (!Global.Configuration.IsInEstimationMode) {
						PersonDay.IsValid = false;
					}
					return;
				}

				ArrivalTime = IsHalfTourFromOrigin
						? Math.Max(1, DepartureTime - duration)
						: Math.Min(Constants.Time.MINUTES_IN_A_DAY, DepartureTime + duration);

/* doesn't have much effect - turn off for now
                if (!Global.Configuration.AllowTripArrivalTimeOverlaps && timeWindow.IsBusy(ArrivalTime))   {
                    // move entire trip up to 15 minutes later or earlier depending on half tour direction.  
                    // Find the smallest shift that will make the arrival time a non-busy minute while still leaving 
                    // a gap between the departure time and the arrival time at the trip origin (non-0 activity duration)
                    //NOTE: This was copied over from the old version above.
                    // This could possibly cause some inconsistencies for times for different people on joint tours, if it is done separately for each
                    // (will work better if done before cloning....)
                    const int moveLimit = 15;

                    if (IsHalfTourFromOrigin)     {
                        int originArrivalTime = Sequence == 1 ? Tour.DestinationDepartureTime : PreviousTrip.ArrivalTime;
                        int moveLater = 0;
                        do       {
                            moveLater++;
                        } while (moveLater <= moveLimit && DepartureTime + moveLater < originArrivalTime && timeWindow.IsBusy(ArrivalTime + moveLater));

                        if (!timeWindow.IsBusy(ArrivalTime + moveLater)) {
                            ArrivalTime += moveLater;
                            DepartureTime += moveLater;
                            if (Sequence == 1) Tour.DestinationArrivalTime += moveLater;
                            if (Global.Configuration.ReportInvalidPersonDays) Global.PrintFile.WriteLine("Tour {0}. Arrival time moved later by {1} minutes, New departure time {2}, Origin arrival {3}", Tour.Id, moveLater, DepartureTime, originArrivalTime);
                        }
                    }
                    else  {
                        int originArrivalTime = Sequence == 1 ? Tour.DestinationArrivalTime : PreviousTrip.ArrivalTime;
                        int moveEarlier = 0;
                        do   {
                            moveEarlier++;
                        } while (moveEarlier <= moveLimit && DepartureTime - moveEarlier > originArrivalTime && timeWindow.IsBusy(ArrivalTime - moveEarlier));

                        if (!timeWindow.IsBusy(ArrivalTime - moveEarlier))   {
                            ArrivalTime -= moveEarlier;
                            DepartureTime -= moveEarlier;
                            if (Sequence == 1) Tour.DestinationDepartureTime -= moveEarlier;
                            if (Global.Configuration.ReportInvalidPersonDays) Global.PrintFile.WriteLine("Tour {0}. Arrival time moved earlier by {1} minutes, New departure time {2}, Origin arrival {3}", Tour.Id, moveEarlier, DepartureTime, originArrivalTime);
                        }
                    }
                }
*/
                //check again after possible adjustment
	
				if (!Global.Configuration.AllowTripArrivalTimeOverlaps && timeWindow.IsBusy(ArrivalTime)) {
					if (Global.Configuration.ReportInvalidPersonDays) Global.PrintFile.WriteLine("Arrival time {0} is busy for trip {1}.", ArrivalTime, Id);
					if (!Global.Configuration.IsInEstimationMode) {
						PersonDay.IsValid = false;
					}
				}
				else //check if another trip needs to be scheduled and there only a few minutes left
					if ((IsHalfTourFromOrigin && ArrivalTime < Tour.EarliestOrignDepartureTime + 3 && DestinationParcel != Tour.OriginParcel)
					|| (!IsHalfTourFromOrigin && ArrivalTime > Tour.LatestOrignArrivalTime - 3 && DestinationParcel != Tour.OriginParcel)) {
						if (!Global.Configuration.IsInEstimationMode) {
							PersonDay.IsValid = false;
						}
					}

				if (Global.Configuration.TraceModelResultValidity) {
					if (PersonDay.HouseholdDay.AttemptedSimulations >= Global.Configuration.InvalidAttemptsBeforeTrace)
						Global.PrintFile.WriteLine("  >> HUpdateTripValues HH/P/T/Hf/T/Arrival time/valid {0} {1} {2} {3} {4} {5} {6}",
							 Household.Id, Person.Sequence, Tour.Sequence, Direction, Sequence, ArrivalTime, PersonDay.IsValid);
				}
				if (!PersonDay.IsValid) {
					return;
				}

				//if first trip in half tour, use departure time to reset tour times
				if (Sequence == 1) {
					if (IsHalfTourFromOrigin) {
						Tour.DestinationArrivalTime = DepartureTime;
					}
					else {
						Tour.DestinationDepartureTime = DepartureTime;
					}
				}
			}

			//adjust the time window for busy minutes at the stop origin and during the trip - done also in estimation mode
			var earliestBusyMinute = IsHalfTourFromOrigin
				 ? ArrivalTime
				 : Sequence == 1
					  ? Tour.DestinationDepartureTime
					  : PreviousTrip.ArrivalTime;

			var latestBusyMinute = !IsHalfTourFromOrigin
				 ? ArrivalTime
				 : Sequence == 1
					  ? Tour.DestinationArrivalTime
					  : PreviousTrip.ArrivalTime;

			if (Household.Id == 80138 && Tour.PersonDay.HouseholdDay.AttemptedSimulations == 0 && Person.Sequence == 2) {
				bool testbreak = true;
			}

			timeWindow.SetBusyMinutes(earliestBusyMinute, latestBusyMinute + 1);

			if (Global.Configuration.TraceModelResultValidity) {
				if (PersonDay.HouseholdDay.AttemptedSimulations >= Global.Configuration.InvalidAttemptsBeforeTrace) {
					if (Tour.IsHomeBasedTour) {
						Global.PrintFile.WriteLine("  >> HUpdateTripValues SetBusyMinutes HH/P/PDay/Min1/Min2 {0} {1} {2} {3} {4}",
							 Household.Id, Person.Sequence, PersonDay.Id, earliestBusyMinute, latestBusyMinute + 1);
					}
					else {
						Global.PrintFile.WriteLine("  >> HUpdateTripValues SetBusyMinutes HH/P/TOUR/Min1/Min2 {0} {1} {2} {3} {4}",
							 Household.Id, Person.Sequence, Tour.ParentTour.Sequence, earliestBusyMinute, latestBusyMinute + 1);
					}
				}
			}

		}


		public void Invert(int sequence) {
			var tempParcelId = _trip.OriginParcelId;
			_trip.OriginParcelId = _trip.DestinationParcelId;
			_trip.DestinationParcelId = tempParcelId;

			var tempParcel = OriginParcel;
			OriginParcel = DestinationParcel;
			DestinationParcel = tempParcel;

			var tempZoneKey = _trip.OriginZoneKey;
			_trip.OriginZoneKey = _trip.DestinationZoneKey;
			_trip.DestinationZoneKey = tempZoneKey;

			var tempPurpose = _trip.OriginPurpose;
			_trip.OriginPurpose = _trip.DestinationPurpose;
			_trip.DestinationPurpose = tempPurpose;

			var tempAddressType = _trip.OriginAddressType;
			_trip.OriginAddressType = _trip.DestinationAddressType;
			_trip.DestinationAddressType = tempAddressType;

			var tempTime = _trip.ArrivalTime;
			_trip.ArrivalTime = _trip.DepartureTime;
			_trip.DepartureTime = tempTime;

			Sequence = sequence;
		}

		public ITripModeImpedance[] GetTripModeImpedances() {
			var modeImpedances = new TripModeImpedance[DayPeriod.SmallDayPeriods.Length];
			var availableMinutes = Tour.IsHomeBasedTour ? PersonDay.TimeWindow : Tour.ParentTour.TimeWindow;

			for (var i = 0; i < DayPeriod.SmallDayPeriods.Length; i++) {
				var period = DayPeriod.SmallDayPeriods[i];
				var modeImpedance = GetTripModeImpedance(period.Middle);

				modeImpedances[i] = modeImpedance;

				modeImpedance.AdjacentMinutesBefore = availableMinutes.AdjacentAvailableMinutesBefore(period.Start) / ChoiceModelFactory.SmallPeriodDuration;
				modeImpedance.MaxMinutesBefore = availableMinutes.MaxAvailableMinutesBefore(period.Start) / ChoiceModelFactory.SmallPeriodDuration;
				modeImpedance.TotalMinutesBefore = availableMinutes.TotalAvailableMinutesBefore(period.Start) / ChoiceModelFactory.SmallPeriodDuration;

				modeImpedance.AdjacentMinutesAfter = availableMinutes.AdjacentAvailableMinutesAfter(period.End) / ChoiceModelFactory.SmallPeriodDuration;
				modeImpedance.MaxMinutesAfter = availableMinutes.MaxAvailableMinutesAfter(period.End) / ChoiceModelFactory.SmallPeriodDuration;
				modeImpedance.TotalMinutesAfter = availableMinutes.TotalAvailableMinutesAfter(period.End) / ChoiceModelFactory.SmallPeriodDuration;
			}

			return modeImpedances;
		}

		private TripModeImpedance GetTripModeImpedance(int minute, bool includeCostAndDistance = false) {
			var modeImpedance = new TripModeImpedance();
			var costCoefficient = Tour.CostCoefficient;
			var timeCoefficient = Tour.TimeCoefficient;
			PathTypeModel pathType;
			if (Mode == Constants.Mode.TRANSIT && DestinationPurpose == Constants.Purpose.CHANGE_MODE) {
				var parkAndRideZoneId = ChoiceModelFactory.ParkAndRideNodeDao.Get(Tour.ParkAndRideNodeId).ZoneId;
				pathType = PathTypeModel.Run(Household.RandomUtility, OriginParcel.ZoneId, parkAndRideZoneId, minute, 0, DestinationPurpose, costCoefficient, timeCoefficient, true, 1, Person.TransitFareDiscountFraction, false, Mode).First();
			}
			else {
				var useMode = Mode == Constants.Mode.SCHOOL_BUS ? Constants.Mode.HOV3 : Mode;
				if (OriginParcel == null) {
					bool testbreak = true;
				}
				pathType = PathTypeModel.Run(Household.RandomUtility, OriginParcel, DestinationParcel, minute, 0, DestinationPurpose, costCoefficient, timeCoefficient, true, 1, Person.TransitFareDiscountFraction, false, useMode).First();
			}
			modeImpedance.TravelTime = pathType.PathTime;
			modeImpedance.GeneralizedTime = pathType.GeneralizedTimeLogsum;
			modeImpedance.PathType = pathType.PathType;

			if (includeCostAndDistance) {
				modeImpedance.TravelCost = pathType.PathCost;
				modeImpedance.TravelDistance = pathType.PathDistance;
			}

			return modeImpedance;
		}

		public void SetActivityEndTime(int activityEndTime) {
			_trip.ActivityEndTime = activityEndTime.ToMinutesAfterMidnight();
		}

		public void SetOriginAddressType(int originAddressType) {
			_trip.OriginAddressType = originAddressType;
		}

		public void SetTourSequence(int tourSequence) {
			_trip.TourSequence = tourSequence;
		}

		public virtual void SetTripValueOfTime() {
			var costDivisor =
				_trip.Mode == Constants.Mode.HOV2 && Tour.DestinationPurpose == Constants.Purpose.WORK
					? Global.Configuration.Coefficients_HOV2CostDivisor_Work
					: _trip.Mode == Constants.Mode.HOV2 && Tour.DestinationPurpose != Constants.Purpose.WORK
						? Global.Configuration.Coefficients_HOV2CostDivisor_Other
						: _trip.Mode == Constants.Mode.HOV3 && Tour.DestinationPurpose == Constants.Purpose.WORK
							? Global.Configuration.Coefficients_HOV3CostDivisor_Work
							: _trip.Mode == Constants.Mode.HOV3 && Tour.DestinationPurpose != Constants.Purpose.WORK
								? Global.Configuration.Coefficients_HOV3CostDivisor_Other
								: 1.0;

			_trip.ValueOfTime = (Tour.TimeCoefficient * 60) / (Tour.CostCoefficient / costDivisor);
		}

		// utility/export methods

		public void Export() {
			Global.Kernel.Get<TripPersistenceFactory>().TripPersister.Export(_trip);
		}

		public static void Close() {
			Global.Kernel.Get<TripPersistenceFactory>().TripPersister.Dispose();
		}

		public override string ToString() {
			var builder = new StringBuilder();

			builder.AppendLine(string.Format("Trip ID: {0}, Tour ID: {1}",
				_trip.Id,
				_trip.TourId));

			builder.AppendLine(string.Format("Household ID: {0}, Person Sequence: {1}, Day: {2}, Tour Sequence: {3}, Half-tour: {4}, Sequence {5}",
				_trip.HouseholdId,
				_trip.PersonSequence,
				_trip.Day,
				_trip.TourSequence,
				_trip.HalfTour,
				_trip.Sequence));

			builder.AppendLine(string.Format("Destination Parcel ID: {0}, Destination Zone Key: {1}, Destination Purpose: {2}, Mode: {3}, Departure Time: {4}",
				_trip.DestinationParcelId,
				_trip.DestinationZoneKey,
				_trip.DestinationPurpose,
				_trip.Mode,
				_trip.DepartureTime));

			return builder.ToString();
		}
	}
}