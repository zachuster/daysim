// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Linq;
using System.Threading.Tasks;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;

namespace Daysim.ChoiceModels {
	public static class HTripModeTimeModel {
		private const string CHOICE_MODEL_NAME = "HTripModeTimeModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 399;
		//private const int THETA_PARAMETER = 399;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];

		private static void Initialize() {
			if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null) {
				return;
			}

			ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME, Global.Configuration.TripModeTimeModelCoefficients,
					 HTripModeTime.TOTAL_TRIP_MODE_TIMES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
		}

		public static void Run(HouseholdDayWrapper householdDay, TripWrapper trip) {
			if (trip == null) {
				throw new ArgumentNullException("trip");
			}

			Initialize();
			HTripModeTime.InitializeTripModeTimes();

			trip.PersonDay.ResetRandom(40 * (2 * trip.Tour.Sequence - 1 + trip.Direction - 1) + 50 + trip.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
				if (trip.Tour.DestinationParcel == null || trip.Tour.OriginParcel == null || trip.Tour.Mode < Constants.Mode.WALK || trip.Tour.Mode > Constants.Mode.SCHOOL_BUS
					|| trip.DestinationParcel == null || trip.OriginParcel == null || trip.Mode < Constants.Mode.WALK || trip.Mode > Constants.Mode.SCHOOL_BUS
					|| trip.Tour.EarliestOrignDepartureTime < 1 || trip.Tour.LatestOrignArrivalTime < 1) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(trip.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {

				RunModel(choiceProbabilityCalculator, householdDay, trip, new HTripModeTime(trip.Mode, trip.DepartureTime));

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, householdDay, trip);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(trip.Household.RandomUtility);

				if (chosenAlternative == null) {
					Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", trip.PersonDay.Id);
					trip.PersonDay.IsValid = false;
					return;
				}

				var choice = (HTripModeTime) chosenAlternative.Choice;
				var departureTime = choice.GetRandomDepartureTime(trip);
				trip.Mode = choice.GetTripMode(trip);
				trip.DepartureTime = departureTime;
				if (departureTime >= 1 && departureTime <= Constants.Time.MINUTES_IN_A_DAY) {
					trip.UpdateTripValues();
				}
				else {
					trip.PersonDay.IsValid = false;
				}
			}
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, HouseholdDayWrapper householdDay, TripWrapper trip, HTripTime choice = null) {
			var household = trip.Household;
			var householdTotals = household.HouseholdTotals;
			var halfTour = trip.HalfTour;
			var person = trip.Person;
			var personDay = trip.PersonDay;
			var tour = trip.Tour;

			// person inputs
			var partTimeWorkerFlag = person.IsPartTimeWorker.ToFlag();
			var nonworkingAdultFlag = person.IsNonworkingAdult.ToFlag();
			var universityStudentFlag = person.IsUniversityStudent.ToFlag();
			var retiredAdultFlag = person.IsRetiredAdult.ToFlag();
			var drivingAgeStudentFlag = person.IsDrivingAgeStudent.ToFlag();
			var childAge5Through15Flag = person.IsChildAge5Through15.ToFlag();
			var childUnder5Flag = person.IsChildUnder5.ToFlag();

			// set tour inputs
			var workTourFlag = tour.IsWorkPurpose.ToFlag();
			var notWorkTourFlag = (!tour.IsWorkPurpose).ToFlag();
			var notHomeBasedTourFlag = (!tour.IsHomeBasedTour).ToFlag();

			// set trip inputs
			var originChangeMode = trip.Sequence > 1 && trip.PreviousTrip.DestinationPurpose == Constants.Purpose.CHANGE_MODE;
			var originSchoolFlag = trip.IsSchoolOriginPurpose.ToFlag();
			var originEscortFlag = trip.IsEscortOriginPurpose.ToFlag();
			var originShoppingFlag = trip.IsShoppingOriginPurpose.ToFlag();
			var originPersonalBusinessFlag = trip.IsPersonalBusinessOriginPurpose.ToFlag();
			var originMealFlag = trip.IsMealOriginPurpose.ToFlag();
			var originSocialFlag = trip.IsSocialOriginPurpose.ToFlag();
			var sovOrHovTripFlag = trip.UsesSovOrHovModes.ToFlag();
			var transitTripFlag = trip.IsTransitMode.ToFlag();
			var halfTourFromOriginFlag = trip.IsHalfTourFromOrigin.ToFlag();
			var halfTourFromDestinationFlag = (!trip.IsHalfTourFromOrigin).ToFlag();

			// set remaining inputs
			TimeWindow timeWindow = new TimeWindow();
			if (tour.JointTourSequence > 0) {
				foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = pDay.Tours.Find(t => t.JointTourSequence == tour.JointTourSequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
					}
				}
			}
			else if (trip.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION && tour.FullHalfTour1Sequence > 0) {
				foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = pDay.Tours.Find(t => t.FullHalfTour1Sequence == tour.FullHalfTour1Sequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
					}
				}
			}
			else if (trip.Direction == Constants.TourDirection.DESTINATION_TO_ORIGIN && tour.FullHalfTour2Sequence > 0) {
				foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = pDay.Tours.Find(t => t.FullHalfTour2Sequence == tour.FullHalfTour2Sequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
					}
				}
			}
			else if (tour.ParentTour == null) {
				timeWindow.IncorporateAnotherTimeWindow(personDay.TimeWindow);
			}
			else {
				timeWindow.IncorporateAnotherTimeWindow(tour.ParentTour.TimeWindow);
			}

			timeWindow.SetBusyMinutes(Constants.Time.MIDNIGHT, Constants.Time.MINUTES_IN_A_DAY + 1);

			var remainingToursCount = personDay.HomeBasedTours - personDay.TotalSimulatedTours;
			var tripRemainingInHalfTour = (trip.DestinationParcel != null && trip.DestinationParcel != tour.OriginParcel).ToFlag(); // we don't know exact #

			var previousArrivalTime = trip.Sequence > 1 ? trip.PreviousTrip.ArrivalTime :
				 trip.IsHalfTourFromOrigin ? tour.DestinationDepartureTime : tour.DestinationArrivalTime;


			// mode choice household inputs
			var onePersonHouseholdFlag = household.IsOnePersonHousehold.ToFlag();
			var twoPersonHouseholdFlag = household.IsTwoPersonHousehold.ToFlag();
			var income0To25KFlag = household.Has0To25KIncome.ToFlag();
			var income25To45KFlag = household.Has25To45KIncome.ToFlag();
			var childrenAge5Through15 = householdTotals.ChildrenAge5Through15;
			var nonworkingAdults = householdTotals.NonworkingAdults;
			var retiredAdults = householdTotals.RetiredAdults;
			var noCarsInHouseholdFlag = HouseholdWrapper.GetNoCarsInHouseholdFlag(household.VehiclesAvailable);
			var carsLessThanDriversFlag = household.GetCarsLessThanDriversFlag(household.VehiclesAvailable);

			// mode choice person inputs
			var maleFlag = person.IsMale.ToFlag();
			var ageLessThan35Flag = person.AgeIsLessThan35.ToFlag();

			// mode choice tour inputs
			var parkAndRideTourFlag = tour.IsParkAndRideMode.ToFlag();
			var transitTourFlag = tour.IsTransitMode.ToFlag();
			var schoolBusTourFlag = tour.IsSchoolBusMode.ToFlag();
			var hov3TourFlag = tour.IsHov3Mode.ToFlag();
			var hov2TourFlag = tour.IsHov2Mode.ToFlag();
			var sovTourFlag = tour.IsSovMode.ToFlag();
			var bikeTourFlag = tour.IsBikeMode.ToFlag();
			var walkTourFlag = tour.IsWalkMode.ToFlag();
			var homeBasedWorkTourFlag = (tour.IsHomeBasedTour && tour.IsWorkPurpose).ToFlag();
			var homeBasedSchoolTourFlag = (tour.IsHomeBasedTour && tour.IsSchoolPurpose).ToFlag();
			var homeBasedEscortTourFlag = (tour.IsHomeBasedTour && tour.IsEscortPurpose).ToFlag();
			var homeBasedShoppingTourFlag = (tour.IsHomeBasedTour && tour.IsShoppingPurpose).ToFlag();
			var homeBasedMealTourFlag = (tour.IsHomeBasedTour && tour.IsMealPurpose).ToFlag();
			var homeBasedSocialTourFlag = (tour.IsHomeBasedTour && tour.IsSocialPurpose).ToFlag();

			// mode choice trip inputs
			var originHomeEscortFlag = (trip.IsNoneOrHomePurposeByOrigin && trip.IsEscortPurposeByDestination).ToFlag();
			var originWorkEscortFlag = (trip.IsWorkPurposeByOrigin && trip.IsEscortPurposeByDestination).ToFlag();

			var destinationHomeEscortFlag = (trip.IsNoneOrHomePurposeByDestination && trip.IsEscortPurposeByOrigin).ToFlag();
			var destinationWorkEscortFlag = (trip.IsWorkPurposeByDestination && trip.IsEscortPurposeByOrigin).ToFlag();

			// only trip on first half-tour
			var onlyTripOnFirstHalfFlag = (trip.IsHalfTourFromOrigin && halfTour.SimulatedTrips == 1 && trip.IsToTourOrigin).ToFlag();

			// first trip on first half-tour, not only one
			var firstTripOnFirstHalfFlag = (trip.IsHalfTourFromOrigin && halfTour.SimulatedTrips == 1 && !trip.IsToTourOrigin).ToFlag();

			// last trip first half-tour, not only one
			var lastTripOnFirstHalfFlag = (trip.IsHalfTourFromOrigin && halfTour.SimulatedTrips > 1 && trip.IsToTourOrigin).ToFlag();

			// only trip on second half-tour
			var onlyTripOnSecondHalfFlag = (!trip.IsHalfTourFromOrigin && halfTour.SimulatedTrips == 1 && trip.IsToTourOrigin).ToFlag();

			// first trip on second half-tour, not only one
			var firstTripOnSecondHalfFlag = (!trip.IsHalfTourFromOrigin && halfTour.SimulatedTrips == 1 && !trip.IsToTourOrigin).ToFlag();

			// last trip second half-tour, not only one
			var lastTripOnSecondHalfFlag = (!trip.IsHalfTourFromOrigin && halfTour.SimulatedTrips > 1 && trip.IsToTourOrigin).ToFlag();

			// remaining inputs
			var originParcel =
				 trip.IsHalfTourFromOrigin
					  ? trip.DestinationParcel
					  : trip.OriginParcel;

			var destinationParcel =
				 trip.IsHalfTourFromOrigin
					  ? trip.OriginParcel
					  : trip.DestinationParcel;

			var originMixedDensity = originParcel.MixedUse4Index1();
			var originIntersectionDensity = originParcel.NetIntersectionDensity1();

			// availability
			//var tripModeAvailable = new bool[Constants.Mode.TOTAL_MODES];

			//var isLastTripInTour = (!trip.IsHalfTourFromOrigin && halfTour.SimulatedTrips == 1 && trip.IsToTourOrigin) || (!trip.IsHalfTourFromOrigin && halfTour.SimulatedTrips > 1 && trip.IsToTourOrigin);
			//var frequencyPreviousTripModeIsTourMode =
			//    trip.IsHalfTourFromOrigin
			//        ? tour.HalfTourFromOrigin.Trips.Where(x => x.Sequence < trip.Sequence).Count(x => tour.Mode == x.Mode)
			//      : tour.HalfTourFromOrigin.Trips.Union(tour.HalfTourFromDestination.Trips.Where(x => x.Sequence < trip.Sequence)).Count(x => tour.Mode == x.Mode);

			// if a park and ride tour, only car is available
			//if (tour.Mode == Constants.Mode.PARK_AND_RIDE)
			//{
			//    tripModeAvailable[Constants.Mode.SOV] = tour.Household.VehiclesAvailable > 0 && tour.Person.IsDrivingAge;
			//    tripModeAvailable[Constants.Mode.HOV2] = !tripModeAvailable[Constants.Mode.SOV];
			//}
			// if the last trip of the tour and tour mode not yet used, only the tour mode is available
			//else if (isLastTripInTour && frequencyPreviousTripModeIsTourMode == 0)
			//{
			//    tripModeAvailable[tour.Mode] = true;
			//}
			//else
			//{
			//    // set availability based on tour mode
			//    for (var mode = Constants.Mode.WALK; mode <= tour.Mode; mode++)
			//    {
			//        tripModeAvailable[mode] = true;
			//    }
			//}


			//set time needed from intermediate stop to tour origin
			int minimumTimeForIntermediateStop = 0;
			if (!trip.IsToTourOrigin) {
				var pathmode = Math.Min(tour.Mode, Constants.Mode.HOV3);
				var pathTypeModels =
									 PathTypeModel.Run(
									 trip.Household.RandomUtility,
									 trip.DestinationParcel,
									 trip.Tour.OriginParcel,
									 previousArrivalTime,
									 0,
									 tour.DestinationPurpose,
									 tour.CostCoefficient,
									 tour.TimeCoefficient,
									 tour.Person.IsDrivingAge,
									 tour.Household.VehiclesAvailable,
									 tour.Person.TransitFareDiscountFraction,
									 false,
									 pathmode);

				var pathTypeModel = pathTypeModels.First(x => x.Mode == pathmode);

				minimumTimeForIntermediateStop = (pathTypeModel.Available ? (int) (pathTypeModel.PathTime + 0.5) : 0) + Constants.Time.MINIMUM_ACTIVITY_DURATION;
			}
			//set bounds on departure time window
            int bigPeriodStart = trip.IsHalfTourFromOrigin ? tour.DestinationArrivalBigPeriod.Start : tour.DestinationDepartureBigPeriod.Start;
            int bigPeriodEnd = trip.IsHalfTourFromOrigin ? tour.DestinationArrivalBigPeriod.End : tour.DestinationDepartureBigPeriod.End;
			
            int earliestDepartureTime = Math.Max(bigPeriodStart,
				 trip.IsHalfTourFromOrigin
				 ? tour.EarliestOrignDepartureTime + minimumTimeForIntermediateStop
				 : trip.Sequence == 1
					  ? tour.DestinationArrivalTime + Constants.Time.MINIMUM_ACTIVITY_DURATION
					  : trip.PreviousTrip.ArrivalTime + Constants.Time.MINIMUM_ACTIVITY_DURATION);
			int latestDepartureTime = Math.Min(bigPeriodEnd,
				 !trip.IsHalfTourFromOrigin
				 ? tour.LatestOrignArrivalTime - minimumTimeForIntermediateStop
				 : trip.Sequence == 1
					  ? tour.LatestOrignArrivalTime - (int) (tour.IndicatedTravelTimeFromDestination + 0.5) - Constants.Time.MINIMUM_ACTIVITY_DURATION
					  : trip.PreviousTrip.ArrivalTime - Constants.Time.MINIMUM_ACTIVITY_DURATION);

			//for change mode, make it a very small window
			if (originChangeMode && trip.Sequence > 1) {
				if (trip.IsHalfTourFromOrigin) {
					earliestDepartureTime = latestDepartureTime - Constants.Time.MINIMUM_ACTIVITY_DURATION;
				}
				else {
					latestDepartureTime = earliestDepartureTime + Constants.Time.MINIMUM_ACTIVITY_DURATION;
				}
			}

			//set the impedances for the possible combinations
			HTripModeTime.SetModeTimeImpedances(trip, earliestDepartureTime, latestDepartureTime);

			int smallPeriodCount = DayPeriod.H_SMALL_DAY_PERIOD_TOTAL_TRIP_TIMES;

			int firstModeCoef = 200;

			var componentIndex = 0;


			for (var periodIndex = 0; periodIndex < smallPeriodCount; periodIndex++) {
				var period = DayPeriod.HSmallDayPeriods[periodIndex];

				var periodFraction = timeWindow.TotalAvailableMinutes(period.Start, period.End) / (period.End - period.Start + 1D);
				var duration = (trip.IsHalfTourFromOrigin ? latestDepartureTime - period.Middle : period.Middle - earliestDepartureTime);
				var departurePeriodShift = period.Middle / 60.0;
				var durationShift = duration / 60.0;

				componentIndex = periodIndex;
				choiceProbabilityCalculator.CreateUtilityComponent(componentIndex);
				var periodComponent = choiceProbabilityCalculator.GetUtilityComponent(componentIndex);

				if (periodFraction > 0 && period.End >= earliestDepartureTime && period.Start <= latestDepartureTime) {
					if (trip.IsHalfTourFromOrigin) {
						// outbound "departure" (arrival) period constants
						periodComponent.AddUtilityTerm(11, period.Middle.IsBetween(Constants.Time.THREE_AM, Constants.Time.SIX_AM).ToFlag());
						periodComponent.AddUtilityTerm(12, period.Middle.IsLeftExclusiveBetween(Constants.Time.SIX_AM, Constants.Time.SEVEN_AM).ToFlag());
						periodComponent.AddUtilityTerm(13, period.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_AM, Constants.Time.EIGHT_AM).ToFlag());
						periodComponent.AddUtilityTerm(14, period.Middle.IsLeftExclusiveBetween(Constants.Time.EIGHT_AM, Constants.Time.NINE_AM).ToFlag());
						periodComponent.AddUtilityTerm(15, period.Middle.IsLeftExclusiveBetween(Constants.Time.NINE_AM, Constants.Time.TEN_AM).ToFlag());
						periodComponent.AddUtilityTerm(16, period.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_AM, Constants.Time.ONE_PM).ToFlag());
						periodComponent.AddUtilityTerm(17, period.Middle.IsLeftExclusiveBetween(Constants.Time.ONE_PM, Constants.Time.FOUR_PM).ToFlag());
						periodComponent.AddUtilityTerm(18, period.Middle.IsLeftExclusiveBetween(Constants.Time.FOUR_PM, Constants.Time.SEVEN_PM).ToFlag());
						periodComponent.AddUtilityTerm(19, period.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_PM, Constants.Time.TEN_PM).ToFlag());
						periodComponent.AddUtilityTerm(20, period.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_PM, Constants.Time.MINUTES_IN_A_DAY).ToFlag());
					}
					else {
						// return departure period constants
						periodComponent.AddUtilityTerm(21, period.Middle.IsBetween(Constants.Time.THREE_AM, Constants.Time.SEVEN_AM).ToFlag());
						periodComponent.AddUtilityTerm(22, period.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_AM, Constants.Time.TEN_AM).ToFlag());
						periodComponent.AddUtilityTerm(23, period.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_AM, Constants.Time.ONE_PM).ToFlag());
						periodComponent.AddUtilityTerm(24, period.Middle.IsLeftExclusiveBetween(Constants.Time.ONE_PM, Constants.Time.THREE_PM).ToFlag());
						periodComponent.AddUtilityTerm(124, period.Middle.IsLeftExclusiveBetween(Constants.Time.THREE_PM, Constants.Time.FOUR_PM).ToFlag());
						periodComponent.AddUtilityTerm(25, period.Middle.IsLeftExclusiveBetween(Constants.Time.FOUR_PM, Constants.Time.FIVE_PM).ToFlag());
						periodComponent.AddUtilityTerm(26, period.Middle.IsLeftExclusiveBetween(Constants.Time.FIVE_PM, Constants.Time.SIX_PM).ToFlag());
						periodComponent.AddUtilityTerm(27, period.Middle.IsLeftExclusiveBetween(Constants.Time.SIX_PM, Constants.Time.SEVEN_PM).ToFlag());
						periodComponent.AddUtilityTerm(28, period.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_PM, Constants.Time.NINE_PM).ToFlag());
						periodComponent.AddUtilityTerm(29, period.Middle.IsLeftExclusiveBetween(Constants.Time.NINE_PM, Constants.Time.MIDNIGHT).ToFlag());
						periodComponent.AddUtilityTerm(30, period.Middle.IsLeftExclusiveBetween(Constants.Time.MIDNIGHT, Constants.Time.MINUTES_IN_A_DAY).ToFlag());
					}

					periodComponent.AddUtilityTerm(31, duration.IsRightExclusiveBetween(Constants.Time.ZERO_HOURS, Constants.Time.ONE_HOUR).ToFlag()); // 0 - 1  
					periodComponent.AddUtilityTerm(32, duration.IsRightExclusiveBetween(Constants.Time.ONE_HOUR, Constants.Time.TWO_HOURS).ToFlag()); // 1 - 2  
					periodComponent.AddUtilityTerm(33, duration.IsRightExclusiveBetween(Constants.Time.TWO_HOURS, Constants.Time.THREE_HOURS).ToFlag()); // 2 - 3  
					periodComponent.AddUtilityTerm(34, duration.IsRightExclusiveBetween(Constants.Time.THREE_HOURS, Constants.Time.FIVE_HOURS).ToFlag()); // 3 - 5  
					periodComponent.AddUtilityTerm(35, duration.IsRightExclusiveBetween(Constants.Time.FIVE_HOURS, Constants.Time.SEVEN_HOURS).ToFlag()); // 5 - 7  
					periodComponent.AddUtilityTerm(36, duration.IsRightExclusiveBetween(Constants.Time.SEVEN_HOURS, Constants.Time.NINE_HOURS).ToFlag()); // 7 - 9  
					periodComponent.AddUtilityTerm(37, duration.IsRightExclusiveBetween(Constants.Time.NINE_HOURS, Constants.Time.TWELVE_HOURS).ToFlag()); // 9 - 12 
					periodComponent.AddUtilityTerm(38, duration.IsRightExclusiveBetween(Constants.Time.TWELVE_HOURS, Constants.Time.FOURTEEN_HOURS).ToFlag()); // 12 - 14  
					periodComponent.AddUtilityTerm(39, duration.IsRightExclusiveBetween(Constants.Time.FOURTEEN_HOURS, Constants.Time.EIGHTEEN_HOURS).ToFlag()); // 14 - 18  
					periodComponent.AddUtilityTerm(40, (duration >= Constants.Time.EIGHTEEN_HOURS).ToFlag()); // 18 - 24  

					periodComponent.AddUtilityTerm(41, partTimeWorkerFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(43, nonworkingAdultFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(45, universityStudentFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(47, retiredAdultFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(49, drivingAgeStudentFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(51, childAge5Through15Flag * departurePeriodShift);
					periodComponent.AddUtilityTerm(53, childUnder5Flag * departurePeriodShift);
					periodComponent.AddUtilityTerm(131, workTourFlag * halfTourFromOriginFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(133, workTourFlag * halfTourFromDestinationFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(135, notWorkTourFlag * halfTourFromDestinationFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(137, notHomeBasedTourFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(145, originEscortFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(147, originShoppingFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(149, originMealFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(151, originSocialFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(153, originPersonalBusinessFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(155, originSchoolFlag * departurePeriodShift);
					periodComponent.AddUtilityTerm(42, partTimeWorkerFlag * durationShift);
					periodComponent.AddUtilityTerm(44, nonworkingAdultFlag * durationShift);
					periodComponent.AddUtilityTerm(46, universityStudentFlag * durationShift);
					periodComponent.AddUtilityTerm(48, retiredAdultFlag * durationShift);
					periodComponent.AddUtilityTerm(50, drivingAgeStudentFlag * durationShift);
					periodComponent.AddUtilityTerm(52, childAge5Through15Flag * durationShift);
					periodComponent.AddUtilityTerm(54, childUnder5Flag * durationShift);
					periodComponent.AddUtilityTerm(132, workTourFlag * halfTourFromOriginFlag * durationShift);
					periodComponent.AddUtilityTerm(134, workTourFlag * halfTourFromDestinationFlag * durationShift);
					periodComponent.AddUtilityTerm(136, notWorkTourFlag * halfTourFromDestinationFlag * durationShift);
					periodComponent.AddUtilityTerm(138, notHomeBasedTourFlag * durationShift);
					periodComponent.AddUtilityTerm(146, originEscortFlag * durationShift);
					periodComponent.AddUtilityTerm(148, originShoppingFlag * durationShift);
					periodComponent.AddUtilityTerm(150, originMealFlag * durationShift);
					periodComponent.AddUtilityTerm(152, originSocialFlag * durationShift);
					periodComponent.AddUtilityTerm(154, originPersonalBusinessFlag * durationShift);
					periodComponent.AddUtilityTerm(156, originSchoolFlag * durationShift);
					periodComponent.AddUtilityTerm(92, halfTourFromOriginFlag * Math.Log(periodFraction));
					periodComponent.AddUtilityTerm(92, halfTourFromDestinationFlag * Math.Log(periodFraction));
				}

			}


			for (var mode = Constants.Mode.WALK; mode <= Constants.Mode.SCHOOL_BUS - 1; mode++) {
				componentIndex = smallPeriodCount + mode - 1;

				choiceProbabilityCalculator.CreateUtilityComponent(componentIndex);
				var modeComponent = choiceProbabilityCalculator.GetUtilityComponent(componentIndex);

				switch (mode) {
					case (Constants.Mode.SCHOOL_BUS - 1):
						modeComponent.AddUtilityTerm(firstModeCoef + 18, 1);
						modeComponent.AddUtilityTerm(firstModeCoef + 100, schoolBusTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 102, (schoolBusTourFlag * onlyTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 103, (schoolBusTourFlag * onlyTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 104, (schoolBusTourFlag * firstTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 105, (schoolBusTourFlag * firstTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 106, (schoolBusTourFlag * lastTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 107, (schoolBusTourFlag * lastTripOnSecondHalfFlag));
						break;

					case Constants.Mode.TRANSIT:
						modeComponent.AddUtilityTerm(firstModeCoef + 20, 1);
						modeComponent.AddUtilityTerm(firstModeCoef + 22, carsLessThanDriversFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 100, transitTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 102, (transitTourFlag * onlyTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 103, (transitTourFlag * onlyTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 104, (transitTourFlag * firstTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 105, (transitTourFlag * firstTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 106, (transitTourFlag * lastTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 107, (transitTourFlag * lastTripOnSecondHalfFlag));

						break;
					case Constants.Mode.HOV3:
						modeComponent.AddUtilityTerm(firstModeCoef + 30, 1);
						modeComponent.AddUtilityTerm(firstModeCoef + 32, childrenAge5Through15);
						modeComponent.AddUtilityTerm(firstModeCoef + 34, (nonworkingAdults + retiredAdults));
						modeComponent.AddUtilityTerm(firstModeCoef + 36, onePersonHouseholdFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 37, twoPersonHouseholdFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 41, noCarsInHouseholdFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 100, hov3TourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 102, (hov3TourFlag * onlyTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 103, (hov3TourFlag * onlyTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 104, (hov3TourFlag * firstTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 105, (hov3TourFlag * firstTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 106, (hov3TourFlag * lastTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 107, (hov3TourFlag * lastTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 115, transitTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 116, schoolBusTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 149, homeBasedWorkTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 150, homeBasedSchoolTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 152, homeBasedEscortTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 153, homeBasedShoppingTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 154, homeBasedMealTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 155, homeBasedSocialTourFlag);

						break;
					case Constants.Mode.HOV2:
						modeComponent.AddUtilityTerm(firstModeCoef + 40, 1);
						modeComponent.AddUtilityTerm(firstModeCoef + 32, (childrenAge5Through15 * (1 - homeBasedEscortTourFlag)));
						modeComponent.AddUtilityTerm(firstModeCoef + 34, ((nonworkingAdults + retiredAdults) * (1 - homeBasedEscortTourFlag)));
						modeComponent.AddUtilityTerm(firstModeCoef + 38, onePersonHouseholdFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 41, noCarsInHouseholdFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 100, hov2TourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 102, (hov2TourFlag * onlyTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 103, (hov2TourFlag * onlyTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 104, (hov2TourFlag * firstTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 105, (hov2TourFlag * firstTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 106, (hov2TourFlag * lastTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 107, (hov2TourFlag * lastTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 118, transitTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 119, schoolBusTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 120, hov3TourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 149, homeBasedWorkTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 150, homeBasedSchoolTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 152, homeBasedEscortTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 153, homeBasedShoppingTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 154, homeBasedMealTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 155, homeBasedSocialTourFlag);

						break;
					case Constants.Mode.SOV:
						modeComponent.AddUtilityTerm(firstModeCoef + 50, 1);
						modeComponent.AddUtilityTerm(firstModeCoef + 52, carsLessThanDriversFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 54, income0To25KFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 55, income25To45KFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 59, drivingAgeStudentFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 100, sovTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 102, (sovTourFlag * onlyTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 103, (sovTourFlag * onlyTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 104, (sovTourFlag * firstTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 105, (sovTourFlag * firstTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 106, (sovTourFlag * lastTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 107, (sovTourFlag * lastTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 121, parkAndRideTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 122, transitTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 124, hov3TourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 125, hov2TourFlag);

						break;
					case Constants.Mode.BIKE:
						modeComponent.AddUtilityTerm(firstModeCoef + 60, 1);
						modeComponent.AddUtilityTerm(firstModeCoef + 61, maleFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 62, ageLessThan35Flag);
						modeComponent.AddUtilityTerm(firstModeCoef + 65, originIntersectionDensity);
						modeComponent.AddUtilityTerm(firstModeCoef + 100, bikeTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 102, (bikeTourFlag * onlyTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 103, (bikeTourFlag * onlyTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 104, (bikeTourFlag * firstTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 105, (bikeTourFlag * firstTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 106, (bikeTourFlag * lastTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 107, (bikeTourFlag * lastTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 127, transitTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 128, schoolBusTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 130, hov2TourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 131, sovTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 147, notHomeBasedTourFlag);

						break;
					case Constants.Mode.WALK:
						modeComponent.AddUtilityTerm(firstModeCoef + 72, ageLessThan35Flag);
						modeComponent.AddUtilityTerm(firstModeCoef + 75, originIntersectionDensity);
						modeComponent.AddUtilityTerm(firstModeCoef + 78, originMixedDensity); // origin and destination mixed use measures - geometric avg. - half mile from cell, in 1000s
						modeComponent.AddUtilityTerm(firstModeCoef + 100, walkTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 102, (walkTourFlag * onlyTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 103, (walkTourFlag * onlyTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 104, (walkTourFlag * firstTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 105, (walkTourFlag * firstTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 106, (walkTourFlag * lastTripOnFirstHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 107, (walkTourFlag * lastTripOnSecondHalfFlag));
						modeComponent.AddUtilityTerm(firstModeCoef + 141, homeBasedWorkTourFlag);
						modeComponent.AddUtilityTerm(firstModeCoef + 142, homeBasedSchoolTourFlag);

						break;
				}
			}


			foreach (var modeTime in HTripModeTime.ModeTimes) {
				var period = modeTime.DeparturePeriod;
				var mode = modeTime.Mode;
				var altIndex = modeTime.Index;
				var available = modeTime.Available;

				var alternative = choiceProbabilityCalculator.GetAlternative(altIndex, available, choice != null && choice.Index == altIndex);  //JLB 20130420 

				alternative.Choice = modeTime;   // JLB added 20130420 

				//alternative.AddNestedAlternative(HTripModeTime.TOTAL_TRIP_MODE_TIMES + mode, mode - 1, THETA_PARAMETER);

				if (Global.Configuration.IsInEstimationMode && altIndex == choice.Index) {
					Global.PrintFile.WriteLine("Per Mode {0} {1} {2} Int stop &Travel Time {3} {4} Window {5} {6}",
						 period.Start, period.End, mode, minimumTimeForIntermediateStop,
						 modeTime.Available ? modeTime.ModeLOS.PathTime : -1,
						 modeTime.FeasibleWindow.Start,
						 modeTime.FeasibleWindow.End);

				}

				//if in application mode and combination is not available, can skip the rest
				if (Global.Configuration.IsInEstimationMode || alternative.Available) {
					var modeLOS = modeTime.ModeLOS;


					// period combination utility component
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(period.Index));

					// mode utility component
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(smallPeriodCount + mode - 1));

					//even in estimation mode, do not need the rest of the code if not available
					if (!alternative.Available) {
						continue;
					}
					var amPeriodFlag = period.Middle.IsLeftExclusiveBetween(Constants.Time.SIX_AM, Constants.Time.TEN_AM).ToFlag();
					var middayPeriodFlag = period.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_AM, Constants.Time.THREE_PM).ToFlag();
					var pmPeriodFlag = period.Middle.IsLeftExclusiveBetween(Constants.Time.THREE_PM, Constants.Time.SEVEN_PM).ToFlag();
					var eveningPeriodFlag = (period.Middle > Constants.Time.SEVEN_PM).ToFlag();

					if (mode == Constants.Mode.HOV3) {
						alternative.AddUtilityTerm(firstModeCoef + 161, (destinationWorkEscortFlag * amPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 162, (originWorkEscortFlag * pmPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 163, (originHomeEscortFlag * amPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 164, (originHomeEscortFlag * middayPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 165, (originHomeEscortFlag * pmPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 166, (originHomeEscortFlag * eveningPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 167, (destinationHomeEscortFlag * amPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 168, (destinationHomeEscortFlag * middayPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 169, (destinationHomeEscortFlag * pmPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 170, (destinationHomeEscortFlag * eveningPeriodFlag));
					}
					if (mode == Constants.Mode.HOV2) {
						alternative.AddUtilityTerm(firstModeCoef + 161, (destinationWorkEscortFlag * amPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 162, (originWorkEscortFlag * pmPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 163, (originHomeEscortFlag * amPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 164, (originHomeEscortFlag * middayPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 165, (originHomeEscortFlag * pmPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 166, (originHomeEscortFlag * eveningPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 167, (destinationHomeEscortFlag * amPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 168, (destinationHomeEscortFlag * middayPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 169, (destinationHomeEscortFlag * pmPeriodFlag));
						alternative.AddUtilityTerm(firstModeCoef + 170, (destinationHomeEscortFlag * eveningPeriodFlag));
					}
					alternative.AddUtilityTerm(202, modeLOS.GeneralizedTimeLogsum);
					alternative.AddUtilityTerm(99, halfTourFromOriginFlag * Math.Log((modeLOS.PathTime + 1.0) / (period.End - earliestDepartureTime + 2.0)));
					alternative.AddUtilityTerm(99, halfTourFromDestinationFlag * Math.Log((modeLOS.PathTime + 1.0) / (latestDepartureTime - period.Start + 2.0)));
					alternative.AddUtilityTerm(97, remainingToursCount * 60.0 / (1D + timeWindow.TotalAvailableMinutesAfter(period.End)
						 + timeWindow.TotalAvailableMinutesBefore(period.Start)));
				}
			}
		}
	}
}