// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
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
	public class ActumJointHalfTourGenerationModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumJointHalfTourGenerationModel";
		private const int TOTAL_ALTERNATIVES = 7;
		private const int TOTAL_NESTED_ALTERNATIVES = 2;
		private const int TOTAL_LEVELS = 2;
		private const int MAX_PARAMETER = 70;

		public int Run(ActumHouseholdDayWrapper householdDay, int nCallsForTour, bool[] available, int type = Constants.Purpose.NONE_OR_HOME, int subType = Constants.Purpose.NONE_OR_HOME) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.JointHalfTourGenerationModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

			householdDay.ResetRandom(920 + nCallsForTour);

			int choice = 0;

			if (Global.Configuration.IsInEstimationMode) {

				choice = type == 0 ? 0 : (type - 1) * 3 + subType + 1;

				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return choice;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator((householdDay.Household.Id * 397) ^ nCallsForTour);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				//if (tour.PersonDay.TotalStops > 0) {  // TODO:  maybe the restrictions coming from HH pattern shoudl enter here
				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, available, choice);

				choiceProbabilityCalculator.WriteObservation();
				// }
			}

			else if (Global.Configuration.TestEstimationModelInApplicationMode) {
				Global.Configuration.IsInEstimationMode = false;

				//choice = Math.Min(personDay.BusinessStops, 1) + 2 * Math.Min(personDay.SchoolStops, 1);

				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, available);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility, householdDay.Household.Id, choice);

				Global.Configuration.IsInEstimationMode = true;
			}

			else {
				//if (tour.PersonDay.TotalStops > 0) {  // TODO:  maybe the restrictions coming from HH pattern shoudl enter here
				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, available);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;
				//}
				//else {                                      // TODO:  see above TODO:
				//	choice = Constants.Purpose.NONE_OR_HOME;   // this is returned if the model isn't even run because we know there are no tours
				//                                               which is used by the choicemodelrunneer to break tour generation
				// }
			}

			return choice;
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumHouseholdDayWrapper householdDay, int nCallsForTour, bool[] available, int choice = Constants.DEFAULT_VALUE) {
			//var householdDay = (ActumHouseholdDayWrapper)tour.HouseholdDay;
			var household = householdDay.Household;

			Double workTourLogsum = 0;
			Double schoolTourLogsum = 0;

			IEnumerable<ActumPersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointHalfTourParticipationPriority).ToList().Cast<ActumPersonDayWrapper>();
			foreach (ActumPersonDayWrapper personDay in orderedPersonDays) {

				//Double workTourLogsum;
				if (personDay.Person.UsualWorkParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualWorkParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
					var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumWorkTourModeModel") as ActumWorkTourModeModel).RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
					workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}
				else {
					workTourLogsum = 0;
				}

				if (personDay.Person.UsualSchoolParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualSchoolParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
					var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumSchoolTourModeModel") as ActumSchoolTourModeModel).RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualSchoolParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
					schoolTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}
				else {
					schoolTourLogsum = 0;
				}

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

			int youngestAge = 999;

			foreach (ActumPersonWrapper person in householdDay.Household.Persons) {
				// set characteristics here that depend on person characteristics
				if (person.Age < youngestAge) youngestAge = person.Age;
			}


			// NONE_OR_HOME

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, available[0], choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;

			alternative.AddUtilityTerm(1, (nCallsForTour > 1).ToFlag());

			alternative.AddUtilityTerm(2, householdDay.Household.HasChildrenUnder5.ToFlag());
			alternative.AddUtilityTerm(3, householdDay.Household.HasChildrenAge5Through15.ToFlag());
			alternative.AddUtilityTerm(4, (householdDay.Household.Size == 2 && householdDay.AdultsInSharedHomeStay == 2).ToFlag());
			alternative.AddUtilityTerm(5, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildrenUnder16).ToFlag());
			alternative.AddUtilityTerm(6, (householdDay.AdultsInSharedHomeStay == 2 && householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
			//alternative.AddUtilityTerm(7, (householdDay.AdultsInSharedHomeStay == 2 && hasAdultEducLevel12 == 1).ToFlag());
			//alternative.AddUtilityTerm(8, (youngestAge >= 40).ToFlag());

			//alternative.AddUtilityTerm(10, (householdDay.Household.Income >= 300000 && householdDay.Household.Income < 600000).ToFlag());
			//alternative.AddUtilityTerm(11, (householdDay.Household.Income >= 600000 && householdDay.Household.Income < 900000).ToFlag());
			//alternative.AddUtilityTerm(12, (householdDay.Household.Income >= 900000).ToFlag());

			//alternative.AddUtilityTerm(15, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddUtilityTerm(21, personDay.Person.IsPartTimeWorker.ToFlag()); //GV - Aks John to include peson.Day in the model
			//alternative.AddUtilityTerm(2, personDay.Person.IsFulltimeWorker.ToFlag()); //GV - Aks John to include peson.Day in the model

			//alternative.AddUtilityTerm(22, (personDay.Person.Gender == 1).ToFlag()); //GV - Aks John to include peson.Day in the model
			//alternative.AddUtilityTerm(23, (hasAdultEducLevel12 == 1).ToFlag()); //GV - Aks John to include peson.Day in the model

			//alternative.AddUtilityTerm(24, MandatoryTourDay); //GV - Aks John to include peson.Day in the model
			//alternative.AddUtilityTerm(25, nonMandatoryTourDay); //GV - Aks John to include peson.Day in the model
			//alternative.AddUtilityTerm(26, atHomeDay); //GV - Aks John to include peson.Day in the model

			alternative.AddUtilityTerm(7, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());
			alternative.AddUtilityTerm(8, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());

			alternative.AddUtilityTerm(9, (householdDay.Household.Size == 3).ToFlag());
			alternative.AddUtilityTerm(10, (householdDay.Household.Size >= 4).ToFlag());


			// FULL PAIRED
			alternative = choiceProbabilityCalculator.GetAlternative(1, available[1], choice == 1);
			alternative.Choice = 1;
			alternative.AddUtilityTerm(11, 1);

			//alternative.AddUtilityTerm(12, workTourLogsum);
			alternative.AddUtilityTerm(13, schoolTourLogsum);

			//alternative.AddUtilityTerm(13, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddNestedAlternative(12, 1, 70);

			// FULL HalfTour 1
			alternative = choiceProbabilityCalculator.GetAlternative(2, available[2], choice == 2);
			alternative.Choice = 2;
			alternative.AddUtilityTerm(21, 1);

			alternative.AddUtilityTerm(12, workTourLogsum);
			//alternative.AddUtilityTerm(13, schoolTourLogsum);

			//alternative.AddUtilityTerm(23, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddNestedAlternative(12, 1, 70);

			// Full HalfTour 2
			alternative = choiceProbabilityCalculator.GetAlternative(3, available[3], choice == 3);
			alternative.Choice = 3;
			alternative.AddUtilityTerm(31, 1);

			alternative.AddUtilityTerm(12, workTourLogsum);
			//alternative.AddUtilityTerm(13, schoolTourLogsum);

			//alternative.AddUtilityTerm(33, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddNestedAlternative(12, 1, 70);

			// PARTIAL PAIRED
			alternative = choiceProbabilityCalculator.GetAlternative(4, available[4], choice == 4);
			alternative.Choice = 4;
			alternative.AddUtilityTerm(41, 1);

			alternative.AddUtilityTerm(42, workTourLogsum);
			alternative.AddUtilityTerm(43, schoolTourLogsum);

			alternative.AddUtilityTerm(44, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddNestedAlternative(12, 1, 70);

			// PARTIAL HalfTour 1
			alternative = choiceProbabilityCalculator.GetAlternative(5, available[5], choice == 5);
			alternative.Choice = 5;
			alternative.AddUtilityTerm(51, 1);

			alternative.AddUtilityTerm(42, workTourLogsum);
			//alternative.AddUtilityTerm(43, schoolTourLogsum);

			alternative.AddUtilityTerm(54, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddNestedAlternative(12, 1, 70);

			// PARTIAL HalfTour 2
			alternative = choiceProbabilityCalculator.GetAlternative(6, available[6], choice == 6);
			alternative.Choice = 6;
			alternative.AddUtilityTerm(61, 1);

			alternative.AddUtilityTerm(42, workTourLogsum);
			//alternative.AddUtilityTerm(43, schoolTourLogsum);

			alternative.AddUtilityTerm(64, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddNestedAlternative(12, 1, 70);

		}
	}
}