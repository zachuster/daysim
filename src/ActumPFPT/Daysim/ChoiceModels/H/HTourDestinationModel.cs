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
	public static class HTourDestinationModel {
		private const string CHOICE_MODEL_NAME = "HTourDestinationModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 300;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];

		private static void Initialize(int sampleSize) {
			if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null) {
				return;
			}

			ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME, Global.GetInputPath(Global.Configuration.OtherTourDestinationModelCoefficients), sampleSize + 1, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
		}

		public static void Run(ITourWrapper tour, IHouseholdDayWrapper householdDay, int sampleSize, ICondensedParcel constrainedParcel = null) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			Initialize(sampleSize);

			tour.PersonDay.ResetRandom(20 + tour.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (!TourDestinationUtilities.ShouldRunInEstimationModeForModel(tour)) {
					return;
				}
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}
			else if (tour.DestinationPurpose == Constants.Purpose.SCHOOL) {
				// the following lines were redundant.  Usual destination properties are set in GetMandatoryTourSimulatedData() 
				// sets the destination for the school tour
				//tour.DestinationParcelId = tour.Person.UsualSchoolParcelId;
				//tour.DestinationParcel = tour.Person.UsualSchoolParcel;
				//tour.DestinationZoneKey = tour.Person.UsualSchoolZoneKey;
				//tour.DestinationAddressType = Constants.AddressType.USUAL_SCHOOL;
				return;
			}
			else if (tour.DestinationPurpose == Constants.Purpose.WORK 
				&& tour.DestinationAddressType == Constants.AddressType.USUAL_WORKPLACE) {
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
				RunModel(choiceProbabilityCalculator, tour, householdDay, sampleSize, tour.DestinationParcel);

				choiceProbabilityCalculator.WriteObservation();
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

				var choice = (CondensedParcel) chosenAlternative.Choice;

				tour.DestinationParcelId = choice.Id;
				tour.DestinationParcel = choice;
				tour.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[choice.ZoneId];
				tour.DestinationAddressType = Constants.AddressType.OTHER;

			}
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ITourWrapper tour, IHouseholdDayWrapper householdDay, int sampleSize, ICondensedParcel choice = null) {
			//			var household = tour.Household;
			var person = tour.Person;
			var personDay = tour.PersonDay;

			//			var totalAvailableMinutes =
			//				tour.ParentTour == null
			//					? personDay.TimeWindow.TotalAvailableMinutes(1, Constants.Time.MINUTES_IN_A_DAY)
			//					: tour.ParentTour.TimeWindow.TotalAvailableMinutes(1, Constants.Time.MINUTES_IN_A_DAY);


			TimeWindow timeWindow = new TimeWindow();
			if (tour.JointTourSequence > 0) {
				foreach (PersonDayWrapper pDay in householdDay.PersonDays) {
					var tInJoint = pDay.Tours.Find(t => t.JointTourSequence == tour.JointTourSequence);
					if (!(tInJoint == null)) {
						// set jointTour time window
						timeWindow.IncorporateAnotherTimeWindow(tInJoint.PersonDay.TimeWindow);
					}
				}
			}
			else if (tour.ParentTour == null) {
				timeWindow.IncorporateAnotherTimeWindow(personDay.TimeWindow);
			}

			timeWindow.SetBusyMinutes(Constants.Time.END_OF_RELEVANT_WINDOW, Constants.Time.MINUTES_IN_A_DAY + 1);

			var maxAvailableMinutes =
				(tour.JointTourSequence > 0 || tour.ParentTour == null)
				? timeWindow.MaxAvailableMinutesAfter(Constants.Time.FIVE_AM)
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
				var personDay = _tour.PersonDay;
				var householdHasChildren = household.HasChildren;
				var householdHasNoChildren = householdHasChildren ? false : true;

				var destinationParcel = ChoiceModelFactory.Parcels[sampleItem.ParcelId];


				int jointTourFlag = (_tour.JointTourSequence > 0).ToFlag();


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
						case Constants.Purpose.WORK: {
								var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.WORK_TOUR_MODE_MODEL);
								var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.WORK_TOUR_MODE_MODEL);
								var nestedAlternative = HWorkTourModeModel.RunNested(personDay, household.ResidenceParcel, destinationParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);
								tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();

								break;
							}
						case Constants.Purpose.ESCORT: {
								var nestedAlternative = HEscortTourModeModel.RunNested(_tour, destinationParcel);
								tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();

								break;
							}
						default: {
								var nestedAlternative = HOtherHomeBasedTourModeModel.RunNested(_tour, destinationParcel);
								tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();

								break;
							}
					}
				}
				else {
					var nestedAlternative = HWorkBasedSubtourModeModel.RunNested(_tour, destinationParcel);
					tourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}

				//var purpose = _tour.TourPurposeSegment;
				var carOwnership = person.CarOwnershipSegment;
				var votSegment = _tour.VotALSegment;
				var transitAccess = destinationParcel.TransitAccessSegment();
				//var aggregateLogsum = Global.AggregateLogsums[destinationParcel.ZoneId][purpose][carOwnership][votSegment][transitAccess];
				var aggregateLogsumHomeBased = Global.AggregateLogsums[destinationParcel.ZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votSegment][transitAccess];
				var aggregateLogsumWorkBased = Global.AggregateLogsums[destinationParcel.ZoneId][Constants.Purpose.WORK_BASED][carOwnership][votSegment][transitAccess];

				var distanceFromOrigin = _tour.OriginParcel.DistanceFromOrigin(destinationParcel, _tour.DestinationArrivalTime);
				var piecewiseDistanceFrom5To10Miles = Math.Max(0, Math.Min(distanceFromOrigin - .5, 1 - .5));
				var piecewiseDistanceFrom10MilesToInfinity = Math.Max(0, distanceFromOrigin - 1);
				var piecewiseDistanceFrom0To1Mile = Math.Min(distanceFromOrigin, .10);
				var piecewiseDistanceFrom1To5Miles = Math.Max(0, Math.Min(distanceFromOrigin - .1, .5 - .1));
				var piecewiseDistanceFrom1To3AndAHalfMiles = Math.Max(0, Math.Min(distanceFromOrigin - .1, .35 - .1));
				var piecewiseDistanceFrom3AndAHalfTo10Miles = Math.Max(0, Math.Min(distanceFromOrigin - .35, 1 - .35));
				var distanceFromOriginLog = Math.Log(1 + distanceFromOrigin);
				var distanceFromWorkLog = person.UsualWorkParcel.DistanceFromWorkLog(destinationParcel, 1);
				var distanceFromSchoolLog = person.UsualSchoolParcel.DistanceFromSchoolLog(destinationParcel, 1);

				var timePressure = Math.Log(1 - fastestTravelTime / _maxAvailableMinutes);

				// log transforms of buffers for Neighborhood effects
				var logOfOnePlusEmploymentEducationBuffer1 = Math.Log(destinationParcel.EmploymentEducationBuffer1 + 1.0);
				var logOfOnePlusEmploymentFoodBuffer1 = Math.Log(destinationParcel.EmploymentFoodBuffer1 + 1.0);
				var logOfOnePlusEmploymentGovernmentBuffer1 = Math.Log(destinationParcel.EmploymentGovernmentBuffer1 + 1.0);
				var logOfOnePlusEmploymentOfficeBuffer1 = Math.Log(destinationParcel.EmploymentOfficeBuffer1 + 1.0);
				var logOfOnePlusEmploymentRetailBuffer1 = Math.Log(destinationParcel.EmploymentRetailBuffer1 + 1.0);
				var logOfOnePlusEmploymentServiceBuffer1 = Math.Log(destinationParcel.EmploymentServiceBuffer1 + 1.0);
				var logOfOnePlusEmploymentMedicalBuffer1 = Math.Log(destinationParcel.EmploymentMedicalBuffer1 + 1.0);
				var logOfOnePlusEmploymentIndustrial_Ag_ConstructionBuffer1 = Math.Log(destinationParcel.EmploymentIndustrialBuffer1 + destinationParcel.EmploymentAgricultureConstructionBuffer1 + 1.0);
				var logOfOnePlusEmploymentTotalBuffer1 = Math.Log(destinationParcel.EmploymentTotalBuffer1 + 1.0);
				var logOfOnePlusHouseholdsBuffer1 = Math.Log(destinationParcel.HouseholdsBuffer1 + 1.0);
				var logOfOnePlusStudentsK12Buffer1 = Math.Log(destinationParcel.StudentsK8Buffer1 + destinationParcel.StudentsHighSchoolBuffer1 + 1.0);
				var logOfOnePlusStudentsUniversityBuffer1 = Math.Log(destinationParcel.StudentsUniversityBuffer1 + 1.0);
				//				var EMPHOU_B = Math.Log(destinationParcel.EmploymentTotalBuffer1 + destinationParcel.HouseholdsBuffer1 + 1.0);

				var logOfOnePlusParkingOffStreetDailySpacesBuffer1 = Math.Log(1 + destinationParcel.ParkingOffStreetPaidDailySpacesBuffer1);
				// connectivity attributes
				var c34Ratio = destinationParcel.C34RatioBuffer1();

				var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership); // exludes no cars
				var noCarCompetitionFlag = AggregateLogsumsCalculator.GetNoCarCompetitionFlag(carOwnership);
				var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);

				alternative.AddUtilityTerm(2, sampleItem.AdjustmentFactor);
				alternative.AddUtilityTerm(3, tourLogsum);

				//subpopulation-specific terms
				alternative.AddUtilityTerm(260, _secondaryFlag * _workOrSchoolPatternFlag * piecewiseDistanceFrom0To1Mile);
				alternative.AddUtilityTerm(261, _secondaryFlag * _workOrSchoolPatternFlag * piecewiseDistanceFrom1To5Miles);
				alternative.AddUtilityTerm(262, _secondaryFlag * _workOrSchoolPatternFlag * piecewiseDistanceFrom5To10Miles);
				alternative.AddUtilityTerm(263, _secondaryFlag * _workOrSchoolPatternFlag * piecewiseDistanceFrom10MilesToInfinity);
				alternative.AddUtilityTerm(264, _secondaryFlag * _otherPatternFlag * piecewiseDistanceFrom0To1Mile);
				alternative.AddUtilityTerm(265, _secondaryFlag * _otherPatternFlag * piecewiseDistanceFrom1To5Miles);
				alternative.AddUtilityTerm(266, _secondaryFlag * _otherPatternFlag * piecewiseDistanceFrom5To10Miles);
				alternative.AddUtilityTerm(267, _secondaryFlag * _otherPatternFlag * piecewiseDistanceFrom10MilesToInfinity);

				alternative.AddUtilityTerm(268, (!_tour.IsHomeBasedTour).ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(269, household.Has0To15KIncome.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(270, household.HasMissingIncome.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(271, person.IsRetiredAdult.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(272, person.IsUniversityStudent.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(273, person.IsChildAge5Through15.ToFlag() * distanceFromOriginLog);
				alternative.AddUtilityTerm(274, person.IsChildUnder5.ToFlag() * distanceFromOriginLog);

				alternative.AddUtilityTerm(275, (_tour.IsHomeBasedTour).ToFlag() * timePressure);
				alternative.AddUtilityTerm(276, (_tour.IsHomeBasedTour).ToFlag() * distanceFromSchoolLog);
				//alternative.AddUtilityTerm(14, distanceFromWorkLog);

				alternative.AddUtilityTerm(277, (carCompetitionFlag + noCarCompetitionFlag) * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
				alternative.AddUtilityTerm(278, noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixInParcel());
				alternative.AddUtilityTerm(279, carCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());
				alternative.AddUtilityTerm(280, noCarCompetitionFlag * destinationParcel.ParkingHourlyEmploymentCommercialMixBuffer1());

				alternative.AddUtilityTerm(281, noCarsFlag * c34Ratio);
				alternative.AddUtilityTerm(282, noCarCompetitionFlag * c34Ratio);
				alternative.AddUtilityTerm(283, (carCompetitionFlag + noCarCompetitionFlag) * logOfOnePlusParkingOffStreetDailySpacesBuffer1);

				alternative.AddUtilityTerm(284, jointTourFlag * piecewiseDistanceFrom0To1Mile);
				alternative.AddUtilityTerm(285, jointTourFlag * piecewiseDistanceFrom1To5Miles);
				alternative.AddUtilityTerm(286, jointTourFlag * piecewiseDistanceFrom5To10Miles);
				alternative.AddUtilityTerm(287, jointTourFlag * piecewiseDistanceFrom10MilesToInfinity);


				switch (_tour.DestinationPurpose) {
					case Constants.Purpose.WORK:
						alternative.AddUtilityTerm(10, piecewiseDistanceFrom0To1Mile);
						alternative.AddUtilityTerm(11, piecewiseDistanceFrom1To3AndAHalfMiles);
						alternative.AddUtilityTerm(12, piecewiseDistanceFrom3AndAHalfTo10Miles);
						alternative.AddUtilityTerm(13, piecewiseDistanceFrom10MilesToInfinity);
						alternative.AddUtilityTerm(14, aggregateLogsumWorkBased);
						// Neighborhood
						alternative.AddUtilityTerm(20, logOfOnePlusEmploymentEducationBuffer1);
						alternative.AddUtilityTerm(21, logOfOnePlusEmploymentGovernmentBuffer1);
						alternative.AddUtilityTerm(22, logOfOnePlusEmploymentOfficeBuffer1);
						alternative.AddUtilityTerm(23, logOfOnePlusEmploymentServiceBuffer1);
						alternative.AddUtilityTerm(24, logOfOnePlusEmploymentMedicalBuffer1);
						alternative.AddUtilityTerm(25, logOfOnePlusHouseholdsBuffer1);
						alternative.AddUtilityTerm(26, logOfOnePlusStudentsUniversityBuffer1);
						alternative.AddUtilityTerm(27, logOfOnePlusStudentsK12Buffer1);
						alternative.AddUtilityTerm(28, logOfOnePlusEmploymentIndustrial_Ag_ConstructionBuffer1);

						// Size terms
						alternative.AddUtilityTerm(30, destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(31, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(32, destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(33, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(34, destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(35, destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(36, destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(37, destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtilityTerm(38, destinationParcel.Households);
						alternative.AddUtilityTerm(39, destinationParcel.StudentsK12);
						alternative.AddUtilityTerm(40, destinationParcel.StudentsUniversity);

						break;

					case Constants.Purpose.ESCORT:
						alternative.AddUtilityTerm(50, piecewiseDistanceFrom0To1Mile);
						alternative.AddUtilityTerm(51, piecewiseDistanceFrom1To3AndAHalfMiles);
						alternative.AddUtilityTerm(52, piecewiseDistanceFrom3AndAHalfTo10Miles);
						alternative.AddUtilityTerm(53, piecewiseDistanceFrom10MilesToInfinity);
						alternative.AddUtilityTerm(54, aggregateLogsumHomeBased);

						// Neighborhood
						alternative.AddUtilityTerm(60, householdHasNoChildren.ToFlag() * logOfOnePlusEmploymentGovernmentBuffer1);
						alternative.AddUtilityTerm(61, householdHasNoChildren.ToFlag() * logOfOnePlusHouseholdsBuffer1);
						alternative.AddUtilityTerm(62, householdHasChildren.ToFlag() * logOfOnePlusHouseholdsBuffer1);
						alternative.AddUtilityTerm(63, householdHasChildren.ToFlag() * logOfOnePlusStudentsK12Buffer1);
						alternative.AddUtilityTerm(64, logOfOnePlusEmploymentTotalBuffer1);

						// Size terms
						alternative.AddUtilityTerm(70, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(71, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(72, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(73, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(74, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(75, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(76, (!householdHasChildren).ToFlag() * destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(77, (!householdHasChildren).ToFlag() * (destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction));
						alternative.AddUtilityTerm(78, (!householdHasChildren).ToFlag() * destinationParcel.Households);
						alternative.AddUtilityTerm(79, (!householdHasChildren).ToFlag() * destinationParcel.StudentsK12);

						alternative.AddUtilityTerm(80, householdHasChildren.ToFlag() * destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(81, householdHasChildren.ToFlag() * destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(82, householdHasChildren.ToFlag() * destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(83, householdHasChildren.ToFlag() * destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(84, householdHasChildren.ToFlag() * destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(85, householdHasChildren.ToFlag() * destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(86, householdHasChildren.ToFlag() * destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(87, householdHasChildren.ToFlag() * (destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction));
						alternative.AddUtilityTerm(88, householdHasChildren.ToFlag() * destinationParcel.Households);
						alternative.AddUtilityTerm(89, householdHasChildren.ToFlag() * destinationParcel.StudentsK12);

						break;
					case Constants.Purpose.PERSONAL_BUSINESS:
						alternative.AddUtilityTerm(90, piecewiseDistanceFrom0To1Mile);
						alternative.AddUtilityTerm(91, piecewiseDistanceFrom1To3AndAHalfMiles);
						alternative.AddUtilityTerm(92, piecewiseDistanceFrom3AndAHalfTo10Miles);
						alternative.AddUtilityTerm(93, piecewiseDistanceFrom10MilesToInfinity);
						alternative.AddUtilityTerm(94, aggregateLogsumHomeBased);

						// Neighborhood
						alternative.AddUtilityTerm(100, logOfOnePlusEmploymentEducationBuffer1);
						alternative.AddUtilityTerm(101, logOfOnePlusEmploymentOfficeBuffer1);
						alternative.AddUtilityTerm(102, logOfOnePlusEmploymentServiceBuffer1);
						alternative.AddUtilityTerm(103, logOfOnePlusEmploymentMedicalBuffer1);
						alternative.AddUtilityTerm(104, logOfOnePlusHouseholdsBuffer1);
						alternative.AddUtilityTerm(105, logOfOnePlusStudentsUniversityBuffer1);
						alternative.AddUtilityTerm(106, logOfOnePlusEmploymentGovernmentBuffer1);
						alternative.AddUtilityTerm(107, logOfOnePlusEmploymentRetailBuffer1);

						// Size terms
						alternative.AddUtilityTerm(110, destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(111, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(112, destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(113, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(114, destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(115, destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(116, destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(117, destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtilityTerm(118, destinationParcel.Households);
						alternative.AddUtilityTerm(119, destinationParcel.StudentsK12);

						break;
					case Constants.Purpose.SHOPPING:
						alternative.AddUtilityTerm(120, piecewiseDistanceFrom0To1Mile);
						alternative.AddUtilityTerm(121, piecewiseDistanceFrom1To3AndAHalfMiles);
						alternative.AddUtilityTerm(122, piecewiseDistanceFrom3AndAHalfTo10Miles);
						alternative.AddUtilityTerm(123, piecewiseDistanceFrom10MilesToInfinity);
						alternative.AddUtilityTerm(124, aggregateLogsumHomeBased);

						// Neighborhood
						alternative.AddUtilityTerm(130, logOfOnePlusEmploymentEducationBuffer1);
						alternative.AddUtilityTerm(131, logOfOnePlusEmploymentRetailBuffer1);

						// Size terms
						alternative.AddUtilityTerm(140, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(141, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(142, destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(143, destinationParcel.EmploymentService);

						break;
					case Constants.Purpose.MEAL:
						alternative.AddUtilityTerm(150, piecewiseDistanceFrom0To1Mile);
						alternative.AddUtilityTerm(151, piecewiseDistanceFrom1To3AndAHalfMiles);
						alternative.AddUtilityTerm(152, piecewiseDistanceFrom3AndAHalfTo10Miles);
						alternative.AddUtilityTerm(153, piecewiseDistanceFrom10MilesToInfinity);
						alternative.AddUtilityTerm(154, aggregateLogsumHomeBased);

						// Neighborhood
						alternative.AddUtilityTerm(156, logOfOnePlusEmploymentFoodBuffer1);
						alternative.AddUtilityTerm(157, logOfOnePlusEmploymentRetailBuffer1);
						alternative.AddUtilityTerm(158, logOfOnePlusEmploymentServiceBuffer1);

						// Size terms
						alternative.AddUtilityTerm(160, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(161, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(162, destinationParcel.EmploymentTotal);
						alternative.AddUtilityTerm(163, destinationParcel.Households);

						break;
					case Constants.Purpose.SOCIAL:
						alternative.AddUtilityTerm(170, piecewiseDistanceFrom0To1Mile);
						alternative.AddUtilityTerm(171, piecewiseDistanceFrom1To3AndAHalfMiles);
						alternative.AddUtilityTerm(172, piecewiseDistanceFrom3AndAHalfTo10Miles);
						alternative.AddUtilityTerm(173, piecewiseDistanceFrom10MilesToInfinity);
						alternative.AddUtilityTerm(174, aggregateLogsumHomeBased);

						// Neighborhood
						alternative.AddUtilityTerm(180, logOfOnePlusEmploymentOfficeBuffer1);
						alternative.AddUtilityTerm(181, logOfOnePlusEmploymentServiceBuffer1);
						alternative.AddUtilityTerm(182, logOfOnePlusHouseholdsBuffer1);
						alternative.AddUtilityTerm(183, logOfOnePlusStudentsK12Buffer1);
						alternative.AddUtilityTerm(184, logOfOnePlusStudentsUniversityBuffer1);
						alternative.AddUtilityTerm(185, logOfOnePlusEmploymentTotalBuffer1);

						// Size terms
						alternative.AddUtilityTerm(190, destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(191, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(192, destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(193, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(194, destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(195, destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(196, destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(197, destinationParcel.Households);
						alternative.AddUtilityTerm(198, destinationParcel.StudentsUniversity);
						alternative.AddUtilityTerm(199, destinationParcel.StudentsK12);

						break;
					case Constants.Purpose.RECREATION:
						alternative.AddUtilityTerm(200, piecewiseDistanceFrom0To1Mile);
						alternative.AddUtilityTerm(201, piecewiseDistanceFrom1To3AndAHalfMiles);
						alternative.AddUtilityTerm(202, piecewiseDistanceFrom3AndAHalfTo10Miles);
						alternative.AddUtilityTerm(203, piecewiseDistanceFrom10MilesToInfinity);
						alternative.AddUtilityTerm(204, aggregateLogsumHomeBased);

						// Neighborhood
						alternative.AddUtilityTerm(210, logOfOnePlusEmploymentOfficeBuffer1);
						alternative.AddUtilityTerm(211, logOfOnePlusEmploymentServiceBuffer1);
						alternative.AddUtilityTerm(212, logOfOnePlusHouseholdsBuffer1);
						alternative.AddUtilityTerm(213, logOfOnePlusStudentsK12Buffer1);
						alternative.AddUtilityTerm(214, logOfOnePlusStudentsUniversityBuffer1);
						alternative.AddUtilityTerm(215, logOfOnePlusEmploymentTotalBuffer1);

						// Size terms
						alternative.AddUtilityTerm(220, destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(221, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(222, destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(223, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(224, destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(225, destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(226, destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(227, destinationParcel.Households);
						alternative.AddUtilityTerm(228, destinationParcel.StudentsUniversity);
						alternative.AddUtilityTerm(229, destinationParcel.StudentsK12);

						break;

					case Constants.Purpose.MEDICAL:
						alternative.AddUtilityTerm(230, piecewiseDistanceFrom0To1Mile);
						alternative.AddUtilityTerm(231, piecewiseDistanceFrom1To3AndAHalfMiles);
						alternative.AddUtilityTerm(232, piecewiseDistanceFrom3AndAHalfTo10Miles);
						alternative.AddUtilityTerm(233, piecewiseDistanceFrom10MilesToInfinity);
						alternative.AddUtilityTerm(234, aggregateLogsumHomeBased);

						// Neighborhood
						alternative.AddUtilityTerm(240, logOfOnePlusEmploymentEducationBuffer1);
						alternative.AddUtilityTerm(241, logOfOnePlusEmploymentOfficeBuffer1);
						alternative.AddUtilityTerm(242, logOfOnePlusEmploymentServiceBuffer1);
						alternative.AddUtilityTerm(243, logOfOnePlusEmploymentMedicalBuffer1);
						alternative.AddUtilityTerm(244, logOfOnePlusHouseholdsBuffer1);
						alternative.AddUtilityTerm(245, logOfOnePlusStudentsUniversityBuffer1);
						alternative.AddUtilityTerm(246, logOfOnePlusEmploymentGovernmentBuffer1);
						alternative.AddUtilityTerm(247, logOfOnePlusEmploymentRetailBuffer1);

						// Size terms
						alternative.AddUtilityTerm(250, destinationParcel.EmploymentEducation);
						alternative.AddUtilityTerm(251, destinationParcel.EmploymentFood);
						alternative.AddUtilityTerm(252, destinationParcel.EmploymentGovernment);
						alternative.AddUtilityTerm(253, destinationParcel.EmploymentOffice);
						alternative.AddUtilityTerm(254, destinationParcel.EmploymentRetail);
						alternative.AddUtilityTerm(255, destinationParcel.EmploymentService);
						alternative.AddUtilityTerm(256, destinationParcel.EmploymentMedical);
						alternative.AddUtilityTerm(257, destinationParcel.EmploymentIndustrial + destinationParcel.EmploymentAgricultureConstruction);
						alternative.AddUtilityTerm(258, destinationParcel.Households);
						alternative.AddUtilityTerm(259, destinationParcel.StudentsK12);

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