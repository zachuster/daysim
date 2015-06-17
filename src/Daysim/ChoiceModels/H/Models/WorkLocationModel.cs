﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using Daysim.DomainModels;
using Daysim.DomainModels.Default;
using Daysim.DomainModels.Default.Models;
using Daysim.DomainModels.Default.Wrappers;
using Daysim.DomainModels.Extensions;
using Daysim.Framework.ChoiceModels;
using Daysim.Framework.Coefficients;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Models;
using Daysim.Framework.DomainModels.Wrappers;
using Daysim.Framework.Roster;
using Daysim.Framework.Sampling;
using Daysim.Sampling;
using Ninject;

namespace Daysim.ChoiceModels.H.Models {
	public class WorkLocationModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "HWorkLocationModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 2;
		private const int TOTAL_LEVELS = 2;
		private const int MAX_PARAMETER = 99;

		public override void RunInitialize(ICoefficientsReader reader = null)
		{
			int sampleSize = Global.Configuration.WorkLocationModelSampleSize;
			Initialize(CHOICE_MODEL_NAME, Global.Configuration.WorkLocationModelCoefficients, sampleSize + 1, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
		}
		
		public void Run(IPersonWrapper person, int sampleSize) {
			if (person == null) {
				throw new ArgumentNullException("person");
			}
			
			person.ResetRandom(0);

			if (Global.Configuration.IsInEstimationMode) {
				if (!_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(person.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (person.UsualWorkParcel == null) {
					return;
				}

				var choseHome = person.UsualWorkParcelId == person.Household.ResidenceParcelId; // JLB 20120329 added these two lines
				var chosenParcel = choseHome ? null : person.UsualWorkParcel;

				//RunModel(choiceProbabilityCalculator, person, sampleSize, person.UsualWorkParcel);
				RunModel(choiceProbabilityCalculator, person, sampleSize, chosenParcel, choseHome); // JLB 20120329 replaced above line
				// when chosenParcel is null:
				// DestinationSampler doesn't try to assign one of the sampled destinations as chosen
				// choseHome is NOT null, and RunModel sets the oddball location as chosen

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, person, sampleSize);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(person.Household.RandomUtility);
				var choice = (ParcelWrapper) chosenAlternative.Choice;

				person.UsualWorkParcelId = choice.Id;
				person.UsualWorkParcel = choice;
				person.UsualWorkZoneKey = ChoiceModelFactory.ZoneKeys[choice.ZoneId];

				var skimValue = ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Sov, Global.Settings.PathTypes.FullNetwork, Global.Settings.ValueOfTimes.DefaultVot, 1, person.Household.ResidenceParcel, choice);

				person.AutoTimeToUsualWork = skimValue.Variable;
				person.AutoDistanceToUsualWork = skimValue.BlendVariable;

				person.SetWorkParcelPredictions();
			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, IPersonWrapper person, int sampleSize, IParcelWrapper choice = null, bool choseHome = false) {
			var segment = Global.Kernel.Get<SamplingWeightsSettingsFactory>().SamplingWeightsSettings.GetTourDestinationSegment(Global.Settings.Purposes.Work, Global.Settings.TourPriorities.HomeBasedTour, Global.Settings.Modes.Sov, person.PersonType);
			var destinationSampler = new DestinationSampler(choiceProbabilityCalculator, segment, sampleSize, person.Household.ResidenceParcel, choice);
			var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Global.Settings.Models.WorkTourModeModel);
			var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Global.Settings.Models.WorkTourModeModel);
			var workLocationUtilites = new WorkLocationUtilities(person, sampleSize, destinationArrivalTime, destinationDepartureTime);

			destinationSampler.SampleTourDestinations(workLocationUtilites);

			//var alternative = choiceProbabilityCalculator.GetAlternative(countSampled, true);  

			// JLB 20120329 added third call parameter to idenitfy whether this alt is chosen or not
			var alternative = choiceProbabilityCalculator.GetAlternative(sampleSize, true, choseHome);

			alternative.Choice = person.Household.ResidenceParcel;

			alternative.AddUtilityTerm(41, 1);
			alternative.AddUtilityTerm(42, person.IsPartTimeWorker.ToFlag());
			alternative.AddUtilityTerm(43, person.IsStudentAge.ToFlag());
			alternative.AddUtilityTerm(44, person.IsFemale.ToFlag());
			alternative.AddUtilityTerm(90, 100);

			//make oddball alt unavailable and remove nesting for estimation of conditional MNL 
//			alternative.Available = false;
			alternative.AddNestedAlternative(sampleSize + 3, 1, 98);
		}

		private sealed class WorkLocationUtilities : ISamplingUtilities {
			private readonly IPersonWrapper _person;
			private readonly int _sampleSize;
			private readonly int _destinationArrivalTime;
			private readonly int _destinationDepartureTime;
			private readonly int[] _seedValues;

			public WorkLocationUtilities(IPersonWrapper person, int sampleSize, int destinationArrivalTime, int destinationDepartureTime) {
				_person = person;
				_sampleSize = sampleSize;
				_destinationArrivalTime = destinationArrivalTime;
				_destinationDepartureTime = destinationDepartureTime;
				_seedValues = ChoiceModelUtility.GetRandomSampling(_sampleSize, person.SeedValues[0]);
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

				var workTourLogsum = 0D;
				var nestedAlternative = Global.ChoiceModelSession.Get<WorkTourModeTimeModel>().RunNested(_person, _person.Household.ResidenceParcel, destinationParcel, _destinationArrivalTime, _destinationDepartureTime, _person.Household.HouseholdTotals.DrivingAgeMembers, 0.0);
				workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				
				var votSegment = _person.Household.GetVotALSegment();
				var taSegment = destinationParcel.TransitAccessSegment();
				var aggregateLogsum = Global.AggregateLogsums[destinationParcel.ZoneId][Global.Settings.Purposes.HomeBasedComposite][Global.Settings.CarOwnerships.OneOrMoreCarsPerAdult][votSegment][taSegment];

				var distanceFromOrigin = _person.Household.ResidenceParcel.DistanceFromOrigin(destinationParcel, 1);
				var distance1 = Math.Min(distanceFromOrigin, .35);
				var distance2 = Math.Max(0, Math.Min(distanceFromOrigin - .35, 1 - .35));
				var distance3 = Math.Max(0, distanceFromOrigin - 1);
				var distanceLog = Math.Log(1 + distanceFromOrigin);
				var distanceFromSchool = _person.IsFullOrPartTimeWorker ? 0 : _person.UsualSchoolParcel.DistanceFromSchoolLog(destinationParcel, 1);

				// parcel buffers
				var educationBuffer = Math.Log(destinationParcel.EmploymentEducationBuffer2 + 1);
				var governmentBuffer = Math.Log(destinationParcel.EmploymentGovernmentBuffer2 + 1);
				var officeBuffer = Math.Log(destinationParcel.EmploymentOfficeBuffer2 + 1);
				var serviceBuffer = Math.Log(destinationParcel.EmploymentServiceBuffer2 + 1);
				var householdsBuffer = Math.Log(destinationParcel.HouseholdsBuffer2 + 1);

//				var retailBuffer = Math.Log(destinationParcel.EmploymentRetailBuffer2 + 1);
				var industrialAgricultureConstructionBuffer = Math.Log(destinationParcel.EmploymentIndustrialBuffer2 + destinationParcel.EmploymentAgricultureConstructionBuffer2 + 1);
				var foodBuffer = Math.Log(destinationParcel.EmploymentFoodBuffer2 + 1);
				var medicalBuffer = Math.Log(destinationParcel.EmploymentMedicalBuffer2 + 1);
				var employmentTotalBuffer = Math.Log(destinationParcel.EmploymentTotalBuffer2 + 1);
				var studentsUniversityBuffer = Math.Log(destinationParcel.StudentsUniversityBuffer2 + 1);
				var studentsK12Buffer = Math.Log(destinationParcel.StudentsK8Buffer2 + destinationParcel.StudentsHighSchoolBuffer2 + 1);

//				var mixedUse4Index = destinationParcel.MixedUse4Index2();

				//size attributes (derived)
				var employmentIndustrialAgricultureConstruction = destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction;

				// parking attributes
				var parcelParkingDensity = destinationParcel.ParcelParkingPerTotalEmployment();

				// connectivity attributes
				var c34Ratio = destinationParcel.C34RatioBuffer1();

				alternative.AddUtilityTerm(1, sampleItem.AdjustmentFactor);
				alternative.AddUtilityTerm(2, _person.IsFulltimeWorker.ToFlag() * workTourLogsum);
				alternative.AddUtilityTerm(3, _person.IsPartTimeWorker.ToFlag() * workTourLogsum);
				alternative.AddUtilityTerm(4, _person.IsNotFullOrPartTimeWorker.ToFlag() * workTourLogsum);
				alternative.AddUtilityTerm(5, distanceLog); // for distance calibration
				alternative.AddUtilityTerm(6, _person.IsFulltimeWorker.ToFlag() * distance1);
				alternative.AddUtilityTerm(7, _person.IsFulltimeWorker.ToFlag() * distance2);
				alternative.AddUtilityTerm(8, _person.IsFulltimeWorker.ToFlag() * distance3);
				alternative.AddUtilityTerm(9, _person.IsPartTimeWorker.ToFlag() * distanceLog);
				alternative.AddUtilityTerm(10, _person.IsNotFullOrPartTimeWorker.ToFlag() * distanceLog);
				alternative.AddUtilityTerm(11, _person.Household.Has0To15KIncome.ToFlag() * distanceLog);
				alternative.AddUtilityTerm(12, _person.Household.Has50To75KIncome.ToFlag() * distanceLog);
				alternative.AddUtilityTerm(13, _person.Household.Has75To100KIncome.ToFlag() * distanceLog);
				alternative.AddUtilityTerm(14, _person.IsFemale.ToFlag() * distanceLog);
				alternative.AddUtilityTerm(15, _person.IsStudentAge.ToFlag() * distanceFromSchool);
				alternative.AddUtilityTerm(16, _person.IsFulltimeWorker.ToFlag() * aggregateLogsum);
				alternative.AddUtilityTerm(17, _person.IsPartTimeWorker.ToFlag() * aggregateLogsum);
				alternative.AddUtilityTerm(18, _person.IsNotFullOrPartTimeWorker.ToFlag() * aggregateLogsum);
				alternative.AddUtilityTerm(19, parcelParkingDensity);
				alternative.AddUtilityTerm(20, c34Ratio);

				//Neighborhood
				alternative.AddUtilityTerm(21, _person.Household.HasValidIncome.ToFlag() * serviceBuffer);
				alternative.AddUtilityTerm(22, _person.Household.HasValidIncome.ToFlag() * educationBuffer);
				alternative.AddUtilityTerm(23, _person.Household.HasValidIncome.ToFlag() * foodBuffer);
				alternative.AddUtilityTerm(24, _person.Household.HasValidIncome.ToFlag() * governmentBuffer);
				alternative.AddUtilityTerm(25, _person.Household.HasValidIncome.ToFlag() * officeBuffer);
				alternative.AddUtilityTerm(26, _person.Household.HasValidIncome.ToFlag() * medicalBuffer);
				alternative.AddUtilityTerm(27, _person.Household.HasValidIncome.ToFlag() * householdsBuffer);
				alternative.AddUtilityTerm(28, _person.Household.HasValidIncome.ToFlag() * studentsUniversityBuffer);

				alternative.AddUtilityTerm(29, _person.Household.HasValidIncome.ToFlag() * _person.IsFulltimeWorker.ToFlag() * studentsK12Buffer);
				alternative.AddUtilityTerm(30, _person.Household.HasValidIncome.ToFlag() * _person.IsFulltimeWorker.ToFlag() * studentsUniversityBuffer);
				alternative.AddUtilityTerm(31, _person.Household.HasValidIncome.ToFlag() * _person.IsPartTimeWorker.ToFlag() * industrialAgricultureConstructionBuffer);
				alternative.AddUtilityTerm(32, _person.Household.HasValidIncome.ToFlag() * _person.IsNotFullOrPartTimeWorker.ToFlag() * foodBuffer);
				alternative.AddUtilityTerm(33, _person.Household.HasValidIncome.ToFlag() * _person.IsNotFullOrPartTimeWorker.ToFlag() * medicalBuffer);

				alternative.AddUtilityTerm(34, _person.IsFulltimeWorker.ToFlag() * _person.Household.Has75KPlusIncome.ToFlag() * employmentTotalBuffer);
				alternative.AddUtilityTerm(35, _person.IsNotFullOrPartTimeWorker.ToFlag() * _person.Household.HasIncomeUnder50K.ToFlag() * governmentBuffer);
				alternative.AddUtilityTerm(36, _person.IsNotFullOrPartTimeWorker.ToFlag() * _person.Household.HasIncomeUnder50K.ToFlag() * employmentTotalBuffer);

                //PSRC specific district constants for model calibration
                if (Global.Configuration.PSRC == true)
                {
                    var homedist = _person.Household.ResidenceParcel.District;
                    var zonedist = destinationParcel.District;
                    var homeSKitWorkTRP = homedist == 11 && (zonedist == 8 || zonedist == 10 || zonedist == 7) ? 1 : 0;
                    var homeKitWorkTRP = homedist == 9 && (zonedist == 8 || zonedist == 10 || zonedist == 7) ? 1 : 0;
                    var homeEastWorkCBD = homedist == 6 && zonedist == 4 ? 1 : 0;
                    var homeKitWorkCBD = homedist == 9 && (zonedist == 4) ? 1 : 0;
                    var homeTacWorkKit = homedist == 8 && (zonedist == 9 || zonedist== 11) ? 1 : 0;
                    var homeEvWorkEv = homedist == 2 && zonedist == 2 ? 1 : 0;
                    var homeWSWorkEast = homedist == 5 && zonedist == 6 ? 1 : 0;

                    alternative.AddUtilityTerm(37, homeTacWorkKit);
                    alternative.AddUtilityTerm(38, homeEvWorkEv);
                    alternative.AddUtilityTerm(39, homeWSWorkEast);
                    alternative.AddUtilityTerm(40, homeSKitWorkTRP);
                    alternative.AddUtilityTerm(45, homeKitWorkTRP);
                    alternative.AddUtilityTerm(47, homeKitWorkTRP);
                    alternative.AddUtilityTerm(48, homeEastWorkCBD);
                    alternative.AddUtilityTerm(49, homeKitWorkCBD);
                }
				//Size
				alternative.AddUtilityTerm(51, _person.Household.HasValidIncome.ToFlag() * destinationParcel.EmploymentService);
				alternative.AddUtilityTerm(52, _person.Household.HasValidIncome.ToFlag() * destinationParcel.EmploymentEducation);
				alternative.AddUtilityTerm(53, _person.Household.HasValidIncome.ToFlag() * destinationParcel.EmploymentFood);
				alternative.AddUtilityTerm(54, _person.Household.HasValidIncome.ToFlag() * destinationParcel.EmploymentGovernment);
				alternative.AddUtilityTerm(55, _person.Household.HasValidIncome.ToFlag() * destinationParcel.EmploymentOffice);
				alternative.AddUtilityTerm(56, _person.Household.HasValidIncome.ToFlag() * destinationParcel.EmploymentRetail);
				alternative.AddUtilityTerm(57, _person.Household.HasValidIncome.ToFlag() * destinationParcel.EmploymentMedical);
				alternative.AddUtilityTerm(58, _person.Household.HasValidIncome.ToFlag() * employmentIndustrialAgricultureConstruction);
				alternative.AddUtilityTerm(59, _person.Household.HasValidIncome.ToFlag() * destinationParcel.StudentsUniversity);

				alternative.AddUtilityTerm(60, _person.Household.HasValidIncome.ToFlag() * _person.IsFulltimeWorker.ToFlag() * destinationParcel.EmploymentGovernment);
				alternative.AddUtilityTerm(61, _person.Household.HasValidIncome.ToFlag() * _person.IsFulltimeWorker.ToFlag() * employmentIndustrialAgricultureConstruction);
				alternative.AddUtilityTerm(62, _person.Household.HasValidIncome.ToFlag() * _person.IsPartTimeWorker.ToFlag() * employmentIndustrialAgricultureConstruction);
				alternative.AddUtilityTerm(63, _person.Household.HasValidIncome.ToFlag() * _person.IsNotFullOrPartTimeWorker.ToFlag() * destinationParcel.EmploymentEducation);
				alternative.AddUtilityTerm(64, _person.Household.HasValidIncome.ToFlag() * _person.IsNotFullOrPartTimeWorker.ToFlag() * destinationParcel.EmploymentFood);
				alternative.AddUtilityTerm(65, _person.Household.HasValidIncome.ToFlag() * _person.IsNotFullOrPartTimeWorker.ToFlag() * destinationParcel.EmploymentRetail);

				alternative.AddUtilityTerm(66, _person.Household.HasIncomeUnder50K.ToFlag() * destinationParcel.EmploymentRetail);
				alternative.AddUtilityTerm(67, _person.Household.HasIncomeUnder50K.ToFlag() * destinationParcel.EmploymentService);
				alternative.AddUtilityTerm(68, _person.Household.Has50To75KIncome.ToFlag() * destinationParcel.EmploymentMedical);
				alternative.AddUtilityTerm(69, _person.Household.Has50To75KIncome.ToFlag() * destinationParcel.EmploymentOffice);
				alternative.AddUtilityTerm(70, _person.Household.Has75KPlusIncome.ToFlag() * destinationParcel.EmploymentEducation);
				alternative.AddUtilityTerm(71, _person.Household.Has75KPlusIncome.ToFlag() * destinationParcel.EmploymentGovernment);
				alternative.AddUtilityTerm(72, _person.Household.Has75KPlusIncome.ToFlag() * destinationParcel.EmploymentMedical);
				alternative.AddUtilityTerm(73, _person.Household.Has75KPlusIncome.ToFlag() * destinationParcel.EmploymentOffice);

				alternative.AddUtilityTerm(74, _person.IsFulltimeWorker.ToFlag() * _person.Household.Has75KPlusIncome.ToFlag() * destinationParcel.EmploymentGovernment);
				alternative.AddUtilityTerm(75, _person.IsFulltimeWorker.ToFlag() * (!_person.Household.Has75KPlusIncome).ToFlag() * employmentIndustrialAgricultureConstruction);
				alternative.AddUtilityTerm(76, _person.IsPartTimeWorker.ToFlag() * (!_person.Household.HasIncomeUnder50K).ToFlag() * destinationParcel.EmploymentMedical);
				alternative.AddUtilityTerm(77, (!_person.IsFulltimeWorker).ToFlag() * _person.Household.Has75KPlusIncome.ToFlag() * destinationParcel.EmploymentOffice);
				alternative.AddUtilityTerm(78, _person.IsNotFullOrPartTimeWorker.ToFlag() * (!_person.Household.HasIncomeUnder50K).ToFlag() * destinationParcel.EmploymentRetail);

				alternative.AddUtilityTerm(79, _person.Household.HasMissingIncome.ToFlag() * destinationParcel.EmploymentTotal);
				alternative.AddUtilityTerm(80, _person.Household.HasMissingIncome.ToFlag() * destinationParcel.StudentsUniversity);

				// set shadow price depending on persontype and add it to utility
				// we are using the sampling adjustment factor assuming that it is 1
				alternative.AddUtilityTerm(1, destinationParcel.ShadowPriceForEmployment);

				//remove nesting for estimation of conditional MNL 
				alternative.AddNestedAlternative(_sampleSize + 2, 0, 98);
			}
		}
	}
}