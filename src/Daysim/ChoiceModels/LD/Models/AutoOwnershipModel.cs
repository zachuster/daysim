﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Linq;
using Daysim.DomainModels;
using Daysim.DomainModels.LD;
using Daysim.DomainModels.LD.Wrappers;
using Daysim.DomainModels.Extensions;
using Daysim.Framework.ChoiceModels;
using Daysim.Framework.Coefficients;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Wrappers;

namespace Daysim.ChoiceModels.LD.Models {
	public class AutoOwnershipModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "LDAutoOwnershipModel";
		private const int TOTAL_ALTERNATIVES = 3;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 84;

		public override void RunInitialize(ICoefficientsReader reader = null) {
			Initialize(CHOICE_MODEL_NAME, Global.Configuration.AutoOwnershipModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER, reader as CoefficientsReader);
		}

		public void Run(HouseholdWrapper household) {
			if (household == null) {
				throw new ArgumentNullException("household");
			}

			household.ResetRandom(4);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(household.Id);

			var vehicles = household.VehiclesAvailable;

			if (household.VehiclesAvailable > 2) {
				vehicles = 2;
			}

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				RunModel(choiceProbabilityCalculator, household, vehicles);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, household);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(household.RandomUtility);
				var choice = (int) chosenAlternative.Choice;

				household.VehiclesAvailable = choice;
			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, HouseholdWrapper household, int choice = Constants.DEFAULT_VALUE) {
			//			var distanceToTransitCappedUnderQtrMile = household.ResidenceParcel.DistanceToTransitCappedUnderQtrMile();
			//			var distanceToTransitQtrToHalfMile = household.ResidenceParcel.DistanceToTransitQtrToHalfMile();
			var foodRetailServiceMedicalLogBuffer1 = household.ResidenceParcel.FoodRetailServiceMedicalLogBuffer1();

			var workTourLogsumDifference = 0D; // (full or part-time workers) full car ownership vs. no car ownership
			var schoolTourLogsumDifference = 0D; // (school) full car ownership vs. no car ownership
			//			const double workTourOtherLogsumDifference = 0D; // (other workers) full car ownership vs. no car ownership

			// Stefan
			var netIncome = (household.Income / 1000.0) / 2.0; // in 1000s of DKK
			var userCost = 2.441 * 15.0;  //annual cost to use 1 car in 1000s of DKK
			bool isInCopenhagenMunicipality = true; //household.ResidenceParcel.Municipality == 101;  Need to change this after Municipality property is added to LD parcel file

			int numberAdults = 0;
			int numberChildren = 0;
			int numberWorkers = 0;
			int sumAdultAges = 0;
			double averageAdultAge = 0.0;
			bool isMale = false;

			foreach (PersonWrapper person in household.Persons) {
				if (person.IsWorker && person.UsualWorkParcel != null && person.UsualWorkParcelId != household.ResidenceParcelId) {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Global.Settings.Models.WorkTourModeModel);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Global.Settings.Models.WorkTourModeModel);

					var nestedAlternative1 = Global.ChoiceModelSession.Get<WorkTourModeTimeModel>().RunNested(person, household.ResidenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, household.HouseholdTotals.DrivingAgeMembers, 0.0);
					var nestedAlternative2 = Global.ChoiceModelSession.Get<WorkTourModeTimeModel>().RunNested(person, household.ResidenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, 0, 0.0);

					workTourLogsumDifference += nestedAlternative1 == null ? 0 : nestedAlternative1.ComputeLogsum();
					workTourLogsumDifference -= nestedAlternative2 == null ? 0 : nestedAlternative2.ComputeLogsum();
				}

				if (person.IsDrivingAgeStudent && person.UsualSchoolParcel != null && person.UsualSchoolParcelId != household.ResidenceParcelId) {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Global.Settings.Models.SchoolTourModeModel);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Global.Settings.Models.SchoolTourModeModel);

					var nestedAlternative1 = Global.ChoiceModelSession.Get<SchoolTourModeTimeModel>().RunNested(person, household.ResidenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, household.HouseholdTotals.DrivingAgeMembers, 0.0);
					var nestedAlternative2 = Global.ChoiceModelSession.Get<SchoolTourModeTimeModel>().RunNested(person, household.ResidenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, 0, 0.0);

					schoolTourLogsumDifference += nestedAlternative1 == null ? 0 : nestedAlternative1.ComputeLogsum();
					schoolTourLogsumDifference -= nestedAlternative2 == null ? 0 : nestedAlternative2.ComputeLogsum();
				}
				if (person.Age >= 18) {
					numberAdults++;
					sumAdultAges = sumAdultAges + person.Age;
					isMale = person.IsMale;
					if (person.PersonType == Global.Settings.PersonTypes.FullTimeWorker
						//|| person.PersonType == Constants.PersonType.PART_TIME_WORKER
						) {
						numberWorkers++;
					}
				}
				else {
					numberChildren++;
				}

			}
			averageAdultAge = sumAdultAges / Math.Max(numberAdults, 1);


			// var votSegment = household.VotALSegment;
			//var taSegment = household.ResidenceParcel.TransitAccessSegment();

			//var aggregateLogsumDifference = // full car ownership vs. no car ownership
			//	Global.AggregateLogsums[household.ResidenceZoneId][Global.Settings.Purposes.HomeBasedComposite][Global.Settings.CarOwnerships.OneOrMoreCarsPerAdult][votSegment][taSegment] -
			//	Global.AggregateLogsums[household.ResidenceZoneId][Global.Settings.Purposes.HomeBasedComposite][Global.Settings.CarOwnerships.NoCars][votSegment][taSegment];

			// 0 AUTOS

			var alternative = choiceProbabilityCalculator.GetAlternative(0, true, choice == 0);
			alternative.Choice = 0;
			alternative.AddUtilityTerm(14, Math.Log(Math.Max(netIncome, 1)));


			// 1 AUTO

			var beta010 = -6.59;
			var beta011 = 4.25;
			var beta012 = 5.53;
			var beta013 = 6.54;
			var beta014 = 1.17;
			var beta015 = 0.54;
			var beta016 = 0.81;
			var beta017 = 1.20;
			var beta018 = -0.54;
			var beta019 = 0.0;
			var beta020 = 0.45;
			var beta021 = 0.0;
			var beta022 = -0.04;
			var beta023 = 0.57;
			var beta024 = 0.18;
			var beta025 = -0.82;

			var stefanOneCarUtility =
				beta010 * 1.0 +
				beta011 * household.Has1Driver.ToFlag() +
				beta012 * household.Has2Drivers.ToFlag() +
				beta013 * (household.Has3Drivers || household.Has4OrMoreDrivers).ToFlag() +
				beta014 * Math.Log(Math.Max(netIncome - userCost, 1)) +
				beta015 * (numberChildren == 1).ToFlag() +
				beta016 * (numberChildren == 2).ToFlag() +
				beta017 * (numberChildren > 2).ToFlag() +
				beta018 * (numberAdults == 1 && !isMale).ToFlag() +
				beta019 * (numberAdults == 1 && isMale).ToFlag() +
				beta020 * averageAdultAge / 10.0 +
				beta021 * Math.Pow(averageAdultAge / 10.0, 2.0) +
				beta022 * 0 + //household.ResidenceParcel.PSearchTime16_17 +  // Add this when new parcel variables with seach time are available
				beta023 * household.ResidenceParcel.DistanceToLocalBus +
				beta024 * household.ResidenceParcel.DistanceToExpressBus +
				beta025 * isInCopenhagenMunicipality.ToFlag() +
				0;

			alternative = choiceProbabilityCalculator.GetAlternative(1, true, choice == 1);
			alternative.Choice = 1;
			//Stefan
			//alternative.AddUtilityTerm(10, 1.0);
			//alternative.AddUtilityTerm(11, household.Has1Driver.ToFlag());
			//alternative.AddUtilityTerm(12, household.Has2Drivers.ToFlag());
			//alternative.AddUtilityTerm(13, (household.Has3Drivers || household.Has4OrMoreDrivers).ToFlag());
			//alternative.AddUtilityTerm(14, Math.Log(Math.Max(netIncome - userCost, 1)));
			//alternative.AddUtilityTerm(15, (numberChildren == 1).ToFlag());
			//alternative.AddUtilityTerm(16, (numberChildren == 2).ToFlag());
			//alternative.AddUtilityTerm(17, (numberChildren > 2).ToFlag());
			//alternative.AddUtilityTerm(18, (numberAdults == 1 && !isMale).ToFlag());
			//alternative.AddUtilityTerm(19, (numberAdults == 1 && isMale).ToFlag());
			//alternative.AddUtilityTerm(20, averageAdultAge / 10.0);
			//alternative.AddUtilityTerm(21, Math.Pow(averageAdultAge / 10.0, 2.0));
			////alternative.AddUtilityTerm(22, household.ResidenceParcel.PSearchTime16_17);  // Add this when new parcel variables with seach time are available
			//alternative.AddUtilityTerm(23, household.ResidenceParcel.DistanceToLocalBus);
			//alternative.AddUtilityTerm(24, household.ResidenceParcel.DistanceToExpressBus);
			//alternative.AddUtilityTerm(25, isInCopenhagenMunicipality.ToFlag());
			alternative.AddUtilityTerm(26, stefanOneCarUtility);  //this composite replaces above separate terms 10-25

			alternative.AddUtilityTerm(27, workTourLogsumDifference);  // instead of all Stefan's work-related and logsum variables

			//alternative.AddUtilityTerm(24, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
			//alternative.AddUtilityTerm(25, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);
			//alternative.AddUtilityTerm(26, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
			//alternative.AddUtilityTerm(27, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);

			// 2+ AUTOS

			var beta040 = -9.540;
			var beta041 = 2.79;
			var beta042 = 6.09;
			var beta043 = 7.77;
			var beta044 = 0;
			var beta045 = 0.35;
			var beta046 = 0.81;
			var beta047 = 1.33;
			var beta048 = -1.13;
			var beta049 = 0.60;
			var beta050 = 0.92;
			var beta051 = -0.05;
			var beta052 = -0.09;
			var beta053 = 0.94;
			var beta054 = 0.31;
			var beta055 = -1.54;

			var stefanTwoCarUtility =
				beta040 * 1.0 +
				beta041 * household.Has1Driver.ToFlag() +
				beta042 * household.Has2Drivers.ToFlag() +
				beta043 * (household.Has3Drivers || household.Has4OrMoreDrivers).ToFlag() +
				beta014 * Math.Log(Math.Max(netIncome - userCost * 2.0, 1)) +
				beta045 * (numberChildren == 1).ToFlag() +
				beta046 * (numberChildren == 2).ToFlag() +
				beta047 * (numberChildren > 2).ToFlag() +
				beta048 * (numberAdults == 1 && !isMale).ToFlag() +
				beta049 * (numberAdults == 1 && isMale).ToFlag() +
				beta050 * averageAdultAge / 10.0 +
				beta051 * Math.Pow(averageAdultAge / 10.0, 2.0) +
				beta052 * 0 + //household.ResidenceParcel.PSearchTime16_17 +  // Add this when new parcel variables with seach time are available
				beta053 * household.ResidenceParcel.DistanceToLocalBus +
				beta054 * household.ResidenceParcel.DistanceToExpressBus +
				beta055 * isInCopenhagenMunicipality.ToFlag() +
				0;

			alternative = choiceProbabilityCalculator.GetAlternative(2, true, choice == 2);
			alternative.Choice = 2;
			//Stefan
			//alternative.AddUtilityTerm(40, 1.0);
			//alternative.AddUtilityTerm(41, household.Has1Driver.ToFlag());
			//alternative.AddUtilityTerm(42, household.Has2Drivers.ToFlag());
			//alternative.AddUtilityTerm(43, (household.Has3Drivers || household.Has4OrMoreDrivers).ToFlag());
			//alternative.AddUtilityTerm(14, Math.Log(Math.Max(netIncome - userCost * 2.0, 1)));
			//alternative.AddUtilityTerm(45, (numberChildren == 1).ToFlag());
			//alternative.AddUtilityTerm(46, (numberChildren == 2).ToFlag());
			//alternative.AddUtilityTerm(47, (numberChildren > 2).ToFlag());
			//alternative.AddUtilityTerm(48, (numberAdults == 1 && !isMale).ToFlag());
			//alternative.AddUtilityTerm(49, (numberAdults == 1 && isMale).ToFlag());
			//alternative.AddUtilityTerm(50, averageAdultAge / 10.0);
			//alternative.AddUtilityTerm(51, Math.Pow(averageAdultAge / 10.0, 2.0));
			////alternative.AddUtilityTerm(52, household.ResidenceParcel.PSearchTime16_17);  // Add this when new parcel variables with seach time are available
			//alternative.AddUtilityTerm(53, household.ResidenceParcel.DistanceToLocalBus);
			//alternative.AddUtilityTerm(54, household.ResidenceParcel.DistanceToExpressBus);
			//alternative.AddUtilityTerm(55, isInCopenhagenMunicipality.ToFlag());
			alternative.AddUtilityTerm(56, stefanTwoCarUtility);  //this composite replaces above separate terms 40-55
	
			alternative.AddUtilityTerm(57, workTourLogsumDifference);

			//alternative.AddUtilityTerm(44, household.HouseholdTotals.RetiredAdultsPerDrivingAgeMembers);
			//alternative.AddUtilityTerm(45, household.HouseholdTotals.UniversityStudentsPerDrivingAgeMembers);
			//alternative.AddUtilityTerm(46, household.HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers);
			//alternative.AddUtilityTerm(47, household.HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers);

		}
	}
}