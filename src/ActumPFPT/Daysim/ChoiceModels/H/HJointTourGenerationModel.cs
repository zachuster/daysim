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

namespace Daysim.ChoiceModels.H {
	public static class HJointTourGenerationModel {
		private const string CHOICE_MODEL_NAME = "HJointTourGenerationModel";
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
													  Global.GetInputPath(Global.Configuration.JointTourGenerationModelCoefficients), TOTAL_ALTERNATIVES,
													  TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static int Run(HouseholdDayWrapper householdDay, int nCallsForTour, int choice = Constants.Purpose.NONE_OR_HOME) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			Initialize();

			householdDay.ResetRandom(935 + nCallsForTour);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return choice;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator((householdDay.Household.Id * 397) ^ 2 * nCallsForTour - 1);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, choice);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;
			}

			return choice;
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, HouseholdDayWrapper householdDay, int nCallsForTour, int choice = Constants.DEFAULT_VALUE) {

			IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointTourParticipationPriority).ToList().Cast<PersonDayWrapper>();

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

			int countNonMandatory = 0;
			int countMandatory = 0;
			int countWorkingAtHome = 0;
			
			int mandatoryHTours =0;
			int mandatoryHStops =0;

			int[] mandPerstype = new int[8];
			int[] nonMandPerstype = new int[8];
			int[] atHomePersType = new int[8];

			double autoTimeWork= 0;
			double sumAutoTimeWork = 0;


			int count = 0;
			foreach (PersonDayWrapper personDay in orderedPersonDays) {
				var person = personDay.Person;
				count++;
				if (count > 8) {
					break;
				}

				mandatoryHTours = mandatoryHTours + personDay.WorkTours + personDay.SchoolTours;
				mandatoryHStops = mandatoryHStops + (personDay.WorkStops>0).ToFlag()+ (personDay.SchoolTours>0).ToFlag();

				if (personDay.WorksAtHomeFlag == 1) {
					countWorkingAtHome++;
				}
				if (personDay.PatternType == 1) {
					countMandatory++;
					mandPerstype[personDay.Person.PersonType - 1]++;
				}
				if (personDay.PatternType == 2) {
					countNonMandatory++;
					nonMandPerstype[personDay.Person.PersonType - 1]++;
				}
				if (personDay.PatternType == 3) {

					atHomePersType[personDay.Person.PersonType - 1]++;
				}

				// 20130806 JLB removed this variable.  sum is miscalculated, and variable not (yet) available in application mode
				//if (person.UsualWorkParcel != null & person.UsualWorkParcelId != household.ResidenceParcelId) {
				//
				//	autoTimeWork= person.AutoTimeToUsualWork;
				//	sumAutoTimeWork = sumAutoTimeWork+ sumAutoTimeWork;	
				//}
			}

			sumAutoTimeWork = Math.Log(1+autoTimeWork);

			// NONE_OR_HOME

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, true, choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;

			//alternative.AddUtilityTerm(1, (nCallsForTour == 1).ToFlag());
			alternative.AddUtilityTerm(2, (nCallsForTour == 2).ToFlag());
			alternative.AddUtilityTerm(3, (nCallsForTour >= 3).ToFlag());
			alternative.AddUtilityTerm(5, noCarsFlag);
			//alternative.AddUtilityTerm(6, carCompetitionFlag);
			alternative.AddUtilityTerm(12, atHomePersType[4]);
			alternative.AddUtilityTerm(13,  mandatoryHTours/household.Size);
			//alternative.AddUtilityTerm(14,  mandatoryHStops/household.Size);
			//alternative.AddUtilityTerm(15,  sumAutoTimeWork/ (householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers + 1));

			// WORK
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.WORK, false, choice == Constants.Purpose.WORK);
			alternative.Choice = Constants.Purpose.WORK;
			alternative.AddUtilityTerm(202, 1);


			//  SCHOOL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SCHOOL, false, choice == Constants.Purpose.SCHOOL);
			alternative.Choice = Constants.Purpose.SCHOOL;
			alternative.AddUtilityTerm(203, 1);


			// ESCORT
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.ESCORT, true, choice == Constants.Purpose.ESCORT);
			alternative.Choice = Constants.Purpose.ESCORT;

			alternative.AddUtilityTerm(151, 1);
			alternative.AddUtilityTerm(152, nonMandPerstype[0]);
			alternative.AddUtilityTerm(153, nonMandPerstype[1]);
			alternative.AddUtilityTerm(154, nonMandPerstype[2]);
			alternative.AddUtilityTerm(155, nonMandPerstype[3]);
			alternative.AddUtilityTerm(156, nonMandPerstype[4]);
			alternative.AddUtilityTerm(157, nonMandPerstype[5]);
			alternative.AddUtilityTerm(158, nonMandPerstype[6]);
			alternative.AddUtilityTerm(159, nonMandPerstype[7]);
			alternative.AddUtilityTerm(160, countMandatory);
			//alternative.AddUtilityTerm(161, totalAggregateLogsum);
			alternative.AddUtilityTerm(162, countWorkingAtHome);

			// PERSONAL_BUSINESS
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.PERSONAL_BUSINESS, true, choice == Constants.Purpose.PERSONAL_BUSINESS);
			alternative.Choice = Constants.Purpose.PERSONAL_BUSINESS;

			alternative.AddUtilityTerm(21, 1);
			alternative.AddUtilityTerm(22, nonMandPerstype[0]);
			alternative.AddUtilityTerm(23, nonMandPerstype[1]);
			alternative.AddUtilityTerm(24, nonMandPerstype[2]);
			alternative.AddUtilityTerm(25, nonMandPerstype[3]);
			alternative.AddUtilityTerm(26, nonMandPerstype[4]);
			alternative.AddUtilityTerm(27, nonMandPerstype[5]);
			alternative.AddUtilityTerm(28, nonMandPerstype[6]);
			alternative.AddUtilityTerm(29, nonMandPerstype[7]);
			alternative.AddUtilityTerm(30, countMandatory);
			//alternative.AddUtilityTerm(31, personalBusinessAggregateLogsum);

			// SHOPPING
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SHOPPING, true, choice == Constants.Purpose.SHOPPING);
			alternative.Choice = Constants.Purpose.SHOPPING;

			alternative.AddUtilityTerm(41, 1);
			alternative.AddUtilityTerm(42, nonMandPerstype[0]);
			alternative.AddUtilityTerm(43, nonMandPerstype[1]);
			alternative.AddUtilityTerm(44, nonMandPerstype[2]);
			alternative.AddUtilityTerm(45, nonMandPerstype[3]);
			alternative.AddUtilityTerm(46, nonMandPerstype[4]);
			alternative.AddUtilityTerm(47, nonMandPerstype[5]);
			alternative.AddUtilityTerm(48, nonMandPerstype[6]);
			alternative.AddUtilityTerm(49, nonMandPerstype[7]);
			alternative.AddUtilityTerm(50, countMandatory);
			alternative.AddUtilityTerm(51, shoppingAggregateLogsum);
			//alternative.AddUtilityTerm(52, householdDay.Household.Has0To25KIncome.ToFlag());
			//alternative.AddUtilityTerm(53, Math.Log(1 + householdDay.Household.ResidenceParcel.EmploymentRetailBuffer1));


			// MEAL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEAL, true, choice == Constants.Purpose.MEAL);
			alternative.Choice = Constants.Purpose.MEAL;

			alternative.AddUtilityTerm(61, 1);

			alternative.AddUtilityTerm(62, nonMandPerstype[0]);
			alternative.AddUtilityTerm(63, nonMandPerstype[1]);
			alternative.AddUtilityTerm(64, nonMandPerstype[2]);
			alternative.AddUtilityTerm(65, nonMandPerstype[3]);
			// alternative.AddUtilityTerm(66, nonMandPerstype[4]);
			alternative.AddUtilityTerm(67, nonMandPerstype[5]);
			alternative.AddUtilityTerm(68, nonMandPerstype[6]);
			alternative.AddUtilityTerm(69, nonMandPerstype[7]);
			alternative.AddUtilityTerm(70, countMandatory);
			alternative.AddUtilityTerm(71, mealAggregateLogsum);
			// alternative.AddUtilityTerm(72, householdDay.Household.Has0To25KIncome.ToFlag());
			// alternative.AddUtilityTerm(73, Math.Log(1 + householdDay.Household.ResidenceParcel.EmploymentFoodBuffer1));


			// SOCIAL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SOCIAL, true, choice == Constants.Purpose.SOCIAL);
			alternative.Choice = Constants.Purpose.SOCIAL;

			alternative.AddUtilityTerm(81, 1);
			alternative.AddUtilityTerm(82, nonMandPerstype[0]);
			alternative.AddUtilityTerm(83, nonMandPerstype[1]);
			alternative.AddUtilityTerm(84, nonMandPerstype[2]);
			alternative.AddUtilityTerm(85, nonMandPerstype[3]);
			alternative.AddUtilityTerm(86, nonMandPerstype[4]);
			alternative.AddUtilityTerm(87, nonMandPerstype[5]);
			alternative.AddUtilityTerm(88, nonMandPerstype[6]);
			alternative.AddUtilityTerm(89, nonMandPerstype[7]);
			alternative.AddUtilityTerm(90, countMandatory);
			// alternative.AddUtilityTerm(91, socialAggregateLogsum);
			alternative.AddUtilityTerm(93, Math.Log(1 + householdDay.Household.ResidenceParcel.HouseholdsBuffer1));

			// RECREATION
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.RECREATION, true, choice == Constants.Purpose.RECREATION);
			alternative.Choice = Constants.Purpose.RECREATION;

			alternative.AddUtilityTerm(101, 1);

			alternative.AddUtilityTerm(102, nonMandPerstype[0]);
			alternative.AddUtilityTerm(103, nonMandPerstype[1]);
			alternative.AddUtilityTerm(104, nonMandPerstype[2]);
			alternative.AddUtilityTerm(105, nonMandPerstype[3]);
			alternative.AddUtilityTerm(106, nonMandPerstype[4]);
			alternative.AddUtilityTerm(107, nonMandPerstype[5]);
			alternative.AddUtilityTerm(108, nonMandPerstype[6]);
			alternative.AddUtilityTerm(109, nonMandPerstype[7]);
			alternative.AddUtilityTerm(110, countMandatory);
			alternative.AddUtilityTerm(111, totalAggregateLogsum);
			alternative.AddUtilityTerm(112, Math.Log(1 + householdDay.Household.ResidenceParcel.OpenSpaceType1Buffer1));

			// MEDICAL
			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEDICAL, true, choice == Constants.Purpose.MEDICAL);
			alternative.Choice = Constants.Purpose.MEDICAL;

			alternative.AddUtilityTerm(121, 1);

			alternative.AddUtilityTerm(122, nonMandPerstype[0]);
			alternative.AddUtilityTerm(123, nonMandPerstype[1]);
			alternative.AddUtilityTerm(124, nonMandPerstype[2]);
			alternative.AddUtilityTerm(125, nonMandPerstype[3]);
			alternative.AddUtilityTerm(126, nonMandPerstype[4]);
			// alternative.AddUtilityTerm(127, nonMandPerstype[5]);
			alternative.AddUtilityTerm(128, nonMandPerstype[6]);
			alternative.AddUtilityTerm(129, nonMandPerstype[7]);
			alternative.AddUtilityTerm(130, countMandatory);
			alternative.AddUtilityTerm(131, totalAggregateLogsum);
		}
	}
}
