﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Linq;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using Daysim.DomainModels.Actum;
using System.Collections.Generic;

namespace Daysim.ChoiceModels.Actum {
	public class ActumHouseholdDayPatternTypeModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumHouseholdDayPatternTypeModel";
		private const int TOTAL_ALTERNATIVES = 363;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 700;

		public void Run(ActumHouseholdDayWrapper householdDay, int choice = 0) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			int numberPersonsModeledJointly = 4;  // set this at compile time depending on whether we want to support 4 or 5 household members in this joint model

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.HouseholdDayPatternTypeModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

			householdDay.ResetRandom(902);

			IEnumerable<ActumPersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.Person.HouseholdDayPatternParticipationPriority).ToList().Cast<ActumPersonDayWrapper>();

			var ptypes = new int[6];
			var hhsize = householdDay.Household.Size;
			if (Global.Configuration.IsInEstimationMode) {
				int count = 0;
				foreach (ActumPersonDayWrapper personDay in orderedPersonDays) {
					count++;

					if (personDay.WorkTours > 0 || personDay.SchoolTours > 0 || personDay.BusinessTours > 0) {
						personDay.PatternType = Constants.PatternType.MANDATORY;
					}
					else if (personDay.TotalTours > 0) {
						personDay.PatternType = Constants.PatternType.NONMANDATORY;
					}
					else {
						personDay.PatternType = Constants.PatternType.HOME;
					}
					if (count <= numberPersonsModeledJointly) {
						ptypes[count] = personDay.PatternType;
					}
				}
				if (numberPersonsModeledJointly == 4) {
					if (hhsize == 1) { choice = ptypes[1] - 1; }
					else if (hhsize == 2) { choice = ptypes[1] * 3 + ptypes[2] - 1; }
					else if (hhsize == 3) { choice = ptypes[1] * 9 + ptypes[2] * 3 + ptypes[3] - 1; }
					else { choice = ptypes[1] * 27 + ptypes[2] * 9 + ptypes[3] * 3 + ptypes[4] - 1; }
				}
				else {  // ie numberPersonsModeledJointly == 5
					if (hhsize == 1) { choice = ptypes[1] - 1; }
					else if (hhsize == 2) { choice = ptypes[1] * 3 + ptypes[2] - 1; }
					else if (hhsize == 3) { choice = ptypes[1] * 9 + ptypes[2] * 3 + ptypes[3] - 1; }
					else if (hhsize == 4) { choice = ptypes[1] * 27 + ptypes[2] * 9 + ptypes[3] * 3 + ptypes[4] - 1; }
					else { choice = ptypes[1] * 81 + ptypes[2] * 27 + ptypes[3] * 9 + ptypes[4] * 3 + ptypes[5] - 1; }
				}

				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(householdDay.Household.Id);

			// array associating alternative with the purposes of each of the five possible positions in the alternative
			//  altPTypes[a,p] is the purpose of position p in alternative a  
			int[,] altPTypes = new int[,] {
				//{0,0,0,0,0,0},
				{0,1,0,0,0,0},
				{0,2,0,0,0,0},
				{0,3,0,0,0,0},
				{0,1,1,0,0,0},
				{0,1,2,0,0,0},
				{0,1,3,0,0,0},
				{0,2,1,0,0,0},
				{0,2,2,0,0,0},
				{0,2,3,0,0,0},
				{0,3,1,0,0,0},
				{0,3,2,0,0,0},
				{0,3,3,0,0,0},
				{0,1,1,1,0,0},
				{0,1,1,2,0,0},
				{0,1,1,3,0,0},
				{0,1,2,1,0,0},
				{0,1,2,2,0,0},
				{0,1,2,3,0,0},
				{0,1,3,1,0,0},
				{0,1,3,2,0,0},
				{0,1,3,3,0,0},
				{0,2,1,1,0,0},
				{0,2,1,2,0,0},
				{0,2,1,3,0,0},
				{0,2,2,1,0,0},
				{0,2,2,2,0,0},
				{0,2,2,3,0,0},
				{0,2,3,1,0,0},
				{0,2,3,2,0,0},
				{0,2,3,3,0,0},
				{0,3,1,1,0,0},
				{0,3,1,2,0,0},
				{0,3,1,3,0,0},
				{0,3,2,1,0,0},
				{0,3,2,2,0,0},
				{0,3,2,3,0,0},
				{0,3,3,1,0,0},
				{0,3,3,2,0,0},
				{0,3,3,3,0,0},
				{0,1,1,1,1,0},
				{0,1,1,1,2,0},
				{0,1,1,1,3,0},
				{0,1,1,2,1,0},
				{0,1,1,2,2,0},
				{0,1,1,2,3,0},
				{0,1,1,3,1,0},
				{0,1,1,3,2,0},
				{0,1,1,3,3,0},
				{0,1,2,1,1,0},
				{0,1,2,1,2,0},
				{0,1,2,1,3,0},
				{0,1,2,2,1,0},
				{0,1,2,2,2,0},
				{0,1,2,2,3,0},
				{0,1,2,3,1,0},
				{0,1,2,3,2,0},
				{0,1,2,3,3,0},
				{0,1,3,1,1,0},
				{0,1,3,1,2,0},
				{0,1,3,1,3,0},
				{0,1,3,2,1,0},
				{0,1,3,2,2,0},
				{0,1,3,2,3,0},
				{0,1,3,3,1,0},
				{0,1,3,3,2,0},
				{0,1,3,3,3,0},
				{0,2,1,1,1,0},
				{0,2,1,1,2,0},
				{0,2,1,1,3,0},
				{0,2,1,2,1,0},
				{0,2,1,2,2,0},
				{0,2,1,2,3,0},
				{0,2,1,3,1,0},
				{0,2,1,3,2,0},
				{0,2,1,3,3,0},
				{0,2,2,1,1,0},
				{0,2,2,1,2,0},
				{0,2,2,1,3,0},
				{0,2,2,2,1,0},
				{0,2,2,2,2,0},
				{0,2,2,2,3,0},
				{0,2,2,3,1,0},
				{0,2,2,3,2,0},
				{0,2,2,3,3,0},
				{0,2,3,1,1,0},
				{0,2,3,1,2,0},
				{0,2,3,1,3,0},
				{0,2,3,2,1,0},
				{0,2,3,2,2,0},
				{0,2,3,2,3,0},
				{0,2,3,3,1,0},
				{0,2,3,3,2,0},
				{0,2,3,3,3,0},
				{0,3,1,1,1,0},
				{0,3,1,1,2,0},
				{0,3,1,1,3,0},
				{0,3,1,2,1,0},
				{0,3,1,2,2,0},
				{0,3,1,2,3,0},
				{0,3,1,3,1,0},
				{0,3,1,3,2,0},
				{0,3,1,3,3,0},
				{0,3,2,1,1,0},
				{0,3,2,1,2,0},
				{0,3,2,1,3,0},
				{0,3,2,2,1,0},
				{0,3,2,2,2,0},
				{0,3,2,2,3,0},
				{0,3,2,3,1,0},
				{0,3,2,3,2,0},
				{0,3,2,3,3,0},
				{0,3,3,1,1,0},
				{0,3,3,1,2,0},
				{0,3,3,1,3,0},
				{0,3,3,2,1,0},
				{0,3,3,2,2,0},
				{0,3,3,2,3,0},
				{0,3,3,3,1,0},
				{0,3,3,3,2,0},
				{0,3,3,3,3,0},
				{0,1,1,1,1,1},
				{0,1,1,1,1,2},
				{0,1,1,1,1,3},
				{0,1,1,1,2,1},
				{0,1,1,1,2,2},
				{0,1,1,1,2,3},
				{0,1,1,1,3,1},
				{0,1,1,1,3,2},
				{0,1,1,1,3,3},
				{0,1,1,2,1,1},
				{0,1,1,2,1,2},
				{0,1,1,2,1,3},
				{0,1,1,2,2,1},
				{0,1,1,2,2,2},
				{0,1,1,2,2,3},
				{0,1,1,2,3,1},
				{0,1,1,2,3,2},
				{0,1,1,2,3,3},
				{0,1,1,3,1,1},
				{0,1,1,3,1,2},
				{0,1,1,3,1,3},
				{0,1,1,3,2,1},
				{0,1,1,3,2,2},
				{0,1,1,3,2,3},
				{0,1,1,3,3,1},
				{0,1,1,3,3,2},
				{0,1,1,3,3,3},
				{0,1,2,1,1,1},
				{0,1,2,1,1,2},
				{0,1,2,1,1,3},
				{0,1,2,1,2,1},
				{0,1,2,1,2,2},
				{0,1,2,1,2,3},
				{0,1,2,1,3,1},
				{0,1,2,1,3,2},
				{0,1,2,1,3,3},
				{0,1,2,2,1,1},
				{0,1,2,2,1,2},
				{0,1,2,2,1,3},
				{0,1,2,2,2,1},
				{0,1,2,2,2,2},
				{0,1,2,2,2,3},
				{0,1,2,2,3,1},
				{0,1,2,2,3,2},
				{0,1,2,2,3,3},
				{0,1,2,3,1,1},
				{0,1,2,3,1,2},
				{0,1,2,3,1,3},
				{0,1,2,3,2,1},
				{0,1,2,3,2,2},
				{0,1,2,3,2,3},
				{0,1,2,3,3,1},
				{0,1,2,3,3,2},
				{0,1,2,3,3,3},
				{0,1,3,1,1,1},
				{0,1,3,1,1,2},
				{0,1,3,1,1,3},
				{0,1,3,1,2,1},
				{0,1,3,1,2,2},
				{0,1,3,1,2,3},
				{0,1,3,1,3,1},
				{0,1,3,1,3,2},
				{0,1,3,1,3,3},
				{0,1,3,2,1,1},
				{0,1,3,2,1,2},
				{0,1,3,2,1,3},
				{0,1,3,2,2,1},
				{0,1,3,2,2,2},
				{0,1,3,2,2,3},
				{0,1,3,2,3,1},
				{0,1,3,2,3,2},
				{0,1,3,2,3,3},
				{0,1,3,3,1,1},
				{0,1,3,3,1,2},
				{0,1,3,3,1,3},
				{0,1,3,3,2,1},
				{0,1,3,3,2,2},
				{0,1,3,3,2,3},
				{0,1,3,3,3,1},
				{0,1,3,3,3,2},
				{0,1,3,3,3,3},
				{0,2,1,1,1,1},
				{0,2,1,1,1,2},
				{0,2,1,1,1,3},
				{0,2,1,1,2,1},
				{0,2,1,1,2,2},
				{0,2,1,1,2,3},
				{0,2,1,1,3,1},
				{0,2,1,1,3,2},
				{0,2,1,1,3,3},
				{0,2,1,2,1,1},
				{0,2,1,2,1,2},
				{0,2,1,2,1,3},
				{0,2,1,2,2,1},
				{0,2,1,2,2,2},
				{0,2,1,2,2,3},
				{0,2,1,2,3,1},
				{0,2,1,2,3,2},
				{0,2,1,2,3,3},
				{0,2,1,3,1,1},
				{0,2,1,3,1,2},
				{0,2,1,3,1,3},
				{0,2,1,3,2,1},
				{0,2,1,3,2,2},
				{0,2,1,3,2,3},
				{0,2,1,3,3,1},
				{0,2,1,3,3,2},
				{0,2,1,3,3,3},
				{0,2,2,1,1,1},
				{0,2,2,1,1,2},
				{0,2,2,1,1,3},
				{0,2,2,1,2,1},
				{0,2,2,1,2,2},
				{0,2,2,1,2,3},
				{0,2,2,1,3,1},
				{0,2,2,1,3,2},
				{0,2,2,1,3,3},
				{0,2,2,2,1,1},
				{0,2,2,2,1,2},
				{0,2,2,2,1,3},
				{0,2,2,2,2,1},
				{0,2,2,2,2,2},
				{0,2,2,2,2,3},
				{0,2,2,2,3,1},
				{0,2,2,2,3,2},
				{0,2,2,2,3,3},
				{0,2,2,3,1,1},
				{0,2,2,3,1,2},
				{0,2,2,3,1,3},
				{0,2,2,3,2,1},
				{0,2,2,3,2,2},
				{0,2,2,3,2,3},
				{0,2,2,3,3,1},
				{0,2,2,3,3,2},
				{0,2,2,3,3,3},
				{0,2,3,1,1,1},
				{0,2,3,1,1,2},
				{0,2,3,1,1,3},
				{0,2,3,1,2,1},
				{0,2,3,1,2,2},
				{0,2,3,1,2,3},
				{0,2,3,1,3,1},
				{0,2,3,1,3,2},
				{0,2,3,1,3,3},
				{0,2,3,2,1,1},
				{0,2,3,2,1,2},
				{0,2,3,2,1,3},
				{0,2,3,2,2,1},
				{0,2,3,2,2,2},
				{0,2,3,2,2,3},
				{0,2,3,2,3,1},
				{0,2,3,2,3,2},
				{0,2,3,2,3,3},
				{0,2,3,3,1,1},
				{0,2,3,3,1,2},
				{0,2,3,3,1,3},
				{0,2,3,3,2,1},
				{0,2,3,3,2,2},
				{0,2,3,3,2,3},
				{0,2,3,3,3,1},
				{0,2,3,3,3,2},
				{0,2,3,3,3,3},
				{0,3,1,1,1,1},
				{0,3,1,1,1,2},
				{0,3,1,1,1,3},
				{0,3,1,1,2,1},
				{0,3,1,1,2,2},
				{0,3,1,1,2,3},
				{0,3,1,1,3,1},
				{0,3,1,1,3,2},
				{0,3,1,1,3,3},
				{0,3,1,2,1,1},
				{0,3,1,2,1,2},
				{0,3,1,2,1,3},
				{0,3,1,2,2,1},
				{0,3,1,2,2,2},
				{0,3,1,2,2,3},
				{0,3,1,2,3,1},
				{0,3,1,2,3,2},
				{0,3,1,2,3,3},
				{0,3,1,3,1,1},
				{0,3,1,3,1,2},
				{0,3,1,3,1,3},
				{0,3,1,3,2,1},
				{0,3,1,3,2,2},
				{0,3,1,3,2,3},
				{0,3,1,3,3,1},
				{0,3,1,3,3,2},
				{0,3,1,3,3,3},
				{0,3,2,1,1,1},
				{0,3,2,1,1,2},
				{0,3,2,1,1,3},
				{0,3,2,1,2,1},
				{0,3,2,1,2,2},
				{0,3,2,1,2,3},
				{0,3,2,1,3,1},
				{0,3,2,1,3,2},
				{0,3,2,1,3,3},
				{0,3,2,2,1,1},
				{0,3,2,2,1,2},
				{0,3,2,2,1,3},
				{0,3,2,2,2,1},
				{0,3,2,2,2,2},
				{0,3,2,2,2,3},
				{0,3,2,2,3,1},
				{0,3,2,2,3,2},
				{0,3,2,2,3,3},
				{0,3,2,3,1,1},
				{0,3,2,3,1,2},
				{0,3,2,3,1,3},
				{0,3,2,3,2,1},
				{0,3,2,3,2,2},
				{0,3,2,3,2,3},
				{0,3,2,3,3,1},
				{0,3,2,3,3,2},
				{0,3,2,3,3,3},
				{0,3,3,1,1,1},
				{0,3,3,1,1,2},
				{0,3,3,1,1,3},
				{0,3,3,1,2,1},
				{0,3,3,1,2,2},
				{0,3,3,1,2,3},
				{0,3,3,1,3,1},
				{0,3,3,1,3,2},
				{0,3,3,1,3,3},
				{0,3,3,2,1,1},
				{0,3,3,2,1,2},
				{0,3,3,2,1,3},
				{0,3,3,2,2,1},
				{0,3,3,2,2,2},
				{0,3,3,2,2,3},
				{0,3,3,2,3,1},
				{0,3,3,2,3,2},
				{0,3,3,2,3,3},
				{0,3,3,3,1,1},
				{0,3,3,3,1,2},
				{0,3,3,3,1,3},
				{0,3,3,3,2,1},
				{0,3,3,3,2,2},
				{0,3,3,3,2,3},
				{0,3,3,3,3,1},
				{0,3,3,3,3,2},
				{0,3,3,3,3,3}};


			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {

				RunModel(choiceProbabilityCalculator, householdDay, altPTypes, numberPersonsModeledJointly, choice);

				choiceProbabilityCalculator.WriteObservation();
			}

			else if (Global.Configuration.TestEstimationModelInApplicationMode) {
				Global.Configuration.IsInEstimationMode = false;

				RunModel(choiceProbabilityCalculator, householdDay, altPTypes, numberPersonsModeledJointly);

				//var observedChoice = new HTourModeTime(tour.Mode, tour.DestinationArrivalTime, tour.DestinationDepartureTime);

				//var simulatedChoice = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility, householdDay.Household.Id, altPTypes.);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility, householdDay.Household.Id, choice);

				Global.Configuration.IsInEstimationMode = true;
			}

			else {
				RunModel(choiceProbabilityCalculator, householdDay, altPTypes, numberPersonsModeledJointly);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;

				int i = 0;
				foreach (ActumPersonDayWrapper personDay in orderedPersonDays) {
					i++;
					if (i <= numberPersonsModeledJointly) {
						personDay.PatternType = altPTypes[choice, i];
					}
				}
			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumHouseholdDayWrapper householdDay, int[,] altPTypes, int numberPersonsModeledJointly, int choice = Constants.DEFAULT_VALUE) {

			bool includeThreeWayInteractions = false;   // set this at compile time, dependign on whether we want to include or exclude 3-way interactions.
			int numberPersonTypes = 7;  // set this at compile time; 7 for Actum
			int numberAlternatives = numberPersonsModeledJointly == 4 ? 120 : 363;

			IEnumerable<ActumPersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.Person.HouseholdDayPatternParticipationPriority).ToList().Cast<ActumPersonDayWrapper>();
			var hhsize = householdDay.Household.Size;

			var household = householdDay.Household;

			var carOwnership =
						household.VehiclesAvailable == 0
							? Constants.CarOwnership.NO_CARS
							: household.VehiclesAvailable < household.HouseholdTotals.DrivingAgeMembers
								? Constants.CarOwnership.LT_ONE_CAR_PER_ADULT
								: Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT;

			var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);
			var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership);

			var votALSegment = Constants.VotALSegment.MEDIUM;  // TODO:  calculate a VOT segment that depends on household income
			var transitAccessSegment = household.ResidenceParcel.TransitAccessSegment();
			//var personalBusinessAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
			//	[Constants.Purpose.PERSONAL_BUSINESS][carOwnership][votALSegment][transitAccessSegment];
			var shoppingAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.SHOPPING][carOwnership][votALSegment][transitAccessSegment];
			//var mealAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
			//	[Constants.Purpose.MEAL][carOwnership][votALSegment][transitAccessSegment];
			//var socialAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
			//	[Constants.Purpose.SOCIAL][carOwnership][votALSegment][transitAccessSegment];
			//var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];
			var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.NO_CARS][votALSegment][transitAccessSegment];



			int[,] pt = new int[6, 9];
			int[] li = new int[6];
			int[] ui = new int[6];
			int[] hi = new int[6];
			int[] hc = new int[6];
			int[] lc = new int[6];
			double[] ra = new double[6];
			double[] ea = new double[6];
			int[] c0to1 = new int[6];
			int[] c4to5 = new int[6];
			int[] c6to9 = new int[6];
			int[] c13to15 = new int[6];
			int[] c18to21 = new int[6];
			int[] fem = new int[6];
			int[] rto80 = new int[6];
			int[] wku40 = new int[6];
			int[] wknok = new int[6];
			int[] wkhom = new int[6];
			int[] wtmis = new int[6];
			int[] stmis = new int[6];
			int[] utmis = new int[6];
			int[] pfpt = new int[6];

			int ct = 0;

			foreach (ActumPersonDayWrapper personDay in orderedPersonDays) {
				ct++;
				if (ct <= numberPersonsModeledJointly) {
					ActumPersonWrapper person = (ActumPersonWrapper) personDay.Person;
					// calculate accessibility to work or school
					Double mandatoryLogsum = 0;
					if (person.ActumPersonType <= Constants.ActumPersonType.PART_TIME_WORKER) {
						if (person.UsualWorkParcelId != Constants.DEFAULT_VALUE && person.UsualWorkParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
							if (person.UsualDeparturePeriodFromWork != Constants.DEFAULT_VALUE && person.UsualArrivalPeriodToWork != Constants.DEFAULT_VALUE) {
								var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumWorkTourModeModel") as ActumWorkTourModeModel).RunNested(person, person.Household.ResidenceParcel, person.UsualWorkParcel, person.UsualArrivalPeriodToWork, person.UsualDeparturePeriodFromWork, person.Household.HouseholdTotals.DrivingAgeMembers);
								mandatoryLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
							}
							else {
								var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumWorkTourModeModel") as ActumWorkTourModeModel).RunNested(person, person.Household.ResidenceParcel, person.UsualWorkParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, person.Household.HouseholdTotals.DrivingAgeMembers);
								mandatoryLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
							}
						}
						else {
							mandatoryLogsum = 0;
						}
					}
					else if (person.ActumPersonType >= Constants.ActumPersonType.GYMNASIUM_OR_UNIVERSITY_STUDENT) {
						if (person.UsualSchoolParcelId != 0 && person.UsualSchoolParcelId != -1 && person.UsualSchoolParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
							var schoolNestedAlternative = (Global.ChoiceModelDictionary.Get("ActumSchoolTourModeModel") as ActumSchoolTourModeModel).RunNested(person, person.Household.ResidenceParcel, person.UsualSchoolParcel, Constants.Time.EIGHT_AM, Constants.Time.TWO_PM, person.Household.HouseholdTotals.DrivingAgeMembers);
							mandatoryLogsum = schoolNestedAlternative == null ? 0 : schoolNestedAlternative.ComputeLogsum();
						}
						else {
							mandatoryLogsum = 0;
						}
					}

					// set characteristics here that depend on HH/person characteristics

					// person types
					pt[ct, person.ActumPersonType] = 1;

					// HH income
					li[ct] = (householdDay.Household.Income > 300000 && householdDay.Household.Income <= 600000) ? 1 : 0; // GV changed income; all is relative now to income below 300.000 DKK
					ui[ct] = (householdDay.Household.Income > 600000 && householdDay.Household.Income <= 900000) ? 1 : 0;
					//hi[ct] = (householdDay.Household.Income > 900000 && householdDay.Household.Income <= 1200000) ? 1 : 0;
					hi[ct] = (householdDay.Household.Income > 900000) ? 1 : 0;

					// cars
					//hc[ct] = (householdDay.Household.VehiclesAvailable >= householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers) ? 1 : 0;
					//lc[ct] = (householdDay.Household.VehiclesAvailable < householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers) ? 1 : 0;
					lc[ct] = (householdDay.Household.VehiclesAvailable == 1) ? 1 : 0; //GV changed car ownership to a) 1 (lc) and b) 2+ (hc)
					hc[ct] = (householdDay.Household.VehiclesAvailable >= 2) ? 1 : 0;

					// Logsums
					//ra[ct] = shoppingAggregateLogsum; //retail accessibility
					ra[ct] = compositeLogsum; //GV: changed to composite logsum
					ea[ct] = mandatoryLogsum; //xxxx accessibility

					// Age
					c0to1[ct] = (person.Age == 0 || person.Age == 1) ? 1 : 0;
					c4to5[ct] = (person.Age == 4 || person.Age == 5) ? 1 : 0;
					c6to9[ct] = (person.Age >= 6 && person.Age <= 9) ? 1 : 0;
					c13to15[ct] = (person.Age >= 13 && person.Age <= 15) ? 1 : 0;
					c18to21[ct] = (person.Age >= 18 && person.Age <= 21 && person.ActumPersonType == Constants.ActumPersonType.GYMNASIUM_OR_UNIVERSITY_STUDENT) ? 1 : 0;
					rto80[ct] = (person.Age >= 80 && person.Age <= 98 && person.ActumPersonType == Constants.ActumPersonType.RETIRED_ADULT) ? 1 : 0;
					wku40[ct] = (person.Age >= 16 && person.Age <= 39 && person.IsFullOrPartTimeWorker) ? 1 : 0;


					// extra variables
					wknok[ct] = (person.IsFullOrPartTimeWorker && !householdDay.Household.HasChildren) ? 1 : 0;
					wkhom[ct] = (person.IsFullOrPartTimeWorker && person.WorksAtHome) ? 1 : 0;
					wtmis[ct] = (person.IsFullOrPartTimeWorker && person.UsualWorkParcel == null) ? 1 : 0;
					stmis[ct] = (person.IsChildUnder16 && person.UsualSchoolParcel == null) ? 1 : 0;
					utmis[ct] = (person.ActumPersonType == Constants.ActumPersonType.GYMNASIUM_OR_UNIVERSITY_STUDENT && person.UsualSchoolParcel == null) ? 1 : 0;

					// gender
					fem[ct] = person.Gender == Constants.PersonGender.FEMALE ? 1 : 0;

					// PFPT
					pfpt[ct] = (householdDay.PrimaryPriorityTimeFlag > 0) ? 1 : 0;

				}
			}


			//two-way interaction variables
			int[, , , , , ,] i2 = new int[9, 9, 6, 6, 6, 6, 6];
			// interaction terms for each pair of two different persons in the householdDayPatternType
			// pt[p1,t1] = 1 if person p1 (who may have any Id between 1 and p2-1) has person type t1
			// pt[p2,t2] = 1 if person p2 (who may have any Id between 2 and 5) has person type t2
			// i2[t1,t2,p1,p2,0,0,0] = 1 if person p1 has person type t1 and person p2 has person type t2
			if (hhsize >= 2) {
				for (var t2 = 1; t2 <= numberPersonTypes; t2++) {
					for (var t1 = 1; t1 <= t2; t1++) {

						for (var p2 = 2; p2 <= numberPersonsModeledJointly; p2++) {
							for (var p1 = 1; p1 < p2; p1++) {
								i2[t1, t2, p1, p2, 0, 0, 0] = pt[p1, t1] * pt[p2, t2];  // i2[t1,t2,p1,p2,0,0,0] = 1 if person p1 has person type t1 and person p2 has person type t2
								if (t1 != t2) {
									i2[t1, t2, p1, p2, 0, 0, 0] = i2[t1, t2, p1, p2, 0, 0, 0] + pt[p1, t2] * pt[p2, t1]; // i2[t1,t2,p1,p2,0,0,0] = or if one person p2 person type t1 and person p1 has person type t2
								}
							}
						}


						// pairwise interaction terms for each triplet of three different persons in the householdDayPatternType
						// constructed from two-way interaction variables
						// p1, p2 and p3 must be in ascending personType sequence 

						if (hhsize >= 3) {
							for (var p3 = 3; p3 <= numberPersonsModeledJointly; p3++) {
								for (var p2 = 2; p2 < p3; p2++) {
									for (var p1 = 1; p1 < p2; p1++) {
										i2[t1, t2, p1, p2, p3, 0, 0] = i2[t1, t2, p1, p2, 0, 0, 0]
																			  + i2[t1, t2, p1, p3, 0, 0, 0]
																			  + i2[t1, t2, p2, p3, 0, 0, 0];
									}
								}
							}
						}

						// pairwise interaction terms for each quadruplet of four different persons in the householdDayPatternType

						if (hhsize >= 4) {
							for (var p4 = 4; p4 <= numberPersonsModeledJointly; p4++) {
								for (var p3 = 3; p3 < p4; p3++) {
									for (var p2 = 2; p2 < p3; p2++) {
										for (var p1 = 1; p1 < p2; p1++) {
											i2[t1, t2, p1, p2, p3, p4, 0] = i2[t1, t2, p1, p2, 0, 0, 0]
																					+ i2[t1, t2, p1, p3, 0, 0, 0]
																					+ i2[t1, t2, p1, p4, 0, 0, 0]
																					+ i2[t1, t2, p2, p3, 0, 0, 0]
																					+ i2[t1, t2, p2, p4, 0, 0, 0]
																					+ i2[t1, t2, p3, p4, 0, 0, 0];
										}
									}
								}
							}
						}

						// pairwise interaction terms for the five different persons in the householdDayPatternType
						if (hhsize >= 5 && numberPersonsModeledJointly == 5) {
							i2[t1, t2, 1, 2, 3, 4, 5] = i2[t1, t2, 1, 2, 0, 0, 0]
															  + i2[t1, t2, 1, 3, 0, 0, 0]
															  + i2[t1, t2, 1, 4, 0, 0, 0]
															  + i2[t1, t2, 1, 5, 0, 0, 0]
															  + i2[t1, t2, 2, 3, 0, 0, 0]
															  + i2[t1, t2, 2, 4, 0, 0, 0]
															  + i2[t1, t2, 2, 5, 0, 0, 0]
															  + i2[t1, t2, 3, 4, 0, 0, 0]
															  + i2[t1, t2, 3, 5, 0, 0, 0]
															  + i2[t1, t2, 4, 5, 0, 0, 0];
						}
					}
				}
			}

			// 3-way interaction variables
			int[, , , , , , ,] i3 = new int[4, 4, 4, 6, 6, 6, 6, 6];
			int[,] xt = new int[6, 4];

			if (includeThreeWayInteractions) {

				ct = 0;
				foreach (ActumPersonDayWrapper personDay in orderedPersonDays) {
					ct++;
					if (ct <= numberPersonsModeledJointly) {
						if (personDay.Person.PersonType == 1) { xt[ct, 1] = 1; }
						if (personDay.Person.PersonType == 2 || personDay.Person.PersonType == 4) { xt[ct, 2] = 1; }
						if (personDay.Person.PersonType == 7 || personDay.Person.PersonType == 8) { xt[ct, 3] = 1; }
					}
				}

				// 3-way interaction terms for each triplet of persons in the householdDayPatternType
				// p1, p2 and p3 must be in ascending personType sequence 
				if (hhsize >= 3) {
					for (var t3 = 1; t3 <= 3; t3++) {
						//for (var t2 = 1; t2 <= t3; t2++) {
						//for (var t1 = 1; t1 <= t2; t1++) {
						for (var t2 = 1; t2 <= 3; t2++) {
							for (var t1 = 1; t1 <= 3; t1++) {

								for (var p3 = 3; p3 <= numberPersonsModeledJointly; p3++) {
									for (var p2 = 2; p2 < p3; p2++) {
										for (var p1 = 1; p1 < p2; p1++) {
											i3[t1, t2, t3, p1, p2, p3, 0, 0] = xt[p1, t1] * xt[p2, t2] * xt[p3, t3];
											if (t2 != t3) {
												i3[t1, t2, t3, p1, p2, p3, 0, 0] = i3[t1, t2, t3, p1, p2, p3, 0, 0] + xt[p1, t1] * xt[p2, t3] * xt[p3, t2];
											}
											if (t1 != t2) {
												i3[t1, t2, t3, p1, p2, p3, 0, 0] = i3[t1, t2, t3, p1, p2, p3, 0, 0] + xt[p1, t2] * xt[p2, t1] * xt[p3, t3];
											}
											if (t1 != t3) {
												i3[t1, t2, t3, p1, p2, p3, 0, 0] = i3[t1, t2, t3, p1, p2, p3, 0, 0] + xt[p1, t3] * xt[p2, t2] * xt[p3, t1];
											}
											if (t1 != t3 && t1 != t2 && t2 != t3) {
												i3[t1, t2, t3, p1, p2, p3, 0, 0] = i3[t1, t2, t3, p1, p2, p3, 0, 0] + xt[p1, t2] * xt[p2, t3] * xt[p3, t1] + xt[p1, t3] * xt[p2, t1] * xt[p3, t2];
											}
										}
									}
								}

								// 3-way interaction terms for each quadruplet of persons in the householdDayPatternType
								if (hhsize >= 4) {
									for (var p4 = 4; p4 <= 5; p4++) {
										for (var p3 = 3; p3 < p4; p3++) {
											for (var p2 = 2; p2 < p3; p2++) {
												for (var p1 = 1; p1 < p2; p1++) {
													i3[t1, t2, t3, p1, p2, p3, p4, 0] = i3[t1, t2, t3, p1, p2, p3, 0, 0]
																								 + i3[t1, t2, t3, p1, p2, p4, 0, 0]
																								 + i3[t1, t2, t3, p1, p3, p4, 0, 0]
																								 + i3[t1, t2, t3, p2, p3, p4, 0, 0];
												}
											}
										}
									}
								}

								// 3-way interaction terms for the five different persons in the householdDayPatternType
								if (hhsize >= 5 && numberPersonsModeledJointly == 5) {
									i3[t1, t2, t3, 1, 2, 3, 4, 5]
									= i3[t1, t2, t3, 1, 2, 3, 0, 0]
									+ i3[t1, t2, t3, 1, 2, 4, 0, 0]
									+ i3[t1, t2, t3, 1, 2, 5, 0, 0]
									+ i3[t1, t2, t3, 1, 3, 4, 0, 0]
									+ i3[t1, t2, t3, 1, 3, 5, 0, 0]
									+ i3[t1, t2, t3, 1, 4, 5, 0, 0]
									+ i3[t1, t2, t3, 2, 3, 4, 0, 0]
									+ i3[t1, t2, t3, 2, 3, 5, 0, 0]
									+ i3[t1, t2, t3, 2, 4, 5, 0, 0]
									+ i3[t1, t2, t3, 3, 4, 5, 0, 0];
								}
							}
						}
					}
				}
			}

			int[,] component1 = new int[4, 6];
			int[, , , , ,] component2 = new int[4, 6, 6, 6, 6, 6];
			int[, , , , ,] component3 = new int[4, 6, 6, 6, 6, 6];
			int compNum = 0;

			for (var purp = 1; purp <= 3; purp++) {
				for (var p1 = 1; p1 <= numberPersonsModeledJointly; p1++) {
					//create the personPurpose component
					compNum++;
					component1[purp, p1] = compNum;
					choiceProbabilityCalculator.CreateUtilityComponent(compNum);
					//populate the personPurpose component with utility terms

					//person type coeff.
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 1, pt[p1, 1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 2, pt[p1, 2]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 3, pt[p1, 3]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 4, pt[p1, 4]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 5, pt[p1, 5]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 6, pt[p1, 6]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 7, pt[p1, 7]);

					// PFPT
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 8, pt[p1, 1] * pfpt[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 9, pt[p1, 2] * pfpt[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 10, pt[p1, 3] * pfpt[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 11, pt[p1, 4] * pfpt[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 12, pt[p1, 5] * pfpt[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 13, pt[p1, 6] * pfpt[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 14, pt[p1, 7] * pfpt[p1]);

					//Age - GV; omited
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 15, c0to1[p1]); 
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 16, c4to5[p1]); 
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 17, c6to9[p1]);
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 18, c13to15[p1]);
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 19, c18to21[p1]);
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 20, rto80[p1]);
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 21, wku40[p1]);

					//Gender, female
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 15, pt[p1, 1] * fem[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 16, pt[p1, 2] * fem[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 17, pt[p1, 3] * fem[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 18, pt[p1, 4] * fem[p1]);
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 19, pt[p1, 5] * fem[p1]);
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 20, pt[p1, 6] * fem[p1]);
					//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 21, pt[p1, 7] * fem[p1]);

					//Car availability, 2+ cars
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 22, pt[p1, 1] * hc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 23, pt[p1, 2] * hc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 24, pt[p1, 3] * hc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 25, pt[p1, 4] * hc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 26, pt[p1, 5] * hc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 27, pt[p1, 6] * hc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 28, pt[p1, 7] * hc[p1]);

					//Car availability, 1 cars
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 29, pt[p1, 1] * lc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 30, pt[p1, 2] * lc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 31, pt[p1, 3] * lc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 32, pt[p1, 4] * lc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 33, pt[p1, 5] * lc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 34, pt[p1, 6] * lc[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 35, pt[p1, 7] * lc[p1]);

					//Income, 300-600.000
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 36, pt[p1, 1] * li[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 37, pt[p1, 2] * li[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 38, pt[p1, 3] * li[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 39, pt[p1, 4] * li[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 40, pt[p1, 5] * li[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 41, pt[p1, 6] * li[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 42, pt[p1, 7] * li[p1]);

					//Income, 600-900.000
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 43, pt[p1, 1] * ui[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 44, pt[p1, 2] * ui[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 45, pt[p1, 3] * ui[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 46, pt[p1, 4] * ui[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 47, pt[p1, 5] * ui[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 48, pt[p1, 6] * ui[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 49, pt[p1, 7] * ui[p1]);

					//Income, >900.000
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 50, pt[p1, 1] * hi[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 51, pt[p1, 2] * hi[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 52, pt[p1, 3] * hi[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 53, pt[p1, 4] * hi[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 54, pt[p1, 5] * hi[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 55, pt[p1, 6] * hi[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 56, pt[p1, 7] * hi[p1]);

					//Logsums
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 57, pt[p1, 1] * ea[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 58, pt[p1, 2] * ea[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 59, pt[p1, 3] * ea[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 60, pt[p1, 4] * ea[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 61, pt[p1, 5] * ea[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 62, pt[p1, 6] * ea[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 63, pt[p1, 7] * ea[p1]);

					//Logsums
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 64, pt[p1, 1] * ra[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 65, pt[p1, 2] * ra[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 66, pt[p1, 3] * ra[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 66, pt[p1, 4] * ra[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 66, pt[p1, 5] * ra[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 69, pt[p1, 6] * ra[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 69, pt[p1, 7] * ra[p1]);

					//Work location - GV; all the coeff. in .f12 are set to 0
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 71, wkhom[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 72, wtmis[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 73, stmis[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 74, utmis[p1]);
					choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 75, wknok[p1]);
					// TODO : Add more personPurpose component terms


					// set up 2-person and 3-person interaction components (always need to set them in estimation mode)
					if (hhsize >= 2 || Global.Configuration.IsInEstimationMode) {
						for (var p2 = (p1 + 1); p2 <= numberPersonsModeledJointly; p2++) {
							//create the 2-way component for cases where 2 people share a purpose
							compNum++;
							component2[purp, p1, p2, 0, 0, 0] = compNum;
							choiceProbabilityCalculator.CreateUtilityComponent(compNum);
							//populate the 2-way component with utility terms
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 301, i2[1, 1, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 302, i2[1, 2, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 303, i2[1, 3, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 304, i2[1, 4, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 305, i2[1, 5, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 306, i2[1, 6, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 307, i2[1, 7, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 308, i2[2, 2, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 309, i2[2, 3, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 310, i2[2, 4, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 311, i2[2, 5, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 312, i2[2, 6, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 313, i2[2, 7, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 314, i2[3, 3, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 315, i2[3, 4, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 316, i2[3, 5, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 316, i2[3, 6, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 316, i2[3, 7, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 319, i2[4, 4, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 320, i2[4, 5, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 321, i2[4, 6, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 322, i2[4, 7, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 323, i2[5, 5, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 324, i2[5, 6, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 325, i2[5, 7, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 326, i2[6, 6, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 327, i2[6, 7, p1, p2, 0, 0, 0]);
							choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 328, i2[7, 7, p1, p2, 0, 0, 0]);
							if (numberPersonTypes == 8) {
								choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 329, i2[1, 8, p1, p2, 0, 0, 0]);
								choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 330, i2[2, 8, p1, p2, 0, 0, 0]);
								choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 331, i2[3, 8, p1, p2, 0, 0, 0]);
								choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 332, i2[4, 8, p1, p2, 0, 0, 0]);
								choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 333, i2[5, 8, p1, p2, 0, 0, 0]);
								choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 334, i2[6, 8, p1, p2, 0, 0, 0]);
								choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 335, i2[7, 8, p1, p2, 0, 0, 0]);
								choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 336, i2[8, 8, p1, p2, 0, 0, 0]);
							}

							if (hhsize >= 3 || Global.Configuration.IsInEstimationMode) {
								for (var p3 = (p2 + 1); p3 <= 5; p3++) {
									//create the 2-way component for cases where three people share a purpose
									compNum++;
									component2[purp, p1, p2, p3, 0, 0] = compNum;
									choiceProbabilityCalculator.CreateUtilityComponent(compNum);
									//populate the 2-way component with utility terms for cases where three people share a purpose
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 301, i2[1, 1, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 302, i2[1, 2, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 303, i2[1, 3, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 304, i2[1, 4, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 305, i2[1, 5, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 306, i2[1, 6, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 307, i2[1, 7, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 308, i2[2, 2, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 309, i2[2, 3, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 310, i2[2, 4, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 311, i2[2, 5, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 312, i2[2, 6, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 313, i2[2, 7, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 314, i2[3, 3, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 315, i2[3, 4, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 316, i2[3, 5, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 317, i2[3, 6, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 318, i2[3, 7, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 319, i2[4, 4, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 320, i2[4, 5, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 321, i2[4, 6, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 322, i2[4, 7, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 323, i2[5, 5, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 324, i2[5, 6, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 325, i2[5, 7, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 326, i2[6, 6, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 327, i2[6, 7, p1, p2, p3, 0, 0]);
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 328, i2[7, 7, p1, p2, p3, 0, 0]);
									if (numberPersonTypes == 8) {
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 329, i2[1, 8, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 330, i2[2, 8, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 331, i2[3, 8, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 332, i2[4, 8, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 333, i2[5, 8, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 334, i2[6, 8, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 335, i2[7, 8, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 336, i2[8, 8, p1, p2, p3, 0, 0]);
									}

									//create the 3-way component with utility terms for cases where three people share a purpose
									compNum++;
									component3[purp, p1, p2, p3, 0, 0] = compNum;
									choiceProbabilityCalculator.CreateUtilityComponent(compNum);
									//populate the 3-way component with utility terms for cases where three people share a purpose
									if (includeThreeWayInteractions) {
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 337, i3[1, 1, 1, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 338, i3[1, 1, 2, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 339, i3[1, 1, 3, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 340, i3[1, 2, 2, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 341, i3[1, 2, 3, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 342, i3[1, 3, 3, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 343, i3[2, 2, 2, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 344, i3[2, 2, 3, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 345, i3[2, 3, 3, p1, p2, p3, 0, 0]);
										choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 346, i3[3, 3, 3, p1, p2, p3, 0, 0]);
									}
									//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 353, hhsize >= 3 ? 1.0 : 0.0); // exactly 3 of up to 5 persons in HH have same pattern type
									choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 353, hhsize == 3 ? 1.0 : 0.0); // GV changed: 3-way interactions in the HH==3

									if (hhsize >= 4 || Global.Configuration.IsInEstimationMode) {
										for (var p4 = (p3 + 1); p4 <= 5; p4++) {
											//create the 2-way component for cases where four people share a purpose
											compNum++;
											component2[purp, p1, p2, p3, p4, 0] = compNum;
											choiceProbabilityCalculator.CreateUtilityComponent(compNum);
											//populate the 2-way component with utility terms for cases where four people share a purpose
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 301, i2[1, 1, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 302, i2[1, 2, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 303, i2[1, 3, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 304, i2[1, 4, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 305, i2[1, 5, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 306, i2[1, 6, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 307, i2[1, 7, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 308, i2[2, 2, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 309, i2[2, 3, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 310, i2[2, 4, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 311, i2[2, 5, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 312, i2[2, 6, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 313, i2[2, 7, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 314, i2[3, 3, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 315, i2[3, 4, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 316, i2[3, 5, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 317, i2[3, 6, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 318, i2[3, 7, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 319, i2[4, 4, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 320, i2[4, 5, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 321, i2[4, 6, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 322, i2[4, 7, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 323, i2[5, 5, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 324, i2[5, 6, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 325, i2[5, 7, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 326, i2[6, 6, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 327, i2[6, 7, p1, p2, p3, p4, 0]);
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 328, i2[7, 7, p1, p2, p3, p4, 0]);
											if (numberPersonTypes == 8) {
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 329, i2[1, 8, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 330, i2[2, 8, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 331, i2[3, 8, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 332, i2[4, 8, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 333, i2[5, 8, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 334, i2[6, 8, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 335, i2[7, 8, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 336, i2[8, 8, p1, p2, p3, p4, 0]);
											}

											//create the 3-way component with utility terms for cases where four people share a purpose
											compNum++;
											component3[purp, p1, p2, p3, p4, 0] = compNum;
											choiceProbabilityCalculator.CreateUtilityComponent(compNum);
											//populate the 3-way component with utility terms for cases where four people share a purpose
											if (includeThreeWayInteractions) {
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 337, i3[1, 1, 1, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 338, i3[1, 1, 2, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 339, i3[1, 1, 3, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 340, i3[1, 2, 2, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 341, i3[1, 2, 3, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 342, i3[1, 3, 3, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 343, i3[2, 2, 2, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 344, i3[2, 2, 3, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 345, i3[2, 3, 3, p1, p2, p3, p4, 0]);
												choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 346, i3[3, 3, 3, p1, p2, p3, p4, 0]);
											}
											//choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 354, hhsize >= 4 ? 1.0 : 0.0);  // exactly 4 of up to 5 persons in HH have same pattern type
											choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 354, hhsize >= 4 ? 1.0 : 0.0);  // GV changed: 4-way interactions in the HH==4

											if ((hhsize >= 5 || Global.Configuration.IsInEstimationMode) && numberPersonsModeledJointly == 5) {
												for (var p5 = (p4 + 1); p5 <= numberPersonsModeledJointly; p5++) {
													//create the 2-way component for cases where five people share a purpose
													compNum++;
													component2[purp, p1, p2, p3, p4, p5] = compNum;
													choiceProbabilityCalculator.CreateUtilityComponent(compNum);
													//populate the 2-way component with utility terms for cases where five people share a purpose
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 301, i2[1, 1, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 302, i2[1, 2, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 303, i2[1, 3, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 304, i2[1, 4, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 305, i2[1, 5, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 306, i2[1, 6, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 307, i2[1, 7, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 308, i2[2, 2, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 309, i2[2, 3, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 310, i2[2, 4, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 311, i2[2, 5, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 312, i2[2, 6, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 313, i2[2, 7, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 314, i2[3, 3, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 315, i2[3, 4, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 316, i2[3, 5, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 317, i2[3, 6, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 318, i2[3, 7, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 319, i2[4, 4, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 320, i2[4, 5, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 321, i2[4, 6, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 322, i2[4, 7, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 323, i2[5, 5, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 324, i2[5, 6, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 325, i2[5, 7, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 326, i2[6, 6, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 327, i2[6, 7, p1, p2, p3, p4, p5]);
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 328, i2[7, 7, p1, p2, p3, p4, p5]);
													if (numberPersonTypes == 8) {
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 329, i2[1, 8, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 330, i2[2, 8, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 331, i2[3, 8, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 332, i2[4, 8, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 333, i2[5, 8, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 334, i2[6, 8, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 335, i2[7, 8, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 336, i2[8, 8, p1, p2, p3, p4, p5]);
													}

													//create the 3-way component for cases where five people share a purpose
													compNum++;
													component3[purp, p1, p2, p3, p4, p5] = compNum;
													choiceProbabilityCalculator.CreateUtilityComponent(compNum);
													//populate the 3-way component with utility terms for cases where five people share a purpose
													if (includeThreeWayInteractions) {
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 337, i3[1, 1, 1, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 338, i3[1, 1, 2, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 339, i3[1, 1, 3, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 340, i3[1, 2, 2, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 341, i3[1, 2, 3, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 342, i3[1, 3, 3, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 343, i3[2, 2, 2, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 344, i3[2, 2, 3, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 345, i3[2, 3, 3, p1, p2, p3, p4, p5]);
														choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 346, i3[3, 3, 3, p1, p2, p3, p4, p5]);
													}
													choiceProbabilityCalculator.GetUtilityComponent(compNum).AddUtilityTerm(100 * purp + 355, hhsize >= 5 ? 1.0 : 0.0);  // exactly 5 of up to 5 persons in HH have same pattern type
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}

			//Generate utility funtions for the alternatives
			bool[] available = new bool[numberAlternatives];
			bool[] chosen = new bool[numberAlternatives];
			for (int alt = 0; alt <= numberAlternatives - 1; alt++) {

				available[alt] = false;
				chosen[alt] = false;
				// set availability based on household size
				if ((hhsize == 1 && (alt + 1) <= 3)
				|| (hhsize == 2 && (alt + 1) >= 4 && (alt + 1) <= 12)
				|| (hhsize == 3 && (alt + 1) >= 13 && (alt + 1) <= 39)
				|| ((hhsize == 4 || (hhsize >= 4 && numberPersonsModeledJointly == 4)) && (alt + 1) >= 40 && (alt + 1) <= 120)
				|| (hhsize >= 5 && numberPersonsModeledJointly == 5 && (alt + 1) >= 121 && (alt + 1) <= 363)) {
					available[alt] = true;
				}

				// limit availability of work patterns for people who are neither worker nor student
				ct = 0;
				foreach (ActumPersonDayWrapper personDay in orderedPersonDays) {
					ct++;
					//if (ct <= 5 && altPTypes[alt, ct] == 1 && !personDay.Person.IsWorker && !personDay.Person.IsStudent) {
					if (ct <= numberPersonsModeledJointly && altPTypes[alt, ct] == 1 &&
						(personDay.Person.IsNonworkingAdult || personDay.Person.IsRetiredAdult ||
						(!personDay.Person.IsWorker && !personDay.Person.IsStudent) ||
						(!Global.Configuration.IsInEstimationMode && !personDay.Person.IsWorker && personDay.Person.UsualSchoolParcel == null)
						)) {
						available[alt] = false;
					}
				}



                // GORAN: WHEN YOU RE-ESTIMATE THIS MODEL FOR THE PAPER AND FOR COMPAS, YOU NEED TO FIRST UNCOMMENT THE FOLLOWING LOGIC.
                //    THIS WILL ADDRESS THE CONCERN THE REVIEWERS EXPRESSED ABOUT INCONSISTENT ASSUMPTIONS IN THE HDAP MODEL.
                //    IN OTHER WORDS, WITHOUT THIS CONSTRAINT THE MODEL ASSUMES THAT IT WOULD BE POSSIBLE TO HAVE A PATTERN IN WHICH
                //    PFPT OCCURS AND NOBODY OVER AGE 12 TRAVELS.  BUT THE DATA DOESN'T ALLOW THIS TO HAPPEN.
                //    THE RESULT WOULD BE THAT PRESENCE OF PFPT WOULD HAVE UNREALISTICALLY POSITIVE EFFECT ON THE PRESENCE OF PATTERNS
                //    WITH TRAVEL

                // prohibit HDAPs if PFPT occurs and there are no on-tour persons over age 12 (since PFPT was not reported for these households)
                bool onTourPersonOver12 = false;  //UNCOMMENT THIS LINE
                ct = 0;//UNCOMMENT THIS LINE
                foreach (PersonDayWrapper personDay in orderedPersonDays)
                {//UNCOMMENT THIS LINE
                    ct++;//UNCOMMENT THIS LINE
                    if (ct <= numberPersonsModeledJointly && altPTypes[alt, ct] != 3 && personDay.Person.Age >= 13)//UNCOMMENT THIS LINE 
                    {//UNCOMMENT THIS LINE
                        onTourPersonOver12 = true;//UNCOMMENT THIS LINE
                    }//UNCOMMENT THIS LINE
                }//UNCOMMENT THIS LINE
                if (onTourPersonOver12 = false && householdDay.PrimaryPriorityTimeFlag == 1)
                {//UNCOMMENT THIS LINE
                    available[alt] = false;//UNCOMMENT THIS LINE
                }//UNCOMMENT THIS LINE
                // THIS ENDS THE SECTION WHERE UNCOMMENTING NEEDS TO OCCUR 
				
                                
                
                
                
                // determine choice 
				if (choice == alt) { chosen[alt] = true; }

				//Get the alternative
				var alternative = choiceProbabilityCalculator.GetAlternative(alt, available[alt], chosen[alt]);

				alternative.Choice = alt;

				for (var purp = 1; purp <= 3; purp++) {

					//Add utility components
					//MB>  This looks like it should work fine.  It's actually a bit different and potentially better than the way I coded 'writeut', because that only 
					// allowed the interactions to be applied to one purpose per alterantive, but if you have 4+ people, there could actually be 2 different 2-person pairs
					// such as in an M-N-N-M  pattern, that has both MM and NN pairs. 
					int componentPosition = 1;
					int[] componentP = new int[6];
					for (var p = 1; p <= numberPersonsModeledJointly; p++) {
						if (altPTypes[alt, p] == purp) {
							alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(component1[purp, p]));

							//alternatives position-purpose matches current purpose; note this position in current component position for component types 2 and 3  
							componentP[componentPosition] = p;
							componentPosition++;
						}
					}
					if (componentP[1] > 0 && componentP[2] > componentP[1]
						&& (componentP[3] > componentP[2] || componentP[3] == 0)
						&& (componentP[4] > componentP[3] || componentP[4] == 0)
						&& (componentP[4] > componentP[2] || componentP[4] == 0)
						&& (componentP[5] > componentP[4] || componentP[5] == 0)
						&& (componentP[5] > componentP[3] || componentP[5] == 0)
						&& (componentP[5] > componentP[2] || componentP[5] == 0)
						&& choiceProbabilityCalculator.GetUtilityComponent(component2[purp, componentP[1], componentP[2], componentP[3], componentP[4], componentP[5]]) != null) {
						alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(component2[purp, componentP[1], componentP[2], componentP[3], componentP[4], componentP[5]]));
					}
					//			if (includeThreeWayInteractions) {
					if (componentP[1] > 0 && componentP[2] > componentP[1] && componentP[3] > componentP[2]
						&& (componentP[4] > componentP[3] || componentP[4] == 0)
						&& (componentP[5] > componentP[4] || componentP[5] == 0)
						&& (componentP[5] > componentP[3] || componentP[5] == 0)
						&& choiceProbabilityCalculator.GetUtilityComponent(component3[purp, componentP[1], componentP[2], componentP[3], componentP[4], componentP[5]]) != null) {
						alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(component3[purp, componentP[1], componentP[2], componentP[3], componentP[4], componentP[5]]));
					}
					//			}
				}
			}
		}
	}
}
