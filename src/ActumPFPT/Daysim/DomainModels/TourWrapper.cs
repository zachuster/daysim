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
//using Daysim.DomainModels.Actum;

namespace Daysim.DomainModels {
	public class TourWrapper : Daysim.Framework.Sampling.ITour, ITourWrapper {
		//protected static int _nextTripId;
		protected readonly ITour _tour;

		public TourWrapper(IPersonWrapper person, IPersonDayWrapper personDay, ICondensedParcel originParcel, ICondensedParcel destinationParcel, int destinationArrivalTime, int destinationDepartureTime, int destinationPurpose) {
			if (person == null) {
				throw new ArgumentNullException("person");
			}
			var suppressRandomVOT = true;
			_tour = new Tour();
			Person = person;
			DestinationPurpose = destinationPurpose;
			Person = person;
			PersonDay = personDay;
			OriginParcel = originParcel;
			DestinationParcel = destinationParcel;
			DestinationArrivalTime = destinationArrivalTime;
			DestinationDepartureTime = destinationDepartureTime;
			DestinationPurpose = destinationPurpose;
			Household = person.Household;
			SetValueOfTimeCoefficients(destinationPurpose, suppressRandomVOT);
		}

		public TourWrapper(Daysim.Interfaces.ITour subtour, ITourWrapper tour, int purpose = Constants.Purpose.PERSONAL_BUSINESS, bool suppressRandomVOT = false) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			_tour = subtour;

			Household = tour.Household;
			Person = tour.Person;
			PersonDay = tour.PersonDay;
			ParentTour = tour;

			SetParcelRelationships(subtour);
			SetValueOfTimeCoefficients(purpose, suppressRandomVOT);
		}

		public TourWrapper(Daysim.Interfaces.ITour tour, IPersonDayWrapper personDay, int purpose = Constants.Purpose.PERSONAL_BUSINESS, bool suppressRandomVOT = false) {
			if (personDay == null) {
				throw new ArgumentNullException("personDay");
			}

			_tour = tour;

			Household = personDay.Household;
			Person = personDay.Person;
			PersonDay = personDay;
			Subtours = new List<ITourWrapper>();

			SetParcelRelationships(tour);
			SetValueOfTimeCoefficients(purpose, suppressRandomVOT);

			IsHomeBasedTour = true;

			TimeWindow = new TimeWindow();
		}


		// relations

		public IHouseholdWrapper Household { get; protected set; }

		public IPersonWrapper Person { get; protected set; }

		public IPersonDayWrapper PersonDay { get; protected set; }

		public ITourWrapper ParentTour { get; protected set; }

		public List<ITourWrapper> Subtours { get; protected set; }

		public IHalfTour HalfTourFromOrigin { get; protected set; }

		public IHalfTour HalfTourFromDestination { get; protected set; }

		public ICondensedParcel OriginParcel { get; protected set; }

		IParcel Daysim.Framework.Sampling.ITour.OriginParcel {
			get { return OriginParcel; }
		}

		public ICondensedParcel DestinationParcel { get; set; }


		// domain model properies

		public int Id {
			get { return _tour.Id; }
		}

		public int Sequence {
			get { return _tour.Sequence; }
			set { _tour.Sequence = value; }
		}

		public int TotalSubtours {
			get { return _tour.Subtours; }
			protected set { _tour.Subtours = value; }
		}

		public int DestinationPurpose {
			get { return _tour.DestinationPurpose; }
			set { _tour.DestinationPurpose = value; }
		}

		public int OriginDepartureTime {
			get { return _tour.OriginDepartureTime.ToMinutesAfter3AM(); }
			private set { _tour.OriginDepartureTime = value.ToMinutesAfterMidnight(); }
		}

		public int DestinationArrivalTime {
			get { return _tour.DestinationArrivalTime.ToMinutesAfter3AM(); }
			set { _tour.DestinationArrivalTime = value.ToMinutesAfterMidnight(); }
		}

		public int DestinationDepartureTime {
			get { return _tour.DestinationDepartureTime.ToMinutesAfter3AM(); }
			set { _tour.DestinationDepartureTime = value.ToMinutesAfterMidnight(); }
		}

		private int OriginArrivalTime {
			get { return _tour.OriginArrivalTime.ToMinutesAfter3AM(); }
			set { _tour.OriginArrivalTime = value.ToMinutesAfterMidnight(); }
		}


		public int DestinationAddressType {
			get { return _tour.DestinationAddressType; }
			set { _tour.DestinationAddressType = value; }
		}

		public int OriginParcelId {
			get { return _tour.OriginParcelId; }
		}

		public int DestinationParcelId {
			get { return _tour.DestinationParcelId; }
			set { _tour.DestinationParcelId = value; }
		}

		public int DestinationZoneKey {
			get { return _tour.DestinationZoneKey; }
			set { _tour.DestinationZoneKey = value; }
		}

		public int HalfTour1Trips {
			get { return _tour.HalfTour1Trips; }
			set { _tour.HalfTour1Trips = value; }
		}

		public int HalfTour2Trips {
			get { return _tour.HalfTour2Trips; }
			set { _tour.HalfTour2Trips = value; }
		}

		public int Mode {
			get { return _tour.Mode; }
			set { _tour.Mode = value; }
		}

		public int PathType {
			get { return _tour.PathType; }
			set { _tour.PathType = value; }
		}

		public int PartialHalfTour1Sequence {
			get { return _tour.PartialHalfTour1Sequence; }
			set { _tour.PartialHalfTour1Sequence = value; }
		}

		public int PartialHalfTour2Sequence {
			get { return _tour.PartialHalfTour2Sequence; }
			set { _tour.PartialHalfTour2Sequence = value; }
		}

		public int FullHalfTour1Sequence {
			get { return _tour.FullHalfTour1Sequence; }
			set { _tour.FullHalfTour1Sequence = value; }
		}

		public int FullHalfTour2Sequence {
			get { return _tour.FullHalfTour2Sequence; }
			set { _tour.FullHalfTour2Sequence = value; }
		}

		public int JointTourSequence {
			get { return _tour.JointTourSequence; }
			set { _tour.JointTourSequence = value; }
		}


		// flags, choice model properties, etc.

		public bool IsHomeBasedTour { get; private set; }

		public bool IsWorkPurpose {
			get { return DestinationPurpose == Constants.Purpose.WORK; }
		}

		public bool IsSchoolPurpose {
			get { return DestinationPurpose == Constants.Purpose.SCHOOL; }
		}

		public bool IsEscortPurpose {
			get { return DestinationPurpose == Constants.Purpose.ESCORT; }
		}

		public bool IsPersonalBusinessPurpose {
			get { return DestinationPurpose == Constants.Purpose.PERSONAL_BUSINESS; }
		}

		public bool IsShoppingPurpose {
			get { return DestinationPurpose == Constants.Purpose.SHOPPING; }
		}

		public bool IsMealPurpose {
			get { return DestinationPurpose == Constants.Purpose.MEAL; }
		}

		public bool IsSocialPurpose {
			get { return DestinationPurpose == Constants.Purpose.SOCIAL; }
		}

		public bool IsRecreationPurpose {
			get { return DestinationPurpose == Constants.Purpose.RECREATION; }
		}

		public bool IsMedicalPurpose {
			get { return DestinationPurpose == Constants.Purpose.MEDICAL; }
		}

		public bool IsPersonalBusinessOrMedicalPurpose {
			get { return DestinationPurpose == Constants.Purpose.PERSONAL_BUSINESS || DestinationPurpose == Constants.Purpose.MEDICAL; }
		}

		public bool IsSocialOrRecreationPurpose {
			get { return DestinationPurpose == Constants.Purpose.SOCIAL || DestinationPurpose == Constants.Purpose.RECREATION; }
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

		public bool IsParkAndRideMode {
			get { return Mode == Constants.Mode.PARK_AND_RIDE; }
		}

		public bool IsSchoolBusMode {
			get { return Mode == Constants.Mode.SCHOOL_BUS; }
		}

		public bool IsWalkOrBikeMode {
			get { return Mode == Constants.Mode.WALK || Mode == Constants.Mode.BIKE; }
		}

		public bool HasSubtours {
			get { return Subtours.Count > 0; }
		}

		public bool IsAnHovMode {
			get { return IsHov2Mode || IsHov3Mode; }
		}

		public bool IsAnAutoMode {
			get { return IsSovMode || IsHov2Mode || IsHov3Mode; }
		}

		public bool UsesTransitModes {
			get { return IsTransitMode || IsParkAndRideMode; }
		}

		public int TotalToursByPurpose {
			get {
				switch (DestinationPurpose) {
					case Constants.Purpose.WORK:
						return PersonDay.WorkTours;
					case Constants.Purpose.SCHOOL:
						return PersonDay.SchoolTours;
					case Constants.Purpose.ESCORT:
						return PersonDay.EscortTours;
					case Constants.Purpose.PERSONAL_BUSINESS:
						return PersonDay.PersonalBusinessTours;
					case Constants.Purpose.SHOPPING:
						return PersonDay.ShoppingTours;
					case Constants.Purpose.MEAL:
						return PersonDay.MealTours;
					case Constants.Purpose.SOCIAL:
						return PersonDay.SocialTours;
					default:
						return 0;
				}
			}
		}

		public int TotalSimulatedToursByPurpose {
			get {
				switch (DestinationPurpose) {
					case Constants.Purpose.WORK:
						return PersonDay.SimulatedWorkTours;
					case Constants.Purpose.SCHOOL:
						return PersonDay.SimulatedSchoolTours;
					case Constants.Purpose.ESCORT:
						return PersonDay.SimulatedEscortTours;
					case Constants.Purpose.PERSONAL_BUSINESS:
						return PersonDay.SimulatedPersonalBusinessTours;
					case Constants.Purpose.SHOPPING:
						return PersonDay.SimulatedShoppingTours;
					case Constants.Purpose.MEAL:
						return PersonDay.SimulatedMealTours;
					case Constants.Purpose.SOCIAL:
						return PersonDay.SimulatedSocialTours;
					default:
						return 0;
				}
			}
		}

		public ITimeWindow TimeWindow { get; private set; }

		public int TourPurposeSegment {
			get {
				return
					IsHomeBasedTour
						? Constants.Purpose.HOME_BASED_COMPOSITE
						: Constants.Purpose.WORK_BASED;
			}
		}

		public int TourCategory {
			get {
				var tourCategory =
					IsHomeBasedTour
						? PersonDay.SimulatedHomeBasedTours == 1
							? Constants.TourCategory.PRIMARY
							: Constants.TourCategory.SECONDARY
						: Constants.TourCategory.WORK_BASED;

				if (tourCategory == Constants.TourCategory.SECONDARY && (IsWorkPurpose && !Person.IsFulltimeWorker) || (IsSchoolPurpose && Person.IsAdult)) {
					tourCategory = Constants.TourCategory.HOME_BASED;
				}

				return tourCategory;
			}
		}

		public virtual int VotALSegment {  //ACTUM changed this to virtual so I could create an override in ActumTourWrapper
			get {
				var segment = ((60 * TimeCoefficient) / CostCoefficient < Constants.VotALSegment.VOT_LOW_MEDIUM)
									? Constants.VotALSegment.LOW
									: ((60 * TimeCoefficient) / CostCoefficient < Constants.VotALSegment.VOT_MEDIUM_HIGH)
										? Constants.VotALSegment.MEDIUM
										: Constants.VotALSegment.HIGH;
				return segment;
			}
		}

		public double IndicatedTravelTimeToDestination { get; set; }
		public double IndicatedTravelTimeFromDestination { get; set; }

		public int EarliestOrignDepartureTime { get; set; }
		public int LatestOrignArrivalTime { get; set; }

		public IMinuteSpan DestinationDepartureBigPeriod { get; set; }
		public IMinuteSpan DestinationArrivalBigPeriod { get; set; }

		public double TimeCoefficient { get; private set; }

		public double CostCoefficient { get; private set; }

		public int ParkAndRideNodeId { get; set; }

		public bool DestinationModeAndTimeHaveBeenSimulated { get; set; }

		public bool HalfTour1HasBeenSimulated { get; set; }

		public bool HalfTour2HasBeenSimulated { get; set; }

		public bool IsMissingData { get; set; }

		// wrapper methods

		private void SetParcelRelationships(Daysim.Interfaces.ITour tour) {
			CondensedParcel originParcel;

			if (tour.OriginParcelId != Constants.DEFAULT_VALUE && ChoiceModelFactory.Parcels.TryGetValue(tour.OriginParcelId, out originParcel)) {
				OriginParcel = originParcel;
			}

			CondensedParcel destinationParcel;

			if (tour.DestinationParcelId != Constants.DEFAULT_VALUE && ChoiceModelFactory.Parcels.TryGetValue(tour.DestinationParcelId, out destinationParcel)) {
				DestinationParcel = destinationParcel;
			}
		}


		protected void SetValueOfTimeCoefficients(int purpose, bool suppressRandomVOT) {
			var randomVot = !Global.Configuration.IsInEstimationMode && Global.Configuration.UseRandomVotDistribution && !suppressRandomVOT;

			var income = Household.Income < 0 ? Global.Configuration.Coefficients_BaseCostCoefficientIncomeLevel : Household.Income; // missing converted to 30K
			var incomeMultiple = Math.Min(Math.Max(income / Global.Configuration.Coefficients_BaseCostCoefficientIncomeLevel,
																Global.Coefficients_CostCoefficientIncomeMultipleMinimum),
													Global.Coefficients_CostCoefficientIncomeMultipleMaximum); // ranges for extreme values
			var incomePower =
				(purpose == Constants.Purpose.WORK)
					? Global.Configuration.Coefficients_CostCoefficientIncomePower_Work
					: Global.Configuration.Coefficients_CostCoefficientIncomePower_Other;

			var costCoefficient = Global.Coefficients_BaseCostCoefficientPerMonetaryUnit / Math.Pow(incomeMultiple, incomePower);

			CostCoefficient = costCoefficient;

			const double minimumTimeCoef = 0.001;
			const double maximumTimeCoef = 1.000;

			var mean =
				(purpose == Constants.Purpose.WORK)
					? Global.Configuration.Coefficients_MeanTimeCoefficient_Work
					: Global.Configuration.Coefficients_MeanTimeCoefficient_Other;

			double timeCoefficient;

			if (randomVot) {
				if (Global.Configuration.ShouldSynchronizeRandomSeed && PersonDay != null) {
					PersonDay.ResetRandom(10 + Sequence - 1);
				}

				var sDev = Math.Abs(mean *
										  ((purpose == Constants.Purpose.WORK)
												? Global.Configuration.Coefficients_StdDeviationTimeCoefficient_Work
												: Global.Configuration.Coefficients_StdDeviationTimeCoefficient_Other));

				timeCoefficient = -1.0 * Math.Min(maximumTimeCoef, Math.Max(minimumTimeCoef, Household.RandomUtility.LogNormal(-1.0 * mean, sDev))); // converted to positive and back to negative

				if (timeCoefficient.AlmostEquals(0)) {
					throw new InvalidTimeCoefficientException(string.Format("The time coefficient is invalid where randomVot is true for mean: {0}, sDev: {1}.", mean, sDev));
				}
			}
			else {
				timeCoefficient = mean;

				if (timeCoefficient.AlmostEquals(0)) {
					throw new InvalidTimeCoefficientException(string.Format("The time coefficient is invalid where randomVot is false for mean: {0}.", mean));
				}
			}

			TimeCoefficient = timeCoefficient;

			//			if (randomVot) {
			//				var vot = (60 * timeCoefficient) / costCoefficient;
			//				Global.PrintFile.WriteLine("Value of time is {0}",vot);
			//			}
		}

		public void SetHomeBasedIsSimulated() {
			PersonDay.IncrementSimulatedTours(DestinationPurpose);
		}

		public void SetWorkBasedIsSimulated() {
			PersonDay.IncrementSimulatedStops(DestinationPurpose);
		}

		public void SetHalfTours(int direction) {
			switch (direction) {
				case Constants.TourDirection.ORIGIN_TO_DESTINATION:
					HalfTourFromOrigin = new HalfTour(this);

					HalfTourFromOrigin.SetTrips(direction);

					break;
				case Constants.TourDirection.DESTINATION_TO_ORIGIN:
					HalfTourFromDestination = new HalfTour(this);

					HalfTourFromDestination.SetTrips(direction);

					break;
			}
		}

		public ITimeWindow GetRelevantTimeWindow(IHouseholdDayWrapper householdDay) {

			TimeWindow timeWindow = new TimeWindow();
			if (JointTourSequence > 0) {
				foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = pDay.Tours.Find(t => t.JointTourSequence == JointTourSequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
					}
				}
			}
			else if (FullHalfTour1Sequence > 0 || FullHalfTour2Sequence > 0) {
				if (FullHalfTour1Sequence > 0) {
					foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
						var tInJoint = pDay.Tours.Find(t => t.FullHalfTour1Sequence == FullHalfTour1Sequence);
						if (!(tInJoint == null)) {
							// set jointTour time window
							timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						}
					}
				}
				if (FullHalfTour2Sequence > 0) {
					foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
						var tInJoint = pDay.Tours.Find(t => t.FullHalfTour2Sequence == FullHalfTour2Sequence);
						if (!(tInJoint == null)) {
							// set jointTour time window
							timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						}
					}
				}
			}
			else if (ParentTour == null) {
				timeWindow.IncorporateAnotherTimeWindow(PersonDay.TimeWindow);
			}
			else {
				timeWindow.IncorporateAnotherTimeWindow(ParentTour.TimeWindow);
			}
			return timeWindow;
		}

		public void SetOriginTimes(int direction = 0) {
			if (Global.Configuration.IsInEstimationMode) {
				return;
			}

			// sets origin departure time
			if (direction != Constants.TourDirection.DESTINATION_TO_ORIGIN) {
				OriginDepartureTime = HalfTourFromOrigin.Trips.Last().ArrivalTime;
			}

			// sets origin arrival time
			if (direction != Constants.TourDirection.ORIGIN_TO_DESTINATION) {
				OriginArrivalTime = HalfTourFromDestination.Trips.Last().ArrivalTime;
			}
		}

		public void UpdateTourValues() {
			if (Global.Configuration.IsInEstimationMode || (Global.Configuration.ShouldRunTourModels && !Global.Configuration.ShouldRunTourTripModels) || (Global.Configuration.ShouldRunSubtourModels && !Global.Configuration.ShouldRunSubtourTripModels)) {
				return;
			}

			var useMode = Constants.Mode.SOV;
			var autoPathRoundTrip = PathTypeModel.Run(Household.RandomUtility, OriginParcel, DestinationParcel, DestinationArrivalTime, DestinationDepartureTime, DestinationPurpose, CostCoefficient, TimeCoefficient, true, 1, Person.TransitFareDiscountFraction, false, useMode).First();
			_tour.AutoTimeOneWay = autoPathRoundTrip.PathTime / 2.0;
			_tour.AutoCostOneWay = autoPathRoundTrip.PathCost / 2.0;
			_tour.AutoDistanceOneWay = autoPathRoundTrip.PathDistance / 2.0;

			if (HalfTourFromOrigin != null && HalfTourFromDestination != null) {
				var trips = HalfTourFromOrigin.Trips.Union(HalfTourFromDestination.Trips).ToList();

				for (var i = 0; i < trips.Count; i++) {
					// sets the driver or passenger flag for each trip
					trips[i].SetDriverOrPassenger(trips);

					// sets the activity end time for each trip
					if (trips[i].IsHalfTourFromOrigin) {
						if (i > 0) {
							trips[i].SetActivityEndTime(trips[i - 1].ArrivalTime);
						}
						else {
							trips[i].SetActivityEndTime(DestinationDepartureTime);
						}
					}
					else {
						if (i < trips.Count - 1) {
							trips[i].SetActivityEndTime(trips[i + 1].DepartureTime);
						}
						else {
							var nextDepartureTime = Constants.Time.MINUTES_IN_A_DAY;
							for (var j = 1; j < PersonDay.Tours.Count; j++) {
								var otherTour = PersonDay.Tours[j - 1];
								if (otherTour != null
									&& otherTour.OriginDepartureTime > trips[i].ArrivalTime
									&& otherTour.OriginDepartureTime < nextDepartureTime) {
									nextDepartureTime = otherTour.OriginDepartureTime;
								}
							}
							trips[i].SetActivityEndTime(nextDepartureTime);
						}
					}
				}
			}
		}

		public virtual ITourWrapper CreateSubtour(int originAddressType, int originParcelId, int originZoneKey, int destinationPurpose) {
			TotalSubtours++;

			return new TourWrapper(new Tour {
				Id = _tour.PersonDayId * 100 + PersonDay.GetNextTourSequence(),//++PersonDayWrapper.NextTourId,
				PersonId = _tour.PersonId,
				PersonDayId = _tour.PersonDayId,
				HouseholdId = _tour.HouseholdId,
				PersonSequence = _tour.PersonSequence,
				Day = _tour.Day,
				Sequence = PersonDay.GetCurrentTourSequence(),
				OriginAddressType = originAddressType,
				OriginParcelId = originParcelId,
				OriginZoneKey = originZoneKey,
				DestinationPurpose = destinationPurpose,
				OriginDepartureTime = 180,
				DestinationArrivalTime = 180,
				DestinationDepartureTime = 180,
				OriginArrivalTime = 180,
				PathType = 1,
				ExpansionFactor = Household.ExpansionFactor
			}, this, destinationPurpose);
		}

		public IHalfTour GetHalfTour(int direction) {
			switch (direction) {
				case Constants.TourDirection.ORIGIN_TO_DESTINATION:

					// the half-tour from the origin to destination
					return HalfTourFromOrigin;
				case Constants.TourDirection.DESTINATION_TO_ORIGIN:

					// the half-tour from the destination to origin
					return HalfTourFromDestination;
				default:
					throw new InvalidTourDirectionException();
			}
		}

		public ITourModeImpedance[] GetTourModeImpedances() {
			var modeImpedances = new ITourModeImpedance[DayPeriod.SmallDayPeriods.Length];
			var availableMinutes = IsHomeBasedTour ? PersonDay.TimeWindow : ParentTour.TimeWindow;

			for (var i = 0; i < DayPeriod.SmallDayPeriods.Length; i++) {
				var period = DayPeriod.SmallDayPeriods[i];
				var modeImpedance = GetTourModeImpedance(period.Middle);

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

		private ITourModeImpedance GetTourModeImpedance(int minute) {
			var modeImpedance = new TourModeImpedance();
			var useMode = Mode == Constants.Mode.SCHOOL_BUS ? Constants.Mode.HOV3 : Mode;
			var pathTypeFromOrigin = PathTypeModel.Run(Household.RandomUtility, OriginParcel, DestinationParcel, minute, 0, DestinationPurpose, CostCoefficient, TimeCoefficient, true, 1, Person.TransitFareDiscountFraction, false, useMode).First();
			var pathTypeFromDestination = PathTypeModel.Run(Household.RandomUtility, DestinationParcel, OriginParcel, minute, 0, DestinationPurpose, CostCoefficient, TimeCoefficient, true, 1, Person.TransitFareDiscountFraction, false, useMode).First();

			modeImpedance.GeneralizedTimeFromOrigin = pathTypeFromOrigin.GeneralizedTimeLogsum;
			modeImpedance.GeneralizedTimeFromDestination = pathTypeFromDestination.GeneralizedTimeLogsum;

			return modeImpedance;
		}

		public void SetParentTourSequence(int parentTourSequence) {
			_tour.ParentTourSequence = parentTourSequence;
		}

		public void SetParkAndRideStay() {
			if (!Global.ParkAndRideNodeIsEnabled || !Global.Configuration.ShouldUseParkAndRideShadowPricing || Global.Configuration.IsInEstimationMode) {
				return;
			}
			var arrivalTime = HalfTourFromOrigin.Trips.First(x => x.OriginPurpose == Constants.Purpose.CHANGE_MODE).DepartureTime;
			var mode = HalfTourFromOrigin.Trips.First(x => x.OriginPurpose == Constants.Purpose.CHANGE_MODE).Mode;
			var departureTime = HalfTourFromDestination.Trips.First(x => x.OriginPurpose == Constants.Purpose.CHANGE_MODE).DepartureTime;
			var parkAndRideLoad = ChoiceModelFactory.ParkAndRideNodeDao.Get(ParkAndRideNodeId).ParkAndRideLoad;
			for (var minute = arrivalTime; minute < departureTime; minute++) {
				parkAndRideLoad[minute] += Household.ExpansionFactor / (mode == Constants.Mode.HOV3 ? 3 : mode == Constants.Mode.HOV2 ? 2 : 1);
			}
		}

		// utility/export methods

		public void Export() {
			Global.Kernel.Get<TourPersistenceFactory>().TourPersister.Export(_tour);
		}

		public static void Close() {
			Global.Kernel.Get<TourPersistenceFactory>().TourPersister.Dispose();
		}

		public override string ToString() {
			var builder = new StringBuilder();

			builder.AppendLine(string.Format("Tour ID: {0}, Person Day ID: {1}, Person ID: {2}",
				_tour.Id,
				_tour.PersonDayId,
				_tour.PersonId));

			builder.AppendLine(string.Format("Household ID: {0}, Person Sequence: {1}, Day: {2}, Sequence: {3}, Parent Tour Sequence: {4}",
				_tour.HouseholdId,
				_tour.PersonSequence,
				_tour.Day,
				_tour.Sequence,
				_tour.ParentTourSequence));

			builder.AppendLine(string.Format("Destination Parcel ID: {0}, Destination Zone Key: {1}, Destination Address Type: {2}, Destination Purpose: {3}, Mode: {4}, Destination Arrival Time: {5}, Destination Departure Time: {6}",
				_tour.DestinationParcelId,
				_tour.DestinationZoneKey,
				_tour.DestinationAddressType,
				_tour.DestinationPurpose,
				_tour.Mode,
				_tour.DestinationArrivalTime,
				_tour.DestinationDepartureTime));

			return builder.ToString();
		}


		public sealed class HalfTour : IHalfTour {
			private readonly TourWrapper _t;

			public HalfTour(TourWrapper tour) {
				_t = tour;
			}

			public List<ITripWrapper> Trips { get; private set; }

			public int SimulatedTrips { get; set; }

			public int OneSimulatedTripFlag {
				get { return (SimulatedTrips == 1).ToFlag(); }
			}

			public int TwoSimulatedTripsFlag {
				get { return (SimulatedTrips == 2).ToFlag(); }
			}

			public int ThreeSimulatedTripsFlag {
				get { return (SimulatedTrips == 3).ToFlag(); }
			}

			public int FourSimulatedTripsFlag {
				get { return (SimulatedTrips == 4).ToFlag(); }
			}

			public int FiveSimulatedTripsFlag {
				get { return (SimulatedTrips == 5).ToFlag(); }
			}

			public int FivePlusSimulatedTripsFlag {
				get { return (SimulatedTrips >= 5).ToFlag(); }
			}


			public void SetTrips(int direction) {
				Trips =
					Global.Configuration.IsInEstimationMode
						? GetTripSurveyData(direction)
						: GetTripSimulatedData(direction, 0);
			}

			private List<ITripWrapper> GetTripSurveyData(int direction) {
				var tripsForTours = LoadTripsFromFile().ToList();
				var data = (tripsForTours.Where(trip => trip.HalfTour == direction).Select(trip => Global.Kernel.Get<TripWrapperFactory>().TripWrapperCreator.CreateWrapper(trip, _t, this))).ToList();

				return direction == Constants.TourDirection.ORIGIN_TO_DESTINATION ? data.Invert() : data;
			}

			private List<ITripWrapper> GetTripSimulatedData(int direction, int sequence) {
				return new List<ITripWrapper> { CreateTrip(_t._tour, direction, sequence, true) };
			}

			private IEnumerable<Daysim.Interfaces.ITrip> LoadTripsFromFile() {
				return Global.Kernel.Get<TripPersistenceFactory>().TripPersister.Seek(_t._tour.Id, "tour_fk");
			}

			private ITripWrapper CreateTrip(Daysim.Interfaces.ITour tour, int direction, int sequence, bool isToTourOrigin) {
				ITripWrapper tw = Global.Kernel.Get<TripWrapperFactory>().TripWrapperCreator.
					CreateWrapperWithTrip(_t, tour, _t._tour,
												 _t._tour.Id * 100 + 50 * (direction - 1) + sequence + 1, //++_nextTripId,
												 direction,
												 sequence, isToTourOrigin, this);
				return tw;
			}

			public ITripWrapper CreateNextTrip(ITripWrapper trip, int intermediateStopPurpose, int destinationPurpose) {
				if (trip == null) {
					throw new ArgumentNullException("trip");
				}

				_t.PersonDay.IncrementSimulatedStops(intermediateStopPurpose);

				ITripWrapper tw = Global.Kernel.Get<TripWrapperFactory>().TripWrapperCreator.
					CreateWrapperWithNextTrip(_t, _t._tour, trip,
															trip.Id + 1,//++_nextTripId, 
															intermediateStopPurpose, destinationPurpose, this);
				return tw;
			}
		}
	}
}