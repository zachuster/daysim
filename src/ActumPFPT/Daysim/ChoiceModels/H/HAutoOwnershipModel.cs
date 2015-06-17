// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Daysim.ChoiceModels {
	public static class HAutoOwnershipModel {
		private const string CHOICE_MODEL_NAME = "HAutoOwnershipModel";
		private const int TOTAL_ALTERNATIVES = 5;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER =181;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		private static readonly object _lock = new object();

		private static void Initialize() {
			lock(_lock)
			{
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null)
				{
					return;
				}

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME, Global.GetInputPath(Global.Configuration.AutoOwnershipModelCoefficients),
				                             TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static void Run(HouseholdWrapper household) {
			if (household == null) {
				throw new ArgumentNullException("household");
			}

			Initialize();
			
			household.ResetRandom(4);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(household.Id);

			if (household.VehiclesAvailable > 4) {
				household.VehiclesAvailable = 4;
			}

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				RunModel(choiceProbabilityCalculator, household, household.VehiclesAvailable);

				choiceProbabilityCalculator.WriteObservation();
			}
			else if (Global.Configuration.TestEstimationModelInApplicationMode) {
                
                Global.Configuration.IsInEstimationMode = false;
				RunModel(choiceProbabilityCalculator, household);
				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(household.RandomUtility, household.Id, household.VehiclesAvailable);
                Global.Configuration.IsInEstimationMode = true;
            }
			else {
				RunModel(choiceProbabilityCalculator, household);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(household.RandomUtility);
				var choice = (int) chosenAlternative.Choice;

				household.VehiclesAvailable = choice;
			}
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, HouseholdWrapper household, int choice = Constants.DEFAULT_VALUE) {
//			
			var workTourLogsumDifference = 0D; // (full or part-time workers) full car ownership vs. no car ownership
//			var schoolTourLogsumDifference = 0D; // (school) full car ownership vs. no car ownership

			var countTransitPasses = 0D;
//			var countPayToParkAtWork = 0D;
          var wrkrTransitPass = 0D;

			foreach (var person in household.Persons) {
				if(person.TransitPassOwnershipFlag==1){
					countTransitPasses++;
				}
				if (person.IsWorker && person.UsualWorkParcel != null && person.UsualWorkParcelId != household.ResidenceParcelId) {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.WORK_TOUR_MODE_MODEL);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.WORK_TOUR_MODE_MODEL);

					var nestedAlternative1 = (Global.ChoiceModelDictionary.Get("WorkTourModeModel") as WorkTourModeModel).RunNested(person, household.ResidenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, household.HouseholdTotals.DrivingAgeMembers);
					var nestedAlternative2 = (Global.ChoiceModelDictionary.Get("WorkTourModeModel") as WorkTourModeModel).RunNested(person, household.ResidenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, 0);

					workTourLogsumDifference += nestedAlternative1 == null ? 0 : nestedAlternative1.ComputeLogsum();
					workTourLogsumDifference -= nestedAlternative2 == null ? 0 : nestedAlternative2.ComputeLogsum();
   
				}

				/*if (person.IsDrivingAgeStudent && person.UsualSchoolParcel != null && person.UsualSchoolParcelId != household.ResidenceParcelId) {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);

					var nestedAlternative1 = SchoolTourModeModel.RunNested(person, household.ResidenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, household.HouseholdTotals.DrivingAgeMembers);
					var nestedAlternative2 = SchoolTourModeModel.RunNested(person, household.ResidenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, 0);

					schoolTourLogsumDifference += nestedAlternative1 == null ? 0 : nestedAlternative1.ComputeLogsum();
					schoolTourLogsumDifference -= nestedAlternative2 == null ? 0 : nestedAlternative2.ComputeLogsum();
				}*/
			}

        	int oldestAge= (from persons in household.Persons select persons.Age).Max();
         int youngestAge= (from persons in household.Persons select persons.Age).Min();

			var transitPassesGrEqDrs = countTransitPasses>=household.HouseholdTotals.DrivingAgeMembers ? 1:0;
			var noTransitPasses = countTransitPasses==0?1:0;

			var votSegment = household.VotALSegment;
			var taSegment = household.ResidenceParcel.TransitAccessSegment();

//			var aggregateLogsumDifference = // full car ownership vs. no car ownership
//				Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT][votSegment][taSegment] -
//				Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.NO_CARS][votSegment][taSegment];

           var noCarAggLogsum =
                Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.NO_CARS][votSegment][taSegment];

			var distanceToStop
				= household.ResidenceParcel.DistanceToTransit > 0
					  ? Math.Min(household.ResidenceParcel.DistanceToTransit, 2 * Global.DistanceUnitsPerMile)  // JLBscale
					  : 2 * Global.DistanceUnitsPerMile;

			var ruralFlag = household.ResidenceParcel.RuralFlag();

			//home parcel buffer variables
			var foodRetailServiceMedicalLogBuffer1 = household.ResidenceParcel.FoodRetailServiceMedicalLogBuffer1();
			var intrDens = household.ResidenceParcel.IntersectionDensity34Buffer2();

			// 0 AUTOS

			var alternative = choiceProbabilityCalculator.GetAlternative(0, true, choice == 0);

			alternative.Choice = 0;

			alternative.AddUtilityTerm(1, household.Has1Driver.ToFlag());
			alternative.AddUtilityTerm(2, household.Has2Drivers.ToFlag());
			alternative.AddUtilityTerm(2, household.Has3Drivers.ToFlag());
			alternative.AddUtilityTerm(2, household.Has4OrMoreDrivers.ToFlag());
			alternative.AddUtilityTerm(5, household.HasNoFullOrPartTimeWorker.ToFlag());
			//alternative.AddUtilityTerm(7, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(8, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);			
			alternative.AddUtilityTerm(9, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
	    	//alternative.AddUtilityTerm(10, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);
			//alternative.AddUtilityTerm(11, household.HouseholdTotals.ChildrenUnder5PerDrivingAgeMembers);
			alternative.AddUtilityTerm(12, household.Has0To25KIncome.ToFlag());
			alternative.AddUtilityTerm(13, household.HasIncomeOver50K.ToFlag());
			alternative.AddUtilityTerm(16, household.HasMissingIncome.ToFlag());
			alternative.AddUtilityTerm(17, workTourLogsumDifference);
			//	alternative.AddUtilityTerm(18, schoolTourLogsumDifference);
			//	alternative.AddUtilityTerm(20, aggregateLogsumDifference);
			alternative.AddUtilityTerm(21, Math.Log(distanceToStop));
		//	alternative.AddUtilityTerm(22, Math.Log(1+household.ResidenceParcel.StopsTransitBuffer1));
			//alternative.AddUtilityTerm(23, Math.Log(1+household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer2));
			alternative.AddUtilityTerm(24, foodRetailServiceMedicalLogBuffer1);
			alternative.AddUtilityTerm(27, transitPassesGrEqDrs);
			alternative.AddUtilityTerm(28, noTransitPasses);
			alternative.AddUtilityTerm(29, intrDens);
         alternative.AddUtilityTerm(31,  household.HasChildrenUnder16.ToFlag());
         alternative.AddUtilityTerm(33, (household.Has50To75KIncome).ToFlag()*noCarAggLogsum);
         alternative.AddUtilityTerm(34, (household.Has75KPlusIncome).ToFlag() * noCarAggLogsum);
         alternative.AddUtilityTerm(35, (youngestAge>70).ToFlag());
         alternative.AddUtilityTerm(36, (oldestAge<35).ToFlag());
		
            // 1 AUTO

			alternative = choiceProbabilityCalculator.GetAlternative(1, true, choice == 1);

			alternative.Choice = 1;

			alternative.AddUtilityTerm(37, household.Has2Drivers.ToFlag());
			alternative.AddUtilityTerm(38, household.Has3Drivers.ToFlag());
			alternative.AddUtilityTerm(38, household.Has4OrMoreDrivers.ToFlag());
			alternative.AddUtilityTerm(40, 1D / Math.Max(household.HouseholdTotals.DrivingAgeMembers, 1)); // ratio of 1 car per driving age members
			alternative.AddUtilityTerm(5, household.Has1OrLessFullOrPartTimeWorkers.ToFlag());
        // alternative.AddUtilityTerm(6, household.HouseholdTotals.PartTimeWorkersPerDrivingAgeMembers);
         //alternative.AddUtilityTerm(7, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
         alternative.AddUtilityTerm(8, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);
         alternative.AddUtilityTerm(9, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
        // alternative.AddUtilityTerm(10, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);
       //  alternative.AddUtilityTerm(11, household.HouseholdTotals.ChildrenUnder5PerDrivingAgeMembers);
			alternative.AddUtilityTerm(41, household.Has0To25KIncome.ToFlag());
			alternative.AddUtilityTerm(42, household.Has50To75KIncome.ToFlag());
			alternative.AddUtilityTerm(43, household.Has75To100KIncome.ToFlag());
			alternative.AddUtilityTerm(44, household.Has100KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(45, household.HasMissingIncome.ToFlag());
			alternative.AddUtilityTerm(17, workTourLogsumDifference * household.HasMoreDriversThan1.ToFlag());
			//alternative.AddUtilityTerm(22, Math.Log(1 + household.ResidenceParcel.StopsTransitBuffer1) * household.HasMoreDriversThan1.ToFlag());
			alternative.AddUtilityTerm(23, household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer1 * household.HasMoreDriversThan1.ToFlag());
			alternative.AddUtilityTerm(24, foodRetailServiceMedicalLogBuffer1 * household.HasMoreDriversThan1.ToFlag());
			alternative.AddUtilityTerm(46, intrDens);
			alternative.AddUtilityTerm(47, noTransitPasses);
         alternative.AddUtilityTerm(35, (youngestAge > 70).ToFlag());
         alternative.AddUtilityTerm(36, (oldestAge < 35).ToFlag());
			alternative.AddUtilityTerm(48, household.HasChildrenUnder16.ToFlag());
			alternative.AddUtilityTerm(49, transitPassesGrEqDrs);

			// 2 AUTOS

			alternative = choiceProbabilityCalculator.GetAlternative(2, true, choice == 2);

			alternative.Choice = 2;

			alternative.AddUtilityTerm(50, household.Has1Driver.ToFlag());
			//alternative.AddUtilityTerm(51, household.Has3Drivers.ToFlag());
			alternative.AddUtilityTerm(52, household.Has4OrMoreDrivers.ToFlag());
			alternative.AddUtilityTerm(40, 2D / Math.Max(household.HouseholdTotals.DrivingAgeMembers, 1)); // ratio of 2 cars per driving age members
			alternative.AddUtilityTerm(5, household.Has2OrLessFullOrPartTimeWorkers.ToFlag());
			alternative.AddUtilityTerm(17, workTourLogsumDifference * household.HasMoreDriversThan2.ToFlag());
			//alternative.AddUtilityTerm(18, schoolTourLogsumDifference * household.HasMoreDriversThan2.ToFlag());
			//alternative.AddUtilityTerm(20, aggregateLogsumDifference * household.HasMoreDriversThan2.ToFlag());
		//	alternative.AddUtilityTerm(22, Math.Log(1 + household.ResidenceParcel.StopsTransitBuffer1) * household.HasMoreDriversThan2.ToFlag());
		//	alternative.AddUtilityTerm(23, household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer1 * household.HasMoreDriversThan2.ToFlag());
			alternative.AddUtilityTerm(24, foodRetailServiceMedicalLogBuffer1 * household.HasMoreDriversThan2.ToFlag());
			alternative.AddUtilityTerm(27, ruralFlag);

			// 3 AUTOS

			alternative = choiceProbabilityCalculator.GetAlternative(3, true, choice == 3);

			alternative.Choice = 3;

			alternative.AddUtilityTerm(70, household.Has1Driver.ToFlag());
			alternative.AddUtilityTerm(71, household.Has2Drivers.ToFlag());
			alternative.AddUtilityTerm(72, household.Has4OrMoreDrivers.ToFlag());
			alternative.AddUtilityTerm(40, 3D / Math.Max(household.HouseholdTotals.DrivingAgeMembers, 1)); // ratio of 3 cars per driving age members
			alternative.AddUtilityTerm(5, household.Has3OrLessFullOrPartTimeWorkers.ToFlag());
			alternative.AddUtilityTerm(91, household.HouseholdTotals.PartTimeWorkersPerDrivingAgeMembers);
			alternative.AddUtilityTerm(92, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
			//alternative.AddUtilityTerm(93, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(94, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
			//alternative.AddUtilityTerm(95, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(96, household.HouseholdTotals.ChildrenUnder5PerDrivingAgeMembers);
			alternative.AddUtilityTerm(73, household.Has0To25KIncome.ToFlag());
			alternative.AddUtilityTerm(75, household.Has100KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(76, household.HasMissingIncome.ToFlag());
			alternative.AddUtilityTerm(17, workTourLogsumDifference * household.HasMoreDriversThan3.ToFlag());
			//alternative.AddUtilityTerm(18, schoolTourLogsumDifference * household.HasMoreDriversThan3.ToFlag());
			//alternative.AddUtilityTerm(20, aggregateLogsumDifference * household.HasMoreDriversThan3.ToFlag());
		//	alternative.AddUtilityTerm(22, Math.Log(1 + household.ResidenceParcel.StopsTransitBuffer1) * household.HasMoreDriversThan3.ToFlag());
			//alternative.AddUtilityTerm(23, household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer1 * household.HasMoreDriversThan3.ToFlag());
			alternative.AddUtilityTerm(24, foodRetailServiceMedicalLogBuffer1 * household.HasMoreDriversThan3.ToFlag());
			alternative.AddUtilityTerm(79, ruralFlag);
			// 4+ AUTOS

			alternative = choiceProbabilityCalculator.GetAlternative(4, true, choice == 4);

			alternative.Choice = 4;

			alternative.AddUtilityTerm(100, household.Has1Driver.ToFlag());
			alternative.AddUtilityTerm(101, household.Has2Drivers.ToFlag());
			alternative.AddUtilityTerm(102, household.Has3Drivers.ToFlag());
			alternative.AddUtilityTerm(40, 4D / Math.Max(household.HouseholdTotals.DrivingAgeMembers, 1)); // ratio of 4 cars per driving age members
			alternative.AddUtilityTerm(5, household.Has4OrLessFullOrPartTimeWorkers.ToFlag());
			alternative.AddUtilityTerm(91, household.HouseholdTotals.PartTimeWorkersPerDrivingAgeMembers);
			alternative.AddUtilityTerm(92, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
		//	alternative.AddUtilityTerm(93, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(94, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
		//	alternative.AddUtilityTerm(95, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(96, household.HouseholdTotals.ChildrenUnder5PerDrivingAgeMembers);
			alternative.AddUtilityTerm(107, household.Has0To25KIncome.ToFlag());
			alternative.AddUtilityTerm(108, household.Has50To75KIncome.ToFlag());
			alternative.AddUtilityTerm(109, household.Has75KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(110, household.HasMissingIncome.ToFlag());
			alternative.AddUtilityTerm(17, workTourLogsumDifference * household.HasMoreDriversThan4.ToFlag());
			//alternative.AddUtilityTerm(18, schoolTourLogsumDifference * household.HasMoreDriversThan4.ToFlag());
		//	alternative.AddUtilityTerm(20, aggregateLogsumDifference * household.HasMoreDriversThan4.ToFlag());
		//	alternative.AddUtilityTerm(22, Math.Log(1 + household.ResidenceParcel.StopsTransitBuffer1) * household.HasMoreDriversThan4.ToFlag());
		//	alternative.AddUtilityTerm(23, household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer1 * household.HasMoreDriversThan4.ToFlag());
			alternative.AddUtilityTerm(24, foodRetailServiceMedicalLogBuffer1 * household.HasMoreDriversThan4.ToFlag());
			alternative.AddUtilityTerm(111, ruralFlag);
		}
	}
}