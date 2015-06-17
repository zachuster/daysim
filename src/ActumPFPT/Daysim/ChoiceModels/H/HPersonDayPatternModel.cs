// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Threading.Tasks;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Interfaces;
using Daysim.ModelRunners;

namespace Daysim.ChoiceModels.H {
	public static class HPersonDayPatternModel {
		private const string CHOICE_MODEL_NAME = "HPersonDayPatternModel";
		private const int TOTAL_ALTERNATIVES = 3151;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 1750;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		private static readonly object _lock = new object();
		private static DayPattern[] _dayPatterns;

		private static void Initialize() {
			lock (_lock) {
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null) {
					return;
				}

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME,
													  Global.GetInputPath(Global.Configuration.HouseholdPersonDayPatternModelCoefficients), TOTAL_ALTERNATIVES,
													  TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static void Run(IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay) {
			
			if (householdDay.Household.Id == 4110 && personDay.Person.Sequence == 1 && personDay.Day == 1) {
				bool testbreak = true;
			}

			
			
			if (personDay == null) {
				throw new ArgumentNullException("personDay");
			}

			Initialize();

			personDay.ResetRandom(948);

			if (Global.Configuration.IsInEstimationMode) {

				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					DayPattern dayPattern = new DayPattern(personDay);
					if (dayPattern.EscortTours > 0 && personDay.CreatedEscortTours == 0) {
						personDay.CreatedEscortTours++;
					}
					if (dayPattern.PersonalBusinessTours > 0 && personDay.CreatedPersonalBusinessTours == 0) {
						personDay.CreatedPersonalBusinessTours++;
					}
					if (dayPattern.ShoppingTours > 0 && personDay.CreatedShoppingTours == 0) {
						personDay.CreatedShoppingTours++;
					}
					if (dayPattern.MealTours > 0 && personDay.CreatedMealTours == 0) {
						personDay.CreatedMealTours++;
					}
					if (dayPattern.SocialTours > 0 && personDay.CreatedSocialTours == 0) {
						personDay.CreatedSocialTours++;
					}
					if (dayPattern.RecreationTours > 0 && personDay.CreatedRecreationTours == 0) {
						personDay.CreatedRecreationTours++;
					}
					if (dayPattern.MedicalTours > 0 && personDay.CreatedMedicalTours == 0) {
						personDay.CreatedMedicalTours++;
					}
					return;
				}
			}

			if (personDay.Household.Id == 2065 && personDay.Person.Sequence == 1 && householdDay.AttemptedSimulations > 8) {
				bool testbreak = true;
			}

			InitializeDayPatterns(personDay);

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(personDay.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				DayPattern dayPattern = new DayPattern(personDay);
				RunModel(choiceProbabilityCalculator, personDay, householdDay, dayPattern);
				if (dayPattern.EscortTours > 0 && personDay.CreatedEscortTours == 0) {
					personDay.CreatedEscortTours++;
				}
				if (dayPattern.PersonalBusinessTours > 0 && personDay.CreatedPersonalBusinessTours == 0) {
					personDay.CreatedPersonalBusinessTours++;
				}
				if (dayPattern.ShoppingTours > 0 && personDay.CreatedShoppingTours == 0) {
					personDay.CreatedShoppingTours++;
				}
				if (dayPattern.MealTours > 0 && personDay.CreatedMealTours == 0) {
					personDay.CreatedMealTours++;
				}
				if (dayPattern.SocialTours > 0 && personDay.CreatedSocialTours == 0) {
					personDay.CreatedSocialTours++;
				}
				if (dayPattern.RecreationTours > 0 && personDay.CreatedRecreationTours == 0) {
					personDay.CreatedRecreationTours++;
				}
				if (dayPattern.MedicalTours > 0 && personDay.CreatedMedicalTours == 0) {
					personDay.CreatedMedicalTours++;
				}
				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				if (personDay.Person.UsualWorkParcelId != Constants.OUT_OF_REGION_PARCEL_ID && personDay.Person.UsualSchoolParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {


					RunModel(choiceProbabilityCalculator, personDay, householdDay);
					var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(personDay.Household.RandomUtility);
					if (chosenAlternative == null) {
						personDay.IsValid = false;
						householdDay.IsValid = false;
						return;
					}
					
					var dayPattern = (DayPattern) chosenAlternative.Choice;

					if (dayPattern.EscortTours > 0 && personDay.CreatedEscortTours == 0) {
						personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, Constants.Purpose.ESCORT);
						personDay.CreatedEscortTours++;
					}
					if (dayPattern.PersonalBusinessTours > 0 && personDay.CreatedPersonalBusinessTours == 0) {
						personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, Constants.Purpose.PERSONAL_BUSINESS);
						personDay.CreatedPersonalBusinessTours++;
					}
					if (dayPattern.ShoppingTours > 0 && personDay.CreatedShoppingTours == 0) {
						personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, Constants.Purpose.SHOPPING);
						personDay.CreatedShoppingTours++;
					}
					if (dayPattern.MealTours > 0 && personDay.CreatedMealTours == 0) {
						personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, Constants.Purpose.MEAL);
						personDay.CreatedMealTours++;
					}
					if (dayPattern.SocialTours > 0 && personDay.CreatedSocialTours == 0) {
						personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, Constants.Purpose.SOCIAL);
						personDay.CreatedSocialTours++;
					}
					if (dayPattern.RecreationTours > 0 && personDay.CreatedRecreationTours == 0) {
						personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, Constants.Purpose.RECREATION);
						personDay.CreatedRecreationTours++;
					}
					if (dayPattern.MedicalTours > 0 && personDay.CreatedMedicalTours == 0) {
						personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, Constants.Purpose.MEDICAL);
						personDay.CreatedMedicalTours++;
					}

					personDay.EscortStops = dayPattern.EscortStops;
					personDay.PersonalBusinessStops = dayPattern.PersonalBusinessStops;
					personDay.ShoppingStops = dayPattern.ShoppingStops;
					personDay.MealStops = dayPattern.MealStops;
					personDay.SocialStops = dayPattern.SocialStops;
					personDay.RecreationTours = dayPattern.RecreationStops;
					personDay.MedicalStops = dayPattern.MedicalStops;

				}
			}
		}

		private static void InitializeDayPatterns(IPersonDayWrapper personDay) {
			//			if (_dayPatterns != null) {
			//				return;
			//			}

			var workTours = personDay.WorkTours > 0 ? 1 : 0;
			var schoolTours = personDay.SchoolTours > 0 ? 1 : 0;
			var workStops = personDay.WorkStops > 0 ? 1 : 0;
			var schoolStops = personDay.SchoolStops > 0 ? 1 : 0;
			var priorEscortTours = (personDay.EscortFullHalfTours > 0 || personDay.EscortJointTours > 0) ? 1 : 0;
			var priorPersonalBusinessTours = personDay.PersonalBusinessJointTours > 0 ? 1 : 0;
			var priorShoppingTours = personDay.ShoppingJointTours > 0 ? 1 : 0;
			var priorMealTours = personDay.MealJointTours > 0 ? 1 : 0;
			var priorSocialTours = personDay.SocialJointTours > 0 ? 1 : 0;
			var priorRecreationTours = personDay.RecreationJointTours > 0 ? 1 : 0;
			var priorMedicalTours = personDay.MedicalJointTours > 0 ? 1 : 0;

			_dayPatterns = new DayPattern[TOTAL_ALTERNATIVES];

			var alternativeIndex = -1;

			for (var recreationTours = 0; recreationTours <= 1; recreationTours++) {
				for (var medicalTours = 0; medicalTours <= 1; medicalTours++) {
					for (var escortTours = 0; escortTours <= 1; escortTours++) {
						for (var personalBusinessTours = 0; personalBusinessTours <= 1; personalBusinessTours++) {
							for (var shoppingTours = 0; shoppingTours <= 1; shoppingTours++) {
								for (var mealTours = 0; mealTours <= 1; mealTours++) {
									for (var socialTours = 0; socialTours <= 1; socialTours++) {
										for (var recreationStops = 0; recreationStops <= 1; recreationStops++) {
											for (var medicalStops = 0; medicalStops <= 1; medicalStops++) {
												for (var escortStops = 0; escortStops <= 1; escortStops++) {
													for (var personalBusinessStops = 0; personalBusinessStops <= 1; personalBusinessStops++) {
														for (var shoppingStops = 0; shoppingStops <= 1; shoppingStops++) {
															for (var mealStops = 0; mealStops <= 1; mealStops++) {
																for (var socialStops = 0; socialStops <= 1; socialStops++) {
																	var totalNonMandatoryTourPurposes = recreationTours + medicalTours + escortTours + personalBusinessTours + shoppingTours + mealTours + socialTours;
																	var totalNonMandatoryStopPurposes = recreationStops + medicalStops + escortStops + personalBusinessStops + shoppingStops + mealStops + socialStops;
																	var totalTourPurposes = totalNonMandatoryTourPurposes + workTours + schoolTours;
																	var totalStopPurposes = totalNonMandatoryStopPurposes + workStops + schoolStops;
																	var totalDayPurposes = Math.Min(workTours, workStops) + Math.Min(schoolTours, schoolStops) + Math.Min(escortTours, escortStops) + Math.Min(personalBusinessTours, personalBusinessStops) + Math.Min(shoppingTours, shoppingStops) + Math.Min(mealTours, mealStops) + Math.Min(socialTours, socialStops) + Math.Min(recreationTours, recreationStops) + Math.Min(medicalTours, medicalStops);


																	// checks for:
																	// three tours or less
																	// four stops or less
																	// five stops total or less
																	if (totalNonMandatoryTourPurposes > 3 || totalNonMandatoryStopPurposes > 4 || totalNonMandatoryTourPurposes + totalNonMandatoryStopPurposes > 5) {
																		continue;
																	}

																	alternativeIndex++; // next alternative

																	var tours = new int[Constants.Purpose.TOTAL_PURPOSES];

																	tours[Constants.Purpose.WORK] = workTours;
																	tours[Constants.Purpose.SCHOOL] = schoolTours;
																	tours[Constants.Purpose.ESCORT] = escortTours;
																	tours[Constants.Purpose.PERSONAL_BUSINESS] = personalBusinessTours;
																	tours[Constants.Purpose.SHOPPING] = shoppingTours;
																	tours[Constants.Purpose.MEAL] = mealTours;
																	tours[Constants.Purpose.SOCIAL] = socialTours;
																	tours[Constants.Purpose.RECREATION] = recreationTours;
																	tours[Constants.Purpose.MEDICAL] = medicalTours;

																	var stops = new int[Constants.Purpose.TOTAL_PURPOSES];

																	stops[Constants.Purpose.WORK] = workStops;
																	stops[Constants.Purpose.SCHOOL] = schoolStops;
																	stops[Constants.Purpose.ESCORT] = escortStops;
																	stops[Constants.Purpose.PERSONAL_BUSINESS] = personalBusinessStops;
																	stops[Constants.Purpose.SHOPPING] = shoppingStops;
																	stops[Constants.Purpose.MEAL] = mealStops;
																	stops[Constants.Purpose.SOCIAL] = socialStops;
																	stops[Constants.Purpose.RECREATION] = recreationStops;
																	stops[Constants.Purpose.MEDICAL] = medicalStops;

																	bool available = totalNonMandatoryStopPurposes > 0 && totalTourPurposes == 0 ? false :
																		totalStopPurposes == 0 && totalTourPurposes == 0 ? false :
																		priorEscortTours > 0 && escortTours == 0 ? false :
																		priorPersonalBusinessTours > 0 && personalBusinessTours == 0 ? false :
																		priorShoppingTours > 0 && shoppingTours == 0 ? false :
																		priorMealTours > 0 && mealTours == 0 ? false :
																		priorSocialTours > 0 && socialTours == 0 ? false :
																		priorRecreationTours > 0 && recreationTours == 0 ? false :
																		priorMedicalTours > 0 && medicalTours == 0 ? false :
																		//totalTourPurposes > 3 ? false :
																		//totalStopPurposes > 4 ? false :
																		//totalTourPurposes + totalStopPurposes > 5 ? false :
																		totalTourPurposes > 3 || totalStopPurposes > 4 || totalTourPurposes + totalStopPurposes > 5 ? false :
																		Math.Min(totalStopPurposes, 1) > totalTourPurposes ? false :
																		true;

																	_dayPatterns[alternativeIndex] = new DayPattern(tours, totalTourPurposes, stops, totalStopPurposes, totalDayPurposes, available);
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}

		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay, DayPattern choice = null) {
			var household = personDay.Household;
			var residenceParcel = household.ResidenceParcel;
			var person = personDay.Person;

			var carsPerDriver = household.CarsPerDriver;
			var mixedDensity = residenceParcel.MixedUse3Index2();
			var intersectionDensity = residenceParcel.IntersectionDensity34Minus1Buffer2();

			var usualModeNotSOVFlag = (person.IsWorker && person.UsualModeToWork != Constants.Mode.SOV).ToFlag();
			var jointToursFlag = personDay.JointTours > 0 ? 1 : 0;
			var proxyCompletedDiaryFlag = person.ProxyResponse > 0 ? 1 : 0;

			var purposeLogsums = new double[Constants.Purpose.TOTAL_PURPOSES + 10];
			var atUsualLogsums = new double[3];
			var carOwnership = person.CarOwnershipSegment;
			var votSegment = person.Household.VotALSegment;
			var transitAccess = residenceParcel.TransitAccessSegment();

			if (person.UsualWorkParcel == null || person.UsualWorkParcelId == household.ResidenceParcelId) {
				purposeLogsums[Constants.Purpose.WORK] = 0;
			}
			else {
				var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.WORK_TOUR_MODE_MODEL);
				var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.WORK_TOUR_MODE_MODEL);
				var nestedAlternative = HWorkTourModeModel.RunNested(personDay, residenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);

				purposeLogsums[Constants.Purpose.WORK] = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				atUsualLogsums[Constants.Purpose.WORK] = Global.AggregateLogsums[person.UsualWorkParcel.ZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votSegment][person.UsualWorkParcel.TransitAccessSegment()];
			}

			if (person.UsualSchoolParcel == null || person.UsualSchoolParcelId == household.ResidenceParcelId) {
				purposeLogsums[Constants.Purpose.SCHOOL] = 0;
			}
			else {
				var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
				var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
				var nestedAlternative = HSchoolTourModeModel.RunNested(personDay, residenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);

				purposeLogsums[Constants.Purpose.SCHOOL] = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				atUsualLogsums[Constants.Purpose.SCHOOL] = Global.AggregateLogsums[person.UsualSchoolParcel.ZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votSegment][person.UsualSchoolParcel.TransitAccessSegment()];
			}

			var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votSegment][transitAccess];

			purposeLogsums[Constants.Purpose.ESCORT] = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.ESCORT][carOwnership][votSegment][transitAccess];
			purposeLogsums[Constants.Purpose.PERSONAL_BUSINESS] = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.PERSONAL_BUSINESS][carOwnership][votSegment][transitAccess];
			purposeLogsums[Constants.Purpose.SHOPPING] = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.SHOPPING][carOwnership][votSegment][transitAccess];
			purposeLogsums[Constants.Purpose.MEAL] = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.MEAL][carOwnership][votSegment][transitAccess];
			purposeLogsums[Constants.Purpose.SOCIAL] = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.SOCIAL][carOwnership][votSegment][transitAccess];
			purposeLogsums[Constants.Purpose.RECREATION] = compositeLogsum;
			purposeLogsums[Constants.Purpose.MEDICAL] = compositeLogsum;

			for (var xPurpose = Constants.Purpose.ESCORT; xPurpose <= Constants.Purpose.MEDICAL + 10; xPurpose++) {
				// extra components 1-5 are for 2,3,4,5,6 tour purposes
				// extra components 6-10 are for 2,3,4,5,6 stop puroposes

				// recode purpose to match coefficients
				var purpose = xPurpose <= Constants.Purpose.MEDICAL ? xPurpose :
					xPurpose <= Constants.Purpose.MEDICAL + 5 ? Constants.Purpose.MEDICAL + 1 :
					Constants.Purpose.MEDICAL + 2;

				// get correct multiplier on coefficients.
				var xMultiplier = xPurpose <= Constants.Purpose.MEDICAL ? 1.0 :
					xPurpose <= Constants.Purpose.MEDICAL + 5 ? Math.Log(xPurpose - Constants.Purpose.MEDICAL + 1) :
					Math.Log(xPurpose - Constants.Purpose.MEDICAL - 5 + 1);

				choiceProbabilityCalculator.CreateUtilityComponent(xPurpose);
				var component = choiceProbabilityCalculator.GetUtilityComponent(xPurpose);

				component.AddUtilityTerm(100 * purpose + 1, xMultiplier * person.IsFulltimeWorker.ToFlag());
				component.AddUtilityTerm(100 * purpose + 2, xMultiplier * person.IsPartTimeWorker.ToFlag());
				component.AddUtilityTerm(100 * purpose + 3, xMultiplier * person.IsRetiredAdult.ToFlag());
				component.AddUtilityTerm(100 * purpose + 4, xMultiplier * person.IsNonworkingAdult.ToFlag());
				component.AddUtilityTerm(100 * purpose + 5, xMultiplier * person.IsUniversityStudent.ToFlag());
				component.AddUtilityTerm(100 * purpose + 6, xMultiplier * person.IsDrivingAgeStudent.ToFlag());
				component.AddUtilityTerm(100 * purpose + 7, xMultiplier * person.IsChildAge5Through15.ToFlag());
				component.AddUtilityTerm(100 * purpose + 8, xMultiplier * person.IsChildUnder5.ToFlag());
				component.AddUtilityTerm(100 * purpose + 9, xMultiplier * household.Has0To25KIncome.ToFlag());
				component.AddUtilityTerm(100 * purpose + 10, xMultiplier * household.Has25To45KIncome.ToFlag());
				component.AddUtilityTerm(100 * purpose + 11, xMultiplier * household.Has75KPlusIncome.ToFlag());
				component.AddUtilityTerm(100 * purpose + 12, xMultiplier * carsPerDriver);
				component.AddUtilityTerm(100 * purpose + 13, xMultiplier * person.IsOnlyAdult.ToFlag());
				component.AddUtilityTerm(100 * purpose + 14, xMultiplier * person.IsOnlyFullOrPartTimeWorker.ToFlag());
				component.AddUtilityTerm(100 * purpose + 15, xMultiplier * 0);
				component.AddUtilityTerm(100 * purpose + 16, xMultiplier * person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * (!household.HasChildrenUnder16).ToFlag());
				component.AddUtilityTerm(100 * purpose + 17, xMultiplier * person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenUnder5.ToFlag());
				component.AddUtilityTerm(100 * purpose + 18, xMultiplier * person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenAge5Through15.ToFlag());
				component.AddUtilityTerm(100 * purpose + 19, xMultiplier * person.IsMale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenUnder5.ToFlag());
				component.AddUtilityTerm(100 * purpose + 20, xMultiplier * person.IsMale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenAge5Through15.ToFlag());
				component.AddUtilityTerm(100 * purpose + 21, xMultiplier * person.AgeIsBetween18And25.ToFlag());
				component.AddUtilityTerm(100 * purpose + 22, xMultiplier * person.AgeIsBetween26And35.ToFlag());
				component.AddUtilityTerm(100 * purpose + 23, xMultiplier * person.AgeIsBetween51And65.ToFlag());
				component.AddUtilityTerm(100 * purpose + 24, xMultiplier * person.WorksAtHome.ToFlag());
				component.AddUtilityTerm(100 * purpose + 25, xMultiplier * mixedDensity);
				component.AddUtilityTerm(100 * purpose + 26, xMultiplier * intersectionDensity);
				component.AddUtilityTerm(100 * purpose + 27, xMultiplier * purposeLogsums[purpose]);
				component.AddUtilityTerm(100 * purpose + 28, xMultiplier * person.TransitPassOwnershipFlag);
				component.AddUtilityTerm(100 * purpose + 29, xMultiplier * usualModeNotSOVFlag);
				component.AddUtilityTerm(100 * purpose + 30, xMultiplier * personDay.WorksAtHomeFlag);
				component.AddUtilityTerm(100 * purpose + 31, xMultiplier * jointToursFlag);

			}

			// tour utility
			const int tourComponentIndex = 25;
			choiceProbabilityCalculator.CreateUtilityComponent(tourComponentIndex);
			var tourComponent = choiceProbabilityCalculator.GetUtilityComponent(tourComponentIndex);
			tourComponent.AddUtilityTerm(1701, carsPerDriver);
			tourComponent.AddUtilityTerm(1702, person.WorksAtHome.ToFlag());
			tourComponent.AddUtilityTerm(1703, mixedDensity);
			tourComponent.AddUtilityTerm(1704, mixedDensity * person.IsChildAge5Through15.ToFlag());
			tourComponent.AddUtilityTerm(1705, compositeLogsum);
			tourComponent.AddUtilityTerm(1706, person.TransitPassOwnershipFlag);

			// stop utility
			const int stopComponentIndex = 26;
			choiceProbabilityCalculator.CreateUtilityComponent(stopComponentIndex);
			var stopComponent = choiceProbabilityCalculator.GetUtilityComponent(stopComponentIndex);
			stopComponent.AddUtilityTerm(1711, carsPerDriver);
			stopComponent.AddUtilityTerm(1712, person.WorksAtHome.ToFlag());
			stopComponent.AddUtilityTerm(1713, mixedDensity);
			stopComponent.AddUtilityTerm(1714, mixedDensity * person.IsChildAge5Through15.ToFlag());
			stopComponent.AddUtilityTerm(1715, compositeLogsum);
			stopComponent.AddUtilityTerm(1716, person.TransitPassOwnershipFlag);

			for (var alternativeIndex = 0; alternativeIndex < TOTAL_ALTERNATIVES; alternativeIndex++) {

				var dayPattern = _dayPatterns[alternativeIndex];
				var available = dayPattern.Available;
				var alternative = choiceProbabilityCalculator.GetAlternative(alternativeIndex, available, choice != null && choice.Equals(dayPattern));

				if (!Global.Configuration.IsInEstimationMode && !alternative.Available) {
					continue;
				}

				alternative.Choice = dayPattern;
				bool workSchoolPattern = (dayPattern.Tours[Constants.Purpose.WORK] == 1 
					|| dayPattern.Tours[Constants.Purpose.SCHOOL] == 1);

				// components for the purposes
				for (var purpose = Constants.Purpose.ESCORT; purpose <= Constants.Purpose.MEDICAL; purpose++) {
					if (dayPattern.Tours[purpose] > 0 || dayPattern.Stops[purpose] > 0) {
						alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(purpose));

						if (dayPattern.Tours[purpose] > 0) {
							alternative.AddUtilityTerm(100 * purpose + 50, 1); // tour purpose ASC
							alternative.AddUtilityTerm(100 * purpose + 51, workSchoolPattern.ToFlag() * purposeLogsums[purpose]); // tour purpose logsum on w/s pattern
							alternative.AddUtilityTerm(100 * purpose + 52, (!workSchoolPattern).ToFlag() * purposeLogsums[purpose]); // tour purpose logsum on non-w/s pattern
						}

						if (dayPattern.Stops[purpose] > 0) {
							alternative.AddUtilityTerm(100 * purpose + 60, 1); // stop purpose ASC
							alternative.AddUtilityTerm(100 * purpose + 61, workSchoolPattern.ToFlag() * purposeLogsums[purpose]); // stop purpose logsum on w/s pattern
							alternative.AddUtilityTerm(100 * purpose + 62, (!workSchoolPattern).ToFlag() * purposeLogsums[purpose]); // stop purpose logsum on non-w/s pattern
						}
						if (Global.Configuration.IsInEstimationMode) {
							alternative.AddUtilityTerm(100 * purpose + 70, 1 - person.PaperDiary);
							alternative.AddUtilityTerm(100 * purpose + 71, proxyCompletedDiaryFlag);
						}
					}
				}

				// multiple tour purposes component
				if (dayPattern.TotalTourPurposes > 1) {
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(Math.Min(14, Constants.Purpose.MEDICAL + (dayPattern.TotalTourPurposes - 1))));
				}

				// multiple stop purposes component
				if (dayPattern.TotalStopPurposes > 1) {
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(Math.Min(19, Constants.Purpose.MEDICAL + 5 + (dayPattern.TotalStopPurposes - 1))));
				}

				for (var tourPurpose = Constants.Purpose.WORK; tourPurpose <= Constants.Purpose.MEDICAL; tourPurpose++) {
					for (var stopPurpose = Constants.Purpose.WORK; stopPurpose <= Constants.Purpose.MEDICAL - 1; stopPurpose++) {
						if (tourPurpose > Constants.Purpose.SCHOOL && stopPurpose <= Constants.Purpose.SCHOOL) {
							continue;
						}

						if (dayPattern.Tours[tourPurpose] > 0 && dayPattern.Stops[stopPurpose] > 0) {
							alternative.AddUtilityTerm(1200 + 10 * tourPurpose + stopPurpose, 1); // tour-stop comb. utility
						}
					}
				}

				for (var tourPurpose = Constants.Purpose.WORK; tourPurpose <= Constants.Purpose.SCHOOL; tourPurpose++) {
					if (dayPattern.Tours[tourPurpose] == 1 && dayPattern.TotalStopPurposes >= 1) {
						alternative.AddUtilityTerm(1300 + 10 * tourPurpose, purposeLogsums[tourPurpose]); // usual location logsum x presence of stops in work or school pattern
						alternative.AddUtilityTerm(1300 + 10 * tourPurpose + 1, compositeLogsum); // home aggregate logsum x  presence of stops in work or school pattern
						alternative.AddUtilityTerm(1300 + 10 * tourPurpose + 2, atUsualLogsums[tourPurpose]); // at usual location aggregate logsum x  presence of stops in work or school pattern
					}
				}
				for (var tourPurpose = Constants.Purpose.WORK; tourPurpose <= Constants.Purpose.SCHOOL; tourPurpose++) {
					if (dayPattern.Tours[tourPurpose] == 1 && dayPattern.TotalTourPurposes > 1) {
						alternative.AddUtilityTerm(1300 + 10 * tourPurpose + 3, purposeLogsums[tourPurpose]); // usual location logsum x presence of other tour purposes in work or school pattern
						alternative.AddUtilityTerm(1300 + 10 * tourPurpose + 4, compositeLogsum); // home aggregate logsum x  presence of other tour purposes in work or school pattern
						alternative.AddUtilityTerm(1300 + 10 * tourPurpose + 5, atUsualLogsums[tourPurpose]); // at usual location aggregate logsum x  presence of other tour purposes in work or school pattern
					}
				}
				for (var tourPurpose = Constants.Purpose.WORK; tourPurpose <= Constants.Purpose.MEDICAL - 2; tourPurpose++) {
					for (var tourPurpose2 = tourPurpose + 1; tourPurpose2 <= Constants.Purpose.MEDICAL; tourPurpose2++) {
						if (dayPattern.Tours[tourPurpose] > 0 && dayPattern.Tours[tourPurpose2] > 0) {
							alternative.AddUtilityTerm(1400 + 10 * tourPurpose + tourPurpose2, 1); // tour-tour comb. utility
						}
					}
				}

				for (var stopPurpose = Constants.Purpose.WORK; stopPurpose <= Constants.Purpose.MEDICAL - 2; stopPurpose++) {
					for (var stopPurpose2 = stopPurpose + 1; stopPurpose2 <= Constants.Purpose.MEDICAL; stopPurpose2++) {
						if (dayPattern.Stops[stopPurpose] > 0 && dayPattern.Stops[stopPurpose2] > 0) {
							alternative.AddUtilityTerm(1500 + 10 * stopPurpose + stopPurpose2, 1); // stop-stop comb. utility
						}
					}
				}

				if (dayPattern.TotalTourPurposes > 0 && dayPattern.TotalStopPurposes > 0) {
					var totalStopPurposes = dayPattern.TotalStopPurposes;

					if (totalStopPurposes > 3) {
						totalStopPurposes = 3;
					}

					alternative.AddUtilityTerm(1600 + 10 * dayPattern.TotalTourPurposes + totalStopPurposes, 1); // nttour-ntstop utility
				}
				if (dayPattern.TotalTourPurposes - dayPattern.Tours[Constants.Purpose.WORK] - dayPattern.Tours[Constants.Purpose.SCHOOL] > 0) {
					// tour utility
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(tourComponentIndex));
				}
				if (dayPattern.TotalStopPurposes - dayPattern.Stops[Constants.Purpose.WORK] - dayPattern.Stops[Constants.Purpose.SCHOOL] > 0) {
					// stop utility
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(stopComponentIndex));
				}
				bool workPattern = dayPattern.Tours[Constants.Purpose.WORK] >= 1; 
				bool schoolPattern = dayPattern.Tours[Constants.Purpose.SCHOOL] >= 1;
				bool nonCommutePattern = !workPattern && !schoolPattern;
				bool patternHasTwoPlusTourPurposes = dayPattern.TotalTourPurposes >= 2;
				bool patternHasOnePlusStopPurposes = dayPattern.TotalStopPurposes >= 1;

				alternative.AddUtilityTerm(101,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * purposeLogsums[Constants.Purpose.WORK]);
				alternative.AddUtilityTerm(102,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * atUsualLogsums[Constants.Purpose.WORK]);
				alternative.AddUtilityTerm(103,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * compositeLogsum);
				alternative.AddUtilityTerm(104,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.EscortTours * purposeLogsums[Constants.Purpose.ESCORT]);
				alternative.AddUtilityTerm(105,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.PersonalBusinessTours * purposeLogsums[Constants.Purpose.PERSONAL_BUSINESS]);
				alternative.AddUtilityTerm(105,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.ShoppingTours * purposeLogsums[Constants.Purpose.SHOPPING]);
				alternative.AddUtilityTerm(106,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.MealTours * purposeLogsums[Constants.Purpose.MEAL]);
				alternative.AddUtilityTerm(106,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.SocialTours * purposeLogsums[Constants.Purpose.SOCIAL]);
				alternative.AddUtilityTerm(106,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.RecreationTours * purposeLogsums[Constants.Purpose.RECREATION]);
				alternative.AddUtilityTerm(104,workPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.MedicalTours * purposeLogsums[Constants.Purpose.MEDICAL]);

				alternative.AddUtilityTerm(111,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * purposeLogsums[Constants.Purpose.WORK]);
				alternative.AddUtilityTerm(112,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * atUsualLogsums[Constants.Purpose.WORK]);
				alternative.AddUtilityTerm(113,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * compositeLogsum);
				alternative.AddUtilityTerm(114,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.EscortStops * purposeLogsums[Constants.Purpose.ESCORT]);
				alternative.AddUtilityTerm(115,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.PersonalBusinessStops * purposeLogsums[Constants.Purpose.PERSONAL_BUSINESS]);
				alternative.AddUtilityTerm(115,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.ShoppingStops * purposeLogsums[Constants.Purpose.SHOPPING]);
				alternative.AddUtilityTerm(116,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.MealStops * purposeLogsums[Constants.Purpose.MEAL]);
				alternative.AddUtilityTerm(116,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.SocialStops * purposeLogsums[Constants.Purpose.SOCIAL]);
				alternative.AddUtilityTerm(116,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.RecreationStops * purposeLogsums[Constants.Purpose.RECREATION]);
				alternative.AddUtilityTerm(114,workPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.MedicalStops * purposeLogsums[Constants.Purpose.MEDICAL]);

				alternative.AddUtilityTerm(121,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * purposeLogsums[Constants.Purpose.SCHOOL]);
				alternative.AddUtilityTerm(122,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * atUsualLogsums[Constants.Purpose.SCHOOL]);
				alternative.AddUtilityTerm(123,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * compositeLogsum);
				alternative.AddUtilityTerm(124,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.EscortTours * purposeLogsums[Constants.Purpose.ESCORT]);
				alternative.AddUtilityTerm(125,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.PersonalBusinessTours * purposeLogsums[Constants.Purpose.PERSONAL_BUSINESS]);
				alternative.AddUtilityTerm(125,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.ShoppingTours * purposeLogsums[Constants.Purpose.SHOPPING]);
				alternative.AddUtilityTerm(126,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.MealTours * purposeLogsums[Constants.Purpose.MEAL]);
				alternative.AddUtilityTerm(126,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.SocialTours * purposeLogsums[Constants.Purpose.SOCIAL]);
				alternative.AddUtilityTerm(126,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.RecreationTours * purposeLogsums[Constants.Purpose.RECREATION]);
				alternative.AddUtilityTerm(124,schoolPattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.MedicalTours * purposeLogsums[Constants.Purpose.MEDICAL]);

				alternative.AddUtilityTerm(131,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * purposeLogsums[Constants.Purpose.SCHOOL]);
				alternative.AddUtilityTerm(132,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * atUsualLogsums[Constants.Purpose.SCHOOL]);
				alternative.AddUtilityTerm(133,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * compositeLogsum);
				alternative.AddUtilityTerm(134,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.EscortStops * purposeLogsums[Constants.Purpose.ESCORT]);
				alternative.AddUtilityTerm(135,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.PersonalBusinessStops * purposeLogsums[Constants.Purpose.PERSONAL_BUSINESS]);
				alternative.AddUtilityTerm(135,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.ShoppingStops * purposeLogsums[Constants.Purpose.SHOPPING]);
				alternative.AddUtilityTerm(136,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.MealStops * purposeLogsums[Constants.Purpose.MEAL]);
				alternative.AddUtilityTerm(136,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.SocialStops * purposeLogsums[Constants.Purpose.SOCIAL]);
				alternative.AddUtilityTerm(136,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.RecreationStops * purposeLogsums[Constants.Purpose.RECREATION]);
				alternative.AddUtilityTerm(134,schoolPattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.MedicalStops * purposeLogsums[Constants.Purpose.MEDICAL]);

				alternative.AddUtilityTerm(143,nonCommutePattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * compositeLogsum);
				alternative.AddUtilityTerm(144,nonCommutePattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.EscortTours * purposeLogsums[Constants.Purpose.ESCORT]);
				alternative.AddUtilityTerm(145,nonCommutePattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.PersonalBusinessTours * purposeLogsums[Constants.Purpose.PERSONAL_BUSINESS]);
				alternative.AddUtilityTerm(145,nonCommutePattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.ShoppingTours * purposeLogsums[Constants.Purpose.SHOPPING]);
				alternative.AddUtilityTerm(146,nonCommutePattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.MealTours * purposeLogsums[Constants.Purpose.MEAL]);
				alternative.AddUtilityTerm(146,nonCommutePattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.SocialTours * purposeLogsums[Constants.Purpose.SOCIAL]);
				alternative.AddUtilityTerm(146,nonCommutePattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.RecreationTours * purposeLogsums[Constants.Purpose.RECREATION]);
				alternative.AddUtilityTerm(144,nonCommutePattern.ToFlag() * patternHasTwoPlusTourPurposes.ToFlag() * dayPattern.MedicalTours * purposeLogsums[Constants.Purpose.MEDICAL]);

				alternative.AddUtilityTerm(153,nonCommutePattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * compositeLogsum);
				alternative.AddUtilityTerm(154,nonCommutePattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.EscortStops * purposeLogsums[Constants.Purpose.ESCORT]);
				alternative.AddUtilityTerm(155,nonCommutePattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.PersonalBusinessStops * purposeLogsums[Constants.Purpose.PERSONAL_BUSINESS]);
				alternative.AddUtilityTerm(155,nonCommutePattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.ShoppingStops * purposeLogsums[Constants.Purpose.SHOPPING]);
				alternative.AddUtilityTerm(156,nonCommutePattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.MealStops * purposeLogsums[Constants.Purpose.MEAL]);
				alternative.AddUtilityTerm(156,nonCommutePattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.SocialStops * purposeLogsums[Constants.Purpose.SOCIAL]);
				alternative.AddUtilityTerm(156,nonCommutePattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.RecreationStops * purposeLogsums[Constants.Purpose.RECREATION]);
				alternative.AddUtilityTerm(154,nonCommutePattern.ToFlag() * patternHasOnePlusStopPurposes.ToFlag() * dayPattern.MedicalStops * purposeLogsums[Constants.Purpose.MEDICAL]);

			}
		}

		private sealed class DayPattern {
			private readonly int _hashCode;

			public DayPattern(int[] tours, int totalTourPurposes, int[] stops, int totalStopPurposes, int totalDayPurposes, bool available) {
				Tours = tours;

				WorkTours = tours[Constants.Purpose.WORK];
				SchoolTours = tours[Constants.Purpose.SCHOOL];
				EscortTours = tours[Constants.Purpose.ESCORT];
				PersonalBusinessTours = tours[Constants.Purpose.PERSONAL_BUSINESS];
				ShoppingTours = tours[Constants.Purpose.SHOPPING];
				MealTours = tours[Constants.Purpose.MEAL];
				SocialTours = tours[Constants.Purpose.SOCIAL];
				RecreationTours = tours[Constants.Purpose.RECREATION];
				MedicalTours = tours[Constants.Purpose.MEDICAL];

				TotalTourPurposes = totalTourPurposes;

				Stops = stops;

				WorkStops = stops[Constants.Purpose.WORK];
				SchoolStops = stops[Constants.Purpose.SCHOOL];
				EscortStops = stops[Constants.Purpose.ESCORT];
				PersonalBusinessStops = stops[Constants.Purpose.PERSONAL_BUSINESS];
				ShoppingStops = stops[Constants.Purpose.SHOPPING];
				MealStops = stops[Constants.Purpose.MEAL];
				SocialStops = stops[Constants.Purpose.SOCIAL];
				RecreationStops = stops[Constants.Purpose.RECREATION];
				MedicalStops = stops[Constants.Purpose.MEDICAL];

				TotalStopPurposes = totalStopPurposes;

				TotalDayPurposes = totalDayPurposes;

				Available = available;

				_hashCode = ComputeHashCode();
			}

			public DayPattern(IPersonDayWrapper personDay) {
				Tours = new int[Constants.Purpose.TOTAL_PURPOSES];

				WorkTours = Tours[Constants.Purpose.WORK] = personDay.WorkTours > 0 ? 1 : 0;
				SchoolTours = Tours[Constants.Purpose.SCHOOL] = personDay.SchoolTours > 0 ? 1 : 0;
				EscortTours = Tours[Constants.Purpose.ESCORT] = personDay.EscortTours > 0 ? 1 : 0;
				PersonalBusinessTours = Tours[Constants.Purpose.PERSONAL_BUSINESS] = personDay.PersonalBusinessTours > 0 ? 1 : 0;
				ShoppingTours = Tours[Constants.Purpose.SHOPPING] = personDay.ShoppingTours > 0 ? 1 : 0;
				MealTours = Tours[Constants.Purpose.MEAL] = personDay.MealTours > 0 ? 1 : 0;
				SocialTours = Tours[Constants.Purpose.SOCIAL] = personDay.SocialTours > 0 ? 1 : 0;
				RecreationTours = Tours[Constants.Purpose.RECREATION] = personDay.RecreationTours > 0 ? 1 : 0;
				MedicalTours = Tours[Constants.Purpose.MEDICAL] = personDay.MedicalTours > 0 ? 1 : 0;

				TotalTourPurposes = WorkTours + SchoolTours + EscortTours + PersonalBusinessTours + ShoppingTours + MealTours + SocialTours + RecreationTours + MedicalTours;

				Stops = new int[Constants.Purpose.TOTAL_PURPOSES];

				WorkStops = Stops[Constants.Purpose.WORK] = personDay.WorkStops > 0 ? 1 : 0;
				SchoolStops = Stops[Constants.Purpose.SCHOOL] = personDay.SchoolStops > 0 ? 1 : 0;
				EscortStops = Stops[Constants.Purpose.ESCORT] = personDay.EscortStops > 0 ? 1 : 0;
				PersonalBusinessStops = Stops[Constants.Purpose.PERSONAL_BUSINESS] = personDay.PersonalBusinessStops > 0 ? 1 : 0;
				ShoppingStops = Stops[Constants.Purpose.SHOPPING] = personDay.ShoppingStops > 0 ? 1 : 0;
				MealStops = Stops[Constants.Purpose.MEAL] = personDay.MealStops > 0 ? 1 : 0;
				SocialStops = Stops[Constants.Purpose.SOCIAL] = personDay.SocialStops > 0 ? 1 : 0;
				RecreationStops = Stops[Constants.Purpose.RECREATION] = personDay.RecreationStops > 0 ? 1 : 0;
				MedicalStops = Stops[Constants.Purpose.MEDICAL] = personDay.MedicalStops > 0 ? 1 : 0;

				TotalStopPurposes = WorkStops + SchoolStops + EscortStops + PersonalBusinessStops + ShoppingStops + MealStops + SocialStops + RecreationStops + MedicalStops;

				TotalDayPurposes = Math.Min(WorkTours, WorkStops) +
					Math.Min(SchoolTours, SchoolStops) +
					Math.Min(EscortTours, EscortStops) +
					Math.Min(PersonalBusinessTours, PersonalBusinessStops) +
					Math.Min(ShoppingTours, ShoppingStops) +
					Math.Min(MealTours, MealStops) +
					Math.Min(SocialTours, SocialStops) +
					Math.Min(RecreationTours, RecreationStops) +
					Math.Min(MedicalTours, MedicalStops);

				Available = true;

				_hashCode = ComputeHashCode();
			}

			public int[] Tours { get; private set; }

			public int[] Stops { get; private set; }

			public int WorkTours { get; private set; }

			public int SchoolTours { get; private set; }

			public int EscortTours { get; private set; }

			public int PersonalBusinessTours { get; private set; }

			public int ShoppingTours { get; private set; }

			public int MealTours { get; private set; }

			public int SocialTours { get; private set; }

			public int RecreationTours { get; private set; }

			public int MedicalTours { get; private set; }

			public int TotalTourPurposes { get; private set; }

			public int WorkStops { get; private set; }

			public int SchoolStops { get; private set; }

			public int EscortStops { get; private set; }

			public int PersonalBusinessStops { get; private set; }

			public int ShoppingStops { get; private set; }

			public int MealStops { get; private set; }

			public int SocialStops { get; private set; }

			public int RecreationStops { get; private set; }

			public int MedicalStops { get; private set; }

			public int TotalStopPurposes { get; private set; }

			public int TotalDayPurposes { get; private set; }

			public bool Available { get; private set; }

			private int ComputeHashCode() {
				unchecked {
					var hashCode = (WorkTours > 0).ToFlag();

					hashCode = (hashCode * 397) ^ (SchoolTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (EscortTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (PersonalBusinessTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (ShoppingTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (MealTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (SocialTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (RecreationTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (MedicalTours > 0 ? 1 : 0);

					hashCode = (hashCode * 397) ^ (WorkStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (SchoolStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (EscortStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (PersonalBusinessStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (ShoppingStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (MealStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (SocialStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (RecreationStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (MedicalStops > 0 ? 1 : 0);

					return hashCode;
				}
			}

			public bool Equals(DayPattern other) {
				if (ReferenceEquals(null, other)) {
					return false;
				}

				if (ReferenceEquals(this, other)) {
					return true;
				}

				var workToursFlag = (WorkTours > 0).ToFlag();
				var schoolToursFlag = (SchoolTours > 0).ToFlag();
				var escortToursFlag = (EscortTours > 0).ToFlag();
				var personalBusinessToursFlag = (PersonalBusinessTours > 0).ToFlag();
				var shoppingToursFlag = (ShoppingTours > 0).ToFlag();
				var mealToursFlag = (MealTours > 0).ToFlag();
				var socialToursFlag = (SocialTours > 0).ToFlag();
				var recreationToursFlag = (RecreationTours > 0).ToFlag();
				var medicalToursFlag = (MedicalTours > 0).ToFlag();

				var workTours2Flag = (other.WorkTours > 0).ToFlag();
				var schoolTours2Flag = (other.SchoolTours > 0).ToFlag();
				var escortTours2Flag = (other.EscortTours > 0).ToFlag();
				var personalBusinessTours2Flag = (other.PersonalBusinessTours > 0).ToFlag();
				var shoppingTours2Flag = (other.ShoppingTours > 0).ToFlag();
				var mealTours2Flag = (other.MealTours > 0).ToFlag();
				var socialTours2Flag = (other.SocialTours > 0).ToFlag();
				var recreationTours2Flag = (other.RecreationTours > 0).ToFlag();
				var medicalTours2Flag = (other.MedicalTours > 0).ToFlag();

				var workStopsFlag = (WorkStops > 0).ToFlag();
				var schoolStopsFlag = (SchoolStops > 0).ToFlag();
				var escortStopsFlag = (EscortStops > 0).ToFlag();
				var personalBusinessStopsFlag = (PersonalBusinessStops > 0).ToFlag();
				var shoppingStopsFlag = (ShoppingStops > 0).ToFlag();
				var mealStopsFLag = (MealStops > 0).ToFlag();
				var socialStopsFlag = (SocialStops > 0).ToFlag();
				var recreationStopsFlag = (RecreationStops > 0).ToFlag();
				var medicalStopsFlag = (MedicalStops > 0).ToFlag();

				var workStops2Flag = (other.WorkStops > 0).ToFlag();
				var schoolStops2Flag = (other.SchoolStops > 0).ToFlag();
				var escortStops2Flag = (other.EscortStops > 0).ToFlag();
				var personalBusinessStops2Flag = (other.PersonalBusinessStops > 0).ToFlag();
				var shoppingStops2Flag = (other.ShoppingStops > 0).ToFlag();
				var mealStops2Flag = (other.MealStops > 0).ToFlag();
				var socialStops2Flag = (other.SocialStops > 0).ToFlag();
				var recreationStops2Flag = (other.RecreationStops > 0).ToFlag();
				var medicalStops2Flag = (other.MedicalStops > 0).ToFlag();

				return
					workToursFlag == workTours2Flag &&
					schoolToursFlag == schoolTours2Flag &&
					escortToursFlag == escortTours2Flag &&
					personalBusinessToursFlag == personalBusinessTours2Flag &&
					shoppingToursFlag == shoppingTours2Flag &&
					mealToursFlag == mealTours2Flag &&
					socialToursFlag == socialTours2Flag &&
					recreationToursFlag == recreationTours2Flag &&
					medicalToursFlag == medicalTours2Flag &&
					workStopsFlag == workStops2Flag &&
					schoolStopsFlag == schoolStops2Flag &&
					escortStopsFlag == escortStops2Flag &&
					personalBusinessStopsFlag == personalBusinessStops2Flag &&
					shoppingStopsFlag == shoppingStops2Flag &&
					mealStopsFLag == mealStops2Flag &&
					socialStopsFlag == socialStops2Flag &&
					recreationStopsFlag == recreationStops2Flag &&
					medicalStopsFlag == medicalStops2Flag;
			}

			public override bool Equals(object obj) {
				return Equals(obj as DayPattern);
			}

			public override int GetHashCode() {
				return _hashCode;
			}
		}
	}
}