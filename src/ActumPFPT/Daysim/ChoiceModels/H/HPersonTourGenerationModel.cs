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
using Daysim.Interfaces;

namespace Daysim.ChoiceModels.H {
	public static class HPersonTourGenerationModel {
		private const string CHOICE_MODEL_NAME = "HPersonTourGenerationModel";

		// Add one alternative for the stop choice; Change this hard code
		private const int TOTAL_ALTERNATIVES = 10;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 200;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		private static readonly object _lock = new object();

		private static void Initialize() {
			lock (_lock) {
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null) {
					return;
				}

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME,
													  Global.GetInputPath(Global.Configuration.PersonTourGenerationModelCoefficients), TOTAL_ALTERNATIVES,
													  TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static int Run(IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay, int maxPurpose, int choice = Constants.Purpose.NONE_OR_HOME) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			Initialize();

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
			else {
				RunModel(choiceProbabilityCalculator, personDay, householdDay, maxPurpose);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;
			}

			return choice;
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay, int maxPurpose, int choice = Constants.DEFAULT_VALUE) {

			IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointTourParticipationPriority).ToList().Cast<PersonDayWrapper>();
			
			var person =personDay.Person;
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
				 [Constants.Purpose.SHOPPING][carOwnership][votALSegment][transitAccessSegment];
			var mealAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.MEAL][carOwnership][votALSegment][transitAccessSegment];
			var socialAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.SOCIAL][carOwnership][votALSegment][transitAccessSegment];
			var totalAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				[Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];
			// var recreationAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
			// [Constants.Purpose.RECREATION][carOwnership][votALSegment][transitAccessSegment];
			//  var medicalAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
			//  [Constants.Purpose.MEDICAL][carOwnership][votALSegment][transitAccessSegment];

			int[] nonMandPerstype = new int[8];
			int[] mandPerstype = new int[8];

			//int mandatoryAdult=0;
			//int mandatoryChild=0;
			//int nonMandatoryWorker=0;
			//int nonMandatoryNonWorker=0;
			//int nonMandatoryRetired=0;
			//int nonMandatoryChild=0;

			int countNonMandatory = 0;
			int countMandatory = 0;

			double workLogsum = 0;
			double schoolLogsum= 0;

			//int worksAtHome=0;
			int countWorkingAtHome=0;

			var workDestinationArrivalTime=0;
			var workDestinationDepartureTime=0;

			int numStopPurposes= 0;
			int numTourPurposes =0;

			numTourPurposes= (personDay.CreatedEscortTours>1).ToFlag() +(personDay.CreatedShoppingTours>1).ToFlag()+ (personDay.CreatedMealTours>1).ToFlag()+
											(personDay.CreatedPersonalBusinessTours>1).ToFlag()+(personDay.CreatedSocialTours>1).ToFlag()+
											(personDay.CreatedRecreationTours>1).ToFlag()+(personDay.CreatedMedicalTours>1).ToFlag();
										

			numStopPurposes = (personDay.SimulatedEscortStops>1).ToFlag() +(personDay.SimulatedShoppingStops>1).ToFlag()+ (personDay.SimulatedMealStops>1).ToFlag()+
											(personDay.SimulatedPersonalBusinessStops>1).ToFlag()+(personDay.SimulatedSocialStops>1).ToFlag()+
											(personDay.SimulatedRecreationStops>1).ToFlag()+(personDay.SimulatedMedicalStops>1).ToFlag();
											

			if (person.UsualWorkParcel == null || person.UsualWorkParcelId == household.ResidenceParcelId) {
					workLogsum = 0;
				}
				else {
				   workDestinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.WORK_TOUR_MODE_MODEL);
					workDestinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.WORK_TOUR_MODE_MODEL);
					var nestedAlternative = HWorkTourModeModel.RunNested(personDay, residenceParcel, person.UsualWorkParcel, workDestinationArrivalTime, workDestinationDepartureTime, household.VehiclesAvailable);

					workLogsum= nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}

				if (person.UsualSchoolParcel == null || person.UsualSchoolParcelId == household.ResidenceParcelId) {
					schoolLogsum = 0;
				}
				else {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
					var nestedAlternative = HSchoolTourModeModel.RunNested(personDay, residenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);

					schoolLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}

				//if (personDay.WorksAtHomeFlag == 1) {
				//	worksAtHome =1;
				//}
				

			int count =0;
			foreach (PersonDayWrapper pDay in orderedPersonDays) {
				count++;
				if (count > 8) {
					break;
				}
				if (pDay.WorksAtHomeFlag == 1) {
					countWorkingAtHome++;
				}
				if (pDay.PatternType == 1) {
					countMandatory++;
				}
				if (pDay.PatternType == 2) {
					countNonMandatory++;
				}

			}


			// NONE_OR_HOME

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, true, choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;
			alternative.AddUtilityTerm(3,  (personDay.TotalCreatedTours==3).ToFlag());
			alternative.AddUtilityTerm(4,  (personDay.TotalCreatedTours>=4).ToFlag());
			alternative.AddUtilityTerm(5,  (numStopPurposes>=1).ToFlag());
			alternative.AddUtilityTerm(6,  (numTourPurposes>=2).ToFlag());
			alternative.AddUtilityTerm(7,  person.TransitPassOwnershipFlag);
			alternative.AddUtilityTerm(8,  (personDay.JointTours));
			alternative.AddUtilityTerm(9,  Math.Log(1+ (workDestinationDepartureTime-workDestinationArrivalTime)/60));
			alternative.AddUtilityTerm(10,  (household.Size==1).ToFlag());
			//alternative.AddNestedAlternative(11, 0, 200);

			// WORK
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.WORK, false, choice == Constants.Purpose.WORK);
			alternative.Choice = Constants.Purpose.WORK;
			alternative.AddUtilityTerm(198, 1);
			//alternative.AddNestedAlternative(12, 1, 200);

			//  SCHOOL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SCHOOL, false, choice == Constants.Purpose.SCHOOL);
			alternative.Choice = Constants.Purpose.SCHOOL;
			alternative.AddUtilityTerm(199, 1);
			//alternative.AddNestedAlternative(12, 1, 200);

			// ESCORT
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.ESCORT, maxPurpose <= Constants.Purpose.ESCORT && personDay.CreatedEscortTours > 0, choice == Constants.Purpose.ESCORT);
			alternative.Choice = Constants.Purpose.ESCORT;

			alternative.AddUtilityTerm(11, 1);
			alternative.AddUtilityTerm(12, (personDay.PatternType ==2).ToFlag());
			alternative.AddUtilityTerm(13, personDay.JointTours);
			alternative.AddUtilityTerm(14, (personDay.EscortStops>0).ToFlag());
			alternative.AddUtilityTerm(15, (personDay.CreatedEscortTours > 1).ToFlag());
			alternative.AddUtilityTerm(17, (household.HouseholdType== Constants.HouseholdType.TWO_PLUS_WORKER_STUDENT_ADULTS_WITH_CHILDREN).ToFlag() +(
												household.HouseholdType== Constants.HouseholdType.TWO_PLUS_ADULTS_ONE_PLUS_WORKERS_STUDENTS_WITH_CHILDREN).ToFlag());
			alternative.AddUtilityTerm(20, (household.HouseholdType== Constants.HouseholdType.ONE_ADULT_WITH_CHILDREN).ToFlag());
			alternative.AddUtilityTerm(25, (person.PersonType==Constants.PersonType.PART_TIME_WORKER).ToFlag());
			alternative.AddUtilityTerm(28, (person.PersonType==Constants.PersonType.CHILD_UNDER_5).ToFlag());
			alternative.AddUtilityTerm(29, (person.PersonType==Constants.PersonType.CHILD_AGE_5_THROUGH_15).ToFlag());
			alternative.AddUtilityTerm(31, (person.PersonType==Constants.PersonType.UNIVERSITY_STUDENT).ToFlag());
			alternative.AddUtilityTerm(32, countMandatory);
	
			//alternative.AddNestedAlternative(12, 1, 200);

			// PERSONAL_BUSINESS
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.PERSONAL_BUSINESS, maxPurpose <= Constants.Purpose.PERSONAL_BUSINESS && personDay.CreatedPersonalBusinessTours > 0, choice == Constants.Purpose.PERSONAL_BUSINESS);
			alternative.Choice = Constants.Purpose.PERSONAL_BUSINESS;
			alternative.AddUtilityTerm(41, 1);
			alternative.AddUtilityTerm(42, (personDay.PatternType==2).ToFlag());
			alternative.AddUtilityTerm(43, (personDay.PersonalBusinessStops>0).ToFlag());
			alternative.AddUtilityTerm(44, personalBusinessAggregateLogsum);
			alternative.AddUtilityTerm(45, (household.HouseholdType== Constants.HouseholdType.INDIVIDUAL_WORKER_STUDENT).ToFlag());
			alternative.AddUtilityTerm(46, (household.HouseholdType== Constants.HouseholdType.INDIVIDUAL_NONWORKER_NONSTUDENT).ToFlag());
			//alternative.AddNestedAlternative(12, 1, 200);

			// SHOPPING
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SHOPPING, maxPurpose <= Constants.Purpose.SHOPPING && personDay.CreatedShoppingTours > 0, choice == Constants.Purpose.SHOPPING);
			alternative.Choice = Constants.Purpose.SHOPPING;

			alternative.AddUtilityTerm(61, 1);
			alternative.AddUtilityTerm(52, (personDay.PatternType==2).ToFlag());
			alternative.AddUtilityTerm(54, (personDay.ShoppingStops>0).ToFlag());
			//alternative.AddUtilityTerm(55, Math.Log(1+person.Household.ResidenceParcel.EmploymentRetailBuffer2));
			alternative.AddUtilityTerm(56, shoppingAggregateLogsum);
			alternative.AddUtilityTerm(57, (person.PersonType==Constants.PersonType.RETIRED_ADULT).ToFlag());
			alternative.AddUtilityTerm(58, (household.HouseholdType== Constants.HouseholdType.ONE_PLUS_WORKER_STUDENT_ADULTS_AND_ONE_PLUS_NONWORKER_NONSTUDENT_ADULTS_WITHOUT_CHILDREN).ToFlag());
			alternative.AddUtilityTerm(60, (household.HouseholdType== Constants.HouseholdType.INDIVIDUAL_WORKER_STUDENT).ToFlag());
			alternative.AddUtilityTerm(69, (person.PersonType==Constants.PersonType.UNIVERSITY_STUDENT).ToFlag());
			alternative.AddUtilityTerm(70, (household.Has100KPlusIncome).ToFlag());

			// MEAL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEAL, maxPurpose <= Constants.Purpose.MEAL && personDay.CreatedMealTours > 0, choice == Constants.Purpose.MEAL);
			alternative.Choice = Constants.Purpose.MEAL;
			alternative.AddUtilityTerm(71, 1);
			alternative.AddUtilityTerm(72, (personDay.PatternType ==2).ToFlag());
			alternative.AddUtilityTerm(74, mealAggregateLogsum);
			alternative.AddUtilityTerm(80, (household.HouseholdType== Constants.HouseholdType.INDIVIDUAL_WORKER_STUDENT).ToFlag());
			alternative.AddUtilityTerm(82, (household.HouseholdType== Constants.HouseholdType.TWO_PLUS_NONWORKER_NONSTUDENT_ADULTS_WITHOUT_CHILDREN).ToFlag());
			alternative.AddUtilityTerm(85, (person.PersonType==Constants.PersonType.RETIRED_ADULT).ToFlag());

			// SOCIAL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SOCIAL, maxPurpose <= Constants.Purpose.SOCIAL && personDay.CreatedSocialTours > 0, choice == Constants.Purpose.SOCIAL);
			alternative.Choice = Constants.Purpose.SOCIAL;

			alternative.AddUtilityTerm(111, 1);
			alternative.AddUtilityTerm(112, (personDay.PatternType ==2).ToFlag());
			alternative.AddUtilityTerm(113, household.HouseholdTotals.ChildrenUnder16);
			//alternative.AddUtilityTerm(114, socialAggregateLogsum);
			alternative.AddUtilityTerm(115,  Math.Log(1+person.Household.ResidenceParcel.HouseholdsBuffer2));
			alternative.AddUtilityTerm(122, (household.HouseholdType== Constants.HouseholdType.TWO_PLUS_NONWORKER_NONSTUDENT_ADULTS_WITHOUT_CHILDREN).ToFlag());
			alternative.AddUtilityTerm(123, (person.PersonType==Constants.PersonType.PART_TIME_WORKER).ToFlag());
			alternative.AddUtilityTerm(126, (person.PersonType==Constants.PersonType.CHILD_UNDER_5).ToFlag());
			alternative.AddUtilityTerm(127, (person.PersonType==Constants.PersonType.CHILD_AGE_5_THROUGH_15).ToFlag());
			alternative.AddUtilityTerm(130, (person.PersonType==Constants.PersonType.RETIRED_ADULT).ToFlag());

			// RECREATION
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.RECREATION, maxPurpose <= Constants.Purpose.RECREATION && personDay.CreatedRecreationTours > 0, choice == Constants.Purpose.RECREATION);
			alternative.Choice = Constants.Purpose.RECREATION;
			alternative.AddUtilityTerm(111, 1);
			alternative.AddUtilityTerm(112, (personDay.PatternType ==2).ToFlag());
			alternative.AddUtilityTerm(113, household.HouseholdTotals.ChildrenUnder16);
			//alternative.AddUtilityTerm(114, totalAggregateLogsum);
			alternative.AddUtilityTerm(115,  Math.Log(1+person.Household.ResidenceParcel.HouseholdsBuffer2));
			alternative.AddUtilityTerm(122, (household.HouseholdType== Constants.HouseholdType.TWO_PLUS_NONWORKER_NONSTUDENT_ADULTS_WITHOUT_CHILDREN).ToFlag());
			alternative.AddUtilityTerm(123, (person.PersonType==Constants.PersonType.PART_TIME_WORKER).ToFlag());
			alternative.AddUtilityTerm(126, (person.PersonType==Constants.PersonType.CHILD_UNDER_5).ToFlag());
			alternative.AddUtilityTerm(127, (person.PersonType==Constants.PersonType.CHILD_AGE_5_THROUGH_15).ToFlag());
			alternative.AddUtilityTerm(128, (household.Has100KPlusIncome).ToFlag());

			// MEDICAL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEDICAL, maxPurpose <= Constants.Purpose.MEDICAL && personDay.CreatedMedicalTours > 0, choice == Constants.Purpose.MEDICAL);
			alternative.Choice = Constants.Purpose.MEDICAL;
			alternative.AddUtilityTerm(131, 1);
			alternative.AddUtilityTerm(132, Math.Log(1+household.ResidenceParcel.EmploymentMedicalBuffer2));
			alternative.AddUtilityTerm(133, (person.PersonType==Constants.PersonType.RETIRED_ADULT).ToFlag());
			//alternative.AddNestedAlternative(11, 1, 60);

		}
	}
}
