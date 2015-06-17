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
using System.Collections.Generic;
using System.Collections;
using Daysim.DomainModels.Actum;

namespace Daysim.ChoiceModels.Actum {
	public class ActumPersonTourGenerationModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumPersonTourGenerationModel";

		// Add one alternative for the stop choice; Change this hard code
		private const int TOTAL_ALTERNATIVES = 10;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 2;
		private const int MAX_PARAMETER = 200;

		public int Run(ActumPersonDayWrapper personDay, ActumHouseholdDayWrapper householdDay, int maxPurpose, int choice = Constants.Purpose.NONE_OR_HOME) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.PersonTourGenerationModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

			householdDay.ResetRandom(949 + personDay.TotalCreatedTours);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return choice;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator((personDay.Person.Id * 397) ^ 2 * personDay.TotalCreatedTours - 1);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				RunModel(choiceProbabilityCalculator, personDay, householdDay, maxPurpose, choice);

				choiceProbabilityCalculator.WriteObservation();
			}

			else if (Global.Configuration.TestEstimationModelInApplicationMode) {
				Global.Configuration.IsInEstimationMode = false;

				RunModel(choiceProbabilityCalculator, personDay, householdDay, maxPurpose);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility, personDay.Id, choice);

				Global.Configuration.IsInEstimationMode = true;
			}

			else {
				RunModel(choiceProbabilityCalculator, personDay, householdDay, maxPurpose);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;
			}

			return choice;
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumPersonDayWrapper personDay, ActumHouseholdDayWrapper householdDay, int maxPurpose, int choice = Constants.DEFAULT_VALUE) {

			IEnumerable<ActumPersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointTourParticipationPriority).ToList().Cast<ActumPersonDayWrapper>();

			var household = householdDay.Household;
			var residenceParcel = household.ResidenceParcel;

			var carOwnership =
							household.VehiclesAvailable == 0
								 ? Constants.CarOwnership.NO_CARS
								 : household.VehiclesAvailable < household.HouseholdTotals.DrivingAgeMembers
									  ? Constants.CarOwnership.LT_ONE_CAR_PER_ADULT
									  : Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT;

			var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);
			var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership);

			var votALSegment = household.VotALSegment;
			var transitAccessSegment = household.ResidenceParcel.TransitAccessSegment();
			var personalBusinessAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.PERSONAL_BUSINESS][carOwnership][votALSegment][transitAccessSegment];
			var shoppingAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.SHOPPING][Constants.CarOwnership.NO_CARS][votALSegment][transitAccessSegment];
			//var mealAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
			//	 [Constants.Purpose.MEAL][carOwnership][votALSegment][transitAccessSegment];
			var socialAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.SOCIAL][carOwnership][votALSegment][transitAccessSegment];
			var totalAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];
			// var recreationAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
			// [Constants.Purpose.RECREATION][carOwnership][votALSegment][transitAccessSegment];
			//  var medicalAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
			//  [Constants.Purpose.MEDICAL][carOwnership][votALSegment][transitAccessSegment];
			//var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];
			var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.NO_CARS][votALSegment][transitAccessSegment];


			int countNonMandatory = 0;
			int countMandatory = 0;
			int countWorkingAtHome = 0;


			double totalTimeWork = 0;


			int[] mandPerstype = new int[8];
			int[] nonMandPerstype = new int[8];

			double[] workLogsum = new double[8];
			double[] schoolLogsum = new double[8];
			int count = 0;
			foreach (ActumPersonDayWrapper pDay in orderedPersonDays) {
				var person = pDay.Person;
				count++;
				if (count > 8) {
					break;
				}
				if (pDay.WorksAtHomeFlag == 1) {
					countWorkingAtHome++;
				}
				if (pDay.PatternType == 1) {
					countMandatory++;
					mandPerstype[pDay.Person.PersonType - 1]++;
				}
				if (pDay.PatternType == 2) {
					countNonMandatory++;
					nonMandPerstype[pDay.Person.PersonType - 1]++;
				}

				if (person.UsualWorkParcel == null || person.UsualWorkParcelId == household.ResidenceParcelId) {
					workLogsum[count - 1] = 0;
				}
				else {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.WORK_TOUR_MODE_MODEL);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.WORK_TOUR_MODE_MODEL);
					var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumWorkTourModeModel") as ActumWorkTourModeModel).RunNested(pDay, residenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);

					workLogsum[count - 1] = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}

				if (person.UsualSchoolParcel == null || person.UsualSchoolParcelId == household.ResidenceParcelId) {
					schoolLogsum[count - 1] = 0;
				}
				else {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
					var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumSchoolTourModeModel") as ActumSchoolTourModeModel).RunNested(pDay, residenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);

					schoolLogsum[count - 1] = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}
			}


			// NONE_OR_HOME

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, true, choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;

			//alternative.AddUtilityTerm(1, (personDay.TotalCreatedTours == 1).ToFlag());
			alternative.AddUtilityTerm(2, (personDay.TotalCreatedTours == 2).ToFlag());
			alternative.AddUtilityTerm(3, (personDay.TotalCreatedTours >= 3).ToFlag());
			//alternative.AddUtilityTerm(4, (personDay.TotalCreatedTours >= 4).ToFlag());

			//alternative.AddUtilityTerm(5, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddUtilityTerm(6, householdDay.Household.HasChildren.ToFlag());

			alternative.AddUtilityTerm(4, householdDay.Household.HasChildrenUnder5.ToFlag());
			alternative.AddUtilityTerm(5, householdDay.Household.HasChildrenAge5Through15.ToFlag());
			//alternative.AddUtilityTerm(6, (householdDay.Household.Size == 2 && householdDay.AdultsInSharedHomeStay == 2).ToFlag());
			//alternative.AddUtilityTerm(7, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildrenUnder16).ToFlag());
			//alternative.AddUtilityTerm(8, (householdDay.AdultsInSharedHomeStay == 2 && householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());

			//alternative.AddUtilityTerm(10, (householdDay.Household.Income >= 300000 && householdDay.Household.Income < 600000).ToFlag());
			//alternative.AddUtilityTerm(11, (householdDay.Household.Income >= 600000 && householdDay.Household.Income < 900000).ToFlag());
			//alternative.AddUtilityTerm(12, (householdDay.Household.Income >= 900000).ToFlag());

			alternative.AddUtilityTerm(13, householdDay.PrimaryPriorityTimeFlag);

			alternative.AddUtilityTerm(14, personDay.Person.IsPartTimeWorker.ToFlag());
			//alternative.AddUtilityTerm(15, personDay.Person.WorksAtHome.ToFlag());
			//alternative.AddUtilityTerm(16, personDay.Person.IsFulltimeWorker.ToFlag());

			//alternative.AddUtilityTerm(15, (personDay.Person.Gender == 1).ToFlag());

			//alternative.AddUtilityTerm(10, (householdDay.Household.Size == 3).ToFlag());
			//alternative.AddUtilityTerm(11, (householdDay.Household.Size == 4).ToFlag());
			//alternative.AddUtilityTerm(12, (householdDay.Household.Size >= 5).ToFlag());

			//alternative.AddNestedAlternative(11, 0, 200);


			// WORK
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.WORK, false, choice == Constants.Purpose.WORK);
			alternative.Choice = Constants.Purpose.WORK;
			alternative.AddUtilityTerm(202, 1);
			//alternative.AddNestedAlternative(12, 1, 200);

			//  SCHOOL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SCHOOL, false, choice == Constants.Purpose.SCHOOL);
			alternative.Choice = Constants.Purpose.SCHOOL;
			alternative.AddUtilityTerm(203, 1);
			//alternative.AddNestedAlternative(12, 1, 200);

			// ESCORT
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.ESCORT, maxPurpose <= Constants.Purpose.ESCORT && personDay.CreatedEscortTours > 0, choice == Constants.Purpose.ESCORT);
			alternative.Choice = Constants.Purpose.ESCORT;

			alternative.AddUtilityTerm(151, 1);
			alternative.AddUtilityTerm(152, (personDay.CreatedEscortTours > 1).ToFlag());

			//alternative.AddUtilityTerm(152, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddUtilityTerm(153, (householdDay.Household.Size == 3).ToFlag()); 
			//alternative.AddUtilityTerm(154, (householdDay.Household.Size >= 4).ToFlag());

			//alternative.AddUtilityTerm(155, (householdDay.Household.Size > 4).ToFlag());

			alternative.AddUtilityTerm(155, compositeLogsum);

			//alternative.AddUtilityTerm(156, (householdDay.Household.VehiclesAvailable == 0).ToFlag());

			//alternative.AddNestedAlternative(12, 1, 200);


			// PERSONAL_BUSINESS
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.PERSONAL_BUSINESS, maxPurpose <= Constants.Purpose.PERSONAL_BUSINESS && personDay.CreatedPersonalBusinessTours > 0, choice == Constants.Purpose.PERSONAL_BUSINESS);
			alternative.Choice = Constants.Purpose.PERSONAL_BUSINESS;

			alternative.AddUtilityTerm(21, 1);
			//alternative.AddUtilityTerm(22, (personDay.CreatedPersonalBusinessTours > 1).ToFlag()); //GV: 30. april 2013 - goes to infinity

			alternative.AddUtilityTerm(156, compositeLogsum);

			//alternative.AddNestedAlternative(12, 1, 200);

			// SHOPPING
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SHOPPING, maxPurpose <= Constants.Purpose.SHOPPING && personDay.CreatedShoppingTours > 0, choice == Constants.Purpose.SHOPPING);
			alternative.Choice = Constants.Purpose.SHOPPING;

			alternative.AddUtilityTerm(41, 1);
			//alternative.AddUtilityTerm(42, (personDay.CreatedShoppingTours > 1).ToFlag()); //GV: cannot be estimated

			//alternative.AddUtilityTerm(42, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddUtilityTerm(43, (householdDay.Household.Size == 3).ToFlag());
			//alternative.AddUtilityTerm(44, (householdDay.Household.Size == 4).ToFlag());
			//alternative.AddUtilityTerm(45, (householdDay.Household.Size > 4).ToFlag());

			//alternative.AddUtilityTerm(46, (householdDay.Household.VehiclesAvailable == 0).ToFlag());

			//alternative.AddUtilityTerm(157, compositeLogsum); //GV wrong sign
			//alternative.AddUtilityTerm(157, shoppingAggregateLogsum); //GV wrong sign

			//alternative.AddNestedAlternative(12, 1, 200);


			// MEAL

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEAL, false, choice == Constants.Purpose.MEAL);
			alternative.Choice = Constants.Purpose.MEAL;

			alternative.AddUtilityTerm(61, 1);
			alternative.AddUtilityTerm(62, (personDay.CreatedMealTours > 1).ToFlag());

			//alternative.AddNestedAlternative(12, 1, 200);

			// SOCIAL

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SOCIAL, maxPurpose <= Constants.Purpose.SOCIAL && personDay.CreatedSocialTours > 0, choice == Constants.Purpose.SOCIAL);
			alternative.Choice = Constants.Purpose.SOCIAL;

			alternative.AddUtilityTerm(81, 1);
			alternative.AddUtilityTerm(82, (personDay.CreatedSocialTours > 1).ToFlag());

			//alternative.AddUtilityTerm(82, householdDay.PrimaryPriorityTimeFlag);

			//alternative.AddUtilityTerm(83, (householdDay.Household.Size == 3).ToFlag());
			//alternative.AddUtilityTerm(84, (householdDay.Household.Size == 4).ToFlag());
			//alternative.AddUtilityTerm(85, (householdDay.Household.Size > 4).ToFlag());

			alternative.AddUtilityTerm(158, compositeLogsum);

			//alternative.AddNestedAlternative(12, 1, 200);

			// RECREATION

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.RECREATION, false, choice == Constants.Purpose.RECREATION);
			alternative.Choice = Constants.Purpose.RECREATION;

			alternative.AddUtilityTerm(101, 1);
			//alternative.AddUtilityTerm(102, (personDay.CreatedRecreationTours > 1).ToFlag());

			//alternative.AddNestedAlternative(12, 1, 60);

			// MEDICAL

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEDICAL, false, choice == Constants.Purpose.MEDICAL);
			alternative.Choice = Constants.Purpose.MEDICAL;

			alternative.AddUtilityTerm(121, 1);
			//alternative.AddUtilityTerm(122, (personDay.CreatedMedicalTours > 1).ToFlag());

			//alternative.AddNestedAlternative(11, 1, 60);

		}
	}
}
