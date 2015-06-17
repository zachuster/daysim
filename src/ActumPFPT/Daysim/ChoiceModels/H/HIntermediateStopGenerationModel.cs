// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Daysim.Framework.Roster;
using Daysim.Interfaces;

namespace Daysim.ChoiceModels {
	public static class HIntermediateStopGenerationModel {
		private const string CHOICE_MODEL_NAME = "HIntermediateStopGenerationModel";
		private const int TOTAL_ALTERNATIVES = 10;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 250;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		private static readonly object _lock = new object();

		private static void Initialize() {
			lock(_lock)
			{
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null)
				{
					return;
				}

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME,
				                             Global.GetInputPath(Global.Configuration.IntermediateStopGenerationModelCoefficients), TOTAL_ALTERNATIVES,
				                             TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static int Run(ITripWrapper trip, IHouseholdDayWrapper householdDay, int choice = Constants.Purpose.NONE_OR_HOME) {
			if (trip == null) {
				throw new ArgumentNullException("trip");
			}

			Initialize();
			
			trip.PersonDay.ResetRandom(40 * (2 * trip.Tour.Sequence - 1 + trip.Direction - 1) + 20 + trip.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return choice;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(trip.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (trip.OriginParcel == null) {
					return Constants.DEFAULT_VALUE;
				}
				RunModel(choiceProbabilityCalculator, trip, householdDay, choice);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, trip, householdDay);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(trip.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;
			}
			
			return choice;
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ITripWrapper trip, IHouseholdDayWrapper householdDay, int choice = Constants.DEFAULT_VALUE) {
			var household = trip.Household;
			var person = trip.Person;
			var personDay = trip.PersonDay;
			var tour = trip.Tour;
			var halfTour = trip.HalfTour;
			var personDays = householdDay.PersonDays;

			var isJointTour = tour.JointTourSequence > 0 ? 1: 0 ;
			var isIndividualTour = isJointTour==1 ? 0 : 1;
			var destinationParcel = tour.DestinationParcel;
			var fullJointHalfTour = ((trip.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION && tour.FullHalfTour1Sequence > 0)
					|| (trip.Direction == Constants.TourDirection.DESTINATION_TO_ORIGIN && tour.FullHalfTour2Sequence > 0)).ToFlag();

			//destination parcel variables
			var foodBuffer2 = 0.0;
			var totEmpBuffer2 = 0.0;
			var retailBuffer2 = 0.0;

			if(destinationParcel != null){
			foodBuffer2 = Math.Log(1+destinationParcel.EmploymentFoodBuffer2);
			totEmpBuffer2 = Math.Log(1+destinationParcel.EmploymentTotalBuffer2);
			retailBuffer2 = Math.Log(1+destinationParcel.EmploymentRetailBuffer2);
			}
			
			var carOwnership = person.CarOwnershipSegment;
				
			// household inputs
			var onePersonHouseholdFlag = household.IsOnePersonHousehold.ToFlag();
			//var householdInc75KP = household.Has75KPlusIncome;

			var votALSegment = tour.VotALSegment;
			var transitAccessSegment = household.ResidenceParcel.TransitAccessSegment();

			var totalAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId] 
				[Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];
			
			var homeFoodBuffer2 = Math.Log(1+household.ResidenceParcel.EmploymentFoodBuffer2);
			var homeTotEmpBuffer2 = Math.Log(1+household.ResidenceParcel.EmploymentTotalBuffer2);
			var homeRetailBuffer2 = Math.Log(1+household.ResidenceParcel.EmploymentRetailBuffer2);

			// person-day inputs
			var homeBasedTours = personDay.HomeBasedTours;
			var simulatedToursFlag = personDay.HasSimulatedTours.ToFlag();
			var simulatedWorkStops = personDay.SimulatedWorkStops;
			var simulatedWorkStopsFlag = personDay.HasSimulatedWorkStops.ToFlag();
			var simulatedSchoolStops = personDay.SimulatedSchoolStops;
			var simulatedEscortStops = personDay.SimulatedEscortStops;
			var simulatedPersonalBusinessStops = personDay.SimulatedPersonalBusinessStops;
			var simulatedShoppingStops = personDay.SimulatedShoppingStops;
			var simulatedMealStops = personDay.SimulatedMealStops;
			var simulatedSocialStops = personDay.SimulatedSocialStops;
			var simulatedRecreationStops = personDay.SimulatedRecreationStops;
			var simulatedMedicalStops = personDay.SimulatedMedicalStops;

			// tour inputs
			var hov2TourFlag = tour.IsHov2Mode.ToFlag();
			var hov3TourFlag = tour.IsHov3Mode.ToFlag();
			var transitTourFlag = tour.IsTransitMode.ToFlag();
			var notHomeBasedTourFlag = (!tour.IsHomeBasedTour).ToFlag();
			var workTourFlag = tour.IsWorkPurpose.ToFlag();
			var personalBusinessOrMedicalTourFlag = tour.IsPersonalBusinessOrMedicalPurpose.ToFlag();
			var socialTourFlag = tour.IsSocialPurpose.ToFlag();
			var socialOrRecreationTourFlag = tour.IsSocialOrRecreationPurpose.ToFlag();
			var schoolTourFlag = tour.IsSchoolPurpose.ToFlag();
			var escortTourFlag = tour.IsEscortPurpose.ToFlag();
			var shoppingTourFlag = tour.IsShoppingPurpose.ToFlag();
			var mealTourFlag = tour.IsMealPurpose.ToFlag();

			// trip inputs
			var oneSimulatedTripFlag = halfTour.OneSimulatedTripFlag;
			var twoSimulatedTripsFlag = halfTour.TwoSimulatedTripsFlag;
			var threeSimulatedTripsFlag = halfTour.ThreeSimulatedTripsFlag;
			var fourSimulatedTripsFlag = halfTour.FourSimulatedTripsFlag;
			var fiveSimulatedTripsFlag = halfTour.FiveSimulatedTripsFlag;
			var halfTourFromOriginFlag = trip.IsHalfTourFromOrigin.ToFlag();
			var halfTourFromDestinationFlag = (!trip.IsHalfTourFromOrigin).ToFlag();
			var beforeMandatoryDestinationFlag = trip.IsBeforeMandatoryDestination.ToFlag();

			// remaining inputs, including joint tour variables
			var time = trip.IsHalfTourFromOrigin ? tour.DestinationArrivalTime : tour.DestinationDepartureTime;

			var from7AMto9AMFlag = (time >= Constants.Time.SEVEN_AM && time < Constants.Time.NINE_AM).ToFlag();
			var from9AMto11AMFlag = (time >= Constants.Time.NINE_AM && time < Constants.Time.ELEVEN_AM).ToFlag();
			var from11AMto1PMFlag = (time >= Constants.Time.ELEVEN_AM && time < Constants.Time.ONE_PM).ToFlag();
			var from1PMto3PMFlag = (time >= Constants.Time.ONE_PM && time < Constants.Time.THREE_PM).ToFlag();
			var from3PMto5PMFlag = (time >= Constants.Time.THREE_PM && time < Constants.Time.FIVE_PM).ToFlag();
			var from7PMto9PMFlag = (time >= Constants.Time.SEVEN_PM && time < Constants.Time.NINE_PM).ToFlag();
			var from9PMto11PMFlag = (time >= Constants.Time.NINE_PM && time < Constants.Time.ELEVEN_PM).ToFlag();
			var from11PMto7AMFlag = (time >= Constants.Time.ELEVEN_PM).ToFlag();

			var remainingToursCount = personDay.HomeBasedTours - personDay.TotalSimulatedTours;

				var destinationDepartureTime =
				trip.IsHalfTourFromOrigin // first trip in half tour, use tour destination time
					? trip.Sequence == 1
						  ? tour.DestinationArrivalTime
						  : trip.PreviousTrip.ArrivalTime
					: trip.Sequence == 1
						  ? tour.DestinationDepartureTime
						  : trip.PreviousTrip.ArrivalTime;

			IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointTourParticipationPriority).ToList().Cast<PersonDayWrapper>();
			int numChildrenOnJointTour= 0;
			int numAdultsOnJointTour=0;
			int totHHToursJT=0;
			//int totHHStopsJT=0;

			TimeWindow timeWindow = new TimeWindow();
			if (tour.JointTourSequence > 0) {
				foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = pDay.Tours.Find(t => t.JointTourSequence == tour.JointTourSequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						totHHToursJT = personDay.HomeBasedTours +totHHToursJT;

						if(pDay.Person.Age<=18) 
						{
							numChildrenOnJointTour++;
						}

						if(pDay.Person.Age>=18) 
						{
							numAdultsOnJointTour++;
						}

					}
				}
			}
			else if (trip.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION && tour.FullHalfTour1Sequence > 0) {
				foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = pDay.Tours.Find(t => t.FullHalfTour1Sequence == tour.FullHalfTour1Sequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						totHHToursJT = personDay.HomeBasedTours +totHHToursJT;

						if(pDay.Person.Age<18) 
						{
							numChildrenOnJointTour++;
						}

						if(pDay.Person.Age>=18) 
						{
							numAdultsOnJointTour++;
						}

					}
				}
			}
			else if (trip.Direction == Constants.TourDirection.DESTINATION_TO_ORIGIN && tour.FullHalfTour2Sequence > 0) {
				foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = pDay.Tours.Find(t => t.FullHalfTour2Sequence == tour.FullHalfTour2Sequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						totHHToursJT = personDay.HomeBasedTours +totHHToursJT;

						if(pDay.Person.Age<18) 
						{
							numChildrenOnJointTour++;
						}

						if(pDay.Person.Age>=18) 
						{
							numAdultsOnJointTour++;
						}
					}
				}
			}
			else if (tour.ParentTour == null) {
				timeWindow.IncorporateAnotherTimeWindow(personDay.TimeWindow);
			}
			else {
				timeWindow.IncorporateAnotherTimeWindow(tour.ParentTour.TimeWindow);
			}

			timeWindow.SetBusyMinutes(Constants.Time.END_OF_RELEVANT_WINDOW, Constants.Time.MINUTES_IN_A_DAY + 1);

			// time window in minutes for yet unmodeled portion of halftour, only consider persons on this trip
			var availableWindow = timeWindow.AvailableWindow(destinationDepartureTime, Constants.TimeDirection.BOTH);
			
			var duration = availableWindow/ 60D;
			
			// connectivity attributes
			var c34Ratio = trip.OriginParcel.C34RatioBuffer1();

			var adis= 0.0;
			var logDist = 0.0;
			var minute = DayPeriod.BigDayPeriods[DayPeriod.MIDDAY].Start;

			//distance from origin to destination
			if(tour.OriginParcel != null && tour.DestinationParcel != null)
			{
				if (trip.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION) {
							 adis = ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, minute, tour.OriginParcel, destinationParcel).Variable;
							
				}
			else {
					adis = ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, minute, destinationParcel, tour.OriginParcel).Variable;
				}
				logDist = Math.Log(1+adis);
			}

			// 0 - NO MORE STOPS

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, true, choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;

			alternative.AddUtilityTerm(1, twoSimulatedTripsFlag * halfTourFromOriginFlag * isIndividualTour);
			alternative.AddUtilityTerm(2, threeSimulatedTripsFlag * halfTourFromOriginFlag * isIndividualTour);
			alternative.AddUtilityTerm(3, fourSimulatedTripsFlag * halfTourFromOriginFlag * isIndividualTour);
			alternative.AddUtilityTerm(4, fiveSimulatedTripsFlag * halfTourFromOriginFlag * isIndividualTour);
			alternative.AddUtilityTerm(5, twoSimulatedTripsFlag * halfTourFromDestinationFlag * isIndividualTour);
			alternative.AddUtilityTerm(6, threeSimulatedTripsFlag * halfTourFromDestinationFlag * isIndividualTour);
			alternative.AddUtilityTerm(7, fourSimulatedTripsFlag * halfTourFromDestinationFlag * isIndividualTour);
			alternative.AddUtilityTerm(8, fiveSimulatedTripsFlag * halfTourFromDestinationFlag * isIndividualTour);
			alternative.AddUtilityTerm(9, homeBasedTours*isIndividualTour);
			alternative.AddUtilityTerm(10, homeBasedTours*isJointTour);
			alternative.AddUtilityTerm(11, notHomeBasedTourFlag);
			//alternative.AddUtilityTerm(12, beforeMandatoryDestinationFlag*isJointTour);
			alternative.AddUtilityTerm(13, beforeMandatoryDestinationFlag);
			alternative.AddUtilityTerm(14, numAdultsOnJointTour);
			alternative.AddUtilityTerm(15, numChildrenOnJointTour);
			alternative.AddUtilityTerm(16, totHHToursJT);
		//	alternative.AddUtilityTerm(17, totHHStopsJT);
			alternative.AddUtilityTerm(22,(threeSimulatedTripsFlag+fourSimulatedTripsFlag  + fiveSimulatedTripsFlag)* halfTourFromOriginFlag * isJointTour);
			alternative.AddUtilityTerm(26, threeSimulatedTripsFlag * halfTourFromDestinationFlag * isJointTour);
			alternative.AddUtilityTerm(27, fourSimulatedTripsFlag * halfTourFromDestinationFlag * isJointTour);
			alternative.AddUtilityTerm(28, fiveSimulatedTripsFlag * halfTourFromDestinationFlag* isJointTour);
			
			// 1 - WORK STOP

			if ((personDay.WorkStops > 0 && tour.DestinationPurpose <= Constants.Purpose.SCHOOL) || (isJointTour==1)) {
				alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.WORK, true, choice == Constants.Purpose.WORK);

				alternative.Choice = Constants.Purpose.WORK;

				//alternative.AddUtilityTerm(32, isIndividualTour);
				alternative.AddUtilityTerm(33, workTourFlag);
				alternative.AddUtilityTerm(34, schoolTourFlag);
				alternative.AddUtilityTerm(35, halfTourFromOriginFlag);
				alternative.AddUtilityTerm(36, simulatedWorkStops);
				alternative.AddUtilityTerm(37, simulatedWorkStopsFlag);
				alternative.AddUtilityTerm(39, duration);
				alternative.AddUtilityTerm(40, from9AMto11AMFlag + from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag);
				//alternative.AddUtilityTerm(42, logDist);
				//alternative.AddUtilityTerm(43, transitTourFlag);
				 //alternative.AddUtilityTerm(44, (person.IsPartTimeWorker).ToFlag());
				alternative.AddUtilityTerm(46, totalAggregateLogsum);
				//alternative.AddUtilityTerm(47,totEmpBuffer2);
				alternative.AddUtilityTerm(48, hov2TourFlag+hov3TourFlag);

			}
			else {
				choiceProbabilityCalculator.GetAlternative(Constants.Purpose.WORK, false, choice == Constants.Purpose.WORK);
			}

			// 2 - SCHOOL STOP

			if ((personDay.SchoolStops > 0 && tour.DestinationPurpose <= Constants.Purpose.SCHOOL)|| (isJointTour==1)) {
				alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SCHOOL, true, choice == Constants.Purpose.SCHOOL);

				alternative.Choice = Constants.Purpose.SCHOOL;

				alternative.AddUtilityTerm(51, workTourFlag);
				alternative.AddUtilityTerm(52, schoolTourFlag);
				//alternative.AddUtilityTerm(53, halfTourFromOriginFlag);
				alternative.AddUtilityTerm(54, simulatedSchoolStops);
				//alternative.AddUtilityTerm(55, remainingToursCount);
				alternative.AddUtilityTerm(56, duration);
				alternative.AddUtilityTerm(57, from7AMto9AMFlag + from7PMto9PMFlag + from9PMto11PMFlag + from11PMto7AMFlag);
				alternative.AddUtilityTerm(58, oneSimulatedTripFlag);
				//alternative.AddUtilityTerm(59, logDist);
				alternative.AddUtilityTerm(61, fullJointHalfTour*numChildrenOnJointTour);
				alternative.AddUtilityTerm(65, (person.Age<12).ToFlag());
				//alternative.AddUtilityTerm(66,  (person.IsUniversityStudent).ToFlag());
			}
			else {
				choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SCHOOL, false, choice == Constants.Purpose.SCHOOL);
			}

			// 3 - ESCORT STOP

			if ((personDay.EscortStops > 0 && tour.DestinationPurpose <= Constants.Purpose.ESCORT) || (isJointTour==1)) {
				alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.ESCORT, true, choice == Constants.Purpose.ESCORT);

				alternative.Choice = Constants.Purpose.ESCORT;

				alternative.AddUtilityTerm(71, workTourFlag + schoolTourFlag);
				alternative.AddUtilityTerm(72, isJointTour);
				alternative.AddUtilityTerm(74, escortTourFlag);
				//alternative.AddUtilityTerm(75, socialOrRecreationTourFlag);
				//alternative.AddUtilityTerm(76, remainingToursCount);
				alternative.AddUtilityTerm(77, duration);
				alternative.AddUtilityTerm(78, from7AMto9AMFlag);
				//alternative.AddUtilityTerm(79, from9AMto11AMFlag + from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag);
				alternative.AddUtilityTerm(81, hov2TourFlag);
				alternative.AddUtilityTerm(82, hov3TourFlag);
				alternative.AddUtilityTerm(83, simulatedEscortStops*isJointTour);
				alternative.AddUtilityTerm(84, simulatedEscortStops*isIndividualTour);
				//alternative.AddUtilityTerm(85, totalAggregateLogsum);
				alternative.AddUtilityTerm(86, fullJointHalfTour);
				//alternative.AddUtilityTerm(88, enrollmentK8Buffer2);
				alternative.AddUtilityTerm(89, numChildrenOnJointTour);
				alternative.AddUtilityTerm(90, halfTourFromOriginFlag);
			}
			else {
				choiceProbabilityCalculator.GetAlternative(Constants.Purpose.ESCORT, false, choice == Constants.Purpose.ESCORT);
			}

			// 4 - PERSONAL BUSINESS STOP

			if (personDay.PersonalBusinessStops > 0|| isJointTour==1) {
				alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.PERSONAL_BUSINESS, true, choice == Constants.Purpose.PERSONAL_BUSINESS);

				alternative.Choice = Constants.Purpose.PERSONAL_BUSINESS;

				alternative.AddUtilityTerm(91, (workTourFlag + schoolTourFlag));
				alternative.AddUtilityTerm(92, isJointTour);
				alternative.AddUtilityTerm(93, escortTourFlag);
				alternative.AddUtilityTerm(94, personalBusinessOrMedicalTourFlag*isIndividualTour);
				alternative.AddUtilityTerm(95, shoppingTourFlag);
				alternative.AddUtilityTerm(96, mealTourFlag);
				alternative.AddUtilityTerm(97, socialOrRecreationTourFlag);
				alternative.AddUtilityTerm(98, halfTourFromOriginFlag);
				alternative.AddUtilityTerm(99, simulatedPersonalBusinessStops*isIndividualTour);
				alternative.AddUtilityTerm(100, simulatedPersonalBusinessStops*isJointTour);
				alternative.AddUtilityTerm(101, duration);
				alternative.AddUtilityTerm(102, (from7AMto9AMFlag + from7PMto9PMFlag + from9PMto11PMFlag + from11PMto7AMFlag));
				alternative.AddUtilityTerm(103, from9AMto11AMFlag + from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag);
				alternative.AddUtilityTerm(105, hov2TourFlag);
				alternative.AddUtilityTerm(106, hov3TourFlag);
				alternative.AddUtilityTerm(109, fullJointHalfTour);
				alternative.AddUtilityTerm(110, totEmpBuffer2);
				//alternative.AddUtilityTerm(111, totalAggregateLogsum);
				alternative.AddUtilityTerm(112, personalBusinessOrMedicalTourFlag*isJointTour);

			}
			else {
				choiceProbabilityCalculator.GetAlternative(Constants.Purpose.PERSONAL_BUSINESS, false, choice == Constants.Purpose.PERSONAL_BUSINESS);
			}

			// 5 - SHOPPING STOP

			if (personDay.ShoppingStops > 0 || isJointTour==1) {
				alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SHOPPING, true, choice == Constants.Purpose.SHOPPING);

				alternative.Choice = Constants.Purpose.SHOPPING;

				alternative.AddUtilityTerm(121, workTourFlag + schoolTourFlag);
				alternative.AddUtilityTerm(122, isJointTour);
				alternative.AddUtilityTerm(123, escortTourFlag);
				alternative.AddUtilityTerm(124, personalBusinessOrMedicalTourFlag);
				alternative.AddUtilityTerm(125, shoppingTourFlag*isIndividualTour);
				alternative.AddUtilityTerm(126, mealTourFlag);
				alternative.AddUtilityTerm(127, socialOrRecreationTourFlag);
				alternative.AddUtilityTerm(128, halfTourFromOriginFlag);
				alternative.AddUtilityTerm(129, simulatedShoppingStops*isIndividualTour);
				alternative.AddUtilityTerm(130, simulatedShoppingStops*isJointTour);
				alternative.AddUtilityTerm(131, duration);
				alternative.AddUtilityTerm(132, from7AMto9AMFlag + from9PMto11PMFlag + from11PMto7AMFlag);
				alternative.AddUtilityTerm(133, (from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag));
				//alternative.AddUtilityTerm(134, adultFemaleOnJointTour);
				alternative.AddUtilityTerm(135, hov2TourFlag);
				alternative.AddUtilityTerm(136, hov3TourFlag);
				alternative.AddUtilityTerm(137, Math.Log(1+adis));
				alternative.AddUtilityTerm(138, shoppingTourFlag*isJointTour);
				//alternative.AddUtilityTerm(140, shoppingAggregateLogsum);
				//alternative.AddUtilityTerm(141, retailBuffer2);
				//alternative.AddUtilityTerm(142, numChildrenOnJointTour);
				//alternative.AddUtilityTerm(143, (household.Has100KPlusIncome).ToFlag());
			}
			else {
				choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SHOPPING, false, choice == Constants.Purpose.SHOPPING);
			}

			// 6 - MEAL STOP

			if (personDay.MealStops > 0 ||isJointTour==1) {
				alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEAL, true, choice == Constants.Purpose.MEAL);

				alternative.Choice = Constants.Purpose.MEAL;

				alternative.AddUtilityTerm(151, workTourFlag);
				//alternative.AddUtilityTerm(152, isJointTour);
				alternative.AddUtilityTerm(153, schoolTourFlag);
				alternative.AddUtilityTerm(154, escortTourFlag);
				alternative.AddUtilityTerm(155, personalBusinessOrMedicalTourFlag);
				alternative.AddUtilityTerm(156, shoppingTourFlag);
				alternative.AddUtilityTerm(157, mealTourFlag);
				alternative.AddUtilityTerm(158, socialOrRecreationTourFlag);
				alternative.AddUtilityTerm(159, halfTourFromOriginFlag);
				alternative.AddUtilityTerm(160, simulatedMealStops*isIndividualTour);
				alternative.AddUtilityTerm(161, simulatedMealStops*isJointTour);
				alternative.AddUtilityTerm(162, duration);
				alternative.AddUtilityTerm(164, from11AMto1PMFlag + from1PMto3PMFlag);
				alternative.AddUtilityTerm(166, onePersonHouseholdFlag);
				alternative.AddUtilityTerm(167, hov2TourFlag);
				alternative.AddUtilityTerm(168, hov3TourFlag);
				alternative.AddUtilityTerm(170, numChildrenOnJointTour);
				alternative.AddUtilityTerm(171, oneSimulatedTripFlag);
				alternative.AddUtilityTerm(172,  Math.Log(1+adis));
				alternative.AddUtilityTerm(174, fullJointHalfTour);
				alternative.AddUtilityTerm(175, foodBuffer2);
				//alternative.AddUtilityTerm(176, homeFoodBuffer2);
				//alternative.AddUtilityTerm(177, (household.Has100KPlusIncome).ToFlag());
				alternative.AddUtilityTerm(178, numAdultsOnJointTour);
			}
			else {
				choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEAL, false, choice == Constants.Purpose.MEAL);
			}

			// 7 - SOCIAL (OR RECREATION) STOP

			if (personDay.SocialStops > 0 || isJointTour==1) {
				alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SOCIAL, true, choice == Constants.Purpose.SOCIAL);

				alternative.Choice = Constants.Purpose.SOCIAL;

				alternative.AddUtilityTerm(181, workTourFlag + schoolTourFlag);
				alternative.AddUtilityTerm(182, isJointTour);
				alternative.AddUtilityTerm(183, escortTourFlag);
				alternative.AddUtilityTerm(184, personalBusinessOrMedicalTourFlag);
				alternative.AddUtilityTerm(185, shoppingTourFlag);
				alternative.AddUtilityTerm(186, mealTourFlag);
				alternative.AddUtilityTerm(187, socialOrRecreationTourFlag);
				alternative.AddUtilityTerm(188, halfTourFromOriginFlag);
				alternative.AddUtilityTerm(189, simulatedSocialStops*isIndividualTour);
				alternative.AddUtilityTerm(197, simulatedSocialStops*isJointTour);
				alternative.AddUtilityTerm(190, remainingToursCount);
				alternative.AddUtilityTerm(191, duration);
				alternative.AddUtilityTerm(192, from7AMto9AMFlag + from11PMto7AMFlag);
				alternative.AddUtilityTerm(194, hov2TourFlag);
				alternative.AddUtilityTerm(195, hov3TourFlag);
				alternative.AddUtilityTerm(196, logDist);
				alternative.AddUtilityTerm(200, numAdultsOnJointTour);
			}
			else {
				choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SOCIAL, false, choice == Constants.Purpose.SOCIAL);
			}


				// 8 - RECREATION) STOP

			if (personDay.RecreationStops > 0 || isJointTour==1) {
				alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.RECREATION, true, choice == Constants.Purpose.RECREATION);

				alternative.Choice = Constants.Purpose.RECREATION;

				alternative.AddUtilityTerm(211, workTourFlag + schoolTourFlag);
				alternative.AddUtilityTerm(212, isJointTour);
				alternative.AddUtilityTerm(213, escortTourFlag);
				alternative.AddUtilityTerm(214, personalBusinessOrMedicalTourFlag);
				alternative.AddUtilityTerm(215, shoppingTourFlag);
				alternative.AddUtilityTerm(216, mealTourFlag);
				alternative.AddUtilityTerm(217, socialOrRecreationTourFlag);
				alternative.AddUtilityTerm(218, halfTourFromOriginFlag);
				alternative.AddUtilityTerm(219, simulatedRecreationStops*isIndividualTour);
				alternative.AddUtilityTerm(229, simulatedRecreationStops*isJointTour);
				alternative.AddUtilityTerm(220, remainingToursCount);
				alternative.AddUtilityTerm(221, duration);
				alternative.AddUtilityTerm(222, from7AMto9AMFlag + from11PMto7AMFlag);
				alternative.AddUtilityTerm(223, from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag);
				alternative.AddUtilityTerm(225, hov3TourFlag);
				alternative.AddUtilityTerm(226, numChildrenOnJointTour);
				alternative.AddUtilityTerm(227, numAdultsOnJointTour);
				//alternative.AddUtilityTerm(228, fullJointHalfTour);
				//alternative.AddUtilityTerm(229, openSpaceBuffer2);
			}
			else {
				choiceProbabilityCalculator.GetAlternative(Constants.Purpose.RECREATION, false, choice == Constants.Purpose.RECREATION);
			}

				// 9 - MEDICAL STOP

			if (personDay.MedicalStops > 0 || isJointTour==1) {
				alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEDICAL, true, choice == Constants.Purpose.MEDICAL);

				alternative.Choice = Constants.Purpose.MEDICAL;

				alternative.AddUtilityTerm(231, workTourFlag + schoolTourFlag + escortTourFlag);
				alternative.AddUtilityTerm(232, isJointTour);
				alternative.AddUtilityTerm(233, personalBusinessOrMedicalTourFlag*isIndividualTour);
				alternative.AddUtilityTerm(234, personalBusinessOrMedicalTourFlag*isJointTour);
				alternative.AddUtilityTerm(235, shoppingTourFlag);
				alternative.AddUtilityTerm(236, mealTourFlag);
				alternative.AddUtilityTerm(237, socialOrRecreationTourFlag);
				alternative.AddUtilityTerm(238, halfTourFromOriginFlag);
				alternative.AddUtilityTerm(239, simulatedMedicalStops*isJointTour);
				alternative.AddUtilityTerm(240, simulatedMedicalStops*isIndividualTour);
				//alternative.AddUtilityTerm(240, fullJointHalfTour);
				alternative.AddUtilityTerm(241, duration);
				alternative.AddUtilityTerm(242, from7AMto9AMFlag + from11PMto7AMFlag);
				alternative.AddUtilityTerm(243, from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag);
				//alternative.AddUtilityTerm(248, numChildrenOnJointTour);
				//alternative.AddUtilityTerm(249, adultFemaleOnJointTour);
			}
			else {
				choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEDICAL, false, choice == Constants.Purpose.MEDICAL);
			}


		}
	}
}