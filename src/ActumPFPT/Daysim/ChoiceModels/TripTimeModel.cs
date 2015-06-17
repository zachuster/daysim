// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Threading.Tasks;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Interfaces;
using Daysim.ModelRunners;

namespace Daysim.ChoiceModels {
	public class TripTimeModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "TripTimeModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 156;

		public void Run(ITripWrapper trip) {
			if (trip == null) {
				throw new ArgumentNullException("trip");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.TripTimeModelCoefficients, TripTime.TOTAL_TRIP_TIMES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			TripTime.InitializeTripTimes();
			
			trip.PersonDay.ResetRandom(40 * (2 * trip.Tour.Sequence - 1 + trip.Direction - 1) + 50 + trip.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(trip.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (trip.DestinationParcel == null || trip.OriginParcel == null || trip.Mode <= Constants.Mode.NONE || trip.Mode == Constants.Mode.OTHER) {
					return;
				}

				RunModel(choiceProbabilityCalculator, trip, new TripTime(trip.DepartureTime));

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, trip);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(trip.Household.RandomUtility);

				if (chosenAlternative == null) {
					Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", trip.PersonDay.Id);
					trip.PersonDay.IsValid = false;
					return;
				}

				var choice = (TripTime) chosenAlternative.Choice;
				var departureTime = choice.GetDepartureTime(trip);

				trip.DepartureTime = departureTime;
				if (departureTime >= 1 && departureTime <= Constants.Time.MINUTES_IN_A_DAY) {
					trip.UpdateTripValues();
				}
				else {
					trip.PersonDay.IsValid = false;
				}
			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ITripWrapper trip, TripTime choice = null) {
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
			var timeWindow = tour.IsHomeBasedTour ? personDay.TimeWindow : tour.ParentTour.TimeWindow;
			var impedances = trip.GetTripModeImpedances();
			var remainingToursCount = personDay.HomeBasedTours - personDay.TotalSimulatedTours;
			var tripRemainingInHalfTour = (trip.DestinationParcel != null && trip.DestinationParcel != tour.OriginParcel).ToFlag(); // we don't know exact #

			for (var arrivalPeriodIndex = 1; arrivalPeriodIndex < DayPeriod.SmallDayPeriods.Length; arrivalPeriodIndex++) {
				var arrivalPeriod = DayPeriod.SmallDayPeriods[arrivalPeriodIndex];
				var previousArrivalTime = trip.PreviousTrip.ArrivalTime;

				if (previousArrivalTime < arrivalPeriod.Start || previousArrivalTime > arrivalPeriod.End) {
					continue;
				}
				var arrivalImpedance = impedances[arrivalPeriod.Index]; // moved to here so not reset for every alternative

				foreach (var time in TripTime.Times) {
					var departurePeriod = time.DeparturePeriod; // moved to here so can use travel time
					var departureImpedance = impedances[departurePeriod.Index];

					// change availability check to include travel duration
					var travelDuration = (int) Math.Round(departureImpedance.TravelTime + 0.5);

					// if not the trip home, on a home-based tour, also include fastest time from the destinatinon to home
//					if (trip.Tour.IsHomeBasedTour && trip.DestinationPurpose != Constants.Purpose.NONE_OR_HOME) {
//						var fastestMode = Math.Min(trip.Tour.Mode, Constants.Mode.HOV3);
// 						var pathTypeModel = PathTypeModel.Run(trip.DestinationParcel, trip.Household.ResidenceParcel, departurePeriod.Middle, 0, 
//							   trip.Tour.DestinationPurpose, trip.Tour.CostCoefficient, trip.Tour.TimeCoefficient, 
//								trip.Person.IsDrivingAge, trip.Household.VehiclesAvailable, trip.Tour.Person.TransitFareDiscountFraction, false, fastestMode).First();
//						travelDuration += (int) Math.Round(pathTypeModel.PathTime + 0.5);
//					}

					var bestArrivalTime
						= trip.IsHalfTourFromOrigin
							  ? Math.Max(departurePeriod.End - travelDuration, 1)
							  : Math.Min(departurePeriod.Start + travelDuration, Constants.Time.MINUTES_IN_A_DAY);

					var available =
						originChangeMode
							? arrivalPeriod.Index == time.DeparturePeriod.Index
							: (trip.IsHalfTourFromOrigin && // if change mode, must be in same period
							   arrivalPeriod.Index > departurePeriod.Index &&
							   timeWindow.EntireSpanIsAvailable(bestArrivalTime, arrivalPeriod.Start - 1)) ||
							  (!trip.IsHalfTourFromOrigin &&
							   arrivalPeriod.Index < departurePeriod.Index &&
							   timeWindow.EntireSpanIsAvailable(arrivalPeriod.End, bestArrivalTime - 1)) ||
							  arrivalPeriod.Index == time.DeparturePeriod.Index &&
							  timeWindow.TotalAvailableMinutes(arrivalPeriod.Start, arrivalPeriod.End) > travelDuration;

					var departurePeriodFraction = timeWindow.TotalAvailableMinutes(departurePeriod.Start, departurePeriod.End) / (departurePeriod.End - departurePeriod.Start + 1D);
					var duration = Math.Abs(departurePeriod.Middle - arrivalPeriod.Middle);

					available = available && departurePeriodFraction > 0;

					var alternative = choiceProbabilityCalculator.GetAlternative(time.Index, available, choice != null && choice.Equals(time));

//					if (choice.Equals(tripTime) && !available) {
//						Console.WriteLine(available);
//					}

					if (!alternative.Available) {
						continue;
					}

					alternative.Choice = time;

					var departurePeriodShift = time.DeparturePeriod.Index * (48.0 / DayPeriod.SMALL_DAY_PERIOD_TOTAL_TRIP_TIMES); //adjust shift amount if period lengths change

					if (trip.IsHalfTourFromOrigin) {
						// outbound "departure" (arrival) period constants
						alternative.AddUtilityTerm(11, time.DeparturePeriod.Middle.IsBetween(Constants.Time.THREE_AM, Constants.Time.SIX_AM).ToFlag());
						alternative.AddUtilityTerm(12, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SIX_AM, Constants.Time.SEVEN_AM).ToFlag());
						alternative.AddUtilityTerm(13, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_AM, Constants.Time.EIGHT_AM).ToFlag());
						alternative.AddUtilityTerm(14, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.EIGHT_AM, Constants.Time.NINE_AM).ToFlag());
						alternative.AddUtilityTerm(15, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.NINE_AM, Constants.Time.TEN_AM).ToFlag());
						alternative.AddUtilityTerm(16, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_AM, Constants.Time.ONE_PM).ToFlag());
						alternative.AddUtilityTerm(17, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.ONE_PM, Constants.Time.FOUR_PM).ToFlag());
						alternative.AddUtilityTerm(18, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.FOUR_PM, Constants.Time.SEVEN_PM).ToFlag());
						alternative.AddUtilityTerm(19, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_PM, Constants.Time.TEN_PM).ToFlag());
						alternative.AddUtilityTerm(20, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_PM, Constants.Time.MINUTES_IN_A_DAY).ToFlag());
					}
					else {
						// return departure period constants
						alternative.AddUtilityTerm(21, time.DeparturePeriod.Middle.IsBetween(Constants.Time.THREE_AM, Constants.Time.SEVEN_AM).ToFlag());
						alternative.AddUtilityTerm(22, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_AM, Constants.Time.TEN_AM).ToFlag());
						alternative.AddUtilityTerm(23, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_AM, Constants.Time.ONE_PM).ToFlag());
						alternative.AddUtilityTerm(24, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.ONE_PM, Constants.Time.THREE_PM).ToFlag());
						alternative.AddUtilityTerm(124, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.THREE_PM, Constants.Time.FOUR_PM).ToFlag());
						alternative.AddUtilityTerm(25, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.FOUR_PM, Constants.Time.FIVE_PM).ToFlag());
						alternative.AddUtilityTerm(26, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.FIVE_PM, Constants.Time.SIX_PM).ToFlag());
						alternative.AddUtilityTerm(27, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SIX_PM, Constants.Time.SEVEN_PM).ToFlag());
						alternative.AddUtilityTerm(28, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_PM, Constants.Time.NINE_PM).ToFlag());
						alternative.AddUtilityTerm(29, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.NINE_PM, Constants.Time.MIDNIGHT).ToFlag());
						alternative.AddUtilityTerm(30, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.MIDNIGHT, Constants.Time.MINUTES_IN_A_DAY).ToFlag());
					}

					alternative.AddUtilityTerm(31, duration.IsRightExclusiveBetween(Constants.Time.ZERO_HOURS, Constants.Time.ONE_HOUR).ToFlag()); // 0 - 1  
					alternative.AddUtilityTerm(32, duration.IsRightExclusiveBetween(Constants.Time.ONE_HOUR, Constants.Time.TWO_HOURS).ToFlag()); // 1 - 2  
					alternative.AddUtilityTerm(33, duration.IsRightExclusiveBetween(Constants.Time.TWO_HOURS, Constants.Time.THREE_HOURS).ToFlag()); // 2 - 3  
					alternative.AddUtilityTerm(34, duration.IsRightExclusiveBetween(Constants.Time.THREE_HOURS, Constants.Time.FIVE_HOURS).ToFlag()); // 3 - 5  
					alternative.AddUtilityTerm(35, duration.IsRightExclusiveBetween(Constants.Time.FIVE_HOURS, Constants.Time.SEVEN_HOURS).ToFlag()); // 5 - 7  
					alternative.AddUtilityTerm(36, duration.IsRightExclusiveBetween(Constants.Time.SEVEN_HOURS, Constants.Time.NINE_HOURS).ToFlag()); // 7 - 9  
					alternative.AddUtilityTerm(37, duration.IsRightExclusiveBetween(Constants.Time.NINE_HOURS, Constants.Time.TWELVE_HOURS).ToFlag()); // 9 - 12 
					alternative.AddUtilityTerm(38, duration.IsRightExclusiveBetween(Constants.Time.TWELVE_HOURS, Constants.Time.FOURTEEN_HOURS).ToFlag()); // 12 - 14  
					alternative.AddUtilityTerm(39, duration.IsRightExclusiveBetween(Constants.Time.FOURTEEN_HOURS, Constants.Time.EIGHTEEN_HOURS).ToFlag()); // 14 - 18  
					alternative.AddUtilityTerm(40, (duration >= Constants.Time.EIGHTEEN_HOURS).ToFlag()); // 18 - 24  

					alternative.AddUtilityTerm(41, partTimeWorkerFlag * departurePeriodShift);
					alternative.AddUtilityTerm(43, nonworkingAdultFlag * departurePeriodShift);
					alternative.AddUtilityTerm(45, universityStudentFlag * departurePeriodShift);
					alternative.AddUtilityTerm(47, retiredAdultFlag * departurePeriodShift);
					alternative.AddUtilityTerm(49, drivingAgeStudentFlag * departurePeriodShift);
					alternative.AddUtilityTerm(51, childAge5Through15Flag * departurePeriodShift);
					alternative.AddUtilityTerm(53, childUnder5Flag * departurePeriodShift);
					alternative.AddUtilityTerm(131, workTourFlag * halfTourFromOriginFlag * departurePeriodShift);
					alternative.AddUtilityTerm(133, workTourFlag * halfTourFromDestinationFlag * departurePeriodShift);
					alternative.AddUtilityTerm(135, notWorkTourFlag * halfTourFromDestinationFlag * departurePeriodShift);
					alternative.AddUtilityTerm(137, notHomeBasedTourFlag * departurePeriodShift);
					alternative.AddUtilityTerm(145, originEscortFlag * departurePeriodShift);
					alternative.AddUtilityTerm(147, originShoppingFlag * departurePeriodShift);
					alternative.AddUtilityTerm(149, originMealFlag * departurePeriodShift);
					alternative.AddUtilityTerm(151, originSocialFlag * departurePeriodShift);
					alternative.AddUtilityTerm(153, originPersonalBusinessFlag * departurePeriodShift);
					alternative.AddUtilityTerm(155, originSchoolFlag * departurePeriodShift);
					alternative.AddUtilityTerm(42, partTimeWorkerFlag * departurePeriodShift);
					alternative.AddUtilityTerm(44, nonworkingAdultFlag * departurePeriodShift);
					alternative.AddUtilityTerm(46, universityStudentFlag * departurePeriodShift);
					alternative.AddUtilityTerm(48, retiredAdultFlag * departurePeriodShift);
					alternative.AddUtilityTerm(50, drivingAgeStudentFlag * departurePeriodShift);
					alternative.AddUtilityTerm(52, childAge5Through15Flag * departurePeriodShift);
					alternative.AddUtilityTerm(54, childUnder5Flag * departurePeriodShift);
					alternative.AddUtilityTerm(132, workTourFlag * halfTourFromOriginFlag * departurePeriodShift);
					alternative.AddUtilityTerm(134, workTourFlag * halfTourFromDestinationFlag * departurePeriodShift);
					alternative.AddUtilityTerm(136, notWorkTourFlag * halfTourFromDestinationFlag * departurePeriodShift);
					alternative.AddUtilityTerm(138, notHomeBasedTourFlag * departurePeriodShift);
					alternative.AddUtilityTerm(146, originEscortFlag * departurePeriodShift);
					alternative.AddUtilityTerm(148, originShoppingFlag * departurePeriodShift);
					alternative.AddUtilityTerm(150, originMealFlag * departurePeriodShift);
					alternative.AddUtilityTerm(152, originSocialFlag * departurePeriodShift);
					alternative.AddUtilityTerm(154, originPersonalBusinessFlag * departurePeriodShift);
					alternative.AddUtilityTerm(156, originSchoolFlag * departurePeriodShift);
					alternative.AddUtilityTerm(86, sovOrHovTripFlag * Math.Max(departureImpedance.GeneralizedTime, 0) * tour.TimeCoefficient);
					alternative.AddUtilityTerm(88, transitTripFlag * Math.Max(departureImpedance.GeneralizedTime, 0) * tour.TimeCoefficient);
					alternative.AddUtilityTerm(89, transitTripFlag * (departureImpedance.GeneralizedTime < 0).ToFlag());
					alternative.AddUtilityTerm(92, halfTourFromOriginFlag * Math.Log(departurePeriodFraction));
					alternative.AddUtilityTerm(92, halfTourFromDestinationFlag * Math.Log(departurePeriodFraction));
					alternative.AddUtilityTerm(99, tripRemainingInHalfTour / (1D + halfTourFromOriginFlag * departureImpedance.AdjacentMinutesBefore + halfTourFromDestinationFlag * departureImpedance.AdjacentMinutesAfter));
					alternative.AddUtilityTerm(97, remainingToursCount / (1D + halfTourFromOriginFlag * (arrivalImpedance.TotalMinutesAfter + departureImpedance.TotalMinutesBefore) + halfTourFromDestinationFlag * (arrivalImpedance.TotalMinutesBefore + departureImpedance.TotalMinutesAfter)));
					alternative.AddUtilityTerm(98, remainingToursCount / (1D + halfTourFromOriginFlag * Math.Max(arrivalImpedance.MaxMinutesBefore, departureImpedance.MaxMinutesBefore) + halfTourFromDestinationFlag * Math.Max(arrivalImpedance.MaxMinutesBefore, departureImpedance.MaxMinutesAfter)));
				}
			}
		}
	}
}