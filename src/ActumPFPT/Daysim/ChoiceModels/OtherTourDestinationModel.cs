// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
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
	public class OtherTourDestinationModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "OtherTourDestinationModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 120;
		
		public void Run(ITourWrapper tour, int sampleSize) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.OtherTourDestinationModelCoefficients, sampleSize + 1, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			
			tour.PersonDay.ResetRandom(20 + tour.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
				if (!TourDestinationUtilities.ShouldRunInEstimationModeForModel(tour)) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(tour.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				RunModel(choiceProbabilityCalculator, tour, sampleSize, tour.DestinationParcel);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, tour, sampleSize);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(tour.Household.RandomUtility);

				if (chosenAlternative == null) {
					Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", tour.PersonDay.Id);
					tour.PersonDay.IsValid = false;

					return;
				}

				var choice = (CondensedParcel) chosenAlternative.Choice;

				tour.DestinationParcelId = choice.Id;
				tour.DestinationParcel = choice;
				tour.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[choice.ZoneId];
				tour.DestinationAddressType = choice.Id == tour.Person.UsualWorkParcelId ? Constants.AddressType.USUAL_WORKPLACE : Constants.AddressType.OTHER;

				if (choice.Id == tour.Person.UsualWorkParcelId) {
					tour.PersonDay.UsualWorkplaceTours++;
				}
			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ITourWrapper tour, int sampleSize, ICondensedParcel choice = null) {
//			var household = tour.Household;
			var person = tour.Person;
			var personDay = tour.PersonDay;

//			var totalAvailableMinutes =
//				tour.ParentTour == null
//					? personDay.TimeWindow.TotalAvailableMinutes(1, Constants.Time.MINUTES_IN_A_DAY)
//					: tour.ParentTour.TimeWindow.TotalAvailableMinutes(1, Constants.Time.MINUTES_IN_A_DAY);

			var maxAvailableMinutes =
				tour.ParentTour == null
					? personDay.TimeWindow.MaxAvailableMinutesAfter(121)
					: tour.ParentTour.DestinationDepartureTime - tour.ParentTour.DestinationArrivalTime;

//			var hoursAvailableInverse =
//				tour.IsHomeBasedTour
//					? (personDay.HomeBasedTours - personDay.SimulatedHomeBasedTours + 1) / (Math.Max(totalAvailableMinutes - 360, 30) / 60D)
//					: 1 / (Math.Max(totalAvailableMinutes, 1) / 60D);

			var fastestAvailableTimeOfDay =
				tour.IsHomeBasedTour || tour.ParentTour == null
					? 1
					: tour.ParentTour.DestinationArrivalTime + (tour.ParentTour.DestinationDepartureTime - tour.ParentTour.DestinationArrivalTime) / 2;

			var tourCategory = tour.TourCategory;
//			var primaryFlag = ChoiceModelUtility.GetPrimaryFlag(tourCategory);
			var secondaryFlag = ChoiceModelUtility.GetSecondaryFlag(tourCategory);

			ChoiceModelUtility.DrawRandomTourTimePeriods(tour, tourCategory);

			var segment = Global.Kernel.Get<SamplingWeightsSettingsFactory>().SamplingWeightsSettings.GetTourDestinationSegment(tour.DestinationPurpose, tour.IsHomeBasedTour ? Constants.TourPriority.HOME_BASED_TOUR : Constants.TourPriority.WORK_BASED_TOUR, Constants.Mode.SOV, person.PersonType);
			var destinationSampler = new DestinationSampler(choiceProbabilityCalculator, segment, sampleSize, tour.OriginParcel, choice);
			var tourDestinationUtilities = new TourDestinationUtilities(tour, sampleSize, secondaryFlag, personDay.IsWorkOrSchoolPattern.ToFlag(), personDay.IsOtherPattern.ToFlag(), fastestAvailableTimeOfDay, maxAvailableMinutes);

			destinationSampler.SampleTourDestinations(tourDestinationUtilities);
		}

		private sealed class TourDestinationUtilities : ISamplingUtilities {
			private readonly ITourWrapper _tour;
			private readonly int _secondaryFlag;
			private readonly int _workOrSchoolPatternFlag;
			private readonly int _otherPatternFlag;
			private readonly int _fastestAvailableTimeOfDay;
			private readonly int _maxAvailableMinutes;
			private readonly int[] _seedValues;

			public TourDestinationUtilities(ITourWrapper tour, int sampleSize, int secondaryFlag, int workOrSchoolPatternFlag, int otherPatternFlag, int fastestAvailableTimeOfDay, int maxAvailableMinutes) {
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

				var alternative = sampleItem.Alternative;

				if (!alternative.Available) {
					return;
				}

				var household = _tour.Household;
				var person = _tour.Person;
//				var personDay = _tour.PersonDay;
				var householdHasChildren = household.HasChildren;

				var destinationParcel = ChoiceModelFactory.Parcels[sampleItem.ParcelId];

				var fastestTravelTime =
					ImpedanceRoster.GetValue("ivtime", Constants.Mode.HOV3, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, _fastestAvailableTimeOfDay, _tour.OriginParcel, destinationParcel).Variable +
					ImpedanceRoster.GetValue("ivtime", Constants.Mode.HOV3, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, _fastestAvailableTimeOfDay, destinationParcel, _tour.OriginParcel).Variable;

				if (fastestTravelTime >= _maxAvailableMinutes) {
					alternative.Available = false;

					return;
				}

				alternative.Choice = destinationParcel;

				double tourLogsum;

				if (_tour.IsHomeBasedTour) {
					switch (_tour.DestinationPurpose) {
						case Constants.Purpose.ESCORT: {
							var nestedAlternative = (Global.ChoiceModelDictionary.Get("EscortTourModeModel") as EscortTourModeModel).RunNested(_tour, destinationParcel);
							tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();

							break;
						}
						default: {
							var nestedAlternative = (Global.ChoiceModelDictionary.Get("OtherHomeBasedTourModeModel") as OtherHomeBasedTourModeModel).RunNested(_tour, destinationParcel);
							tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();

							break;
						}
					}
				}
				else {
					var nestedAlternative = (Global.ChoiceModelDictionary.Get("WorkBasedSubtourModeModel") as WorkBasedSubtourModeModel).RunNested(_tour, destinationParcel);
					tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}

				//var purpose = _tour.TourPurposeSegment;
				var carOwnership = person.CarOwnershipSegment;
				//var votSegment = _tour.VotALSegment;
				//var transitAccess = destinationParcel.TransitAccessSegment();
				//var aggregateLogsum = Global.AggregateLogsums[destinationParcel.ZoneId][purpose][carOwnership][votSegment][transitAccess];
				//var aggregateLogsumHomeBased = Global.AggregateLogsums[destinationParcel.ZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votSegment][transitAccess];
				//var aggregateLogsumWorkBased = Global.AggregateLogsums[destinationParcel.ZoneId][Constants.Purpose.WORK_BASED][carOwnership][votSegment][transitAccess];

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

				var timePressure = Math.Log(1 - fastestTravelTime / _maxAvailableMinutes);

				// log transforms of buffers for Neighborhood effects
				var empEduBuffer = Math.Log(destinationParcel.EmploymentEducationBuffer1 + 1.0);
				var empFooBuffer = Math.Log(destinationParcel.EmploymentFoodBuffer1 + 1.0);
//				var EMPGOV_B = Math.Log(destinationParcel.EmploymentGovernmentBuffer1 + 1.0);
				var empOfcBuffer = Math.Log(destinationParcel.EmploymentOfficeBuffer1 + 1.0);
				var empRetBuffer = Math.Log(destinationParcel.EmploymentRetailBuffer1 + 1.0);
				var empSvcBuffer = Math.Log(destinationParcel.EmploymentServiceBuffer1 + 1.0);
				var empMedBuffer = Math.Log(destinationParcel.EmploymentMedicalBuffer1 + 1.0);
//				var EMPIND_B = Math.Log(destinationParcel.EmploymentIndustrialBuffer1 + destinationParcel.EmploymentAgricultureConstructionBuffer1 + 1.0);
				var empTotBuffer = Math.Log(destinationParcel.EmploymentTotalBuffer1 + 1.0);
				var housesBuffer = Math.Log(destinationParcel.HouseholdsBuffer1 + 1.0);
				var studK12Buffer = Math.Log(destinationParcel.StudentsK8Buffer1 + destinationParcel.StudentsHighSchoolBuffer1 + 1.0);
				var studUniBuffer = Math.Log(destinationParcel.StudentsUniversityBuffer1 + 1.0);
//				var EMPHOU_B = Math.Log(destinationParcel.EmploymentTotalBuffer1 + destinationParcel.HouseholdsBuffer1 + 1.0);

				// connectivity attributes
				var c34Ratio = destinationParcel.C34RatioBuffer1();

				var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership); // exludes no cars
				var noCarCompetitionFlag = AggregateLogsumsCalculator.GetNoCarCompetitionFlag(carOwnership);
				var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);

				alternative.AddUtilityTerm(1, sampleItem.AdjustmentFactor);
				alternative.AddUtilityTerm(2, (_tour.IsHomeBasedTour).ToFlag() * timePressure);

				alternative.AddUtilityTerm(3, _secondaryFlag * _workOrSchoolPatternFlag * distanceFromOrigin0);
				alternative.AddUtilityTerm(4, _secondaryFlag * _otherPatternFlag * distanceFromOrigin5);
				alternative.AddUtilityTerm(5, _secondaryFlag * _otherPatternFlag * distanceFromOrigin0);
				alternative.AddUtilityTerm(6, _secondaryFlag * _otherPatternFlag * distanceFromOrigin3);

				alternative.AddUtilityTerm(7, (!_tour.IsHomeBasedTour).ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(8, household.Has0To15KIncome.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(9, household.HasMissingIncome.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(10, person.IsRetiredAdult.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(11, person.IsChildAge5Through15.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(12, person.IsChildUnder5.ToFlag() * distanceFromOriginLog);

				alternative.AddUtilityTerm(13, (_tour.IsHomeBasedTour).ToFlag() * distanceFromSchoolLog);
				alternative.AddUtilityTerm(14, distanceFromWorkLog);

				alternative.AddUtilityTerm(15, carCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
				alternative.AddUtilityTerm(16, noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
				alternative.AddUtilityTerm(17, carCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());
				alternative.AddUtilityTerm(18, noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());

				alternative.AddUtilityTerm(19, noCarsFlag * c34Ratio);

				switch (_tour.DestinationPurpose) {
					case Constants.Purpose.ESCORT:
						alternative.AddUtilityTerm(20, tourLogsum);
						alternative.AddUtilityTerm(21, distanceFromOrigin4);
						alternative.AddUtilityTerm(22, distanceFromOrigin8);
						alternative.AddUtilityTerm(23, distanceFromOrigin9);
//						alternative.AddUtility(999, distanceFromOrigin3);

						// Neighborhood
						alternative.AddUtilityTerm(24, householdHasChildren.ToFlag() * studK12Buffer);
						alternative.AddUtilityTerm(25, empTotBuffer);

						// Size terms
						alternative.AddUtilityTerm(71, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(72, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(73, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(74, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(75, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(76, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(77, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(78, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtilityTerm(79, (!householdHasChildren).ToFlag() * destinationParcel.Households);

						alternative.AddUtilityTerm(80, householdHasChildren.ToFlag() * destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(81, householdHasChildren.ToFlag() * destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(82, householdHasChildren.ToFlag() * destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(83, householdHasChildren.ToFlag() * destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(84, householdHasChildren.ToFlag() * destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(85, householdHasChildren.ToFlag() * destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(86, householdHasChildren.ToFlag() * destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtilityTerm(87, householdHasChildren.ToFlag() * destinationParcel.Households);
						alternative.AddUtilityTerm(88, householdHasChildren.ToFlag() * destinationParcel.StudentsK12);

						break;
					case Constants.Purpose.PERSONAL_BUSINESS:
					case Constants.Purpose.MEDICAL:
						alternative.AddUtilityTerm(26, tourLogsum);
						alternative.AddUtilityTerm(27, distanceFromOrigin4);
						alternative.AddUtilityTerm(28, distanceFromOrigin8);
						alternative.AddUtilityTerm(29, distanceFromOrigin9);
						alternative.AddUtilityTerm(30, distanceFromOrigin3);

						// Neighborhood
						alternative.AddUtilityTerm(31, empEduBuffer);
						alternative.AddUtilityTerm(32, empSvcBuffer);
						alternative.AddUtilityTerm(33, empMedBuffer);
						alternative.AddUtilityTerm(34, housesBuffer); // also psrc
						alternative.AddUtilityTerm(35, studUniBuffer);

						// Size terms
						alternative.AddUtilityTerm(89, destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(90, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(91, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(92, destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(93, destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(94, destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(95, destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtilityTerm(96, destinationParcel.Households);
						alternative.AddUtilityTerm(97, destinationParcel.StudentsK12);

						break;
					case Constants.Purpose.SHOPPING:
						alternative.AddUtilityTerm(36, tourLogsum);
						alternative.AddUtilityTerm(37, distanceFromOrigin4);
						alternative.AddUtilityTerm(38, distanceFromOrigin8);
						alternative.AddUtilityTerm(39, distanceFromOrigin9);
						alternative.AddUtilityTerm(40, distanceFromOrigin3);

						// Neighborhood
						alternative.AddUtilityTerm(41, empEduBuffer); // also psrc
						alternative.AddUtilityTerm(42, empRetBuffer); // also psrc

						// Size terms
						alternative.AddUtilityTerm(98, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(99, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(100, destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(101, destinationParcel.EmploymentService);

						break;
					case Constants.Purpose.MEAL:
						alternative.AddUtilityTerm(43, tourLogsum);
						alternative.AddUtilityTerm(44, distanceFromOrigin4);
						alternative.AddUtilityTerm(45, distanceFromOrigin8);
						alternative.AddUtilityTerm(46, distanceFromOrigin9);
						alternative.AddUtilityTerm(47, distanceFromOrigin3);

						// Neighborhood
						alternative.AddUtilityTerm(48, empFooBuffer); // psrc

						// Size terms
						alternative.AddUtilityTerm(102, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(103, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(104, destinationParcel.EmploymentTotal);
						alternative.AddUtilityTerm(105, destinationParcel.Households);

						break;
					case Constants.Purpose.SOCIAL:
					case Constants.Purpose.RECREATION:
						alternative.AddUtilityTerm(49, tourLogsum);
						alternative.AddUtilityTerm(50, distanceFromOrigin4);
						alternative.AddUtilityTerm(51, distanceFromOrigin8);
						alternative.AddUtilityTerm(52, distanceFromOrigin9);
						alternative.AddUtilityTerm(53, distanceFromOrigin3);

						// Neighborhood
						alternative.AddUtilityTerm(54, empOfcBuffer); // also psrc
						alternative.AddUtilityTerm(55, empSvcBuffer); // also psrc
						alternative.AddUtilityTerm(56, housesBuffer); // also psrc
						alternative.AddUtilityTerm(57, studUniBuffer); // psrc

						// Size terms
						alternative.AddUtilityTerm(106, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(107, destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(108, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(109, destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(110, destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(111, destinationParcel.Households);
						alternative.AddUtilityTerm(112, destinationParcel.StudentsUniversity);
						alternative.AddUtilityTerm(113, destinationParcel.StudentsK12);

						break;
				}
			}

			public static bool ShouldRunInEstimationModeForModel(ITourWrapper tour) {
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