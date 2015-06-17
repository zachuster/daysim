﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Interfaces;
using Daysim.ModelRunners;
using IHouseholdWrapper = Daysim.Interfaces.IHouseholdWrapper;

namespace Daysim.ChoiceModels {
	public class AutoOwnershipModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "AutoOwnershipModel";
		private const int TOTAL_ALTERNATIVES = 5;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 84;

		public void Run(IHouseholdWrapper household, ICoefficientsReader reader = null)
		{
			if (household == null) {
				throw new ArgumentNullException("household");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.AutoOwnershipModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER, reader as CoefficientsReader);
			
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
			else {
				RunModel(choiceProbabilityCalculator, household);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(household.RandomUtility);
				var choice = (int) chosenAlternative.Choice;

				household.VehiclesAvailable = choice;
			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, IHouseholdWrapper household, int choice = Constants.DEFAULT_VALUE) {
//			var distanceToTransitCappedUnderQtrMile = household.ResidenceParcel.DistanceToTransitCappedUnderQtrMile();
//			var distanceToTransitQtrToHalfMile = household.ResidenceParcel.DistanceToTransitQtrToHalfMile();
			var foodRetailServiceMedicalLogBuffer1 = household.ResidenceParcel.FoodRetailServiceMedicalLogBuffer1();

			var workTourLogsumDifference = 0D; // (full or part-time workers) full car ownership vs. no car ownership
			var schoolTourLogsumDifference = 0D; // (school) full car ownership vs. no car ownership
//			const double workTourOtherLogsumDifference = 0D; // (other workers) full car ownership vs. no car ownership

			foreach (var person in household.Persons) {
				if (person.IsWorker && person.UsualWorkParcel != null && person.UsualWorkParcelId != household.ResidenceParcelId) {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.WORK_TOUR_MODE_MODEL);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.WORK_TOUR_MODE_MODEL);

					var nestedAlternative1 = (Global.ChoiceModelDictionary.Get("WorkTourModeModel") as WorkTourModeModel).RunNested(person, household.ResidenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, household.HouseholdTotals.DrivingAgeMembers);
					var nestedAlternative2 = (Global.ChoiceModelDictionary.Get("WorkTourModeModel") as WorkTourModeModel).RunNested(person, household.ResidenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, 0);

					workTourLogsumDifference += nestedAlternative1 == null ? 0 : nestedAlternative1.ComputeLogsum();
					workTourLogsumDifference -= nestedAlternative2 == null ? 0 : nestedAlternative2.ComputeLogsum();
				}

				if (person.IsDrivingAgeStudent && person.UsualSchoolParcel != null && person.UsualSchoolParcelId != household.ResidenceParcelId) {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);

					var nestedAlternative1 = (Global.ChoiceModelDictionary.Get("SchoolTourModeModel") as SchoolTourModeModel).RunNested(person, household.ResidenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, household.HouseholdTotals.DrivingAgeMembers);
					var nestedAlternative2 = (Global.ChoiceModelDictionary.Get("SchoolTourModeModel") as SchoolTourModeModel).RunNested(person, household.ResidenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, 0);

					schoolTourLogsumDifference += nestedAlternative1 == null ? 0 : nestedAlternative1.ComputeLogsum();
					schoolTourLogsumDifference -= nestedAlternative2 == null ? 0 : nestedAlternative2.ComputeLogsum();
				}
			}

			// var votSegment = household.VotALSegment;
			//var taSegment = household.ResidenceParcel.TransitAccessSegment();

			//var aggregateLogsumDifference = // full car ownership vs. no car ownership
			//	Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT][votSegment][taSegment] -
			//	Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.NO_CARS][votSegment][taSegment];

			var distanceToStop
				= household.ResidenceParcel.DistanceToTransit > 0
					  ? Math.Min(household.ResidenceParcel.DistanceToTransit, 2 * Global.DistanceUnitsPerMile)  // JLBscale
					  : 2 * Global.DistanceUnitsPerMile;

			var ruralFlag = household.ResidenceParcel.RuralFlag();

			// 0 AUTOS

			var alternative = choiceProbabilityCalculator.GetAlternative(0, true, choice == 0);

			alternative.Choice = 0;

			alternative.AddUtilityTerm(1, household.Has1Driver.ToFlag());
			alternative.AddUtilityTerm(5, household.Has2Drivers.ToFlag());
			alternative.AddUtilityTerm(9, household.Has3Drivers.ToFlag());
			alternative.AddUtilityTerm(13, household.Has4OrMoreDrivers.ToFlag());
//			alternative.AddUtility(17, 0);
			alternative.AddUtilityTerm(18, household.HasNoFullOrPartTimeWorker.ToFlag());
			alternative.AddUtilityTerm(19, household.HouseholdTotals.PartTimeWorkersPerDrivingAgeMembers);
//			alternative.AddUtility(23, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
//			alternative.AddUtility(27, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(31, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(35, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);
//			alternative.AddUtility(39, household.HouseholdTotals.ChildrenUnder5PerDrivingAgeMembers);
			alternative.AddUtilityTerm(43, household.Has0To15KIncome.ToFlag());
			alternative.AddUtilityTerm(47, household.Has50To75KIncome.ToFlag());
			alternative.AddUtilityTerm(51, household.Has75KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(55, household.HasMissingIncome.ToFlag());
			alternative.AddUtilityTerm(59, workTourLogsumDifference);
//			alternative.AddUtility(61, workTourOtherLogsumDifference);
			alternative.AddUtilityTerm(63, schoolTourLogsumDifference);
//			alternative.AddUtility(67, aggregateLogsumDifference);
			alternative.AddUtilityTerm(69, Math.Log(distanceToStop));
//			alternative.AddUtility(70, Math.Log(1+household.ResidenceParcel.StopsTransitBuffer1));
			alternative.AddUtilityTerm(73, household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer1);
			alternative.AddUtilityTerm(75, foodRetailServiceMedicalLogBuffer1);
			alternative.AddUtilityTerm(77, workTourLogsumDifference * ruralFlag);
//			alternative.AddUtility(78, workTourOtherLogsumDifference * ruralFlag);
//			alternative.AddUtility(79, aggregateLogsumDifference * ruralFlag);
//			alternative.AddUtility(80, Math.Log(distanceToStop)*ruralFlag);
//			alternative.AddUtility(81, Math.Log(1+household.ResidenceParcel.StopsTransitBuffer1)*ruralFlag);
//			alternative.AddUtility(82, foodRetailServiceMedicalQtrMileLog * ruralFlag);
			alternative.AddUtilityTerm(83, ruralFlag);

			// 1 AUTO

			alternative = choiceProbabilityCalculator.GetAlternative(1, true, choice == 1);

			alternative.Choice = 1;

			alternative.AddUtilityTerm(6, household.Has2Drivers.ToFlag());
			alternative.AddUtilityTerm(10, household.Has3Drivers.ToFlag());
			alternative.AddUtilityTerm(14, household.Has4OrMoreDrivers.ToFlag());
//			alternative.AddUtility(17, 1D / Math.Max(household.HouseholdTotals.DrivingAgeMembers, 1)); // ratio of 1 car per driving age members
			alternative.AddUtilityTerm(18, household.Has1OrLessFullOrPartTimeWorkers.ToFlag());
			alternative.AddUtilityTerm(20, household.HouseholdTotals.PartTimeWorkersPerDrivingAgeMembers);
			alternative.AddUtilityTerm(24, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(28, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(32, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(36, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);
//			alternative.AddUtility(40, household.HouseholdTotals.ChildrenUnder5PerDrivingAgeMembers);
			alternative.AddUtilityTerm(44, household.Has0To15KIncome.ToFlag());
			alternative.AddUtilityTerm(48, household.Has50To75KIncome.ToFlag());
			alternative.AddUtilityTerm(52, household.Has75KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(56, household.HasMissingIncome.ToFlag());
			alternative.AddUtilityTerm(60, workTourLogsumDifference * household.HasMoreDriversThan1.ToFlag());
//			alternative.AddUtility(62, workTourOtherLogsumDifference * household.HasMoreDriversThan1.ToFlag());
//			alternative.AddUtility(64, schoolTourLogsumDifference * household.HasMoreDriversThan1.ToFlag());
//			alternative.AddUtility(68, aggregateLogsumDifference * household.HasMoreDriversThan1.ToFlag());
			alternative.AddUtilityTerm(72, Math.Log(1 + household.ResidenceParcel.StopsTransitBuffer1) * household.HasMoreDriversThan1.ToFlag());
			//alternative.AddUtility(74, household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer1 * household.HasMoreDriversThan1.ToFlag());
			alternative.AddUtilityTerm(76, foodRetailServiceMedicalLogBuffer1 * household.HasMoreDriversThan1.ToFlag());

			// 2 AUTOS

			alternative = choiceProbabilityCalculator.GetAlternative(2, true, choice == 2);

			alternative.Choice = 2;

			alternative.AddUtilityTerm(2, household.Has1Driver.ToFlag());
			alternative.AddUtilityTerm(11, household.Has3Drivers.ToFlag());
			alternative.AddUtilityTerm(15, household.Has4OrMoreDrivers.ToFlag());
//			alternative.AddUtility(17, 2D / Math.Max(household.HouseholdTotals.DrivingAgeMembers, 1)); // ratio of 2 cars per driving age members
			alternative.AddUtilityTerm(18, household.Has2OrLessFullOrPartTimeWorkers.ToFlag());
			alternative.AddUtilityTerm(60, workTourLogsumDifference * household.HasMoreDriversThan2.ToFlag());
//			alternative.AddUtility(62, workTourOtherLogsumDifference * household.HasMoreDriversThan2.ToFlag());
//			alternative.AddUtility(64, schoolTourLogsumDifference * household.HasMoreDriversThan2.ToFlag());
//			alternative.AddUtility(68, aggregateLogsumDifference * household.HasMoreDriversThan2.ToFlag());
			alternative.AddUtilityTerm(72, Math.Log(1 + household.ResidenceParcel.StopsTransitBuffer1) * household.HasMoreDriversThan2.ToFlag());
//			alternative.AddUtility(74, household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer1 * household.HasMoreDriversThan2.ToFlag());
			alternative.AddUtilityTerm(76, foodRetailServiceMedicalLogBuffer1 * household.HasMoreDriversThan2.ToFlag());

			// 3 AUTOS

			alternative = choiceProbabilityCalculator.GetAlternative(3, true, choice == 3);

			alternative.Choice = 3;

			alternative.AddUtilityTerm(3, household.Has1Driver.ToFlag());
			alternative.AddUtilityTerm(7, household.Has2Drivers.ToFlag());
			alternative.AddUtilityTerm(16, household.Has4OrMoreDrivers.ToFlag());
//			alternative.AddUtility(17, 3D / Math.Max(household.HouseholdTotals.DrivingAgeMembers, 1)); // ratio of 3 cars per driving age members
			alternative.AddUtilityTerm(18, household.Has3OrLessFullOrPartTimeWorkers.ToFlag());
//			alternative.AddUtility(21, household.HouseholdTotals.PartTimeWorkersPerDrivingAgeMembers);
			alternative.AddUtilityTerm(25, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(29, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);
//			alternative.AddUtility(33, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(37, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(41, household.HouseholdTotals.ChildrenUnder5PerDrivingAgeMembers);
			alternative.AddUtilityTerm(45, household.Has0To15KIncome.ToFlag());
			alternative.AddUtilityTerm(49, household.Has50To75KIncome.ToFlag());
			alternative.AddUtilityTerm(53, household.Has75KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(57, household.HasMissingIncome.ToFlag());
			alternative.AddUtilityTerm(60, workTourLogsumDifference * household.HasMoreDriversThan3.ToFlag());
//			alternative.AddUtility(62, workTourOtherLogsumDifference * household.HasMoreDriversThan3.ToFlag());
//			alternative.AddUtility(64, schoolTourLogsumDifference * household.HasMoreDriversThan3.ToFlag());
//			alternative.AddUtility(68, aggregateLogsumDifference * household.HasMoreDriversThan3.ToFlag());
			alternative.AddUtilityTerm(72, Math.Log(1 + household.ResidenceParcel.StopsTransitBuffer1) * household.HasMoreDriversThan3.ToFlag());
//			alternative.AddUtility(74, household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer1 * household.HasMoreDriversThan3.ToFlag());
			alternative.AddUtilityTerm(76, foodRetailServiceMedicalLogBuffer1 * household.HasMoreDriversThan3.ToFlag());

			// 4+ AUTOS

			alternative = choiceProbabilityCalculator.GetAlternative(4, true, choice == 4);

			alternative.Choice = 4;

			alternative.AddUtilityTerm(4, household.Has1Driver.ToFlag());
			alternative.AddUtilityTerm(8, household.Has2Drivers.ToFlag());
			alternative.AddUtilityTerm(12, household.Has3Drivers.ToFlag());
//			alternative.AddUtility(17, 4D / Math.Max(household.HouseholdTotals.DrivingAgeMembers, 1)); // ratio of 4 cars per driving age members
			alternative.AddUtilityTerm(18, household.Has4OrLessFullOrPartTimeWorkers.ToFlag());
			alternative.AddUtilityTerm(22, household.HouseholdTotals.PartTimeWorkersPerDrivingAgeMembers);
			alternative.AddUtilityTerm(26, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
//			alternative.AddUtility(30, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(34, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(38, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);
			alternative.AddUtilityTerm(42, household.HouseholdTotals.ChildrenUnder5PerDrivingAgeMembers);
			alternative.AddUtilityTerm(46, household.Has0To15KIncome.ToFlag());
			alternative.AddUtilityTerm(50, household.Has50To75KIncome.ToFlag());
			alternative.AddUtilityTerm(54, household.Has75KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(58, household.HasMissingIncome.ToFlag());
			alternative.AddUtilityTerm(60, workTourLogsumDifference * household.HasMoreDriversThan4.ToFlag());
//			alternative.AddUtility(62, workTourOtherLogsumDifference * household.HasMoreDriversThan4.ToFlag());
//			alternative.AddUtility(64, schoolTourLogsumDifference * household.HasMoreDriversThan4.ToFlag());
//			alternative.AddUtility(68, aggregateLogsumDifference * household.HasMoreDriversThan4.ToFlag());
			alternative.AddUtilityTerm(72, Math.Log(1 + household.ResidenceParcel.StopsTransitBuffer1) * household.HasMoreDriversThan4.ToFlag());
//			alternative.AddUtility(74, household.ResidenceParcel.ParkingOffStreetPaidDailyPriceBuffer1 * household.HasMoreDriversThan4.ToFlag());
			alternative.AddUtilityTerm(76, foodRetailServiceMedicalLogBuffer1 * household.HasMoreDriversThan4.ToFlag());
		}
	}
}