// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// 
// Copyright 2011-2012 John Bowman, Mark Bradley, and RSG, Inc.
// 
// This file is part of Daysim.
// 
// Daysim is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Daysim is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Daysim. If not, see <http://www.gnu.org/licenses/>.

using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Framework.Sampling;
using Daysim.Framework.Roster;

namespace Daysim.ChoiceModels {
	public static class TourDestinationModel {
		private const string CHOICE_MODEL_NAME = "TourDestinationModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 102;
		private const int TOTAL_LEVELS = 2;
		private const int MAX_PARAMETER = 699;

		private static ChoiceModelHelper _helper;

		private static void Initialize(int sampleSize) {
			if (_helper != null) {
				return;
			}

			ChoiceModelHelper.Initialize(ref _helper, CHOICE_MODEL_NAME, Global.Configuration.TourDestinationModelCoefficients, sampleSize + 1, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
		}

		public static void Run(TourWrapper tour, int sampleSize) {

			Initialize(sampleSize);
			_helper.OpenTrace();

			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
				if (!TourDestinationUtilities.ShouldRunInEstimationModeForModel(tour)) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helper.GetChoiceProbabilityCalculator(tour.Id);

			if (_helper.ModelIsInEstimationMode) {
				RunModel(choiceProbabilityCalculator, tour, sampleSize, tour.DestinationParcel);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, tour, sampleSize);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice();

				if (chosenAlternative == null) {
					Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", tour.PersonDay.Id);
					tour.PersonDay.IsValid = false;

					return;
				}

				var choice = (CondensedParcel) chosenAlternative.Choice;

				tour.DestinationParcelId = choice.Id;
				tour.DestinationParcel = choice;
				tour.DestinationZoneKey = ChoiceModelRunner.ZoneKeys[choice.ZoneId];
				tour.DestinationAddressType = choice.Id == tour.Person.UsualWorkParcelId ? Constants.AddressType.USUAL_WORKPLACE : Constants.AddressType.OTHER;

				if (choice.Id == tour.Person.UsualWorkParcelId) {
					tour.PersonDay.UsualWorkplaceTours++;
				}
			}

			_helper.CloseTrace(tour);
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, TourWrapper tour, int sampleSize, CondensedParcel choice = null) {
			var household = tour.Household;
			var person = tour.Person;
			var personDay = tour.PersonDay;

			var totalAvailableMinutes =
				tour.ParentTour == null
					? personDay.TimeWindow.TotalAvailableMinutes(1, Constants.Time.MINUTES_IN_A_DAY)
					: tour.ParentTour.TimeWindow.TotalAvailableMinutes(1, Constants.Time.MINUTES_IN_A_DAY);

			var maxAvailableMinutes =
				tour.ParentTour == null
					? personDay.TimeWindow.MaxAvailableMinutesAfter(121)
					: tour.ParentTour.DestinationDepartureTime - tour.ParentTour.DestinationArrivalTime;

			var hoursAvailableInverse =
				tour.IsHomeBasedTour
					? (personDay.HomeBasedTours - personDay.SimulatedHomeBasedTours + 1) / (Math.Max(totalAvailableMinutes - 360, 30) / 60D)
					: 1 / (Math.Max(totalAvailableMinutes, 1) / 60D);

			int fastestAvailableTimeOfDay =
				tour.IsHomeBasedTour
				? 1
				: tour.ParentTour.DestinationArrivalTime + (tour.ParentTour.DestinationDepartureTime - tour.ParentTour.DestinationArrivalTime) / 2;

			var tourCategory = tour.TourCategory;
			var primaryFlag = ChoiceModelUtility.GetPrimaryFlag(tourCategory);
			var secondaryFlag = ChoiceModelUtility.GetSecondaryFlag(tourCategory);

			ChoiceModelUtility.DrawRandomTourTimePeriods(tour, tourCategory);

			var segment = SamplingWeightsSettings.GetTourDestinationSegment(tour.DestinationPurpose, tour.IsHomeBasedTour ? Constants.TourPriority.HOME_BASED_TOUR : Constants.TourPriority.WORK_BASED_TOUR, Constants.Mode.SOV, person.PersonType);
			var excludedParcel = person.UsualWorkParcel == null || person.UsualWorkParcelId == household.ResidenceParcelId || tour.DestinationPurpose != Constants.Purpose.WORK || tour.TourCategory == Constants.TourCategory.WORK_BASED ? null : person.UsualWorkParcel;
			var destinationSampler = new DestinationSampler(choiceProbabilityCalculator, segment, sampleSize, tour.OriginParcel, excludedParcel, excludedParcel, choice);
			var tourDestinationUtilities = new TourDestinationUtilities(tour, sampleSize, primaryFlag, secondaryFlag, hoursAvailableInverse, personDay.IsWorkOrSchoolPattern.ToFlag(), personDay.IsOtherPattern.ToFlag(), fastestAvailableTimeOfDay, maxAvailableMinutes);

			destinationSampler.SampleTourDestinations(tourDestinationUtilities);
		}

		private sealed class TourDestinationUtilities : ISamplingUtilities {
			private readonly TourWrapper _tour;
			private readonly int _sampleSize;
			private readonly int _primaryFlag;
			private readonly int _secondaryFlag;
			private readonly double _hoursAvailableInverse;
			private readonly int _workOrSchoolPatternFlag;
			private readonly int _otherPatternFlag;
			private readonly int _fastestAvailableTimeOfDay;
			private readonly int _maxAvailableMinutes;

			public TourDestinationUtilities(TourWrapper tour, int sampleSize, int primaryFlag, int secondaryFlag, double hoursAvailableInverse, int workOrSchoolPatternFlag, int otherPatternFlag, int fastestAvailableTimeOfDay, int maxAvailableMinutes) {
				_tour = tour;
				_sampleSize = sampleSize;
				_primaryFlag = primaryFlag;
				_secondaryFlag = secondaryFlag;
				_hoursAvailableInverse = hoursAvailableInverse;
				_workOrSchoolPatternFlag = workOrSchoolPatternFlag;
				_otherPatternFlag = otherPatternFlag;
				_fastestAvailableTimeOfDay = fastestAvailableTimeOfDay;
				_maxAvailableMinutes = maxAvailableMinutes;
			}

			public void SetUtilities(ISampleItem sampleItem, int sampleFrequency) {
				if (sampleItem == null) {
					throw new ArgumentNullException("sampleItem");
				}

				var alternative = sampleItem.Alternative;

				if (!alternative.Available) {
					return;
				}

				var household = _tour.Household;
				var person = _tour.Person;
				var personDay = _tour.PersonDay;
				var householdHasChildren = household.HasChildren;

				var destinationParcel = ChoiceModelRunner.Parcels[sampleItem.ParcelId];
				var destinationZoneTotals = ChoiceModelRunner.ZoneTotals[destinationParcel.ZoneId];

				var excludedParcel = person.UsualWorkParcel == null || person.UsualWorkParcelId == household.ResidenceParcelId || _tour.DestinationPurpose != Constants.Purpose.WORK || _tour.TourCategory == Constants.TourCategory.WORK_BASED ? null : person.UsualWorkParcel;
				var usualWorkParcel = (excludedParcel != null && excludedParcel.Id == destinationParcel.Id);  // only 1 for oddball alternative on tours with oddball alternative
				var usualWorkParcelFlag = usualWorkParcel.ToFlag();

				// use this block of code to eliminate the oddball alternative for estimation of the conditional model
				if (usualWorkParcelFlag == 1) {
					alternative.Available = false;
					return;
				}

				var fastestTravelTime = ImpedanceRoster.GetValue("ivtime", Constants.Mode.HOV3, Constants.PathType.FULL_NETWORK, Constants.VotGroup.VERY_HIGH, _fastestAvailableTimeOfDay, _tour.OriginParcel, destinationParcel).Variable
					+ ImpedanceRoster.GetValue("ivtime", Constants.Mode.HOV3, Constants.PathType.FULL_NETWORK, Constants.VotGroup.VERY_HIGH, _fastestAvailableTimeOfDay, destinationParcel, _tour.OriginParcel).Variable;
				if (fastestTravelTime >= _maxAvailableMinutes) {
					alternative.Available = false;
					return;
				}

				alternative.Choice = destinationParcel;

				double tourLogsum;

				if (_tour.IsHomeBasedTour) {
					switch (_tour.DestinationPurpose) {
						case Constants.Purpose.WORK: {
								var nestedAlternative = WorkTourModeModel.RunNested(personDay, _tour.OriginParcel, destinationParcel, _tour.DestinationArrivalTime, _tour.DestinationDepartureTime, household.VehiclesAvailable, _tour.TimeCoefficient);
								tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();

								break;
							}
						case Constants.Purpose.ESCORT: {
								var nestedAlternative = EscortTourModeModel.RunNested(_tour, destinationParcel);
								tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();

								break;
							}
						default: {
								var nestedAlternative = OtherHomeBasedTourModeModel.RunNested(_tour, destinationParcel);
								tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();

								break;
							}
					}
				}
				else {
					var nestedAlternative = WorkBasedSubtourModeModel.RunNested(_tour, destinationParcel);
					tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}


				var purpose = _tour.TourPurposeSegment;
				var nonWorkPurpose = _tour.DestinationPurpose != Constants.Purpose.WORK;
				var carOwnership = person.CarOwnershipSegment;
				var transitAccess = destinationParcel.TransitAccessSegment();
				var aggregateLogsum = Global.AggregateLogsums[destinationParcel.ZoneId][purpose][carOwnership][transitAccess];
				var aggregateLogsumHomeBased = Global.AggregateLogsums[destinationParcel.ZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][transitAccess];
				var aggregateLogsumWorkBased = Global.AggregateLogsums[destinationParcel.ZoneId][Constants.Purpose.WORK_BASED][carOwnership][transitAccess];

				var distanceFromOrigin = _tour.OriginParcel.DistanceFromOrigin(destinationParcel, _tour.DestinationArrivalTime);
				var distanceFromOrigin0 = Math.Max(0, Math.Min(distanceFromOrigin - .5, 1 - .5));
				var distanceFromOrigin3 = Math.Max(0, distanceFromOrigin - 1);
				var distanceFromOrigin4 = Math.Min(distanceFromOrigin, .10);
				var distanceFromOrigin5 = Math.Max(0, Math.Min(distanceFromOrigin - .1, .5 - .1));
				var distanceFromOrigin8 = Math.Max(0, Math.Min(distanceFromOrigin - .1, .35 - .1));
				var distanceFromOrigin9 = Math.Max(0, Math.Min(distanceFromOrigin - .35, 1 - .35));
				var distanceFromOriginLog = Math.Log(1 + distanceFromOrigin);
				var distanceFromWorkLog = person.UsualWorkParcel.DistanceFromWorkLog(destinationParcel, 1);
				var distanceFromSchoolLog = person.UsualSchoolParcel.DistanceFromSchoolLog(destinationParcel, 1);
				var millionsSquareFeet = destinationZoneTotals.MillionsSquareFeet();

				var timePressure = Math.Log(1 - fastestTravelTime / _maxAvailableMinutes);

				// log transforms of buffers for Neighborhood effects
				var EMPEDU_B = Math.Log(destinationParcel.EmploymentEducationBuffer1 + 1.0);
				var EMPFOO_B = Math.Log(destinationParcel.EmploymentFoodBuffer1 + 1.0);
				var EMPGOV_B = Math.Log(destinationParcel.EmploymentGovernmentBuffer1 + 1.0);
				var EMPOFC_B = Math.Log(destinationParcel.EmploymentOfficeBuffer1 + 1.0);
				var EMPRET_B = Math.Log(destinationParcel.EmploymentRetailBuffer1 + 1.0);
				var EMPSVC_B = Math.Log(destinationParcel.EmploymentServiceBuffer1 + 1.0);
				var EMPMED_B = Math.Log(destinationParcel.EmploymentMedicalBuffer1 + 1.0);
				var EMPIND_B = Math.Log(destinationParcel.EmploymentIndustrialBuffer1 + destinationParcel.EmploymentAgricultureConstructionBuffer1 + 1.0);
				var EMPTOT_B = Math.Log(destinationParcel.EmploymentTotalBuffer1 + 1.0);
				var HOUSES_B = Math.Log(destinationParcel.HouseholdsBuffer1 + 1.0);
				var STUDK12B = Math.Log(destinationParcel.StudentsK8Buffer1 + destinationParcel.StudentsHighSchoolBuffer1 + 1.0);
				var STUDUNIB = Math.Log(destinationParcel.StudentsUniversityBuffer1 + 1.0);
				var EMPHOU_B = Math.Log(destinationParcel.EmploymentTotalBuffer1 + destinationParcel.HouseholdsBuffer1 + 1.0);

				// zone densities
				var eduDensity = destinationZoneTotals.GetEmploymentEducationDensity(millionsSquareFeet);
				var govDensity = destinationZoneTotals.GetEmploymentGovernmentDensity(millionsSquareFeet);
				var offDensity = destinationZoneTotals.GetEmploymentOfficeDensity(millionsSquareFeet);
				var serDensity = destinationZoneTotals.GetEmploymentServiceDensity(millionsSquareFeet);
				var houDensity = destinationZoneTotals.GetHouseholdsDensity(millionsSquareFeet);

				// parking attributes
				var parcelParkingDensity = destinationParcel.ParcelParkingPerTotalEmployment();
				var zoneParkingDensity = destinationParcel.ZoneParkingPerTotalEmploymentAndK12UniversityStudents(destinationZoneTotals, millionsSquareFeet);
				var ParkingPaidDailyLogBuffer1 = Math.Log(1 + destinationParcel.ParkingOffStreetPaidDailySpacesBuffer1);

				// connectivity attributes
				var c34Ratio = destinationParcel.C34RatioBuffer1();

				var carDeficitFlag = AggregateLogsumsCalculator.GetCarDeficitFlag(carOwnership);  // includes no cars
				var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership); // exludes no cars
				var noCarCompetitionFlag = AggregateLogsumsCalculator.GetNoCarCompetitionFlag(carOwnership);
				var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);


				alternative.AddUtility(1, sampleItem.AdjustmentFactor);
				alternative.AddUtility(2, tourLogsum);  // maybe make these purpose-specific (but they will probably be constrained to +1)

				alternative.AddUtility(23, _secondaryFlag * _workOrSchoolPatternFlag * distanceFromOrigin4);
				alternative.AddUtility(24, _secondaryFlag * _workOrSchoolPatternFlag * distanceFromOrigin5);
				alternative.AddUtility(25, _secondaryFlag * _workOrSchoolPatternFlag * distanceFromOrigin0);
				alternative.AddUtility(26, _secondaryFlag * _workOrSchoolPatternFlag * distanceFromOrigin3);
				alternative.AddUtility(27, _secondaryFlag * _otherPatternFlag * distanceFromOrigin4);
				alternative.AddUtility(28, _secondaryFlag * _otherPatternFlag * distanceFromOrigin5);
				alternative.AddUtility(29, _secondaryFlag * _otherPatternFlag * distanceFromOrigin0);
				alternative.AddUtility(30, _secondaryFlag * _otherPatternFlag * distanceFromOrigin3);
				alternative.AddUtility(31, (!_tour.IsHomeBasedTour).ToFlag() * distanceFromOriginLog);
				alternative.AddUtility(32, household.Has0To15KIncome.ToFlag() * distanceFromOriginLog);
				alternative.AddUtility(33, household.HasMissingIncome.ToFlag() * distanceFromOriginLog);
				alternative.AddUtility(34, person.IsRetiredAdult.ToFlag() * distanceFromOriginLog);
				alternative.AddUtility(35, person.IsUniversityStudent.ToFlag() * distanceFromOriginLog);
				alternative.AddUtility(36, person.IsChildAge5Through15.ToFlag() * distanceFromOriginLog);
				alternative.AddUtility(37, person.IsChildUnder5.ToFlag() * distanceFromOriginLog);
				alternative.AddUtility(38, (_tour.IsHomeBasedTour).ToFlag() * _hoursAvailableInverse * distanceFromOriginLog);
				alternative.AddUtility(39, (_tour.IsHomeBasedTour).ToFlag() * _hoursAvailableInverse * Constants.Time.EIGHTEEN_HOURS * tourLogsum);
				alternative.AddUtility(40, (_tour.IsHomeBasedTour).ToFlag() * distanceFromSchoolLog);
				alternative.AddUtility(49, timePressure);
				alternative.AddUtility(56, nonWorkPurpose.ToFlag() * carCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
				alternative.AddUtility(57, nonWorkPurpose.ToFlag() * noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
				alternative.AddUtility(58, nonWorkPurpose.ToFlag() * carCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());
				alternative.AddUtility(59, nonWorkPurpose.ToFlag() * noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());
				alternative.AddUtility(60, nonWorkPurpose.ToFlag() * noCarsFlag * c34Ratio);
				alternative.AddUtility(61, nonWorkPurpose.ToFlag() * carCompetitionFlag * c34Ratio);
				alternative.AddUtility(62, nonWorkPurpose.ToFlag() * noCarCompetitionFlag * c34Ratio);
				alternative.AddUtility(423, carCompetitionFlag * ParkingPaidDailyLogBuffer1);
				alternative.AddUtility(424, noCarCompetitionFlag * ParkingPaidDailyLogBuffer1);

				switch (_tour.DestinationPurpose) {
					case Constants.Purpose.WORK:

						alternative.AddUtility(502, usualWorkParcelFlag);
						alternative.AddUtility(503, person.IsPartTimeWorker.ToFlag() * usualWorkParcelFlag);
						alternative.AddUtility(504, person.IsStudentAge.ToFlag() * usualWorkParcelFlag);
						alternative.AddUtility(512, _primaryFlag * personDay.HasWorkTours.ToFlag() * usualWorkParcelFlag);
						alternative.AddUtility(513, personDay.HasWorkStops.ToFlag() * usualWorkParcelFlag);
						alternative.AddUtility(516, _secondaryFlag * usualWorkParcelFlag);

						alternative.AddUtility(518, person.IsFulltimeWorker.ToFlag() * tourLogsum);
						alternative.AddUtility(519, person.IsPartTimeWorker.ToFlag() * tourLogsum);
						alternative.AddUtility(520, person.IsNotFullOrPartTimeWorker.ToFlag() * tourLogsum);

						alternative.AddUtility(210, distanceFromOrigin4);
						alternative.AddUtility(211, distanceFromOrigin8);
						alternative.AddUtility(212, distanceFromOrigin9);
						alternative.AddUtility(213, distanceFromOrigin3);

						alternative.AddUtility(522, person.IsFulltimeWorker.ToFlag() * distanceFromOriginLog);
						alternative.AddUtility(523, person.IsPartTimeWorker.ToFlag() * distanceFromOriginLog);
						alternative.AddUtility(524, person.IsNotFullOrPartTimeWorker.ToFlag() * distanceFromOriginLog);
						alternative.AddUtility(535, _secondaryFlag * distanceFromOriginLog);
						alternative.AddUtility(537, (!usualWorkParcel).ToFlag() * distanceFromWorkLog);
						alternative.AddUtility(538, person.IsStudentAge.ToFlag() * distanceFromSchoolLog);

						alternative.AddUtility(539, person.IsFulltimeWorker.ToFlag() * aggregateLogsumWorkBased);
						alternative.AddUtility(541, person.IsNotFullOrPartTimeWorker.ToFlag() * aggregateLogsumWorkBased);

						alternative.AddUtility(552, parcelParkingDensity);
						alternative.AddUtility(554, zoneParkingDensity);
						alternative.AddUtility(557, carDeficitFlag * c34Ratio);
						alternative.AddUtility(558, carCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
						alternative.AddUtility(559, noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
						alternative.AddUtility(560, carCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());
						alternative.AddUtility(561, noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());

						// Neighborhood (consider conditioning these by fulltime, parttime, notFTPT, an income (see original sacog spec)
						alternative.AddUtility(220, EMPEDU_B);
						alternative.AddUtility(221, EMPGOV_B);
						alternative.AddUtility(222, EMPOFC_B);
						alternative.AddUtility(223, EMPSVC_B);
						alternative.AddUtility(224, EMPMED_B);
						alternative.AddUtility(225, HOUSES_B);
						alternative.AddUtility(226, STUDUNIB);
						alternative.AddUtility(227, STUDK12B);
						alternative.AddUtility(228, EMPIND_B);

						// Size terms (consider conditioning these by fulltime, parttime, notFTPT, an income (see original sacog spec)
						alternative.AddUtility(230, destinationParcel.EmploymentEducation);
						alternative.AddUtility(231, destinationParcel.EmploymentFood);
						alternative.AddUtility(232, destinationParcel.EmploymentGovernment);
						alternative.AddUtility(233, destinationParcel.EmploymentOffice);
						alternative.AddUtility(234, destinationParcel.EmploymentRetail);
						alternative.AddUtility(235, destinationParcel.EmploymentService);
						alternative.AddUtility(236, destinationParcel.EmploymentMedical);
						alternative.AddUtility(237, destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtility(238, destinationParcel.Households);
						alternative.AddUtility(239, destinationParcel.StudentsK12);
						alternative.AddUtility(240, destinationParcel.StudentsUniversity);

						break;
					case Constants.Purpose.ESCORT:
						alternative.AddUtility(44, tourLogsum);  // maybe make these purpose-specific (but they will probably be constrained to +1)
						alternative.AddUtility(3, distanceFromOrigin4);
						alternative.AddUtility(4, distanceFromOrigin8);
						alternative.AddUtility(5, distanceFromOrigin9);
						alternative.AddUtility(6, distanceFromOrigin3);
						alternative.AddUtility(41, aggregateLogsumWorkBased);

						// Neighborhood
						alternative.AddUtility(64, (!householdHasChildren).ToFlag() * EMPGOV_B);
						alternative.AddUtility(67, (!householdHasChildren).ToFlag() * HOUSES_B);
						alternative.AddUtility(74, householdHasChildren.ToFlag() * HOUSES_B);
						alternative.AddUtility(75, householdHasChildren.ToFlag() * STUDK12B);
						//alternative.AddUtility(264, EMPTOT_B);

						// Size terms
						alternative.AddUtility(101, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentEducation);
						alternative.AddUtility(102, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentFood);
						alternative.AddUtility(103, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentGovernment);
						alternative.AddUtility(104, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentOffice);
						alternative.AddUtility(106, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentRetail);
						alternative.AddUtility(107, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentService);
						alternative.AddUtility(108, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentMedical);
						alternative.AddUtility(109, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtility(111, (!householdHasChildren).ToFlag() * destinationParcel.Households);
						alternative.AddUtility(113, (!householdHasChildren).ToFlag() * destinationParcel.StudentsK12);
						alternative.AddUtility(114, householdHasChildren.ToFlag() * destinationParcel.EmploymentEducation);
						alternative.AddUtility(116, householdHasChildren.ToFlag() * destinationParcel.EmploymentGovernment);
						alternative.AddUtility(117, householdHasChildren.ToFlag() * destinationParcel.EmploymentOffice);
						alternative.AddUtility(119, householdHasChildren.ToFlag() * destinationParcel.EmploymentRetail);
						alternative.AddUtility(120, householdHasChildren.ToFlag() * destinationParcel.EmploymentService);
						alternative.AddUtility(121, householdHasChildren.ToFlag() * destinationParcel.EmploymentMedical);
						alternative.AddUtility(122, householdHasChildren.ToFlag() * destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtility(124, householdHasChildren.ToFlag() * destinationParcel.Households);
						alternative.AddUtility(126, householdHasChildren.ToFlag() * destinationParcel.StudentsK12);

						break;
					case Constants.Purpose.PERSONAL_BUSINESS:
					case Constants.Purpose.MEDICAL:
						alternative.AddUtility(45, tourLogsum);  // maybe make these purpose-specific (but they will probably be constrained to +1)
						alternative.AddUtility(7, distanceFromOrigin4);
						alternative.AddUtility(8, distanceFromOrigin8);
						alternative.AddUtility(9, distanceFromOrigin9);
						alternative.AddUtility(10, distanceFromOrigin3);
						alternative.AddUtility(42, aggregateLogsumWorkBased);

						// Neighborhood
						alternative.AddUtility(76, EMPEDU_B);
						alternative.AddUtility(78, EMPOFC_B); // also parc
						alternative.AddUtility(79, EMPSVC_B);
						alternative.AddUtility(80, EMPMED_B);
						alternative.AddUtility(81, HOUSES_B); // also psrc
						alternative.AddUtility(82, STUDUNIB);
						//alternative.AddUtility(307, EMPRET_B);  // psrc

						// Size terms
						alternative.AddUtility(127, destinationParcel.EmploymentEducation);
						alternative.AddUtility(128, destinationParcel.EmploymentFood);
						alternative.AddUtility(129, destinationParcel.EmploymentGovernment);
						alternative.AddUtility(130, destinationParcel.EmploymentOffice);
						alternative.AddUtility(132, destinationParcel.EmploymentRetail);
						alternative.AddUtility(133, destinationParcel.EmploymentService);
						alternative.AddUtility(134, destinationParcel.EmploymentMedical);
						alternative.AddUtility(135, destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtility(137, destinationParcel.Households);
						alternative.AddUtility(139, destinationParcel.StudentsK12);

						break;
					case Constants.Purpose.SHOPPING:
						alternative.AddUtility(46, tourLogsum);  // maybe make these purpose-specific (but they will probably be constrained to +1)
						alternative.AddUtility(11, distanceFromOrigin4);
						alternative.AddUtility(12, distanceFromOrigin8);
						alternative.AddUtility(13, distanceFromOrigin9);
						alternative.AddUtility(14, distanceFromOrigin3);
						alternative.AddUtility(43, aggregateLogsumWorkBased);

						// Neighborhood
						alternative.AddUtility(83, EMPEDU_B); // also psrc
						alternative.AddUtility(86, EMPRET_B); // also psrc

						// Size terms
						alternative.AddUtility(141, destinationParcel.EmploymentFood);
						alternative.AddUtility(143, destinationParcel.EmploymentOffice);
						alternative.AddUtility(145, destinationParcel.EmploymentRetail);
						alternative.AddUtility(146, destinationParcel.EmploymentService);

						break;
					case Constants.Purpose.MEAL:
						alternative.AddUtility(47, tourLogsum);  // maybe make these purpose-specific (but they will probably be constrained to +1)
						alternative.AddUtility(15, distanceFromOrigin4);
						alternative.AddUtility(16, distanceFromOrigin8);
						alternative.AddUtility(17, distanceFromOrigin9);
						alternative.AddUtility(18, distanceFromOrigin3);
						alternative.AddUtility(354, aggregateLogsumWorkBased); // prob not

						// Neighborhood
						alternative.AddUtility(356, EMPFOO_B); // psrc
						alternative.AddUtility(357, EMPRET_B); // psrc
						alternative.AddUtility(358, EMPSVC_B); // psrc

						// Size terms
						alternative.AddUtility(154, destinationParcel.EmploymentFood);
						alternative.AddUtility(156, destinationParcel.EmploymentOffice);
						alternative.AddUtility(162, destinationParcel.EmploymentTotal);
						alternative.AddUtility(163, destinationParcel.Households);

						break;
					case Constants.Purpose.SOCIAL:
					case Constants.Purpose.RECREATION:
						alternative.AddUtility(48, tourLogsum);  // maybe make these purpose-specific (but they will probably be constrained to +1)
						alternative.AddUtility(19, distanceFromOrigin4);
						alternative.AddUtility(20, distanceFromOrigin8);
						alternative.AddUtility(21, distanceFromOrigin9);
						alternative.AddUtility(22, distanceFromOrigin3);
						alternative.AddUtility(374, aggregateLogsumWorkBased); // prob not

						// Neighborhood
						alternative.AddUtility(98, EMPOFC_B); // also psrc
						alternative.AddUtility(99, EMPSVC_B); // also psrc
						alternative.AddUtility(100, HOUSES_B); // also psrc
						alternative.AddUtility(383, STUDK12B); // psrc
						alternative.AddUtility(384, STUDUNIB); // psrc
						alternative.AddUtility(385, EMPTOT_B); // psrc

						// Size terms
						alternative.AddUtility(166, destinationParcel.EmploymentEducation);
						alternative.AddUtility(167, destinationParcel.EmploymentFood);
						alternative.AddUtility(168, destinationParcel.EmploymentGovernment);
						alternative.AddUtility(169, destinationParcel.EmploymentOffice);
						alternative.AddUtility(171, destinationParcel.EmploymentRetail);
						alternative.AddUtility(172, destinationParcel.EmploymentService);
						alternative.AddUtility(173, destinationParcel.EmploymentMedical);
						alternative.AddUtility(176, destinationParcel.Households);
						alternative.AddUtility(177, destinationParcel.StudentsUniversity);
						alternative.AddUtility(178, destinationParcel.StudentsK12);

						break;
				}
				//// Comment out these nesting calls when estimatign the conditional flat model
				//// model is NL with oddball alternative
				//if (usualWorkParcelFlag == 0) {
				//	// this alternative is in the non-oddball nest
				//	alternative.AddNestedAlternative(_sampleSize + 2, 0, 694); // associates alternative with non-oddball nest
				//}
				//else {
				//	// this is the oddball alternative
				//	alternative.AddNestedAlternative(_sampleSize + 3, 1, 694); // associates alternative with oddball nest
				//}
			}

			public static bool ShouldRunInEstimationModeForModel(TourWrapper tour) {
				//   {determine validity and need, then characteristics}
				//   {detect and skip invalid trip records (error = true) and those that trips that don't require stop location choice (need = false)}
				int excludeReason = 0;
				//				if (_maxZone == -1) {
				//					// TODO: Verify / Optimize
				//					_maxZone = ChoiceModelRunner.ZoneKeys.Max(z => z.Key);
				//				}
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
					}
					//					else if (tour.OriginParcelId > _maxParcel) {
					//						excludeReason = 7;
					//					}
					//					else if (tour.OriginParcelId <= 0) {
					//						excludeReason = 8;
					//					}
					else if (tour.OriginParcelId == tour.DestinationParcelId) {
						excludeReason = 9;
					}
					// TODO: Verify this condition... it used to check that the zone was == null. 
					// I'm not sure what the appropriate condition should be though.
					else if (tour.OriginParcel.ZoneId == -1) {
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