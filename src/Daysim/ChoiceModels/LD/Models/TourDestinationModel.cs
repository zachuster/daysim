﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.Linq;
using Daysim.DomainModels;
using Daysim.DomainModels.LD;
using Daysim.DomainModels.LD.Wrappers;
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
using HouseholdDayWrapper = Daysim.DomainModels.LD.Wrappers.HouseholdDayWrapper;
using HouseholdWrapper = Daysim.DomainModels.LD.Wrappers.HouseholdWrapper;
using PersonDayWrapper = Daysim.DomainModels.LD.Wrappers.PersonDayWrapper;
using PersonWrapper = Daysim.DomainModels.LD.Wrappers.PersonWrapper;
using TourWrapper = Daysim.DomainModels.LD.Wrappers.TourWrapper;

namespace Daysim.ChoiceModels.LD.Models {
	public class TourDestinationModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "LDTourDestinationModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 300;

		private static int timesStartedRunModel;

		public override void RunInitialize(ICoefficientsReader reader = null) {
			int sampleSize = Global.Configuration.TourDestinationModelSampleSize;
			Initialize(CHOICE_MODEL_NAME, Global.Configuration.TourDestinationModelCoefficients, sampleSize, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
		}

		public void Run(TourWrapper tour, HouseholdDayWrapper householdDay, int sampleSize, IParcelWrapper constrainedParcel = null) {

			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			tour.PersonDay.ResetRandom(20 + tour.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
				if (!TourDestinationUtilities.ShouldRunInEstimationModeForModel(tour)) {
					return;
				}
				// JLB 20140421 add the following to keep from estimatign twice for the same tour
				if (tour.DestinationModeAndTimeHaveBeenSimulated) {
					return;
				}
			}

			if (tour.DestinationPurpose == Global.Settings.Purposes.School) {
				// the following lines were redundant.  Usual destination properties are set in GetMandatoryTourSimulatedData() 
				// sets the destination for the school tour
				//tour.DestinationParcelId = tour.Person.UsualSchoolParcelId;
				//tour.DestinationParcel = tour.Person.UsualSchoolParcel;
				//tour.DestinationZoneKey = tour.Person.UsualSchoolZoneKey;
				//tour.DestinationAddressType = Global.Settings.AddressTypes.UsualSchool;
				return;
			}
			else if (tour.DestinationPurpose == Global.Settings.Purposes.Work) {
				return;
			}
			else if (constrainedParcel != null) {
				tour.DestinationParcel = constrainedParcel;
				tour.DestinationParcelId = constrainedParcel.Id;
				tour.DestinationZoneKey = constrainedParcel.ZoneId;
				return;
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(tour.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (constrainedParcel == null) {
					RunModel(choiceProbabilityCalculator, tour, householdDay, sampleSize, tour.DestinationParcel);

					choiceProbabilityCalculator.WriteObservation();
				}
			}
			else {
				RunModel(choiceProbabilityCalculator, tour, householdDay, sampleSize);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(tour.Household.RandomUtility);

				if (chosenAlternative == null) {
					Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", tour.PersonDay.Id);
					if (!Global.Configuration.IsInEstimationMode) {
						tour.PersonDay.IsValid = false;
						tour.PersonDay.HouseholdDay.IsValid = false;
					}

					return;
				}

				var choice = (IParcelWrapper) chosenAlternative.Choice;

				tour.DestinationParcelId = choice.Id;
				tour.DestinationParcel = choice;
				tour.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[choice.ZoneId];
				tour.DestinationAddressType = Global.Settings.AddressTypes.Other;

			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, TourWrapper tour, HouseholdDayWrapper householdDay, int sampleSize, IParcelWrapper choice = null) {
			timesStartedRunModel++;
			HouseholdWrapper household = (HouseholdWrapper) tour.Household;
			PersonWrapper person = (PersonWrapper) tour.Person;
			PersonDayWrapper personDay = (PersonDayWrapper) tour.PersonDay;

			//			var totalAvailableMinutes =
			//				tour.ParentTour == null
			//					? personDay.TimeWindow.TotalAvailableMinutes(1, Global.Settings.Times.MinutesInADay)
			//					: tour.ParentTour.TimeWindow.TotalAvailableMinutes(1, Global.Settings.Times.MinutesInADay);


			TimeWindow timeWindow = new TimeWindow();
			if (tour.JointTourSequence > 0) {
				foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
					TourWrapper tInJoint = (TourWrapper) pDay.Tours.Find(t => t.JointTourSequence == tour.JointTourSequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
					}
				}
			}
			else if (tour.ParentTour == null) {
				timeWindow.IncorporateAnotherTimeWindow(personDay.TimeWindow);
			}

			timeWindow.SetBusyMinutes(Global.Settings.Times.EndOfRelevantWindow, Global.Settings.Times.MinutesInADay + 1);

			var maxAvailableMinutes =
				 (tour.JointTourSequence > 0 || tour.ParentTour == null)
				 ? timeWindow.MaxAvailableMinutesAfter(Global.Settings.Times.FiveAM)
					  : tour.ParentTour.DestinationDepartureTime - tour.ParentTour.DestinationArrivalTime;


			//			var hoursAvailableInverse =
			//				tour.IsHomeBasedTour
			//					? (personDay.HomeBasedTours - personDay.SimulatedHomeBasedTours + 1) / (Math.Max(totalAvailableMinutes - 360, 30) / 60D)
			//					: 1 / (Math.Max(totalAvailableMinutes, 1) / 60D);

			var householdHasChildren = household.HasChildren;
			var householdHasNoChildren = householdHasChildren ? false : true;

			var fastestAvailableTimeOfDay =
				 tour.IsHomeBasedTour || tour.ParentTour == null
					  ? 1
					  : tour.ParentTour.DestinationArrivalTime + (tour.ParentTour.DestinationDepartureTime - tour.ParentTour.DestinationArrivalTime) / 2;

			var tourCategory = tour.GetTourCategory();
			//			var primaryFlag = ChoiceModelUtility.GetPrimaryFlag(tourCategory);
			var secondaryFlag = ChoiceModelUtility.GetSecondaryFlag(tourCategory);
			var workOrSchoolPatternFlag = personDay.GetIsWorkOrSchoolPattern().ToFlag();
			var otherPatternFlag = personDay.GetIsOtherPattern().ToFlag();
			int jointTourFlag = (tour.JointTourSequence > 0).ToFlag();



			ChoiceModelUtility.DrawRandomTourTimePeriods(tour, tourCategory);

			if (tour.Household.Id == 80049 && tour.PersonDay.Day == 1 && tour.Person.Sequence == 2 && tour.Sequence == 4) {
				bool testbreak = true;
			}

			var segment = Global.Kernel.Get<SamplingWeightsSettingsFactory>().SamplingWeightsSettings.GetTourDestinationSegment(tour.DestinationPurpose, tour.IsHomeBasedTour ? Global.Settings.TourPriorities.HomeBasedTour : Global.Settings.TourPriorities.WorkBasedTour, Global.Settings.Modes.Sov, person.PersonType);


			var destinationSampler = new DestinationSampler(choiceProbabilityCalculator, segment, sampleSize, tour.OriginParcel, choice);
			var tourDestinationUtilities = new TourDestinationUtilities(tour, sampleSize, secondaryFlag, personDay.GetIsWorkOrSchoolPattern().ToFlag(), personDay.GetIsOtherPattern().ToFlag(), fastestAvailableTimeOfDay, maxAvailableMinutes);

			// get destination sample and perform code that used to be in SetUtilities below
			var sampleItems = destinationSampler.SampleAndReturnTourDestinations(tourDestinationUtilities);

			int index = 0;
			foreach (var sampleItem in sampleItems) {
				bool available = sampleItem.Key.Available;
				bool isChosen = sampleItem.Key.IsChosen;
				double adjustmentFactor = sampleItem.Key.AdjustmentFactor;
				var destinationParcel = ChoiceModelFactory.Parcels[sampleItem.Key.ParcelId];

				if (isChosen) Global.PrintFile.WriteLine("Sequence {0}: Chosen parcel {1} Available {2} Sample item {3} of {4}",timesStartedRunModel,destinationParcel.Id,available,index,sampleItems.Count); 

				var alternative = choiceProbabilityCalculator.GetAlternative(index++, available, isChosen);


				if (!available) {
					continue;
				}

				var fastestTravelTime =
				  ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Hov3, Global.Settings.PathTypes.FullNetwork, Global.Settings.ValueOfTimes.DefaultVot, fastestAvailableTimeOfDay, tour.OriginParcel, destinationParcel).Variable +
				  ImpedanceRoster.GetValue("ivtime", Global.Settings.Modes.Hov3, Global.Settings.PathTypes.FullNetwork, Global.Settings.ValueOfTimes.DefaultVot, fastestAvailableTimeOfDay, destinationParcel, tour.OriginParcel).Variable;

				if (fastestTravelTime >= maxAvailableMinutes) {
					alternative.Available = false;

					continue;
				}

				alternative.Choice = destinationParcel;

				double tourLogsum;

				if (tour.IsHomeBasedTour) {
					if (tour.DestinationPurpose == Global.Settings.Purposes.Work) {
						//JLB 201406
						//var nestedAlternative = Global.ChoiceModelSession.Get<WorkTourModeModel>().RunNested(tour, destinationParcel);
						var nestedAlternative = Global.ChoiceModelSession.Get<WorkTourModeTimeModel>().RunNested(tour, destinationParcel, tour.Household.VehiclesAvailable, tour.Person.GetTransitFareDiscountFraction());
						tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
					}

					// JLB201406
					//else if (tour.DestinationPurpose == Global.Settings.Purposes.Escort) {
					//	var nestedAlternative = Global.ChoiceModelSession.Get<EscortTourModeModel>().RunNested(tour, destinationParcel);
					//	tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
					//}
					else {
						// JLB201406
						//var nestedAlternative = Global.ChoiceModelSession.Get<OtherHomeBasedTourModeModel>().RunNested(tour, destinationParcel);
						var nestedAlternative = Global.ChoiceModelSession.Get<OtherHomeBasedTourModeTimeModel>().RunNested(tour, destinationParcel, tour.Household.VehiclesAvailable, tour.Person.GetTransitFareDiscountFraction());
						tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
					}
				}
				else {
					// JLB201406
					//var nestedAlternative = Global.ChoiceModelSession.Get<WorkBasedSubtourModeModel>().RunNested(tour, destinationParcel);
					var nestedAlternative = Global.ChoiceModelSession.Get<WorkBasedSubtourModeTimeModel>().RunNested(tour, destinationParcel, tour.Household.VehiclesAvailable, tour.Person.GetTransitFareDiscountFraction());
					tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}

				//var purpose = tour.TourPurposeSegment;
				var carOwnership = person.GetCarOwnershipSegment();
				var votSegment = tour.GetVotALSegment();
				var transitAccess = destinationParcel.TransitAccessSegment();
				//var aggregateLogsum = Global.AggregateLogsums[destinationParcel.ZoneId][purpose][carOwnership][votSegment][transitAccess];
				var aggregateLogsumHomeBased = Global.AggregateLogsums[destinationParcel.ZoneId][Global.Settings.Purposes.HomeBasedComposite][carOwnership][votSegment][transitAccess];
				var aggregateLogsumWorkBased = Global.AggregateLogsums[destinationParcel.ZoneId][Global.Settings.Purposes.WorkBased][carOwnership][votSegment][transitAccess];

				var distanceFromOrigin = tour.OriginParcel.DistanceFromOrigin(destinationParcel, tour.DestinationArrivalTime);


				// 1. new from GV: Cph KM-distances
				var piecewiseDistanceFrom0To1Km = Math.Min(distanceFromOrigin, .10);

				var piecewiseDistanceFrom0To2Km = Math.Min(distanceFromOrigin, .20); //GV: added July 7th
				var piecewiseDistanceFrom0To5Km = Math.Min(distanceFromOrigin, .50); //GV: added July 7th

				var piecewiseDistanceFrom1To2Km = Math.Max(0, Math.Min(distanceFromOrigin - .1, .2 - .1));
				var piecewiseDistanceFrom2To5Km = Math.Max(0, Math.Min(distanceFromOrigin - .2, .5 - .2));
				var piecewiseDistanceFrom5To10Km = Math.Max(0, Math.Min(distanceFromOrigin - .5, 1 - .5));
				var piecewiseDistanceFrom10To20Km = Math.Max(0, Math.Min(distanceFromOrigin - 1, 2 - 1));
				var piecewiseDistanceFrom20KmToInfinity = Math.Max(0, distanceFromOrigin - 2);

				var piecewiseDistanceFrom10KmToInfinity = Math.Max(0, distanceFromOrigin - 1);
				// 1. finished

				var distanceFromOriginLog = Math.Log(1 + distanceFromOrigin);
				var distanceFromWorkLog = person.UsualWorkParcel.DistanceFromWorkLog(destinationParcel, 1);
				var distanceFromSchoolLog = person.UsualSchoolParcel.DistanceFromSchoolLog(destinationParcel, 1);

				var timePressure = Math.Log(1 - fastestTravelTime / maxAvailableMinutes);


				// 2. new from GV: Cph buffers for neighborhood effects
				// log transforms of buffers for Neighborhood effects
				var logOfOnePlusEducationK8Buffer2 = Math.Log(destinationParcel.StudentsK8Buffer2 + 1.0);
				var logOfOnePlusEducationUniStuBuffer2 = Math.Log(destinationParcel.StudentsUniversityBuffer2 + 1.0);
				var logOfOnePlusEmploymentEducationBuffer2 = Math.Log(destinationParcel.EmploymentEducationBuffer2 + 1.0);
				var logOfOnePlusEmploymentGovernmentBuffer2 = Math.Log(destinationParcel.EmploymentGovernmentBuffer2 + 1.0);
				var logOfOnePlusEmploymentIndustrialBuffer2 = Math.Log(destinationParcel.EmploymentIndustrialBuffer2 + 1.0);
				var logOfOnePlusEmploymentOfficeBuffer2 = Math.Log(destinationParcel.EmploymentOfficeBuffer2 + 1.0);
				var logOfOnePlusEmploymentRetailBuffer2 = Math.Log(destinationParcel.EmploymentRetailBuffer2 + 1.0);
				var logOfOnePlusEmploymentServiceBuffer2 = Math.Log(destinationParcel.EmploymentServiceBuffer2 + 1.0);
				var logOfOnePlusEmploymentAgrConstrBuffer2 = Math.Log(destinationParcel.EmploymentAgricultureConstructionBuffer2 + 1.0);
				var logOfOnePlusEmploymentJobsBuffer2 = Math.Log(destinationParcel.EmploymentTotalBuffer2 + 1.0);
				var logOfOnePlusHouseholdsBuffer2 = Math.Log(destinationParcel.HouseholdsBuffer2 + 1.0);
				// 2. finished 


				var logOfOnePlusParkingOffStreetDailySpacesBuffer1 = Math.Log(1 + destinationParcel.ParkingOffStreetPaidDailySpacesBuffer1);
				// connectivity attributes
				var c34Ratio = destinationParcel.C34RatioBuffer1();

				var carCompetitionFlag = FlagUtility.GetCarCompetitionFlag(carOwnership); // exludes no cars
				var noCarCompetitionFlag = FlagUtility.GetNoCarCompetitionFlag(carOwnership);
				var noCarsFlag = FlagUtility.GetNoCarsFlag(carOwnership);

				alternative.AddUtilityTerm(2, household.Id);
				alternative.AddUtilityTerm(3, personDay.Day);
				alternative.AddUtilityTerm(4, person.Sequence);
				alternative.AddUtilityTerm(5, tour.Sequence);

				alternative.AddUtilityTerm(8, adjustmentFactor);
				alternative.AddUtilityTerm(9, tourLogsum);

				// 3. new from GV: definition of Cph variables

				//alternative.AddUtilityTerm(260, secondaryFlag * workOrSchoolPatternFlag * piecewiseDistanceFrom0To1Km);
				alternative.AddUtilityTerm(260, secondaryFlag * workOrSchoolPatternFlag * piecewiseDistanceFrom0To2Km); //GV: added July 7th               
				//alternative.AddUtilityTerm(261, secondaryFlag * workOrSchoolPatternFlag * piecewiseDistanceFrom1To2Km);
				alternative.AddUtilityTerm(262, secondaryFlag * workOrSchoolPatternFlag * piecewiseDistanceFrom2To5Km);
				alternative.AddUtilityTerm(263, secondaryFlag * workOrSchoolPatternFlag * piecewiseDistanceFrom5To10Km);
				alternative.AddUtilityTerm(264, secondaryFlag * workOrSchoolPatternFlag * piecewiseDistanceFrom10To20Km);
				alternative.AddUtilityTerm(265, secondaryFlag * workOrSchoolPatternFlag * piecewiseDistanceFrom20KmToInfinity);

				//alternative.AddUtilityTerm(266, secondaryFlag * otherPatternFlag * piecewiseDistanceFrom0To1Km);
				alternative.AddUtilityTerm(266, secondaryFlag * otherPatternFlag * piecewiseDistanceFrom0To2Km); //GV: added July 7th               
				//alternative.AddUtilityTerm(267, secondaryFlag * otherPatternFlag * piecewiseDistanceFrom1To2Km);
				alternative.AddUtilityTerm(268, secondaryFlag * otherPatternFlag * piecewiseDistanceFrom2To5Km);
				alternative.AddUtilityTerm(269, secondaryFlag * otherPatternFlag * piecewiseDistanceFrom5To10Km);
				alternative.AddUtilityTerm(270, secondaryFlag * otherPatternFlag * piecewiseDistanceFrom10To20Km);
				alternative.AddUtilityTerm(271, secondaryFlag * otherPatternFlag * piecewiseDistanceFrom20KmToInfinity);

				//alternative.AddUtilityTerm(268, (!_tour.IsHomeBasedTour).ToFlag() * distanceFromOriginLog);
				//alternative.AddUtilityTerm(269, household.Has0To15KIncome.ToFlag() * distanceFromOriginLog);
				//alternative.AddUtilityTerm(270, household.HasMissingIncome.ToFlag() * distanceFromOriginLog);
				//alternative.AddUtilityTerm(271, person.IsRetiredAdult.ToFlag() * distanceFromOriginLog);
				//alternative.AddUtilityTerm(272, person.IsUniversityStudent.ToFlag() * distanceFromOriginLog);
				//alternative.AddUtilityTerm(273, person.IsChildAge5Through15.ToFlag() * distanceFromOriginLog);
				//alternative.AddUtilityTerm(274, person.IsChildUnder5.ToFlag() * distanceFromOriginLog);

				alternative.AddUtilityTerm(272, (!tour.IsHomeBasedTour).ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(273, (household.Income >= 300000 && household.Income < 600000).ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(274, (household.Income >= 600000 && household.Income < 900000).ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(275, (household.Income >= 900000).ToFlag() * distanceFromOriginLog);

				//alternative.AddUtilityTerm(276, person.IsChildUnder5.ToFlag() * distanceFromOriginLog); // commented out by GV, July 7th
				//alternative.AddUtilityTerm(277, person.IsChildAge5Through15.ToFlag() * distanceFromOriginLog); // commented out by GV, July 7th
				//alternative.AddUtilityTerm(278, person.IsChildUnder16.ToFlag() * distanceFromOriginLog); // commented out by GV, July 7th
				alternative.AddUtilityTerm(279, person.IsUniversityStudent.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(280, person.IsAdultMale.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(281, person.IsAdultFemale.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(282, person.IsRetiredAdult.ToFlag() * distanceFromOriginLog);

				//alternative.AddUtilityTerm(283, (tour.IsHomeBasedTour).ToFlag() * timePressure); //commented out by GV: 7th July 2013
				alternative.AddUtilityTerm(284, (tour.IsHomeBasedTour).ToFlag() * distanceFromSchoolLog);
				//alternative.AddUtilityTerm(14, distanceFromWorkLog);

				// GV commented out this - on TO DO list
				//alternative.AddUtilityTerm(277, (carCompetitionFlag + noCarCompetitionFlag) * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
				//alternative.AddUtilityTerm(278, noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
				//alternative.AddUtilityTerm(279, carCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());
				//alternative.AddUtilityTerm(280, noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());
				//alternative.AddUtilityTerm(281, noCarsFlag * c34Ratio);
				//alternative.AddUtilityTerm(282, noCarCompetitionFlag * c34Ratio);
				//alternative.AddUtilityTerm(283, (carCompetitionFlag + noCarCompetitionFlag) * logOfOnePlusParkingOffStreetDailySpacesBuffer1);


				//alternative.AddUtilityTerm(285, jointTourFlag * piecewiseDistanceFrom0To1Km);
				//alternative.AddUtilityTerm(286, jointTourFlag * piecewiseDistanceFrom1To2Km);
				alternative.AddUtilityTerm(286, jointTourFlag * piecewiseDistanceFrom0To2Km);
				alternative.AddUtilityTerm(287, jointTourFlag * piecewiseDistanceFrom2To5Km);
				alternative.AddUtilityTerm(288, jointTourFlag * piecewiseDistanceFrom5To10Km);
				alternative.AddUtilityTerm(289, jointTourFlag * piecewiseDistanceFrom10To20Km);
				alternative.AddUtilityTerm(290, jointTourFlag * piecewiseDistanceFrom20KmToInfinity);
				// 3. finished


				//4. new from GV: purpose utilities
				// COMPAS puposes are: Work, Education, Escort, Shopping, Leisure, Personal business, business
				// You need NO "Work" and "Education", their destinations are known in the synthetic population
				if (tour.DestinationPurpose == Global.Settings.Purposes.Business) {
					//alternative.AddUtilityTerm(10, piecewiseDistanceFrom0To1Km);
					//alternative.AddUtilityTerm(11, piecewiseDistanceFrom1To2Km);
					//alternative.AddUtilityTerm(12, piecewiseDistanceFrom2To5Km);
					alternative.AddUtilityTerm(12, piecewiseDistanceFrom0To5Km);
					alternative.AddUtilityTerm(13, piecewiseDistanceFrom5To10Km);
					alternative.AddUtilityTerm(14, piecewiseDistanceFrom10To20Km);
					alternative.AddUtilityTerm(15, piecewiseDistanceFrom20KmToInfinity);

					alternative.AddUtilityTerm(16, aggregateLogsumWorkBased);

					// Neighborhood
					//GV: commented out just temp.
					//alternative.AddUtilityTerm(20, logOfOnePlusEducationK8Buffer2);
					//alternative.AddUtilityTerm(21, logOfOnePlusEducationUniStuBuffer2);
					//alternative.AddUtilityTerm(22, logOfOnePlusEmploymentEducationBuffer2);
					alternative.AddUtilityTerm(23, logOfOnePlusEmploymentGovernmentBuffer2);
					//alternative.AddUtilityTerm(24, logOfOnePlusEmploymentIndustrialBuffer2);
					alternative.AddUtilityTerm(25, logOfOnePlusEmploymentOfficeBuffer2);
					alternative.AddUtilityTerm(26, logOfOnePlusEmploymentRetailBuffer2);
					alternative.AddUtilityTerm(27, logOfOnePlusEmploymentServiceBuffer2);
					alternative.AddUtilityTerm(28, logOfOnePlusEmploymentAgrConstrBuffer2);
					//alternative.AddUtilityTerm(29, logOfOnePlusEmploymentJobsBuffer2);

					// Size terms
					alternative.AddUtilityTerm(30, destinationParcel.EmploymentEducation);
					alternative.AddUtilityTerm(31, destinationParcel.EmploymentGovernment);
					alternative.AddUtilityTerm(32, destinationParcel.EmploymentIndustrial);
					alternative.AddUtilityTerm(33, destinationParcel.EmploymentOffice);
					alternative.AddUtilityTerm(34, destinationParcel.EmploymentRetail);
					// GV: 35 is fixed to zero
					alternative.AddUtilityTerm(35, destinationParcel.EmploymentService);
					alternative.AddUtilityTerm(36, destinationParcel.EmploymentAgricultureConstruction);
					alternative.AddUtilityTerm(37, destinationParcel.EmploymentTotal);
					alternative.AddUtilityTerm(38, destinationParcel.Households);
				}
				else if (tour.DestinationPurpose == Global.Settings.Purposes.Escort) {
					//alternative.AddUtilityTerm(50, piecewiseDistanceFrom0To1Km);
					alternative.AddUtilityTerm(51, piecewiseDistanceFrom0To2Km);
					//alternative.AddUtilityTerm(52, piecewiseDistanceFrom2To5Km);
					alternative.AddUtilityTerm(52, piecewiseDistanceFrom2To5Km);
					alternative.AddUtilityTerm(53, piecewiseDistanceFrom5To10Km);
					//alternative.AddUtilityTerm(54, piecewiseDistanceFrom10To20Km);
					alternative.AddUtilityTerm(55, piecewiseDistanceFrom10KmToInfinity);
					//alternative.AddUtilityTerm(55, piecewiseDistanceFrom20KmToInfinity);

					alternative.AddUtilityTerm(56, aggregateLogsumHomeBased);

					// Neighborhood
					//GV: commented out just temp.
					alternative.AddUtilityTerm(60, householdHasNoChildren.ToFlag() * logOfOnePlusEmploymentJobsBuffer2);
					//alternative.AddUtilityTerm(61, householdHasNoChildren.ToFlag() * logOfOnePlusHouseholdsBuffer2);
					//alternative.AddUtilityTerm(62, householdHasChildren.ToFlag() * logOfOnePlusHouseholdsBuffer2);
					//alternative.AddUtilityTerm(64, logOfOnePlusEmploymentJobsBuffer2);

					// Size terms
					// GV: no observations   
					alternative.AddUtilityTerm(70, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentEducation);
					alternative.AddUtilityTerm(71, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentGovernment);
					alternative.AddUtilityTerm(72, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentIndustrial);
					alternative.AddUtilityTerm(73, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentOffice);
					alternative.AddUtilityTerm(74, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentRetail);
					// GV: 75 is fixed to zero
					alternative.AddUtilityTerm(75, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentService);
					alternative.AddUtilityTerm(76, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentAgricultureConstruction);
					alternative.AddUtilityTerm(77, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentTotal);
					alternative.AddUtilityTerm(78, (!householdHasChildren).ToFlag() * destinationParcel.Households);

					alternative.AddUtilityTerm(80, (householdHasChildren).ToFlag() * destinationParcel.EmploymentEducation);
					alternative.AddUtilityTerm(81, (householdHasChildren).ToFlag() * destinationParcel.EmploymentGovernment);
					alternative.AddUtilityTerm(82, (householdHasChildren).ToFlag() * destinationParcel.EmploymentIndustrial);
					alternative.AddUtilityTerm(83, (householdHasChildren).ToFlag() * destinationParcel.EmploymentOffice);
					alternative.AddUtilityTerm(84, (householdHasChildren).ToFlag() * destinationParcel.EmploymentRetail);
					// GV 85 is fixed to zero at the moment
					alternative.AddUtilityTerm(85, (householdHasChildren).ToFlag() * destinationParcel.EmploymentService);
					alternative.AddUtilityTerm(86, (householdHasChildren).ToFlag() * destinationParcel.EmploymentAgricultureConstruction);
					alternative.AddUtilityTerm(87, (householdHasChildren).ToFlag() * destinationParcel.EmploymentTotal);
					alternative.AddUtilityTerm(88, (householdHasChildren).ToFlag() * destinationParcel.Households);
				}
				else if (tour.DestinationPurpose == Global.Settings.Purposes.PersonalBusiness) {
					alternative.AddUtilityTerm(90, piecewiseDistanceFrom0To2Km);
					//alternative.AddUtilityTerm(91, piecewiseDistanceFrom1To2Km);
					alternative.AddUtilityTerm(92, piecewiseDistanceFrom2To5Km);
					alternative.AddUtilityTerm(93, piecewiseDistanceFrom5To10Km);
					alternative.AddUtilityTerm(94, piecewiseDistanceFrom10To20Km);
					alternative.AddUtilityTerm(95, piecewiseDistanceFrom20KmToInfinity);

					alternative.AddUtilityTerm(96, aggregateLogsumHomeBased);

					// Neighborhood
					//GV: commented out just temp.
					//alternative.AddUtilityTerm(100, logOfOnePlusEmploymentEducationBuffer2);
					//alternative.AddUtilityTerm(101, logOfOnePlusEmploymentGovernmentBuffer2);
					//alternative.AddUtilityTerm(102, logOfOnePlusEmploymentIndustrialBuffer2);
					alternative.AddUtilityTerm(103, logOfOnePlusEmploymentOfficeBuffer2);
					alternative.AddUtilityTerm(104, logOfOnePlusEmploymentRetailBuffer2);
					alternative.AddUtilityTerm(105, logOfOnePlusEmploymentServiceBuffer2);
					//alternative.AddUtilityTerm(106, logOfOnePlusEmploymentAgrConstrBuffer2);
					//alternative.AddUtilityTerm(107, logOfOnePlusEmploymentJobsBuffer2);

					// Size terms
					alternative.AddUtilityTerm(110, destinationParcel.EmploymentEducation);
					alternative.AddUtilityTerm(111, destinationParcel.EmploymentGovernment);
					alternative.AddUtilityTerm(112, destinationParcel.EmploymentIndustrial);
					alternative.AddUtilityTerm(113, destinationParcel.EmploymentOffice);
					alternative.AddUtilityTerm(114, destinationParcel.EmploymentRetail);
					// GV 115 is fixed to zero
					alternative.AddUtilityTerm(115, destinationParcel.EmploymentService);
					alternative.AddUtilityTerm(116, destinationParcel.EmploymentAgricultureConstruction);
					alternative.AddUtilityTerm(117, destinationParcel.EmploymentTotal);
					alternative.AddUtilityTerm(118, destinationParcel.Households);
				}
				else if (tour.DestinationPurpose == Global.Settings.Purposes.Shopping) {
					//alternative.AddUtilityTerm(120, piecewiseDistanceFrom0To1Km);
					alternative.AddUtilityTerm(121, piecewiseDistanceFrom0To2Km);
					alternative.AddUtilityTerm(122, piecewiseDistanceFrom2To5Km);
					alternative.AddUtilityTerm(123, piecewiseDistanceFrom5To10Km);
					alternative.AddUtilityTerm(124, piecewiseDistanceFrom10To20Km);
					alternative.AddUtilityTerm(125, piecewiseDistanceFrom20KmToInfinity);

					alternative.AddUtilityTerm(126, aggregateLogsumHomeBased);

					// Neighborhood
					//GV: commented out just temp.
					//alternative.AddUtilityTerm(130, logOfOnePlusEmploymentEducationBuffer2);
					alternative.AddUtilityTerm(131, logOfOnePlusEmploymentRetailBuffer2);
					//alternative.AddUtilityTerm(132, logOfOnePlusEmploymentJobsBuffer2);

					// Size terms
					alternative.AddUtilityTerm(140, destinationParcel.EmploymentOffice);
					alternative.AddUtilityTerm(141, destinationParcel.EmploymentRetail);
					// GV 142 is fixed to zero
					alternative.AddUtilityTerm(142, destinationParcel.EmploymentService);
					alternative.AddUtilityTerm(143, destinationParcel.EmploymentTotal);
				}
				else if (tour.DestinationPurpose == Global.Settings.Purposes.Social) {
					//alternative.AddUtilityTerm(170, piecewiseDistanceFrom0To1Km);
					//alternative.AddUtilityTerm(171, piecewiseDistanceFrom1To2Km);
					alternative.AddUtilityTerm(170, piecewiseDistanceFrom0To2Km);
					alternative.AddUtilityTerm(172, piecewiseDistanceFrom2To5Km);
					alternative.AddUtilityTerm(173, piecewiseDistanceFrom5To10Km);
					alternative.AddUtilityTerm(174, piecewiseDistanceFrom10To20Km);
					alternative.AddUtilityTerm(175, piecewiseDistanceFrom20KmToInfinity);

					alternative.AddUtilityTerm(176, aggregateLogsumHomeBased);

					// Neighborhood
					//GV: commented out just temp.
					//alternative.AddUtilityTerm(180, logOfOnePlusEmploymentOfficeBuffer2);
					alternative.AddUtilityTerm(181, logOfOnePlusEmploymentRetailBuffer2);
					alternative.AddUtilityTerm(182, logOfOnePlusEmploymentServiceBuffer2);
					//alternative.AddUtilityTerm(183, logOfOnePlusEmploymentJobsBuffer2);

					// Size terms
					alternative.AddUtilityTerm(190, destinationParcel.EmploymentEducation);
					alternative.AddUtilityTerm(191, destinationParcel.EmploymentGovernment);
					alternative.AddUtilityTerm(192, destinationParcel.EmploymentIndustrial);
					alternative.AddUtilityTerm(193, destinationParcel.EmploymentOffice);
					alternative.AddUtilityTerm(194, destinationParcel.EmploymentRetail);
					// GV 195 is fixed to zero
					alternative.AddUtilityTerm(195, destinationParcel.EmploymentService);
					alternative.AddUtilityTerm(196, destinationParcel.EmploymentAgricultureConstruction);
					alternative.AddUtilityTerm(197, destinationParcel.EmploymentTotal);
					alternative.AddUtilityTerm(198, destinationParcel.Households);
				}
			}


		}

		private sealed class TourDestinationUtilities : ISamplingUtilities {
			private readonly TourWrapper _tour;
			private readonly int _secondaryFlag;
			private readonly int _workOrSchoolPatternFlag;
			private readonly int _otherPatternFlag;
			private readonly int _fastestAvailableTimeOfDay;
			private readonly int _maxAvailableMinutes;
			private readonly int[] _seedValues;

			public TourDestinationUtilities(TourWrapper tour, int sampleSize, int secondaryFlag, int workOrSchoolPatternFlag, int otherPatternFlag, int fastestAvailableTimeOfDay, int maxAvailableMinutes) {
				_tour = tour;
				_secondaryFlag = secondaryFlag;
				_workOrSchoolPatternFlag = workOrSchoolPatternFlag;
				_otherPatternFlag = otherPatternFlag;
				_fastestAvailableTimeOfDay = fastestAvailableTimeOfDay;
				_maxAvailableMinutes = maxAvailableMinutes;
				_seedValues = ChoiceModelUtility.GetRandomSampling(sampleSize, tour.Person.SeedValues[20 + tour.Sequence - 1]);
			}

			public int[] SeedValues {
				get { return _seedValues; }
			}

			public void SetUtilities(ISampleItem sampleItem, int sampleFrequency) {
				if (sampleItem == null) {
					throw new ArgumentNullException("sampleItem");
				}

			}

			public static bool ShouldRunInEstimationModeForModel(DomainModels.Default.Wrappers.TourWrapper tour) {
				// determine validity and need, then characteristics
				// detect and skip invalid trip records (error = true) and those that trips that don't require stop location choice (need = false)
				var excludeReason = 0;

				//				if (_maxZone == -1) {
				//					// TODO: Verify / Optimize
				//					_maxZone = ChoiceModelRunner.ZoneKeys.Max(z => z.Key);
				//				}
				//
				//				if (_maxParcel == -1) {
				//					// TODO: Optimize
				//					_maxParcel = ChoiceModelRunner.Parcels.Values.Max(parcel => parcel.Id);
				//				}

				if (Global.Configuration.IsInEstimationMode) {
					//					if (tour.OriginParcelId > _maxParcel) {
					//						excludeReason = 3;
					//					}

					if (tour.OriginParcelId <= 0) {
						excludeReason = 4;
					}
					//					else if (tour.DestinationAddressType > _maxParcel) {
					//						excludeReason = 5;
					//					}
					else if (tour.DestinationParcelId <= 0) {
						excludeReason = 6;
						tour.DestinationParcelId = tour.OriginParcelId;
						tour.DestinationParcel = tour.OriginParcel;
						tour.DestinationZoneKey = tour.OriginParcelId;
					}
					//					else if (tour.OriginParcelId > _maxParcel) {
					//						excludeReason = 7;
					//					}
					//					else if (tour.OriginParcelId <= 0) {
					//						excludeReason = 8;
					//					}
					//JLB 20130705 dropp following screen for LD
					//else if (tour.OriginParcelId == tour.DestinationParcelId) {
					//	excludeReason = 9;
					//}
					else if (tour.OriginParcel.ZoneId == -1) {
						// TODO: Verify this condition... it used to check that the zone was == null. 
						// I'm not sure what the appropriate condition should be though.

						excludeReason = 10;
					}

					if (excludeReason > 0) {
						Global.PrintFile.WriteEstimationRecordExclusionMessage(CHOICE_MODEL_NAME, "ShouldRunInEstimationModeForModel", tour.Household.Id, tour.Person.Sequence, 0, tour.Sequence, 0, 0, excludeReason);
					}
				}

				var shouldRun = (excludeReason == 0);

				return shouldRun;
			}
		}
	}
}