﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// 
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// 
// This file is part of Daysim.
// 
// Daysim is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Daysim is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Daysim. If not, see <http://www.gnu.org/licenses/>.

using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using Daysim.Framework.Roster;
using Daysim.DomainModels.Actum;

namespace Daysim.ChoiceModels {
	public class ActumIntermediateStopGenerationModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumIntermediateStopGenerationModel";
		//		private const int TOTAL_ALTERNATIVES = 20;
		//		private const int TOTAL_NESTED_ALTERNATIVES = 10;
		//		private const int TOTAL_LEVELS = 2;
		private const int TOTAL_ALTERNATIVES = 7;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 250;

		public int Run(ActumTripWrapper trip, ActumHouseholdDayWrapper householdDay, int choice = Constants.Purpose.NONE_OR_HOME) {
			if (trip == null) {
				throw new ArgumentNullException("trip");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.IntermediateStopGenerationModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

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

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumTripWrapper trip, ActumHouseholdDayWrapper householdDay, int choice = Constants.DEFAULT_VALUE) {
			var household = (ActumHouseholdWrapper) trip.Household;
			var person = (ActumPersonWrapper) trip.Person;
			var personDay = (ActumPersonDayWrapper) trip.PersonDay;
			var tour = (ActumTourWrapper) trip.Tour;
			var halfTour = (ActumTourWrapper.HalfTour) trip.HalfTour;
			var personDays = householdDay.PersonDays;

			var isJointTour = tour.JointTourSequence > 0 ? 1 : 0;
			var isIndividualTour = isJointTour == 1 ? 0 : 1;
			var destinationParcel = tour.DestinationParcel;
			var jointHalfOfFullJointHalfTour = ((trip.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION && tour.FullHalfTour1Sequence > 0)
					|| (trip.Direction == Constants.TourDirection.DESTINATION_TO_ORIGIN && tour.FullHalfTour2Sequence > 0)).ToFlag();
			var individualHalfOfFullJointHalfTour =
				((trip.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION
				&& tour.FullHalfTour1Sequence == 0
				&& tour.FullHalfTour2Sequence > 0)
					|| (trip.Direction == Constants.TourDirection.DESTINATION_TO_ORIGIN
					&& tour.FullHalfTour2Sequence == 0
					&& tour.FullHalfTour1Sequence > 0)).ToFlag();
			var individualHalfTour = (isIndividualTour == 1 || individualHalfOfFullJointHalfTour == 1) ? 1 : 0;
			var jointHalfTour = 1 - individualHalfTour;

			//destination parcel variables
			var foodBuffer2 = 0.0;
			var totEmpBuffer2 = 0.0;
			var retailBuffer2 = 0.0;

			if (destinationParcel != null) {
				foodBuffer2 = Math.Log(1 + destinationParcel.EmploymentFoodBuffer2);
				totEmpBuffer2 = Math.Log(1 + destinationParcel.EmploymentTotalBuffer2);
				retailBuffer2 = Math.Log(1 + destinationParcel.EmploymentRetailBuffer2);
			}

			var carOwnership = person.CarOwnershipSegment;

			// household inputs
			var onePersonHouseholdFlag = household.IsOnePersonHousehold.ToFlag();
			//var householdInc75KP = household.Has75KPlusIncome;

			var votALSegment = tour.VotALSegment;
			var transitAccessSegment = household.ResidenceParcel.TransitAccessSegment();

			var totalAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];

			var homeFoodBuffer2 = Math.Log(1 + household.ResidenceParcel.EmploymentFoodBuffer2);
			var homeTotEmpBuffer2 = Math.Log(1 + household.ResidenceParcel.EmploymentTotalBuffer2);
			var homeRetailBuffer2 = Math.Log(1 + household.ResidenceParcel.EmploymentRetailBuffer2);

			// person-day inputs
			var homeBasedTours = personDay.HomeBasedTours;
			var simulatedToursFlag = personDay.HasSimulatedTours.ToFlag();
			var simulatedBusinessStops = personDay.SimulatedBusinessStops;
			int simulatedBusinessStopsFlag = simulatedBusinessStops > 0 ? 1 : 0;
			var simulatedSchoolStops = personDay.SimulatedSchoolStops;
			var simulatedEscortStops = personDay.SimulatedEscortStops;
			var simulatedPersonalBusinessStops = personDay.SimulatedPersonalBusinessStops;
			var simulatedShoppingStops = personDay.SimulatedShoppingStops;
			var simulatedMealStops = personDay.SimulatedMealStops;
			var simulatedSocialStops = personDay.SimulatedSocialStops;
			var simulatedRecreationStops = personDay.SimulatedRecreationStops;
			var simulatedMedicalStops = personDay.SimulatedMedicalStops;
			var primaryFamilyTimeFlag = householdDay.PrimaryPriorityTimeFlag;

			// tour inputs
			var hovDriverTourFlag = tour.IsHovDriverMode.ToFlag();
			var hovPassengerTourFlag = tour.IsHovPassengerMode.ToFlag();
			var transitTourFlag = tour.IsTransitMode.ToFlag();
			var walkTourFlag = tour.IsWalkMode.ToFlag();
			var bikeTourFlag = tour.IsBikeMode.ToFlag();
			var autoTourFlag = tour.IsAnAutoMode.ToFlag();
			var notHomeBasedTourFlag = (!tour.IsHomeBasedTour).ToFlag();
			var workTourFlag = tour.IsWorkPurpose.ToFlag();
			var businessTourFlag = tour.IsBusinessPurpose.ToFlag();
			var personalBusinessTourFlag = tour.IsPersonalBusinessPurpose.ToFlag();
			var socialTourFlag = tour.IsSocialPurpose.ToFlag();
			var socialOrRecreationTourFlag = tour.IsSocialOrRecreationPurpose.ToFlag();
			var schoolTourFlag = tour.IsSchoolPurpose.ToFlag();
			var escortTourFlag = tour.IsEscortPurpose.ToFlag();
			var shoppingTourFlag = tour.IsShoppingPurpose.ToFlag();

			// trip inputs
			var oneSimulatedTripFlag = halfTour.OneSimulatedTripFlag;
			var twoSimulatedTripsFlag = halfTour.TwoSimulatedTripsFlag;
			var threeSimulatedTripsFlag = halfTour.ThreeSimulatedTripsFlag;
			var fourSimulatedTripsFlag = halfTour.FourSimulatedTripsFlag;
			var fivePlusSimulatedTripsFlag = halfTour.FivePlusSimulatedTripsFlag;
			var twoPlusSimulatedTripsFlag = twoSimulatedTripsFlag + threeSimulatedTripsFlag + fourSimulatedTripsFlag + fivePlusSimulatedTripsFlag;
			var halfTourFromOriginFlag = trip.IsHalfTourFromOrigin.ToFlag();
			var halfTourFromDestinationFlag = (!trip.IsHalfTourFromOrigin).ToFlag();
			var beforeMandatoryDestinationFlag = trip.IsBeforeMandatoryDestination.ToFlag();

			// remaining inputs, including joint tour variables

			var remainingToursCount = personDay.HomeBasedTours - personDay.TotalSimulatedTours;

			var destinationDepartureTime =
			trip.IsHalfTourFromOrigin // first trip in half tour, use tour destination time
				? trip.Sequence == 1
					  ? tour.DestinationArrivalTime
					  : trip.PreviousTrip.ArrivalTime
				: trip.Sequence == 1
					  ? tour.DestinationDepartureTime
					  : trip.PreviousTrip.ArrivalTime;

			//var time = trip.IsHalfTourFromOrigin ? tour.DestinationArrivalTime : tour.DestinationDepartureTime;
			var time = destinationDepartureTime;

			bool timeIsAvailableForAnotherTrip = true;
			if ((trip.IsHalfTourFromOrigin && time < Constants.Time.FOUR_AM)
				|| (!trip.IsHalfTourFromOrigin && time > Constants.Time.TWO_AM)) {
				timeIsAvailableForAnotherTrip = false;
			}

			bool stopsNeeded = false;

			//if ((halfTour.SimulatedTrips <= 5) 
			//	&& (timeIsAvailableForAnotherTrip)  
			//	&& (trip.Direction == 2)
			//	&&((trip.Tour.Sequence == trip.PersonDay.TotalCreatedTours)
			//	&& ((simulatedSchoolStops == 0 && personDay.SchoolStops > 0)
			//	||(simulatedBusinessStops == 0 && personDay.BusinessStops > 0)
			//	||(simulatedEscortStops == 0 && personDay.EscortStops > 0)
			//	||(simulatedPersonalBusinessStops == 0 && personDay.PersonalBusinessStops > 0)
			//	||(simulatedShoppingStops == 0 && personDay.ShoppingStops > 0)
			//	||(simulatedSocialStops == 0 && personDay.SocialStops > 0)))) {
			//		stopsNeeded = true;
			//}


			var from7AMto9AMFlag = (time >= Constants.Time.SEVEN_AM && time < Constants.Time.NINE_AM).ToFlag();
			var from9AMto3PMFlag = (time >= Constants.Time.NINE_AM && time < Constants.Time.THREE_PM).ToFlag();
			var from3PMto6PMFlag = (time >= Constants.Time.THREE_PM && time < Constants.Time.SIX_PM).ToFlag();
			var from6PMto10PMFlag = (time >= Constants.Time.SIX_PM && time < Constants.Time.TEN_PM).ToFlag();
			var from10PMto7AMFlag = (time >= Constants.Time.TEN_PM).ToFlag();


			var from9AMto11AMFlag = (time >= Constants.Time.NINE_AM && time < Constants.Time.ELEVEN_AM).ToFlag();
			var from11AMto1PMFlag = (time >= Constants.Time.ELEVEN_AM && time < Constants.Time.ONE_PM).ToFlag();
			var from1PMto3PMFlag = (time >= Constants.Time.ONE_PM && time < Constants.Time.THREE_PM).ToFlag();
			var from3PMto5PMFlag = (time >= Constants.Time.THREE_PM && time < Constants.Time.FIVE_PM).ToFlag();
			var from7PMto9PMFlag = (time >= Constants.Time.SEVEN_PM && time < Constants.Time.NINE_PM).ToFlag();
			var from9PMto11PMFlag = (time >= Constants.Time.NINE_PM && time < Constants.Time.ELEVEN_PM).ToFlag();
			var from11PMto7AMFlag = (time >= Constants.Time.ELEVEN_PM).ToFlag();





			IEnumerable<ActumPersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointTourParticipationPriority).ToList().Cast<ActumPersonDayWrapper>();
			int numChildrenOnJointTour = 0;
			int numAdultsOnJointTour = 0;
			int totHHToursJT = 0;
			//int totHHStopsJT=0;

			TimeWindow timeWindow = new TimeWindow();
			if (tour.JointTourSequence > 0) {
				foreach (ActumPersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = (ActumTourWrapper) pDay.Tours.Find(t => t.JointTourSequence == tour.JointTourSequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						totHHToursJT = personDay.HomeBasedTours + totHHToursJT;

						if (pDay.Person.Age < 18) {
							numChildrenOnJointTour++;
						}

						if (pDay.Person.Age >= 18) {
							numAdultsOnJointTour++;
						}

					}
				}
			}
			else if (trip.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION && tour.FullHalfTour1Sequence > 0) {
				foreach (ActumPersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = (ActumTourWrapper) pDay.Tours.Find(t => t.FullHalfTour1Sequence == tour.FullHalfTour1Sequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);

						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						totHHToursJT = personDay.HomeBasedTours + totHHToursJT;

						if (pDay.Person.Age < 18) {
							numChildrenOnJointTour++;
						}

						if (pDay.Person.Age >= 18) {
							numAdultsOnJointTour++;
						}

					}
				}
			}
			else if (trip.Direction == Constants.TourDirection.DESTINATION_TO_ORIGIN && tour.FullHalfTour2Sequence > 0) {
				foreach (ActumPersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = (ActumTourWrapper) pDay.Tours.Find(t => t.FullHalfTour2Sequence == tour.FullHalfTour2Sequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
						totHHToursJT = personDay.HomeBasedTours + totHHToursJT;

						if (pDay.Person.Age < 18) {
							numChildrenOnJointTour++;
						}

						if (pDay.Person.Age >= 18) {
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
			var timePressure = 1000 * remainingToursCount / (Math.Max(1D, availableWindow));
			//alternative.AddUtilityTerm(98, 1000 * remainingToursCount / (Math.Max(1D, maxWindowRemaining))); 


			//var duration = availableWindow / 60D;

			// connectivity attributes
			//var c34Ratio = trip.OriginParcel.C34RatioBuffer1();

			var adis = 0.0;
			var logDist = 0.0;
			var minute = DayPeriod.BigDayPeriods[DayPeriod.MIDDAY].Start;

			//distance from origin to destination
			if (tour.OriginParcel != null && tour.DestinationParcel != null) {
				if (trip.Direction == Constants.TourDirection.ORIGIN_TO_DESTINATION) {
					adis = ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, minute, tour.OriginParcel, destinationParcel).Variable;

				}
				else {
					adis = ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, minute, destinationParcel, tour.OriginParcel).Variable;
				}
				logDist = Math.Log(1 + adis);
			}

			// 0 - NO MORE STOPS

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, !stopsNeeded, choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;
			//alternative.AddNestedAlternative(_nestedAlternativeIds[0], _nestedAlternativeIndexes[0], THETA_PARAMETER);

			alternative.AddUtilityTerm(1, oneSimulatedTripFlag);
			alternative.AddUtilityTerm(2, twoSimulatedTripsFlag);
			alternative.AddUtilityTerm(2, threeSimulatedTripsFlag);
			alternative.AddUtilityTerm(2, fourSimulatedTripsFlag);
			alternative.AddUtilityTerm(2, fivePlusSimulatedTripsFlag);
			alternative.AddUtilityTerm(6, transitTourFlag);
			alternative.AddUtilityTerm(7, bikeTourFlag);
			alternative.AddUtilityTerm(8, walkTourFlag);
			alternative.AddUtilityTerm(9, jointHalfTour);
			alternative.AddUtilityTerm(10, halfTourFromOriginFlag);
			alternative.AddUtilityTerm(11, totalAggregateLogsum);

			alternative.AddUtilityTerm(12, businessTourFlag);
			alternative.AddUtilityTerm(13, personalBusinessTourFlag);
			alternative.AddUtilityTerm(14, socialTourFlag);
			//alternative.AddUtilityTerm(15, schoolTourFlag);
			alternative.AddUtilityTerm(16, escortTourFlag);
			alternative.AddUtilityTerm(17, shoppingTourFlag);
			alternative.AddUtilityTerm(18, timePressure);
			//alternative.AddUtilityTerm(19, primaryFamilyTimeFlag);

			//alternative.AddUtilityTerm(15, from11PMto7AMFlag);

			//alternative.AddUtilityTerm(1, twoSimulatedTripsFlag * halfTourFromOriginFlag * isIndividualTour);
			//alternative.AddUtilityTerm(2, threeSimulatedTripsFlag * halfTourFromOriginFlag * isIndividualTour);
			//alternative.AddUtilityTerm(3, fourSimulatedTripsFlag * halfTourFromOriginFlag * isIndividualTour);
			//alternative.AddUtilityTerm(4, fivePlusSimulatedTripsFlag * halfTourFromOriginFlag * isIndividualTour);
			//alternative.AddUtilityTerm(5, twoSimulatedTripsFlag * halfTourFromDestinationFlag * isIndividualTour);
			//alternative.AddUtilityTerm(6, threeSimulatedTripsFlag * halfTourFromDestinationFlag * isIndividualTour);
			//alternative.AddUtilityTerm(7, fourSimulatedTripsFlag * halfTourFromDestinationFlag * isIndividualTour);
			//alternative.AddUtilityTerm(8, fivePlusSimulatedTripsFlag * halfTourFromDestinationFlag * isIndividualTour);
			//alternative.AddUtilityTerm(9, homeBasedTours * isIndividualTour);
			//alternative.AddUtilityTerm(10, homeBasedTours * isJointTour);
			//alternative.AddUtilityTerm(11, notHomeBasedTourFlag);
			//alternative.AddUtilityTerm(12, beforeMandatoryDestinationFlag*isJointTour);
			//alternative.AddUtilityTerm(13, beforeMandatoryDestinationFlag);
			//alternative.AddUtilityTerm(14, numAdultsOnJointTour);
			//alternative.AddUtilityTerm(15, numChildrenOnJointTour);
			//alternative.AddUtilityTerm(16, totHHToursJT);
			//	alternative.AddUtilityTerm(17, totHHStopsJT);
			//alternative.AddUtilityTerm(22, (threeSimulatedTripsFlag + fourSimulatedTripsFlag + fivePlusSimulatedTripsFlag) * halfTourFromOriginFlag * isJointTour);
			//alternative.AddUtilityTerm(26, threeSimulatedTripsFlag * halfTourFromDestinationFlag * isJointTour);
			//alternative.AddUtilityTerm(27, fourSimulatedTripsFlag * halfTourFromDestinationFlag * isJointTour);
			//alternative.AddUtilityTerm(28, fivePlusSimulatedTripsFlag * halfTourFromDestinationFlag * isJointTour);

			// 1 - BUSINESS STOP

			//if (personDay.BusinessStops > 0 && (tour.DestinationPurpose <= Constants.Purpose.SCHOOL || tour.DestinationPurpose == Constants.Purpose.BUSINESS)) {
			// JLB 20130704 business stops are allowed on escort tours per data prep
			alternative = choiceProbabilityCalculator.GetAlternative(1,
				(personDay.BusinessStops > 0
				&& (tour.DestinationPurpose <= Constants.Purpose.ESCORT || tour.DestinationPurpose == Constants.Purpose.BUSINESS)
				&& (halfTour.SimulatedTrips <= 5)
				&& timeIsAvailableForAnotherTrip),
				choice == Constants.Purpose.BUSINESS);

			alternative.Choice = Constants.Purpose.BUSINESS;
			//alternative.AddNestedAlternative(_nestedAlternativeIds[1], _nestedAlternativeIndexes[1], THETA_PARAMETER);

			//alternative.AddUtilityTerm(32, isIndividualTour);
			alternative.AddUtilityTerm(32, 1.0);
			alternative.AddUtilityTerm(33, businessTourFlag);
			//alternative.AddUtilityTerm(34, schoolTourFlag);
			//alternative.AddUtilityTerm(35, halfTourFromOriginFlag);
			//alternative.AddUtilityTerm(36, simulatedBusinessStops);
			//alternative.AddUtilityTerm(37, simulatedBusinessStopsFlag);
			//alternative.AddUtilityTerm(39, duration);

			//alternative.AddUtilityTerm(40, from9AMto11AMFlag + from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag);
			alternative.AddUtilityTerm(40, from9AMto3PMFlag + from3PMto6PMFlag);


			//alternative.AddUtilityTerm(42, logDist);
			//alternative.AddUtilityTerm(43, transitTourFlag);
			//alternative.AddUtilityTerm(44, (person.IsPartTimeWorker).ToFlag());
			
			//GV: 21. aug - I commented out as it is the only logsum in the model
			//alternative.AddUtilityTerm(46, totalAggregateLogsum);
			
			//alternative.AddUtilityTerm(47,totEmpBuffer2);
			//alternative.AddUtilityTerm(48, hovDriverTourFlag + hovPassengerTourFlag);


			// 2 - SCHOOL STOP

			alternative = choiceProbabilityCalculator.GetAlternative(2,
				(((personDay.SchoolStops > 0 && tour.DestinationPurpose <= Constants.Purpose.SCHOOL) || (isJointTour == 1))
				&& halfTour.SimulatedTrips <= 5
				&& timeIsAvailableForAnotherTrip),
				choice == Constants.Purpose.SCHOOL);

			alternative.Choice = Constants.Purpose.SCHOOL;
			//alternative.AddNestedAlternative(_nestedAlternativeIds[2], _nestedAlternativeIndexes[2], THETA_PARAMETER);

			//alternative.AddUtilityTerm(51, workTourFlag);
			alternative.AddUtilityTerm(51, 1.0);
			//alternative.AddUtilityTerm(52, schoolTourFlag);
			//alternative.AddUtilityTerm(53, halfTourFromOriginFlag);
			//alternative.AddUtilityTerm(54, simulatedSchoolStops);
			//alternative.AddUtilityTerm(55, remainingToursCount);
			//alternative.AddUtilityTerm(56, duration);

			//alternative.AddUtilityTerm(57, from7AMto9AMFlag + from7PMto9PMFlag + from9PMto11PMFlag + from11PMto7AMFlag);
			alternative.AddUtilityTerm(57, from7AMto9AMFlag + from9AMto3PMFlag + from10PMto7AMFlag);

			//alternative.AddUtilityTerm(58, oneSimulatedTripFlag);
			//alternative.AddUtilityTerm(59, logDist);
			alternative.AddUtilityTerm(61, jointHalfOfFullJointHalfTour * numChildrenOnJointTour);
			//alternative.AddUtilityTerm(65, (person.Age < 12).ToFlag());
			//alternative.AddUtilityTerm(66,  (person.IsUniversityStudent).ToFlag());




			// 3 - ESCORT STOP

			//if ((personDay.EscortStops > 0 && (tour.DestinationPurpose <= Constants.Purpose.ESCORT || tour.DestinationPurpose == Constants.Purpose.BUSINESS)) || (isJointTour==1)) {
			// JLB 20130704 no escort stops allowed on business tours per data prep
			alternative = choiceProbabilityCalculator.GetAlternative(3,
				(((personDay.EscortStops > 0 && tour.DestinationPurpose <= Constants.Purpose.ESCORT) || (isJointTour == 1))
				&& halfTour.SimulatedTrips <= 5
				&& timeIsAvailableForAnotherTrip),
				choice == Constants.Purpose.ESCORT);

			alternative.Choice = Constants.Purpose.ESCORT;
			//alternative.AddNestedAlternative(_nestedAlternativeIds[3], _nestedAlternativeIndexes[3], THETA_PARAMETER);

			alternative.AddUtilityTerm(71, 1.0);
			//alternative.AddUtilityTerm(72, workTourFlag + schoolTourFlag);
			//alternative.AddUtilityTerm(72, isJointTour);
			//alternative.AddUtilityTerm(74, escortTourFlag);
			//alternative.AddUtilityTerm(75, socialOrRecreationTourFlag);
			//alternative.AddUtilityTerm(76, remainingToursCount);
			//alternative.AddUtilityTerm(77, duration);
			alternative.AddUtilityTerm(78, from7AMto9AMFlag);
			//alternative.AddUtilityTerm(79, from9AMto11AMFlag + from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag);
			//alternative.AddUtilityTerm(81, hovDriverTourFlag);
			//alternative.AddUtilityTerm(82, hovPassengerTourFlag);
			//alternative.AddUtilityTerm(83, simulatedEscortStops * isJointTour);
			//alternative.AddUtilityTerm(84, simulatedEscortStops * isIndividualTour);
			//alternative.AddUtilityTerm(85, totalAggregateLogsum);
			//alternative.AddUtilityTerm(86, jointHalfOfFullJointHalfTour);
			//alternative.AddUtilityTerm(88, enrollmentK8Buffer2);
			//alternative.AddUtilityTerm(89, numChildrenOnJointTour);
			//alternative.AddUtilityTerm(90, halfTourFromOriginFlag);



			// 4 - PERSONAL BUSINESS STOP


			alternative = choiceProbabilityCalculator.GetAlternative(4,
				((personDay.PersonalBusinessStops > 0 || isJointTour == 1)
				&& halfTour.SimulatedTrips <= 5
				&& timeIsAvailableForAnotherTrip),
				choice == Constants.Purpose.PERSONAL_BUSINESS);

			alternative.Choice = Constants.Purpose.PERSONAL_BUSINESS;
			//alternative.AddNestedAlternative(_nestedAlternativeIds[4], _nestedAlternativeIndexes[4], THETA_PARAMETER);

			alternative.AddUtilityTerm(91, 1.0);
			//alternative.AddUtilityTerm(92, (workTourFlag + schoolTourFlag + businessTourFlag));
			//alternative.AddUtilityTerm(92, isJointTour);
			//alternative.AddUtilityTerm(93, escortTourFlag);
			//alternative.AddUtilityTerm(94, personalBusinessOrMedicalTourFlag * isIndividualTour);
			//alternative.AddUtilityTerm(95, shoppingTourFlag);
			//alternative.AddUtilityTerm(96, mealTourFlag);
			//alternative.AddUtilityTerm(97, socialOrRecreationTourFlag);
			//alternative.AddUtilityTerm(98, halfTourFromOriginFlag);
			//alternative.AddUtilityTerm(99, simulatedPersonalBusinessStops * isIndividualTour);
			//alternative.AddUtilityTerm(100, simulatedPersonalBusinessStops * isJointTour);
			//alternative.AddUtilityTerm(101, duration);
			//alternative.AddUtilityTerm(102, (from7AMto9AMFlag + from7PMto9PMFlag + from9PMto11PMFlag + from11PMto7AMFlag));

			//alternative.AddUtilityTerm(103, from9AMto11AMFlag + from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag);
			alternative.AddUtilityTerm(103, from9AMto3PMFlag + from3PMto6PMFlag);

			//alternative.AddUtilityTerm(105, hovDriverTourFlag);
			//alternative.AddUtilityTerm(106, hovPassengerTourFlag);
			//alternative.AddUtilityTerm(109, jointHalfOfFullJointHalfTour);
			//alternative.AddUtilityTerm(110, totEmpBuffer2);
			//alternative.AddUtilityTerm(111, totalAggregateLogsum);
			//alternative.AddUtilityTerm(112, personalBusinessOrMedicalTourFlag * isJointTour);



			// 5 - SHOPPING STOP

			alternative = choiceProbabilityCalculator.GetAlternative(5,
				((personDay.ShoppingStops > 0 || isJointTour == 1)
				&& halfTour.SimulatedTrips <= 5
				&& timeIsAvailableForAnotherTrip),
				choice == Constants.Purpose.SHOPPING);

			alternative.Choice = Constants.Purpose.SHOPPING;
			//alternative.AddNestedAlternative(_nestedAlternativeIds[5], _nestedAlternativeIndexes[5], THETA_PARAMETER);

			alternative.AddUtilityTerm(121, 1.0);
			//alternative.AddUtilityTerm(122, workTourFlag + schoolTourFlag + businessTourFlag);
			//alternative.AddUtilityTerm(122, isJointTour);
			//alternative.AddUtilityTerm(123, escortTourFlag);
			//alternative.AddUtilityTerm(124, personalBusinessOrMedicalTourFlag);
			//alternative.AddUtilityTerm(125, shoppingTourFlag * isIndividualTour);
			//alternative.AddUtilityTerm(125, shoppingTourFlag);
			//alternative.AddUtilityTerm(126, mealTourFlag);
			//alternative.AddUtilityTerm(127, socialOrRecreationTourFlag);
			//alternative.AddUtilityTerm(128, halfTourFromOriginFlag);
			//alternative.AddUtilityTerm(129, simulatedShoppingStops * isIndividualTour);
			//alternative.AddUtilityTerm(130, simulatedShoppingStops * isJointTour);
			//alternative.AddUtilityTerm(131, duration);

			//alternative.AddUtilityTerm(132, from7AMto9AMFlag + from9PMto11PMFlag + from11PMto7AMFlag);
			//alternative.AddUtilityTerm(133, (from11AMto1PMFlag + from1PMto3PMFlag + from3PMto5PMFlag));

			//alternative.AddUtilityTerm(132, from7AMto9AMFlag + from6PMto10PMFlag);
			alternative.AddUtilityTerm(133, (from9AMto3PMFlag + from3PMto6PMFlag + from6PMto10PMFlag));

			//alternative.AddUtilityTerm(134, adultFemaleOnJointTour);
			//alternative.AddUtilityTerm(135, hovDriverTourFlag);
			//alternative.AddUtilityTerm(136, hovPassengerTourFlag);
			//alternative.AddUtilityTerm(137, Math.Log(1 + adis));
			//alternative.AddUtilityTerm(138, shoppingTourFlag * isJointTour);
			//alternative.AddUtilityTerm(140, shoppingAggregateLogsum);
			//alternative.AddUtilityTerm(141, retailBuffer2);
			//alternative.AddUtilityTerm(142, numChildrenOnJointTour);
			//alternative.AddUtilityTerm(143, (household.Has100KPlusIncome).ToFlag());
			alternative.AddUtilityTerm(134, primaryFamilyTimeFlag);



			// 6 - MEAL STOP

			//alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEAL, false, choice == Constants.Purpose.MEAL);
			//alternative.Choice = Constants.Purpose.MEAL;
			//alternative.AddNestedAlternative(12, 2, THETA_PARAMETER);


			// 6 - SOCIAL (OR RECREATION) STOP

			alternative = choiceProbabilityCalculator.GetAlternative(6,
				((personDay.SocialStops > 0 || isJointTour == 1)
				&& halfTour.SimulatedTrips <= 5
				&& timeIsAvailableForAnotherTrip),
				choice == Constants.Purpose.SOCIAL);

			alternative.Choice = Constants.Purpose.SOCIAL;
			//alternative.AddNestedAlternative(_nestedAlternativeIds[6], _nestedAlternativeIndexes[6], THETA_PARAMETER);

			alternative.AddUtilityTerm(181, 1.0);
			//alternative.AddUtilityTerm(182, workTourFlag + schoolTourFlag + businessTourFlag);
			//alternative.AddUtilityTerm(182, isJointTour);
			//alternative.AddUtilityTerm(183, escortTourFlag);
			//alternative.AddUtilityTerm(184, personalBusinessOrMedicalTourFlag);
			//alternative.AddUtilityTerm(185, shoppingTourFlag);
			//alternative.AddUtilityTerm(186, mealTourFlag);
			//alternative.AddUtilityTerm(187, socialOrRecreationTourFlag);
			//alternative.AddUtilityTerm(188, halfTourFromOriginFlag);
			//alternative.AddUtilityTerm(189, simulatedSocialStops * isIndividualTour);
			//alternative.AddUtilityTerm(197, simulatedSocialStops * isJointTour);
			//alternative.AddUtilityTerm(190, remainingToursCount);
			//alternative.AddUtilityTerm(191, duration);

			//alternative.AddUtilityTerm(192, from7AMto9AMFlag + from11PMto7AMFlag);
			//alternative.AddUtilityTerm(192, from7AMto9AMFlag);
			alternative.AddUtilityTerm(192, from9AMto3PMFlag + from3PMto6PMFlag + from6PMto10PMFlag);

			//alternative.AddUtilityTerm(194, hovDriverTourFlag);
			//alternative.AddUtilityTerm(195, hovPassengerTourFlag);
			//alternative.AddUtilityTerm(196, logDist);
			//alternative.AddUtilityTerm(200, numAdultsOnJointTour);


			// 8 - RECREATION STOP

			//alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.RECREATION, false, choice == Constants.Purpose.RECREATION);
			//alternative.Choice = Constants.Purpose.RECREATION;
			//alternative.AddNestedAlternative(12, 2, THETA_PARAMETER);

			// 9 - MEDICAL STOP

			//alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEDICAL, false, choice == Constants.Purpose.MEDICAL);
			//alternative.Choice = Constants.Purpose.MEDICAL;
			//alternative.AddNestedAlternative(12, 2, THETA_PARAMETER);


		}
	}
}