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
	public class ActumJointTourGenerationModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumJointTourGenerationModel";
		private const int TOTAL_ALTERNATIVES = Constants.Purpose.MEDICAL;
		private const int TOTAL_NESTED_ALTERNATIVES = 2;
		private const int TOTAL_LEVELS = 2;
		private const int MAX_PARAMETER = 60;

		public int Run(ActumHouseholdDayWrapper householdDay, int nCallsForTour, int choice = Constants.Purpose.NONE_OR_HOME) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.JointTourGenerationModelCoefficients, TOTAL_ALTERNATIVES,
						  TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

			householdDay.ResetRandom(935 + nCallsForTour); // TODO:  fix the ResetRandom call parameter

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return choice;
				}
			}

			var choiceProbabilityCalculator =
				_helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator((householdDay.Household.Id * 397) ^ 2 * nCallsForTour - 1);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, choice);

				choiceProbabilityCalculator.WriteObservation();
			}

			else if (Global.Configuration.TestEstimationModelInApplicationMode) {
				Global.Configuration.IsInEstimationMode = false;

				//choice = Math.Min(personDay.BusinessStops, 1) + 2 * Math.Min(personDay.SchoolStops, 1);

				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility, householdDay.Household.Id, choice);

				Global.Configuration.IsInEstimationMode = true;
			}

			else {
				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;
			}

			return choice;
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumHouseholdDayWrapper householdDay,
									 int nCallsForTour, int choice = Constants.DEFAULT_VALUE) {
			//var householdDay = (ActumHouseholdDayWrapper)tour.HouseholdDay;
			var household = householdDay.Household;

			var carOwnership =
				household.VehiclesAvailable == 0
					? Constants.CarOwnership.NO_CARS
					: household.VehiclesAvailable < household.HouseholdTotals.DrivingAgeMembers
						  ? Constants.CarOwnership.LT_ONE_CAR_PER_ADULT
						  : Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT;

			var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);
			var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership);

			var votALSegment = Constants.VotALSegment.MEDIUM; // TODO:  calculate a VOT segment that depends on household income
			var transitAccessSegment = household.ResidenceParcel.TransitAccessSegment();
			var personalBusinessAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.PERSONAL_BUSINESS][carOwnership][votALSegment][transitAccessSegment];
			var shoppingAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				//[Constants.Purpose.SHOPPING][carOwnership][votALSegment][transitAccessSegment];
				[Constants.Purpose.SHOPPING][Constants.CarOwnership.NO_CARS][votALSegment][transitAccessSegment];
			var mealAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.MEAL][carOwnership][votALSegment][transitAccessSegment];
			var socialAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.SOCIAL][carOwnership][votALSegment][transitAccessSegment];
			//var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];
			var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.NO_CARS][votALSegment][transitAccessSegment];


			int hasAdultEducLevel12 = 0;
			//int allAdultEducLevel12 = 1;
			int youngestAge = 999;

			foreach (ActumPersonWrapper person in householdDay.Household.Persons) {
				// set characteristics here that depend on person characteristics
				if (person.Age >= 18 && person.EducationLevel >= 12) hasAdultEducLevel12 = 1;
				//if (person.Age >= 18 && person.EducationLevel < 12) allAdultEducLevel12 = 0;
				if (person.Age < youngestAge) youngestAge = person.Age;
			}

			// NONE_OR_HOME

			var noneOrHomeAvailable = true;
			if (Global.Configuration.ShouldRunActumPrimaryPriorityTimeModel && householdDay.JointTourFlag == 1
				&& nCallsForTour == 1) {
				noneOrHomeAvailable = false;
			}

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, noneOrHomeAvailable, choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;
            alternative.AddUtilityTerm(1, (nCallsForTour == 2).ToFlag());
            alternative.AddUtilityTerm(13, (nCallsForTour > 2).ToFlag());
            
            //alternative.AddUtilityTerm(2, noCarsFlag);
            //alternative.AddUtilityTerm(3, carCompetitionFlag);
            //alternative.AddUtilityTerm(4, householdDay.PrimaryPriorityTimeFlag);


            //alternative.AddUtilityTerm(2, householdDay.Household.HasChildren.ToFlag());

            //GV Sep 2014 - commented out
            //alternative.AddUtilityTerm(2, householdDay.Household.HasChildrenUnder5.ToFlag());
            //alternative.AddUtilityTerm(3, householdDay.Household.HasChildrenAge5Through15.ToFlag());
            //alternative.AddUtilityTerm(4, (householdDay.Household.Size == 2 && householdDay.AdultsInSharedHomeStay == 2).ToFlag());
            //alternative.AddUtilityTerm(5, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildrenUnder16).ToFlag());
            //alternative.AddUtilityTerm(6, (householdDay.AdultsInSharedHomeStay == 2 && householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
            //alternative.AddUtilityTerm(7, (householdDay.AdultsInSharedHomeStay == 2 && hasAdultEducLevel12 == 1).ToFlag());
            //alternative.AddUtilityTerm(8, (youngestAge >= 40).ToFlag());
            //alternative.AddUtilityTerm(10, (householdDay.Household.Income >= 300000 && householdDay.Household.Income < 600000).ToFlag());
            //alternative.AddUtilityTerm(11, (householdDay.Household.Income >= 600000 && householdDay.Household.Income < 900000).ToFlag());
            //alternative.AddUtilityTerm(12, (householdDay.Household.Income >= 900000).ToFlag());

            
            //GV old
            //alternative.AddNestedAlternative(11, 0, 60);


			// WORK
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.WORK, false, choice == Constants.Purpose.WORK);
			alternative.Choice = Constants.Purpose.WORK;
			alternative.AddUtilityTerm(52, 1);

			//alternative.AddNestedAlternative(12, 1, 60);
                       
            
            // SCHOOL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SCHOOL, false, choice == Constants.Purpose.SCHOOL);
			alternative.Choice = Constants.Purpose.SCHOOL;
			alternative.AddUtilityTerm(53, 1);

			//alternative.AddNestedAlternative(12, 1, 60);



			// ESCORT
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.ESCORT, false, choice == Constants.Purpose.ESCORT);
			alternative.Choice = Constants.Purpose.ESCORT;
			alternative.AddUtilityTerm(54, 1);
			//alternative.AddUtilityTerm(22, householdDay.PrimaryPriorityTimeFlag);
			//alternative.AddUtilityTerm(23, (householdDay.Household.Size == 3).ToFlag());
			//alternative.AddUtilityTerm(24, (householdDay.Household.Size >= 4).ToFlag());
			//alternative.AddUtilityTerm(25, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());
			//alternative.AddUtilityTerm(58, compositeLogsum);


			// PERSONAL_BUSINESS
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.PERSONAL_BUSINESS, true, choice == Constants.Purpose.PERSONAL_BUSINESS);
			alternative.Choice = Constants.Purpose.PERSONAL_BUSINESS;


            alternative.AddUtilityTerm(21, 1);
            //GV: NEW
            //alternative.AddUtilityTerm(22, householdDay.PrimaryPriorityTimeFlag);
            alternative.AddUtilityTerm(23, (householdDay.Household.Size == 3).ToFlag());
            alternative.AddUtilityTerm(24, (householdDay.Household.Size >= 4).ToFlag());
            alternative.AddUtilityTerm(28, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());
            alternative.AddUtilityTerm(56, compositeLogsum);


            
            //alternative.AddUtilityTerm(25, (householdDay.Household.Size >= 5).ToFlag());

            //alternative.AddUtilityTerm(26, (householdDay.Household.VehiclesAvailable == 0).ToFlag());
            //alternative.AddUtilityTerm(27, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());
            //alternative.AddUtilityTerm(28, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());

            //alternative.AddUtilityTerm(27, (householdDay.Household.Size == 2 && householdDay.AdultsInSharedHomeStay == 2).ToFlag());
            //alternative.AddUtilityTerm(28, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildrenUnder16).ToFlag());

            //alternative.AddUtilityTerm(56, personalBusinessAggregateLogsum);
            
            //alternative.AddUtilityTerm(56, compositeLogsum);


			//alternative.AddNestedAlternative(12, 1, 60);

			// SHOPPING
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SHOPPING, true, choice == Constants.Purpose.SHOPPING);
			alternative.Choice = Constants.Purpose.SHOPPING;

            alternative.AddUtilityTerm(31, 1);

            //GV: NEW
            //alternative.AddUtilityTerm(32, householdDay.PrimaryPriorityTimeFlag);
            alternative.AddUtilityTerm(33, (householdDay.Household.Size == 3).ToFlag());
            alternative.AddUtilityTerm(34, (householdDay.Household.Size >= 4).ToFlag());
            alternative.AddUtilityTerm(37, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());
            alternative.AddUtilityTerm(59, compositeLogsum);




            //alternative.AddUtilityTerm(35, (householdDay.Household.Size >= 5).ToFlag());

            
            //alternative.AddUtilityTerm(36, (householdDay.Household.VehiclesAvailable == 0).ToFlag());
            //alternative.AddUtilityTerm(37, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());

            //GV Sep 2014 commeted out
            //alternative.AddUtilityTerm(38, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());


            //GV old
            //alternative.AddUtilityTerm(37, (householdDay.Household.Size == 2 && householdDay.AdultsInSharedHomeStay == 2).ToFlag());
            //alternative.AddUtilityTerm(38, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildrenUnder16).ToFlag());

            
            //alternative.AddUtilityTerm(57, shoppingAggregateLogsum);

            //alternative.AddUtilityTerm(59, compositeLogsum);

            //alternative.AddNestedAlternative(12, 1, 60);


			// MEAL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEAL, false, choice == Constants.Purpose.MEAL);
			alternative.Choice = Constants.Purpose.MEAL;

			alternative.AddUtilityTerm(55, 1);

			//alternative.AddNestedAlternative(12, 1, 60);

			// SOCIAL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SOCIAL, true, choice == Constants.Purpose.SOCIAL);
			alternative.Choice = Constants.Purpose.SOCIAL;

            //alternative.AddUtilityTerm(41, 1);

            //GV: NEW
            //alternative.AddUtilityTerm(42, householdDay.PrimaryPriorityTimeFlag);
            alternative.AddUtilityTerm(43, (householdDay.Household.Size == 3).ToFlag());
            alternative.AddUtilityTerm(44, (householdDay.Household.Size >= 4).ToFlag());
            
            
            
            //alternative.AddUtilityTerm(45, (householdDay.Household.Size >= 5).ToFlag());

            //alternative.AddUtilityTerm(46, (householdDay.Household.VehiclesAvailable >= 1).ToFlag());
            //alternative.AddUtilityTerm(47, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());


            //GV Sep 2014 commeted out
            //alternative.AddUtilityTerm(48, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());

            //alternative.AddUtilityTerm(46, (householdDay.Household.VehiclesAvailable > 0 && householdDay.Household.HasChildren).ToFlag());
            //alternative.AddUtilityTerm(46, (householdDay.Household.VehiclesAvailable == 0).ToFlag()); cars have no impact on fully joint social tour

            //alternative.AddUtilityTerm(47, householdDay.Household.HasChildrenUnder5.ToFlag());
            //alternative.AddUtilityTerm(48, householdDay.Household.HasChildrenAge5Through15.ToFlag());

            //alternative.AddUtilityTerm(47, (householdDay.Household.Size == 2 && householdDay.AdultsInSharedHomeStay == 2).ToFlag());
            //alternative.AddUtilityTerm(48, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildrenUnder16).ToFlag());

            //alternative.AddUtilityTerm(58, socialAggregateLogsum);
            //alternative.AddUtilityTerm(58, compositeLogsum);

            //alternative.AddNestedAlternative(12, 1, 60); 


		}
	}
}