﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Threading.Tasks;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using Daysim.DomainModels.Actum;

namespace Daysim.ChoiceModels {
	public class ActumTripTimeModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumTripTimeModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 172;
		
		public void Run(ActumHouseholdDayWrapper householdDay, ActumTripWrapper trip) {
			if (trip == null) {
				throw new ArgumentNullException("trip");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.TripTimeModelCoefficients, HTripTime.TOTAL_TRIP_TIMES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			HTripTime.InitializeTripTimes();

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

				RunModel(choiceProbabilityCalculator, householdDay, trip, new HTripTime(trip.DepartureTime));

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, householdDay, trip);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(trip.Household.RandomUtility);

				if (chosenAlternative == null) {
					Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", trip.PersonDay.Id);
					if (Global.Configuration.IsInEstimationMode) {
						trip.PersonDay.IsValid = false;
					}
					return;
				}

				var choice = (HTripTime) chosenAlternative.Choice;

				trip.DepartureTime = choice.GetRandomFeasibleMinute(trip, choice);

			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumHouseholdDayWrapper householdDay, ActumTripWrapper trip, HTripTime choice = null) {
			
			if (householdDay.Household.Id == 80066 && trip.Person.Sequence == 1 && trip.Tour.Sequence == 2
				&& trip.Direction == 2 && trip.Sequence == 1) {
				bool testbreak = true;
			}

			var person = (ActumPersonWrapper) trip.Person;
			var personDay = (ActumPersonDayWrapper) trip.PersonDay;
			var tour = (ActumTourWrapper) trip.Tour;

			// person inputs + househol_PFPT
			var partTimeWorkerFlag = person.IsPartTimeWorker.ToFlag();
			var nonworkingAdultFlag = person.IsNonworkingAdult.ToFlag();
			var universityStudentFlag = person.IsUniversityStudent.ToFlag();
			var retiredAdultFlag = person.IsRetiredAdult.ToFlag();
			//var drivingAgeStudentFlag = person.IsDrivingAgeStudent.ToFlag(); // excluded by GV
			var childAge5Through15Flag = person.IsChildAge5Through15.ToFlag();
			var childUnder5Flag = person.IsChildUnder5.ToFlag();
			var femaleFlag = person.IsFemale.ToFlag();
			var fullTimeWorkerFlag = person.IsFulltimeWorker.ToFlag();
			var primaryFamilyTimeFlag = householdDay.PrimaryPriorityTimeFlag;
			
			// set tour inputs
			var workTourFlag = tour.IsWorkPurpose.ToFlag();
			var schoolTourFlag = tour.IsSchoolPurpose.ToFlag();
			var businessTourFlag = tour.IsBusinessPurpose.ToFlag();
			var escortTourFlag = tour.IsEscortPurpose.ToFlag();
			var personalBusinessTourFlag = tour.IsPersonalBusinessPurpose.ToFlag();
			var shoppingTourFlag = tour.IsShoppingPurpose.ToFlag();
			var socialTourFlag = tour.IsSocialPurpose.ToFlag();
			var notWorkSchoolTourFlag = 1- workTourFlag - schoolTourFlag;
			var notWorkTourFlag = (!tour.IsWorkPurpose).ToFlag();
			var notHomeBasedTourFlag = (!tour.IsHomeBasedTour).ToFlag();
			var jointTourFlag = (tour.JointTourSequence > 0) ? 1 : 0;
			var partialHalfTourFlag = (trip.IsHalfTourFromOrigin ? tour.PartialHalfTour1Sequence > 0 : tour.PartialHalfTour2Sequence > 0) ? 1 : 0;
			var fullHalfTourFlag = (trip.IsHalfTourFromOrigin ? tour.FullHalfTour1Sequence > 0 : tour.FullHalfTour2Sequence > 0) ? 1 : 0;

			// set trip inputs - travel purpose
			var originChangeMode = trip.Sequence > 1 && trip.PreviousTrip.DestinationPurpose == Constants.Purpose.CHANGE_MODE;
			var originSchoolFlag = trip.IsSchoolOriginPurpose.ToFlag();
			var originEscortFlag = trip.IsEscortOriginPurpose.ToFlag();
			var originShoppingFlag = trip.IsShoppingOriginPurpose.ToFlag();
			var originPersonalBusinessFlag = trip.IsPersonalBusinessOriginPurpose.ToFlag();
			var originMealFlag = trip.IsMealOriginPurpose.ToFlag();
			var originSocialFlag = trip.IsSocialOriginPurpose.ToFlag();
			var originBusinessFlag = trip.IsBusinessOriginPurpose.ToFlag();
			
			// set trip inputs - travel modes
			var sovOrHovTripFlag = trip.UsesSovOrHovModes.ToFlag();
			var bikeTripFlag = trip.IsBikeMode.ToFlag();
			var walkTripFlag = trip.IsWalkMode.ToFlag();
			var transitTripFlag = trip.IsTransitMode.ToFlag();
			var carDriverAloneFlag = trip.IsSovMode.ToFlag();
			var carDriverNotAloneFlag = trip.IsHov2Mode.ToFlag();
			var carPassengerFlag = trip.IsHov3Mode.ToFlag();

			var halfTourFromOriginFlag = trip.IsHalfTourFromOrigin.ToFlag();
			var halfTourFromDestinationFlag = (!trip.IsHalfTourFromOrigin).ToFlag();

			// set remaining inputs
			// set remaining inputs
			TimeWindow timeWindow = new TimeWindow();
			if (tour.JointTourSequence > 0) {
				foreach (ActumPersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = (ActumTourWrapper) pDay.Tours.Find(t => t.JointTourSequence == tour.JointTourSequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
					}
				}
			}
			else if (trip.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION && tour.FullHalfTour1Sequence > 0) {
				foreach (ActumPersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = (ActumTourWrapper) pDay.Tours.Find(t => t.FullHalfTour1Sequence == tour.FullHalfTour1Sequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
					}
				}
			}
			else if (trip.Direction == Constants.TourDirection.DESTINATION_TO_ORIGIN && tour.FullHalfTour2Sequence > 0) {
				foreach (ActumPersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = (ActumTourWrapper) pDay.Tours.Find(t => t.FullHalfTour2Sequence == tour.FullHalfTour2Sequence);
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

			//set the availability and impedances for the periods
			HTripTime.SetTimeImpedances(trip);

			var remainingToursCount = personDay.HomeBasedTours - personDay.TotalSimulatedTours;
			var tripRemainingInHalfTour = (trip.DestinationParcel != null && trip.DestinationParcel != tour.OriginParcel).ToFlag(); // we don't know exact #

			var previousArrivalTime = trip.IsHalfTourFromOrigin
					 ? (trip.Sequence == 1 ? tour.DestinationDepartureTime : trip.PreviousTrip.ArrivalTime)
					 : (trip.Sequence == 1 ? tour.DestinationArrivalTime : trip.PreviousTrip.ArrivalTime);

			var previousArrivalPeriod = new HTripTime(previousArrivalTime).DeparturePeriod;

			foreach (var time in HTripTime.Times) {
				var period = time.DeparturePeriod;

				var departurePeriodFraction = timeWindow.TotalAvailableMinutes(period.Start, period.End) / (period.End - period.Start + 1D);

				var departureShiftHours = period.Middle / 60.0;
				var durationShiftMinutes = Math.Abs(period.Middle - previousArrivalPeriod.Middle);
				var durationShiftHours = durationShiftMinutes / 60.0;


				
				var available = time.Available && departurePeriodFraction > 0;

				var alternative = choiceProbabilityCalculator.GetAlternative(time.Index, available, choice != null && choice.Equals(time));


				if (!alternative.Available) {
					continue;
				}

				alternative.Choice = time;

				var indicatedTravelTime = (int) time.ModeLOS.PathTime;
				var indicatedArrivalTime = trip.IsHalfTourFromOrigin
					 ? Math.Max(1, period.Middle - indicatedTravelTime)
					 : Math.Min(1440, period.Middle + indicatedTravelTime);

				var totalWindowRemaining = trip.IsHalfTourFromOrigin
					 ? timeWindow.TotalAvailableMinutesBefore(indicatedArrivalTime) + timeWindow.TotalAvailableMinutesAfter(previousArrivalTime)
					 : timeWindow.TotalAvailableMinutesAfter(indicatedArrivalTime) + timeWindow.TotalAvailableMinutesBefore(previousArrivalTime);

				var maxWindowRemaining = trip.IsHalfTourFromOrigin
					 ? timeWindow.MaxAvailableMinutesBefore(indicatedArrivalTime) + timeWindow.MaxAvailableMinutesAfter(previousArrivalTime)
					 : timeWindow.MaxAvailableMinutesAfter(indicatedArrivalTime) + timeWindow.MaxAvailableMinutesBefore(previousArrivalTime);

				if (trip.IsHalfTourFromOrigin) {
					// outbound "departure" (arrival) period constants
					alternative.AddUtilityTerm(11, time.DeparturePeriod.Middle.IsBetween(Constants.Time.THREE_AM, Constants.Time.SIX_AM).ToFlag());
					alternative.AddUtilityTerm(12, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SIX_AM, Constants.Time.SEVEN_AM).ToFlag());
					alternative.AddUtilityTerm(13, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_AM, Constants.Time.EIGHT_AM).ToFlag());
					alternative.AddUtilityTerm(14, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.EIGHT_AM, Constants.Time.NINE_AM).ToFlag());
					alternative.AddUtilityTerm(15, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.NINE_AM, Constants.Time.TEN_AM).ToFlag());
					alternative.AddUtilityTerm(16, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_AM, Constants.Time.ONE_PM).ToFlag());
					 
					//alternative.AddUtilityTerm(17, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.ONE_PM, Constants.Time.FOUR_PM).ToFlag());
					//alternative.AddUtilityTerm(18, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.FOUR_PM, Constants.Time.SEVEN_PM).ToFlag());
					//GV changed to 3pm 
					alternative.AddUtilityTerm(17, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.ONE_PM, Constants.Time.THREE_PM).ToFlag());
					alternative.AddUtilityTerm(18, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.THREE_PM, Constants.Time.SIX_PM).ToFlag());
					alternative.AddUtilityTerm(19, time.DeparturePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SIX_PM, Constants.Time.TEN_PM).ToFlag());
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

				alternative.AddUtilityTerm(31, durationShiftMinutes.IsRightExclusiveBetween(Constants.Time.ZERO_HOURS, Constants.Time.ONE_HOUR).ToFlag()); // 0 - 1  
				alternative.AddUtilityTerm(32, durationShiftMinutes.IsRightExclusiveBetween(Constants.Time.ONE_HOUR, Constants.Time.TWO_HOURS).ToFlag()); // 1 - 2  
				alternative.AddUtilityTerm(33, durationShiftMinutes.IsRightExclusiveBetween(Constants.Time.TWO_HOURS, Constants.Time.THREE_HOURS).ToFlag()); // 2 - 3  
				alternative.AddUtilityTerm(34, durationShiftMinutes.IsRightExclusiveBetween(Constants.Time.THREE_HOURS, Constants.Time.FIVE_HOURS).ToFlag()); // 3 - 5  
				alternative.AddUtilityTerm(35, durationShiftMinutes.IsRightExclusiveBetween(Constants.Time.FIVE_HOURS, Constants.Time.SEVEN_HOURS).ToFlag()); // 5 - 7  
				alternative.AddUtilityTerm(36, durationShiftMinutes.IsRightExclusiveBetween(Constants.Time.SEVEN_HOURS, Constants.Time.NINE_HOURS).ToFlag()); // 7 - 9  
				alternative.AddUtilityTerm(37, durationShiftMinutes.IsRightExclusiveBetween(Constants.Time.NINE_HOURS, Constants.Time.TWELVE_HOURS).ToFlag()); // 9 - 12 
				alternative.AddUtilityTerm(38, durationShiftMinutes.IsRightExclusiveBetween(Constants.Time.TWELVE_HOURS, Constants.Time.FOURTEEN_HOURS).ToFlag()); // 12 - 14  
				alternative.AddUtilityTerm(39, durationShiftMinutes.IsRightExclusiveBetween(Constants.Time.FOURTEEN_HOURS, Constants.Time.EIGHTEEN_HOURS).ToFlag()); // 14 - 18  
				alternative.AddUtilityTerm(40, (durationShiftMinutes >= Constants.Time.EIGHTEEN_HOURS).ToFlag()); // 18 - 24  

				alternative.AddUtilityTerm(41, partTimeWorkerFlag * departureShiftHours);
				alternative.AddUtilityTerm(43, nonworkingAdultFlag * departureShiftHours);
				alternative.AddUtilityTerm(45, universityStudentFlag * departureShiftHours);
				alternative.AddUtilityTerm(47, retiredAdultFlag * departureShiftHours);
				alternative.AddUtilityTerm(49, femaleFlag * departureShiftHours);
				alternative.AddUtilityTerm(51, childAge5Through15Flag * departureShiftHours);
				alternative.AddUtilityTerm(53, childUnder5Flag * departureShiftHours);
				alternative.AddUtilityTerm(61, jointTourFlag * departureShiftHours);
				//alternative.AddUtilityTerm(63, partialHalfTourFlag * departureShiftHours);
				//alternative.AddUtilityTerm(65, fullHalfTourFlag * departureShiftHours);
				alternative.AddUtilityTerm(67, primaryFamilyTimeFlag * departureShiftHours);
					
				//alternative.AddUtilityTerm(131, workTourFlag * halfTourFromOriginFlag * departureShiftHours);
				//alternative.AddUtilityTerm(133, workTourFlag * halfTourFromDestinationFlag * departureShiftHours);
				//alternative.AddUtilityTerm(135, notWorkTourFlag * halfTourFromDestinationFlag * departureShiftHours);
				//alternative.AddUtilityTerm(137, notHomeBasedTourFlag * departureShiftHours);
				alternative.AddUtilityTerm(145, originEscortFlag * departureShiftHours);
				alternative.AddUtilityTerm(147, originShoppingFlag * departureShiftHours);
				alternative.AddUtilityTerm(149, originBusinessFlag * departureShiftHours);
				alternative.AddUtilityTerm(151, originSocialFlag * departureShiftHours);
				alternative.AddUtilityTerm(153, originPersonalBusinessFlag * departureShiftHours);
				alternative.AddUtilityTerm(155, originSchoolFlag * departureShiftHours);

				alternative.AddUtilityTerm(42, partTimeWorkerFlag * durationShiftHours);
				alternative.AddUtilityTerm(44, nonworkingAdultFlag * durationShiftHours);
				alternative.AddUtilityTerm(46, universityStudentFlag * durationShiftHours);
				alternative.AddUtilityTerm(48, retiredAdultFlag * durationShiftHours);
				alternative.AddUtilityTerm(50, femaleFlag * durationShiftHours);
				alternative.AddUtilityTerm(52, childAge5Through15Flag * durationShiftHours);
				alternative.AddUtilityTerm(54, childUnder5Flag * durationShiftHours);
				//alternative.AddUtilityTerm(62, jointTourFlag * durationShiftHours);
				//alternative.AddUtilityTerm(64, partialHalfTourFlag * durationShiftHours);
				//alternative.AddUtilityTerm(66, fullHalfTourFlag * durationShiftHours);
				alternative.AddUtilityTerm(68, primaryFamilyTimeFlag * durationShiftHours);
				
				alternative.AddUtilityTerm(132, workTourFlag * halfTourFromOriginFlag * durationShiftHours);
				alternative.AddUtilityTerm(134, workTourFlag * halfTourFromDestinationFlag * durationShiftHours);
				alternative.AddUtilityTerm(136, notWorkTourFlag * halfTourFromDestinationFlag * durationShiftHours);
				alternative.AddUtilityTerm(138, notHomeBasedTourFlag * durationShiftHours);
				alternative.AddUtilityTerm(146, originEscortFlag * durationShiftHours);
				alternative.AddUtilityTerm(148, originShoppingFlag * durationShiftHours);
				alternative.AddUtilityTerm(150, originBusinessFlag * durationShiftHours); 
				alternative.AddUtilityTerm(152, originSocialFlag * durationShiftHours);
				alternative.AddUtilityTerm(154, originPersonalBusinessFlag * durationShiftHours);
				alternative.AddUtilityTerm(156, originSchoolFlag * durationShiftHours);

				alternative.AddUtilityTerm(158, workTourFlag * halfTourFromOriginFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(159, workTourFlag * halfTourFromDestinationFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(160, schoolTourFlag * halfTourFromOriginFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(161, schoolTourFlag * halfTourFromDestinationFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(162, businessTourFlag * halfTourFromOriginFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(163, businessTourFlag * halfTourFromDestinationFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(164, escortTourFlag * halfTourFromOriginFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(165, escortTourFlag * halfTourFromDestinationFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(166, personalBusinessTourFlag * halfTourFromOriginFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(167, personalBusinessTourFlag * halfTourFromDestinationFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(168, shoppingTourFlag * halfTourFromOriginFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(169, shoppingTourFlag * halfTourFromDestinationFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(170, socialTourFlag * halfTourFromOriginFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);
				alternative.AddUtilityTerm(171, socialTourFlag * halfTourFromDestinationFlag * (trip.Sequence==1).ToFlag() * durationShiftHours);

				//alternative.AddUtilityTerm(172, workTourFlag * halfTourFromOriginFlag * (trip.Sequence==1).ToFlag() * departureShiftHours);


				alternative.AddUtilityTerm(86, sovOrHovTripFlag * Math.Max(time.ModeLOS.GeneralizedTimeLogsum, 0) * tour.TimeCoefficient);
				//alternative.AddUtilityTerm(87, sovOrHovTripFlag * notWorkSchoolTourFlag * Math.Max(time.ModeLOS.GeneralizedTimeLogsum, 0) * tour.TimeCoefficient);
				//alternative.AddUtilityTerm(88, transitTripFlag * Math.Max(time.ModeLOS.GeneralizedTimeLogsum, 0) * tour.TimeCoefficient);
				//alternative.AddUtilityTerm(89, sovOrHovTripFlag * notWorkSchoolTourFlag * (trip.Sequence==1).ToFlag() * Math.Max(time.ModeLOS.GeneralizedTimeLogsum, 0) * tour.TimeCoefficient);
				
				alternative.AddUtilityTerm(92, Math.Log(departurePeriodFraction));
				//alternative.AddUtilityTerm(92, halfTourFromDestinationFlag * Math.Log(departurePeriodFraction));
				alternative.AddUtilityTerm(99, tripRemainingInHalfTour / (Math.Max(1D, Math.Abs(trip.ArrivalTimeLimit - period.Middle))));
				//alternative.AddUtilityTerm(97, remainingToursCount / (Math.Max(1D, totalWindowRemaining)));
				alternative.AddUtilityTerm(98, 1000 * remainingToursCount / (Math.Max(1D, maxWindowRemaining))); 
				
			}
		}
	}
}