// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.Collections;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using System.Linq;


namespace Daysim.ChoiceModels.H {
	public static class HJointHalfTourGenerationModel {
		private const string CHOICE_MODEL_NAME = "HJointHalfTourGenerationModel";
		private const int TOTAL_ALTERNATIVES = 7;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 181;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		private static readonly object _lock = new object();

		private static void Initialize() {
			lock (_lock) {
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null) {
					return;
				}

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME,
													  Global.GetInputPath(Global.Configuration.JointHalfTourGenerationModelCoefficients), TOTAL_ALTERNATIVES,
													  TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static int Run(HouseholdDayWrapper householdDay, int nCallsForTour, bool[] available, int type = Constants.Purpose.NONE_OR_HOME, int subType = Constants.Purpose.NONE_OR_HOME) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			Initialize();

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
				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, available, choice);

				choiceProbabilityCalculator.WriteObservation();

			}
			else {
				RunModel(choiceProbabilityCalculator, householdDay, nCallsForTour, available);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;

			}

			return choice;
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, HouseholdDayWrapper householdDay, int nCallsForTour, bool[] available, int choice = Constants.DEFAULT_VALUE) {
			var household = householdDay.Household;

			IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointHalfTourParticipationPriority).ToList().Cast<PersonDayWrapper>();

			var carOwnership =
						household.VehiclesAvailable == 0
							? Constants.CarOwnership.NO_CARS
							: household.VehiclesAvailable < household.HouseholdTotals.DrivingAgeMembers
								? Constants.CarOwnership.LT_ONE_CAR_PER_ADULT
								: Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT;

			var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);
			var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership);
			var carsGrAdults = household.VehiclesAvailable > household.HouseholdTotals.DrivingAgeMembers ? 1 : 0;

			var votALSegment = household.VotALSegment;
			var transitAccessSegment = household.ResidenceParcel.TransitAccessSegment();
			var totAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];

			int countWorkingAtHome = 0;
			int transitPassOwnership = 0;
			int oldestChild = 0;
			int payParkWork = 0;

			Double workTourLogsum;
			Double schoolTourLogsum;

			//Double maxSchoolLogsum =0;
			//Double minSchoolLogsum = -0;
			Double aveSchoolLogsum = 0;
			//Double maxWorkLogsum=0;
			//Double minWorkLogsum=0;
			Double aveWorkLogsum = 0;

			int count = 0;

			foreach (PersonDayWrapper personDay in orderedPersonDays) {
				var person = personDay.Person;
				count++;
				if (count > 8) {
					break;
				}
				if (personDay.WorksAtHomeFlag == 1) {
					countWorkingAtHome++;
				}
				if (person.TransitPassOwnershipFlag == 1) {
					transitPassOwnership++;
				}

				if (person.PayToParkAtWorkplaceFlag == 1) {
					payParkWork++;
				}

				if (personDay.Person.UsualWorkParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualWorkParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
					if (personDay.Person.UsualDeparturePeriodFromWork != Constants.DEFAULT_VALUE && personDay.Person.UsualArrivalPeriodToWork != Constants.DEFAULT_VALUE) {
						var nestedAlternative = HWorkTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, personDay.Person.UsualArrivalPeriodToWork, personDay.Person.UsualDeparturePeriodFromWork, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
						workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
						aveWorkLogsum = workTourLogsum / (household.HouseholdTotals.FullAndPartTimeWorkers + 1) + aveWorkLogsum;
					}
					else {
						var nestedAlternative = HWorkTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
						workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
						aveWorkLogsum = workTourLogsum / (household.HouseholdTotals.FullAndPartTimeWorkers + 1) + aveWorkLogsum;
					}

				}
				else {
					workTourLogsum = 0;

				}

				if (personDay.Person.UsualSchoolParcelId != 0 && personDay.Person.UsualSchoolParcelId != -1 && personDay.Person.UsualSchoolParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
					var schoolNestedAlternative = HSchoolTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualSchoolParcel, Constants.Time.EIGHT_AM, Constants.Time.TWO_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
					schoolTourLogsum = schoolNestedAlternative == null ? 0 : schoolNestedAlternative.ComputeLogsum();
					aveSchoolLogsum = schoolTourLogsum / (household.HouseholdTotals.AllStudents + 1) + schoolTourLogsum;
				}


			}
			int countMandatoryAdults = (from personDayHH in orderedPersonDays
												 where personDayHH.PatternType == 1 && personDayHH.Person.IsAdult
												 select personDayHH.PatternType).Count();
			int countMandatoryChildren = (from personDayHH in orderedPersonDays
													where personDayHH.PatternType == 1 && personDayHH.Person.IsChildUnder16
													select personDayHH.PatternType).Count();
			int countNonMandatoryAdults = (from personDayHH in orderedPersonDays
													 where personDayHH.PatternType == 2 && personDayHH.Person.IsAdult
													 select personDayHH.PatternType).Count();
			int countKidsAtHome = (from personDayHH in orderedPersonDays 
													where personDayHH.PatternType == 3 && personDayHH.Person.Age<12
													select personDayHH.PatternType).Count();

			int youngestAge = (from person in household.Persons
									 select person.Age).Min();


			if (youngestAge <= 18) {
				oldestChild = (from person in household.Persons
									where person.Age <= 18
									select person.Age).Max();
			}

			double lnYoungestAge = Math.Log(1 + youngestAge);
			double lnOldestChild = Math.Log(1 + oldestChild);

			// NONE_OR_HOME
			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, available[0], choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;
			alternative.AddUtilityTerm(1, (nCallsForTour == 2).ToFlag());
			alternative.AddUtilityTerm(2, (nCallsForTour == 3).ToFlag());
			alternative.AddUtilityTerm(3, (nCallsForTour >= 4).ToFlag());
			alternative.AddUtilityTerm(4, noCarsFlag);
			alternative.AddUtilityTerm(5, carsGrAdults);
			alternative.AddUtilityTerm(6, (countKidsAtHome>0).ToFlag());

			// FULL PAIRED
			// base is two person- two worker household
			alternative = choiceProbabilityCalculator.GetAlternative(1, available[1], choice == 1);
			alternative.Choice = 1;
			alternative.AddUtilityTerm(11, 1);
			alternative.AddUtilityTerm(25, (countMandatoryAdults>2).ToFlag());
			alternative.AddUtilityTerm(13, (household.Has0To25KIncome).ToFlag());
			alternative.AddUtilityTerm(12, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(14, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			alternative.AddUtilityTerm(15, (countMandatoryChildren >= 3).ToFlag());
			alternative.AddUtilityTerm(16, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 0).ToFlag());
			alternative.AddUtilityTerm(17, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(18, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			alternative.AddUtilityTerm(20, (payParkWork));
			alternative.AddUtilityTerm(21, lnYoungestAge);
			//alternative.AddUtilityTerm(22, (household.HouseholdTotals.DrivingAgeStudents>0).ToFlag());
			//alternative.AddUtilityTerm(24, countAtHome);
			alternative.AddUtilityTerm(29, aveWorkLogsum);
			alternative.AddUtilityTerm(30, aveSchoolLogsum);

			// FULL HalfTour 1
			alternative = choiceProbabilityCalculator.GetAlternative(2, available[2], choice == 2);
			alternative.Choice = 2;
			alternative.AddUtilityTerm(31, 1);
			alternative.AddUtilityTerm(32, (countMandatoryAdults>2).ToFlag());
			//alternative.AddUtilityTerm(34, (household.HouseholdType == 4).ToFlag());
			//alternative.AddUtilityTerm(35, (household.HouseholdType == 5).ToFlag());
			alternative.AddUtilityTerm(39, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(40, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			alternative.AddUtilityTerm(41,  (countMandatoryChildren >= 3).ToFlag());
			alternative.AddUtilityTerm(42, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 0).ToFlag());
			alternative.AddUtilityTerm(43, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(44, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			//alternative.AddUtilityTerm(45, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren >= 3).ToFlag());
			alternative.AddUtilityTerm(46, (household.HouseholdTotals.DrivingAgeStudents>0).ToFlag());
			alternative.AddUtilityTerm(47, lnOldestChild);
			alternative.AddUtilityTerm(48, (payParkWork));
			alternative.AddUtilityTerm(49, totAggregateLogsum);
			alternative.AddUtilityTerm(53, (household.HouseholdTotals.ChildrenUnder5 > 1).ToFlag());
			alternative.AddUtilityTerm(54, countWorkingAtHome);
			alternative.AddUtilityTerm(59, aveWorkLogsum);
			alternative.AddUtilityTerm(60, aveSchoolLogsum);


			// Full HalfTour 2
			alternative = choiceProbabilityCalculator.GetAlternative(3, available[3], choice == 3);
			alternative.Choice = 3;
			alternative.AddUtilityTerm(51, 1);
			alternative.AddUtilityTerm(32, (countMandatoryAdults>2).ToFlag());
			//alternative.AddUtilityTerm(34, (household.HouseholdType == 4).ToFlag());
			//alternative.AddUtilityTerm(35, (household.HouseholdType == 5).ToFlag());
			alternative.AddUtilityTerm(39, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(40, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren >= 2).ToFlag());
			alternative.AddUtilityTerm(42, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 0).ToFlag());
			alternative.AddUtilityTerm(43, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(44, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			alternative.AddUtilityTerm(45, (countMandatoryChildren >= 3).ToFlag());
			//alternative.AddUtilityTerm(47, lnOldestChild);
			alternative.AddUtilityTerm(48, (payParkWork));
			alternative.AddUtilityTerm(49, totAggregateLogsum);
			alternative.AddUtilityTerm(53, (household.HouseholdTotals.ChildrenUnder5 > 1).ToFlag());
			alternative.AddUtilityTerm(54, countWorkingAtHome);
			alternative.AddUtilityTerm(46, (household.HouseholdTotals.DrivingAgeStudents>0).ToFlag());
			alternative.AddUtilityTerm(59, aveWorkLogsum);
			alternative.AddUtilityTerm(60, aveSchoolLogsum);

			// PARTIAL PAIRED
			alternative = choiceProbabilityCalculator.GetAlternative(4, available[4], choice == 4);
			alternative.Choice = 4;
			alternative.AddUtilityTerm(61, 1);
			alternative.AddUtilityTerm(62, (countMandatoryAdults>2).ToFlag());
			alternative.AddUtilityTerm(69, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(70, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			alternative.AddUtilityTerm(72, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 0).ToFlag());
			alternative.AddUtilityTerm(73, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(74, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			alternative.AddUtilityTerm(75, lnYoungestAge);
		//	alternative.AddUtilityTerm(77, totAggregateLogsum);
			alternative.AddUtilityTerm(78, household.Has75KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(82, payParkWork);
			alternative.AddUtilityTerm(84, aveSchoolLogsum);
			alternative.AddUtilityTerm(85, aveWorkLogsum);
			alternative.AddUtilityTerm(86, (household.HouseholdTotals.DrivingAgeStudents>0).ToFlag());
			alternative.AddUtilityTerm(87, (countMandatoryChildren >= 3).ToFlag());
		
			// PARTIAL HalfTour 1
			alternative = choiceProbabilityCalculator.GetAlternative(5, available[5], choice == 5);
			alternative.Choice = 5;
			alternative.AddUtilityTerm(91, 1);
			alternative.AddUtilityTerm(92, (countMandatoryAdults>2).ToFlag());
			alternative.AddUtilityTerm(98, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(99, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			//alternative.AddUtilityTerm(100, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren >= 3).ToFlag());
			alternative.AddUtilityTerm(102, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 0).ToFlag());
			alternative.AddUtilityTerm(103, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(104, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			alternative.AddUtilityTerm(105, (countMandatoryChildren >= 3).ToFlag());
			alternative.AddUtilityTerm(108, lnYoungestAge);
			alternative.AddUtilityTerm(109, lnOldestChild);
		//	alternative.AddUtilityTerm(110, totAggregateLogsum);
			alternative.AddUtilityTerm(114, (household.Has0To25KIncome).ToFlag());
			alternative.AddUtilityTerm(115, (household.Has100KPlusIncome).ToFlag());
			alternative.AddUtilityTerm(117, (household.HouseholdTotals.ChildrenAge5Through15 > 1).ToFlag());
			alternative.AddUtilityTerm(118, (household.HouseholdTotals.PartTimeWorkers > 0).ToFlag());
			alternative.AddUtilityTerm(120, (transitPassOwnership == 1).ToFlag());
			//alternative.AddUtilityTerm(122, (household.HouseholdTotals.DrivingAgeStudents>0).ToFlag());

			// PARTIAL HalfTour 2
			alternative = choiceProbabilityCalculator.GetAlternative(6, available[6], choice == 6);
			alternative.Choice = 6;
			alternative.AddUtilityTerm(101, 1);
			alternative.AddUtilityTerm(92, (countMandatoryAdults>2).ToFlag());
			alternative.AddUtilityTerm(98, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(99, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			//alternative.AddUtilityTerm(100, (countMandatoryAdults == household.HouseholdTotals.Adults).ToFlag() * (countMandatoryChildren >= 3).ToFlag());
			alternative.AddUtilityTerm(102, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 0).ToFlag());
			alternative.AddUtilityTerm(103, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 1).ToFlag());
			alternative.AddUtilityTerm(104, (countNonMandatoryAdults > 0).ToFlag() * (countMandatoryChildren == 2).ToFlag());
			alternative.AddUtilityTerm(105, (countMandatoryChildren >= 3).ToFlag());
			alternative.AddUtilityTerm(108, lnYoungestAge);
			alternative.AddUtilityTerm(109, lnOldestChild);
			//alternative.AddUtilityTerm(110, totAggregateLogsum);
			alternative.AddUtilityTerm(114, (household.Has0To25KIncome).ToFlag());
			alternative.AddUtilityTerm(115, (household.Has100KPlusIncome).ToFlag());
			alternative.AddUtilityTerm(117, (household.HouseholdTotals.ChildrenAge5Through15 > 1).ToFlag());
			alternative.AddUtilityTerm(118, (household.HouseholdTotals.PartTimeWorkers > 0).ToFlag());
			alternative.AddUtilityTerm(120, (transitPassOwnership == 1).ToFlag());
			//alternative.AddUtilityTerm(122, (household.HouseholdTotals.DrivingAgeStudents>0).ToFlag());
		}
	}
}