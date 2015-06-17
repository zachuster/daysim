﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;

using Daysim.DomainModels.Actum;


namespace Daysim.ChoiceModels.Actum {
	public class ActumMandatoryTourGenerationModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumMandatoryTourGenerationModel";
		private const int TOTAL_ALTERNATIVES = 4;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 60;

		public int Run(ActumPersonDayWrapper personDay, ActumHouseholdDayWrapper householdDay, int nCallsForTour, int[] simulatedMandatoryTours, int choice = Constants.Purpose.NONE_OR_HOME) {
			if (personDay == null) {
				throw new ArgumentNullException("personDay");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.MandatoryTourGenerationModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

			personDay.Person.ResetRandom(904 + nCallsForTour);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return choice;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator((personDay.Person.Id * 397) ^ nCallsForTour);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				RunModel(choiceProbabilityCalculator, personDay, householdDay, nCallsForTour, simulatedMandatoryTours, choice);
				choiceProbabilityCalculator.WriteObservation();
			}

			else if (Global.Configuration.TestEstimationModelInApplicationMode) {
				Global.Configuration.IsInEstimationMode = false;

				RunModel(choiceProbabilityCalculator, personDay, householdDay, nCallsForTour, simulatedMandatoryTours);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility, personDay.Id, choice);

				Global.Configuration.IsInEstimationMode = true;
			}

			else {
				RunModel(choiceProbabilityCalculator, personDay, householdDay, nCallsForTour, simulatedMandatoryTours);
				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(personDay.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;
				if (choice == 1) {
					personDay.UsualWorkplaceTours++;
					personDay.WorkTours++;
				}
				else if (choice == 2) {
					personDay.BusinessTours++;
				}
				else if (choice == 3) {
					personDay.SchoolTours++;
				}
			}

			return choice;
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumPersonDayWrapper personDay, ActumHouseholdDayWrapper householdDay, int nCallsForTour, int[] simulatedMandatoryTours, int choice = Constants.DEFAULT_VALUE) {
			var household = personDay.Household;

			Double workTourLogsum;
			if (personDay.Person.UsualWorkParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualWorkParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
				var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumWorkTourModeModel") as ActumWorkTourModeModel).RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
				workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
			}
			else {
				workTourLogsum = 0;
			}

			Double schoolTourLogsum;
			if (personDay.Person.UsualSchoolParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualSchoolParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
				var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumSchoolTourModeModel") as ActumSchoolTourModeModel).RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualSchoolParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
				schoolTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
			}
			else {
				schoolTourLogsum = 0;
			}

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
			var personalBusinessAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.PERSONAL_BUSINESS][carOwnership][votALSegment][transitAccessSegment];
			var shoppingAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.SHOPPING][carOwnership][votALSegment][transitAccessSegment];
			var mealAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.MEAL][carOwnership][votALSegment][transitAccessSegment];
			var socialAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.SOCIAL][carOwnership][votALSegment][transitAccessSegment];
			//var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];
			var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.NO_CARS][votALSegment][transitAccessSegment];

			//int hasAdultEducLevel12 = 0;
			//int allAdultEducLevel12 = 1;
			int youngestAge = 999;

			foreach (ActumPersonWrapper person in personDay.Household.Persons) {
				// set characteristics here that depend on person characteristics
				//if (person.Age >= 18 && person.EducationLevel >= 12) hasAdultEducLevel12 = 1;
				//if (person.Age >= 18 && person.EducationLevel < 12) allAdultEducLevel12 = 0;
				if (person.Age < youngestAge) youngestAge = person.Age;
			}

			bool schoolAvailableFlag = true;
			if ((!personDay.Person.IsStudent) || (!Global.Configuration.IsInEstimationMode && personDay.Person.UsualSchoolParcel == null)) {
				schoolAvailableFlag = false;
			}

			// NONE_OR_HOME

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, nCallsForTour > 1, choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;
			//alternative.AddUtilityTerm(1, (nCallsForTour > 2).ToFlag()); // GV; 16.april 2013 - cannot be estimated

			//alternative.AddUtilityTerm(2, householdDay.Household.HasChildren.ToFlag());
			alternative.AddUtilityTerm(3, householdDay.Household.HasChildrenUnder5.ToFlag());
			alternative.AddUtilityTerm(4, householdDay.Household.HasChildrenAge5Through15.ToFlag());
			//alternative.AddUtilityTerm(6, householdDay.Household.HasChildrenUnder16.ToFlag());

			//alternative.AddUtilityTerm(10, (householdDay.Household.Size == 2).ToFlag()); // GV; 16. april 2013 - cannot be estimated
			alternative.AddUtilityTerm(11, (householdDay.Household.Size == 3).ToFlag());
			alternative.AddUtilityTerm(12, (householdDay.Household.Size >= 4).ToFlag());

			//alternative.AddUtilityTerm(14, (householdDay.Household.Income >= 300000 && householdDay.Household.Income < 600000).ToFlag());
			//alternative.AddUtilityTerm(15, (householdDay.Household.Income >= 600000 && householdDay.Household.Income < 900000).ToFlag());
			//alternative.AddUtilityTerm(16, (householdDay.Household.Income >= 900000).ToFlag());

			//alternative.AddNestedAlternative(11, 0, 60); 


			// USUAL WORK
			alternative = choiceProbabilityCalculator.GetAlternative(1, (personDay.Person.UsualWorkParcelId > 0 && simulatedMandatoryTours[2] == 0), choice == 1);
			alternative.Choice = 1;
			alternative.AddUtilityTerm(21, 1);

			//alternative.AddUtilityTerm(22, personDay.Person.IsChildUnder5.ToFlag());
			alternative.AddUtilityTerm(23, personDay.Person.WorksAtHome.ToFlag());
			alternative.AddUtilityTerm(24, personDay.Person.IsFulltimeWorker.ToFlag());
			alternative.AddUtilityTerm(25, personDay.Person.IsMale.ToFlag());
			//alternative.AddUtilityTerm(4, person.IsPartTimeWorker.ToFlag());

			alternative.AddUtilityTerm(26, (householdDay.AdultsInSharedHomeStay == 2 && householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
			//alternative.AddUtilityTerm(14, (householdDay.AdultsInSharedHomeStay == 2).ToFlag());
			//alternative.AddUtilityTerm(15, (householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
			alternative.AddUtilityTerm(27, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildren).ToFlag());

			alternative.AddUtilityTerm(28, workTourLogsum);

			//alternative.AddUtilityTerm(28, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());
			alternative.AddUtilityTerm(29, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());

			alternative.AddUtilityTerm(30, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddNestedAlternative(12, 1, 60);

			// BUSINESS
			alternative = choiceProbabilityCalculator.GetAlternative(2, (personDay.Person.IsWorker && simulatedMandatoryTours[3] == 0), choice == 2);
			alternative.Choice = 2;
			alternative.AddUtilityTerm(31, 1);

			//alternative.AddUtilityTerm(32, personDay.Person.IsChildUnder5.ToFlag());
			alternative.AddUtilityTerm(33, personDay.Person.WorksAtHome.ToFlag());
			alternative.AddUtilityTerm(34, personDay.Person.IsFulltimeWorker.ToFlag());
			alternative.AddUtilityTerm(35, personDay.Person.IsMale.ToFlag());
			//alternative.AddUtilityTerm(4, person.IsPartTimeWorker.ToFlag());

			alternative.AddUtilityTerm(36, (householdDay.AdultsInSharedHomeStay == 2 && householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
			//alternative.AddUtilityTerm(14, (householdDay.AdultsInSharedHomeStay == 2).ToFlag());
			//alternative.AddUtilityTerm(15, (householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
			alternative.AddUtilityTerm(37, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildren).ToFlag());

			alternative.AddUtilityTerm(38, workTourLogsum);

			//alternative.AddUtilityTerm(38, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());
			alternative.AddUtilityTerm(39, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());

			alternative.AddUtilityTerm(40, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddNestedAlternative(12, 1, 60);

			// SCHOOL
			alternative = choiceProbabilityCalculator.GetAlternative(3, schoolAvailableFlag, choice == 3);
			alternative.Choice = 3;
			alternative.AddUtilityTerm(41, 1);

			alternative.AddUtilityTerm(42, personDay.Person.IsNonworkingAdult.ToFlag());
			//alternative.AddUtilityTerm(43, personDay.Person.IsPartTimeWorker.ToFlag());
			alternative.AddUtilityTerm(43, personDay.Person.IsYouth.ToFlag());

			//alternative.AddUtilityTerm(46, (householdDay.AdultsInSharedHomeStay == 2 && householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
			//alternative.AddUtilityTerm(14, (householdDay.AdultsInSharedHomeStay == 2).ToFlag());
			//alternative.AddUtilityTerm(15, (householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
			//alternative.AddUtilityTerm(47, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildren).ToFlag());

			alternative.AddUtilityTerm(48, workTourLogsum);
			alternative.AddUtilityTerm(49, schoolTourLogsum);

			//alternative.AddUtilityTerm(48, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());
			//alternative.AddUtilityTerm(49, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());

			alternative.AddUtilityTerm(50, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddNestedAlternative(12, 1, 60);

		}
	}
}