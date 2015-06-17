﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Framework.Roster;
using Daysim.Framework.Sampling;
using Daysim.Interfaces;
using Daysim.ModelRunners;
using Ninject;

namespace Daysim.ChoiceModels {
	public static class HSchoolLocationModel {
		private const string CHOICE_MODEL_NAME = "HSchoolLocationModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 2;
		private const int TOTAL_LEVELS = 2;
		private const int MAX_PARAMETER = 99;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		private static readonly object _lock = new object();

		private static void Initialize(int sampleSize) {
			lock (_lock)
			{
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null)
				{
					return;
				}

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME, Global.GetInputPath(Global.Configuration.SchoolLocationModelCoefficients),
				                             sampleSize + 1, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static void Run(IPersonWrapper person, int sampleSize) {
			if (person == null) {
				throw new ArgumentNullException("person");
			}

			Initialize(sampleSize);
			
			person.ResetRandom(1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(person.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (person.UsualSchoolParcel == null) {
					return;
				}

				var choseHome = person.UsualSchoolParcelId == person.Household.ResidenceParcelId; // JLB 20120403 added these two lines
				var chosenParcel = choseHome ? null : person.UsualSchoolParcel;

				//RunModel(choiceProbabilityCalculator, person, sampleSize, person.UsualSchoolParcel);
				RunModel(choiceProbabilityCalculator, person, sampleSize, chosenParcel, choseHome); // JLB 20120403 replaced above line
				// when chosenParcel is null:
				// DestinationSampler doesn't try to assign one of the sampled destinations as chosen
				// choseHome is NOT null, and RunModel sets the oddball location as chosen

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, person, sampleSize);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(person.Household.RandomUtility);
				var choice = (CondensedParcel) chosenAlternative.Choice;

				person.UsualSchoolParcelId = choice.Id;
				person.UsualSchoolParcel = choice;
				person.UsualSchoolZoneKey = ChoiceModelFactory.ZoneKeys[choice.ZoneId];

				var skimValue = ImpedanceRoster.GetValue("ivtime", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, 1, person.Household.ResidenceParcel, choice);

				person.AutoTimeToUsualSchool = skimValue.Variable;
				person.AutoDistanceToUsualSchool = skimValue.BlendVariable;

				person.SetSchoolParcelPredictions();
			}
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, IPersonWrapper person, int sampleSize, ICondensedParcel choice = null, bool choseHome = false) {
			var segment = Global.Kernel.Get<SamplingWeightsSettingsFactory>().SamplingWeightsSettings.GetTourDestinationSegment(Constants.Purpose.SCHOOL, Constants.TourPriority.USUAL_LOCATION, Constants.Mode.SOV, person.PersonType);
			var destinationSampler = new DestinationSampler(choiceProbabilityCalculator, segment, sampleSize, person.Household.ResidenceParcel, choice);
			var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
			var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
			var schoolLocationUtilities = new SchoolLocationUtilities(person, sampleSize, destinationArrivalTime, destinationDepartureTime);

			destinationSampler.SampleTourDestinations(schoolLocationUtilities);

//			var alternative = choiceProbabilityCalculator.GetAlternative(countSampled, true);

			// JLB 20120403 added third call parameter to idenitfy whether this alt is chosen or not
			var alternative = choiceProbabilityCalculator.GetAlternative(sampleSize, true, choseHome);

			alternative.Choice = person.Household.ResidenceParcel;

			alternative.AddUtilityTerm(50, 1);
			alternative.AddUtilityTerm(51, (!person.IsStudentAge).ToFlag());
			alternative.AddUtilityTerm(52, person.Household.Size);
			alternative.AddUtilityTerm(98, 100); //dummy size variable for oddball alt

			//make oddball alt unavailable and remove nesting for estimation of conditional MNL 
//			alternative.Available = false;
			alternative.AddNestedAlternative(sampleSize + 3, 1, 99);
		}

		private sealed class SchoolLocationUtilities : ISamplingUtilities {
			private readonly IPersonWrapper _person;
			private readonly int _sampleSize;
			private readonly int _destinationArrivalTime;
			private readonly int _destinationDepartureTime;
			private readonly int[] _seedValues;

			public SchoolLocationUtilities(IPersonWrapper person, int sampleSize, int destinationArrivalTime, int destinationDepartureTime) {
				_person = person;
				_sampleSize = sampleSize;
				_destinationArrivalTime = destinationArrivalTime;
				_destinationDepartureTime = destinationDepartureTime;
				_seedValues = ChoiceModelUtility.GetRandomSampling(_sampleSize, person.SeedValues[1]);
			}

			public int[] SeedValues {
				get { return _seedValues; }
			}

			public void SetUtilities(ISampleItem sampleItem, int sampleFrequency) {
				if (sampleItem == null) {
					throw new ArgumentNullException("sampleItem");
				}

				var alternative = sampleItem.Alternative;

				if (!alternative.Available) {
					return;
				}

				var destinationParcel = ChoiceModelFactory.Parcels[sampleItem.ParcelId];
//				var destinationZoneTotals = ChoiceModelRunner.ZoneTotals[destinationParcel.ZoneId];

				alternative.Choice = destinationParcel;

				var nestedAlternative = (Global.ChoiceModelDictionary.Get("SchoolTourModeModel") as SchoolTourModeModel).RunNested(_person, _person.Household.ResidenceParcel, destinationParcel, _destinationArrivalTime, _destinationDepartureTime, _person.Household.HouseholdTotals.DrivingAgeMembers);
				var schoolTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				var votSegment = _person.Household.VotALSegment;
				var taSegment = destinationParcel.TransitAccessSegment();
				var aggregateLogsum = Global.AggregateLogsums[destinationParcel.ZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT][votSegment][taSegment];

				var distanceFromOrigin = _person.Household.ResidenceParcel.DistanceFromOrigin(destinationParcel, 1);
				var distance1 = Math.Min(distanceFromOrigin, .1);
				var distance2 = Math.Max(0, Math.Min(distanceFromOrigin - .1, .5 - .1));
				var distance3 = Math.Max(0, distanceFromOrigin - .5);
				var distanceLog = Math.Log(1 + distanceFromOrigin);
				var distanceFromWork = _person.IsFullOrPartTimeWorker ? _person.UsualWorkParcel.DistanceFromWorkLog(destinationParcel, 1) : 0;
//				var millionsSquareFeet = destinationZoneTotals.MillionsSquareFeet();

				// zone densities
//				var eduDensity = destinationZoneTotals.GetEmploymentEducationDensity(millionsSquareFeet);
//				var govDensity = destinationZoneTotals.GetEmploymentGovernmentDensity(millionsSquareFeet);
//				var offDensity = destinationZoneTotals.GetEmploymentOfficeDensity(millionsSquareFeet);
//				var serDensity = destinationZoneTotals.GetEmploymentServiceDensity(millionsSquareFeet);
//				var houDensity = destinationZoneTotals.GetHouseholdsDensity(millionsSquareFeet);

				// parcel buffers
				var educationBuffer1 = Math.Log(destinationParcel.EmploymentEducationBuffer1 + 1);
				//var governmentBuffer1 = Math.Log(destinationParcel.EmploymentGovernmentBuffer1 + 1);
				//var officeBuffer1 = Math.Log(destinationParcel.EmploymentOfficeBuffer1 + 1);
				//var serviceBuffer1 = Math.Log(destinationParcel.EmploymentServiceBuffer1 + 1);
				//var householdsBuffer1 = Math.Log(destinationParcel.HouseholdsBuffer1 + 1);
				//var retailBuffer1 = Math.Log(destinationParcel.EmploymentRetailBuffer1 + 1);
				//var industrialAgricultureConstructionBuffer1 = Math.Log(destinationParcel.EmploymentIndustrialBuffer1 + destinationParcel.EmploymentAgricultureConstructionBuffer1 + 1);
				//var foodBuffer1 = Math.Log(destinationParcel.EmploymentFoodBuffer1 + 1);
				//var medicalBuffer1 = Math.Log(destinationParcel.EmploymentMedicalBuffer1 + 1);
				//var employmentTotalBuffer1 = Math.Log(destinationParcel.EmploymentTotalBuffer1 + 1);
				var studentsUniversityBuffer1 = Math.Log(destinationParcel.StudentsUniversityBuffer1 + 1);
				var studentsK8Buffer1 = Math.Log(destinationParcel.StudentsK8Buffer1 + 1);
				var studentsHighSchoolBuffer1 = Math.Log(destinationParcel.StudentsHighSchoolBuffer1 + 1);

				//var educationBuffer2 = Math.Log(destinationParcel.EmploymentEducationBuffer2 + 1);
				//var governmentBuffer2 = Math.Log(destinationParcel.EmploymentGovernmentBuffer2 + 1);
				//var officeBuffer2 = Math.Log(destinationParcel.EmploymentOfficeBuffer2 + 1);
				//var serviceBuffer2 = Math.Log(destinationParcel.EmploymentServiceBuffer2 + 1);
				var householdsBuffer2 = Math.Log(destinationParcel.HouseholdsBuffer2 + 1);
				//var retailBuffer2 = Math.Log(destinationParcel.EmploymentRetailBuffer2 + 1);
				//var industrialAgricultureConstructionBuffer2 = Math.Log(destinationParcel.EmploymentIndustrialBuffer2 + destinationParcel.EmploymentAgricultureConstructionBuffer2 + 1);
				//var foodBuffer2 = Math.Log(destinationParcel.EmploymentFoodBuffer2 + 1);
				//var medicalBuffer2 = Math.Log(destinationParcel.EmploymentMedicalBuffer2 + 1);
				var employmentTotalBuffer2 = Math.Log(destinationParcel.EmploymentTotalBuffer2 + 1);
				var studentsUniversityBuffer2 = Math.Log(destinationParcel.StudentsUniversityBuffer2 + 1);
				//var studentsK8Buffer2 = Math.Log(destinationParcel.StudentsK8Buffer2 + 1);
				//var studentsHighSchoolBuffer2 = Math.Log(destinationParcel.StudentsHighSchoolBuffer2 + 1);

//				var educationBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentEducationBuffer1 - destinationParcel.EmploymentEducation)  + 1);
//				var governmentBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentGovernmentBuffer1 - destinationParcel.EmploymentGovernment)  + 1);
//				var officeBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentOfficeBuffer1 - destinationParcel.EmploymentOffice)  + 1);
//				var serviceBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentServiceBuffer1 - destinationParcel.EmploymentService)  + 1);
//				var householdsBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.HouseholdsBuffer1 - destinationParcel.Households)  + 1);
//				var retailBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentRetailBuffer1 - destinationParcel.EmploymentRetail)  + 1);
//				var industrialAgricultureConstructionBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentIndustrialBuffer1 + destinationParcel.EmploymentAgricultureConstructionBuffer1
//					- destinationParcel.EmploymentIndustrial - destinationParcel.EmploymentAgricultureConstruction)	+ 1);
//				var foodBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentFoodBuffer1 - destinationParcel.EmploymentFood)  + 1);
//				var medicalBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentMedicalBuffer1 - destinationParcel.EmploymentMedical)  + 1);
//				var employmentTotalBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentTotalBuffer1 - destinationParcel.EmploymentTotal)  + 1);
//				var studentsUniversityBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.StudentsUniversityBuffer1 - destinationParcel.StudentsUniversity)  + 1);
//				var studentsK8Buffer1 = Math.Log(Math.Max(0.0, destinationParcel.StudentsK8Buffer1 - destinationParcel.StudentsK8)  + 1);
//				var studentsHighSchoolBuffer1 = Math.Log(Math.Max(0.0, destinationParcel.StudentsHighSchoolBuffer1 - destinationParcel.StudentsHighSchool)  + 1);
//
//				var educationBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentEducationBuffer2 - destinationParcel.EmploymentEducation)  + 1);
//				var governmentBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentGovernmentBuffer2 - destinationParcel.EmploymentGovernment)  + 1);
//				var officeBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentOfficeBuffer2 - destinationParcel.EmploymentOffice)  + 1);
//				var serviceBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentServiceBuffer2 - destinationParcel.EmploymentService)  + 1);
//				var householdsBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.HouseholdsBuffer2 - destinationParcel.Households)  + 1);
//				var retailBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentRetailBuffer2 - destinationParcel.EmploymentRetail)  + 1);
//				var industrialAgricultureConstructionBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentIndustrialBuffer2 + destinationParcel.EmploymentAgricultureConstructionBuffer2
//					- destinationParcel.EmploymentIndustrial - destinationParcel.EmploymentAgricultureConstruction)	+ 1);
//				var foodBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentFoodBuffer2 - destinationParcel.EmploymentFood)  + 1);
//				var medicalBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentMedicalBuffer2 - destinationParcel.EmploymentMedical)  + 1);
//				var employmentTotalBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.EmploymentTotalBuffer2 - destinationParcel.EmploymentTotal)  + 1);
//				var studentsUniversityBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.StudentsUniversityBuffer2 - destinationParcel.StudentsUniversity)  + 1);
//				var studentsK8Buffer2 = Math.Log(Math.Max(0.0, destinationParcel.StudentsK8Buffer2 - destinationParcel.StudentsK8)  + 1);
//				var studentsHighSchoolBuffer2 = Math.Log(Math.Max(0.0, destinationParcel.StudentsHighSchoolBuffer2 - destinationParcel.StudentsHighSchool)  + 1);

				alternative.AddUtilityTerm(1, sampleItem.AdjustmentFactor);

				alternative.AddUtilityTerm(2, _person.IsChildUnder5.ToFlag() * schoolTourLogsum);
				alternative.AddUtilityTerm(3, _person.IsChildAge5Through15.ToFlag() * schoolTourLogsum);
				alternative.AddUtilityTerm(4, _person.IsDrivingAgeStudent.ToFlag() * schoolTourLogsum);
				alternative.AddUtilityTerm(5, _person.IsUniversityStudent.ToFlag() * schoolTourLogsum);
				alternative.AddUtilityTerm(6, (!_person.IsStudentAge).ToFlag() * schoolTourLogsum);

				alternative.AddUtilityTerm(7, _person.IsChildUnder5.ToFlag() * distance1);
				alternative.AddUtilityTerm(8, _person.IsChildUnder5.ToFlag() * distance2);
				alternative.AddUtilityTerm(9, _person.IsChildUnder5.ToFlag() * distance3);
				alternative.AddUtilityTerm(10, _person.IsChildAge5Through15.ToFlag() * distance1);
				alternative.AddUtilityTerm(11, _person.IsChildAge5Through15.ToFlag() * distance2);
				alternative.AddUtilityTerm(12, _person.IsChildAge5Through15.ToFlag() * distance3);
				alternative.AddUtilityTerm(13, _person.IsDrivingAgeStudent.ToFlag() * distanceLog);
				alternative.AddUtilityTerm(14, _person.IsUniversityStudent.ToFlag() * distanceLog);
				alternative.AddUtilityTerm(15, (!_person.IsStudentAge).ToFlag() * distanceLog);
				alternative.AddUtilityTerm(16, (!_person.IsStudentAge).ToFlag() * distanceFromWork);

				alternative.AddUtilityTerm(17, _person.IsChildUnder5.ToFlag() * aggregateLogsum);
				alternative.AddUtilityTerm(18, _person.IsChildAge5Through15.ToFlag() * aggregateLogsum);
				alternative.AddUtilityTerm(19, _person.IsDrivingAgeStudent.ToFlag() * aggregateLogsum);
				alternative.AddUtilityTerm(20, _person.IsUniversityStudent.ToFlag() * aggregateLogsum);
				alternative.AddUtilityTerm(21, (!_person.IsStudentAge).ToFlag() * aggregateLogsum);

				//Neighborhood
				alternative.AddUtilityTerm(30, _person.IsChildUnder5.ToFlag() * householdsBuffer2);
				alternative.AddUtilityTerm(31, _person.IsChildUnder5.ToFlag() * studentsHighSchoolBuffer1);
				alternative.AddUtilityTerm(32, _person.IsChildUnder5.ToFlag() * employmentTotalBuffer2);
				alternative.AddUtilityTerm(33, _person.IsChildAge5Through15.ToFlag() * studentsK8Buffer1);
				alternative.AddUtilityTerm(34, _person.IsDrivingAgeStudent.ToFlag() * studentsHighSchoolBuffer1);
				alternative.AddUtilityTerm(35, _person.IsUniversityStudent.ToFlag() * educationBuffer1);
				alternative.AddUtilityTerm(36, _person.IsAdult.ToFlag() * studentsUniversityBuffer1);
				alternative.AddUtilityTerm(37, _person.IsAdult.ToFlag() * studentsUniversityBuffer2);
				alternative.AddUtilityTerm(38, _person.IsAdult.ToFlag() * studentsK8Buffer1);

				//Size
				alternative.AddUtilityTerm(61, _person.IsChildUnder5.ToFlag() * destinationParcel.EmploymentEducation);
				alternative.AddUtilityTerm(62, _person.IsChildUnder5.ToFlag() * destinationParcel.EmploymentService);
				alternative.AddUtilityTerm(63, _person.IsChildUnder5.ToFlag() * destinationParcel.EmploymentOffice);
				alternative.AddUtilityTerm(64, _person.IsChildUnder5.ToFlag() * destinationParcel.EmploymentTotal);
				alternative.AddUtilityTerm(65, _person.IsChildUnder5.ToFlag() * 10.0 * destinationParcel.Households);
				alternative.AddUtilityTerm(66, _person.IsChildUnder5.ToFlag() * destinationParcel.StudentsK8);
				alternative.AddUtilityTerm(67, _person.IsChildAge5Through15.ToFlag() * destinationParcel.EmploymentEducation);
				alternative.AddUtilityTerm(68, _person.IsChildAge5Through15.ToFlag() * destinationParcel.EmploymentService);
				alternative.AddUtilityTerm(69, _person.IsChildAge5Through15.ToFlag() * destinationParcel.StudentsHighSchool);
				alternative.AddUtilityTerm(70, _person.IsChildAge5Through15.ToFlag() * destinationParcel.EmploymentTotal);
				alternative.AddUtilityTerm(71, _person.IsChildAge5Through15.ToFlag() * 10.0 * destinationParcel.Households);
				alternative.AddUtilityTerm(72, _person.IsChildAge5Through15.ToFlag() * destinationParcel.StudentsK8);
				alternative.AddUtilityTerm(73, _person.IsDrivingAgeStudent.ToFlag() * destinationParcel.EmploymentEducation);
				alternative.AddUtilityTerm(74, _person.IsDrivingAgeStudent.ToFlag() * destinationParcel.EmploymentService);
				alternative.AddUtilityTerm(75, _person.IsDrivingAgeStudent.ToFlag() * destinationParcel.EmploymentOffice);
				alternative.AddUtilityTerm(76, _person.IsDrivingAgeStudent.ToFlag() * destinationParcel.EmploymentTotal);
				alternative.AddUtilityTerm(77, _person.IsDrivingAgeStudent.ToFlag() * 10.0 * destinationParcel.Households);
				alternative.AddUtilityTerm(78, _person.IsDrivingAgeStudent.ToFlag() * destinationParcel.StudentsHighSchool);
				alternative.AddUtilityTerm(79, _person.IsAdult.ToFlag() * destinationParcel.EmploymentEducation);
				alternative.AddUtilityTerm(80, _person.IsAdult.ToFlag() * destinationParcel.EmploymentService);
				alternative.AddUtilityTerm(81, _person.IsAdult.ToFlag() * destinationParcel.EmploymentOffice);
				alternative.AddUtilityTerm(82, _person.IsAdult.ToFlag() * destinationParcel.EmploymentTotal);
				alternative.AddUtilityTerm(83, _person.IsAdult.ToFlag() * destinationParcel.StudentsUniversity);
				alternative.AddUtilityTerm(84, _person.IsAdult.ToFlag() * destinationParcel.StudentsHighSchool);

				// set shadow price depending on persontype and add it to utility
				// we are using the sampling adjustment factor assuming that it is 1

				alternative.AddUtilityTerm(1, _person.IsAdult ? destinationParcel.ShadowPriceForStudentsUniversity : destinationParcel.ShadowPriceForStudentsK12);

				//remove nesting for estimation of conditional MNL 
				alternative.AddNestedAlternative(_sampleSize + 2, 0, 99);
			}
		}
	}
}