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
	public static class HWorkAtHomeModel {
		private const string CHOICE_MODEL_NAME = "HWorkAtHomeModel";
		private const int TOTAL_ALTERNATIVES = 2;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 99;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		private static readonly object _lock = new object();

		private static void Initialize() {
			lock (_lock) {
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null) {
					return;
				}

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME, Global.GetInputPath(Global.Configuration.WorkAtHomeModelCoefficients),
													  TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static void Run(IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay) {
			if (personDay == null) {
				throw new ArgumentNullException("personDay");
			}

			Initialize();

			personDay.Person.ResetRandom(904);

			if (Global.Configuration.IsInEstimationMode) {

				if (personDay.WorkAtHomeDuration >= 120) { personDay.WorksAtHomeFlag = 1; }
				else personDay.WorksAtHomeFlag = 0;
				if (!_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode || !personDay.Person.IsFullOrPartTimeWorker) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(personDay.Person.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {

				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}

				RunModel(choiceProbabilityCalculator, personDay, householdDay, personDay.WorksAtHomeFlag);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {

				int choice;

				if (!personDay.Person.IsFullOrPartTimeWorker) {
					choice = 0;
				}
				else {

					RunModel(choiceProbabilityCalculator, personDay, householdDay);

					var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(personDay.Household.RandomUtility);
					choice = (int) chosenAlternative.Choice;
				}
				personDay.WorksAtHomeFlag = choice;
                personDay.WorkAtHomeDuration = choice * 120; //default predicted duration for output
			}
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay, int choice = Constants.DEFAULT_VALUE) {

			IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointTourParticipationPriority).ToList().Cast<PersonDayWrapper>();

			Double workTourLogsum;

			if (personDay.Person.UsualWorkParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualWorkParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
				if (personDay.Person.UsualDeparturePeriodFromWork != Constants.DEFAULT_VALUE && personDay.Person.UsualArrivalPeriodToWork != Constants.DEFAULT_VALUE) {
					var nestedAlternative = HWorkTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, personDay.Person.UsualArrivalPeriodToWork, personDay.Person.UsualDeparturePeriodFromWork, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
					workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}
				else {
					var nestedAlternative = HWorkTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
					workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}


			}
			else {
				workTourLogsum = 0;

			}
			int atHomeDay = personDay.PatternType == 3 ? 1 : 0;
			int nonMandatoryTourDay = personDay.PatternType == 2 ? 1 : 0;
			int mandatoryTourDay = personDay.PatternType == 1 ? 1 : 0;
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

			int countMandatory = (from personDayHH in orderedPersonDays where personDayHH.PatternType == 1 select personDayHH.PatternType).Count();
			int countNonMandatory = (from personDayHH in orderedPersonDays where personDayHH.PatternType == 2 select personDayHH.PatternType).Count();
			int countAtHome = (from personDayHH in orderedPersonDays where personDayHH.PatternType == 3 select personDayHH.PatternType).Count();


			var usualWorkAtHome = household.ResidenceParcelId == personDay.Person.UsualWorkParcelId ? 1 : 0;


			// 0 Person doesn't work at home more than specified number of minutes
			var alternative = choiceProbabilityCalculator.GetAlternative(0, true, choice == 0);
			alternative.Choice = 0;

			alternative.AddUtilityTerm(1, 0.0);

			// 1 Works at home
			alternative = choiceProbabilityCalculator.GetAlternative(1, true, choice == 1);
			alternative.Choice = 1;

            var adjustedConstant = Global.Configuration.IsInEstimationMode ? 1.0 :
                (-2.0 + Math.Log(1.0 + Global.Configuration.Policy_FractionIncreaseInWorkAtHomeShare)) / -2.0;
            alternative.AddUtilityTerm(1, adjustedConstant);

			alternative.AddUtilityTerm(2, personDay.Person.IsPartTimeWorker.ToFlag());
			alternative.AddUtilityTerm(3, atHomeDay);
			alternative.AddUtilityTerm(4, nonMandatoryTourDay);
			alternative.AddUtilityTerm(7, (personDay.Person.Age < 30).ToFlag());
			alternative.AddUtilityTerm(8, (personDay.Person.Age >= 30 && personDay.Person.Age < 35).ToFlag());
			alternative.AddUtilityTerm(10, countMandatory - mandatoryTourDay);
			alternative.AddUtilityTerm(12, countAtHome - atHomeDay);
			alternative.AddUtilityTerm(14, (personDay.Person.Gender == 1).ToFlag());
			alternative.AddUtilityTerm(17, householdDay.Household.HouseholdTotals.ChildrenAge5Through15);
			alternative.AddUtilityTerm(18, personDay.Person.PayToParkAtWorkplaceFlag);
			alternative.AddUtilityTerm(19, usualWorkAtHome);
			alternative.AddUtilityTerm(20, ((householdDay.Household.Has0To25KIncome).ToFlag()));
			alternative.AddUtilityTerm(23, workTourLogsum);
			alternative.AddUtilityTerm(24, (personDay.Person.IsStudent.ToFlag()));
			alternative.AddUtilityTerm(31, carCompetitionFlag + noCarsFlag);
			alternative.AddUtilityTerm(35, (personDay.Person.UsualModeToWork == 1 || personDay.Person.UsualModeToWork == 2).ToFlag());
			alternative.AddUtilityTerm(36, (personDay.Person.UsualModeToWork == 4 || personDay.Person.UsualModeToWork == 5 || personDay.Person.UsualModeToWork == 6 || personDay.Person.UsualModeToWork == 7).ToFlag());



		}
	}
}