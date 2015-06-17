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
using Daysim.ModelRunners;

using Daysim.DomainModels.Actum;

namespace Daysim.ChoiceModels.Actum {
	public class ActumPersonDayPatternModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumPersonDayPatternModel";
		private const int TOTAL_ALTERNATIVES = 214;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 1716;

		private DayPattern[] _dayPatterns;

		public void Run(ActumPersonDayWrapper personDay, ActumHouseholdDayWrapper householdDay) {

			if (householdDay.Household.Id == 80170 && personDay.Person.Sequence == 1) {
				bool testbreak = true;
			}

			if (personDay == null) {
				throw new ArgumentNullException("personDay");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.HouseholdPersonDayPatternModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

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
					if (dayPattern.SocialTours > 0 && personDay.CreatedSocialTours == 0) {
						personDay.CreatedSocialTours++;
					}
					return;
				}
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
				if (dayPattern.SocialTours > 0 && personDay.CreatedSocialTours == 0) {
					personDay.CreatedSocialTours++;
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
					if (dayPattern.SocialTours > 0 && personDay.CreatedSocialTours == 0) {
						personDay.GetNewTour(Constants.AddressType.HOME, personDay.Household.ResidenceParcelId, personDay.Household.ResidenceZoneKey, Constants.Purpose.SOCIAL);
						personDay.CreatedSocialTours++;
					}

					personDay.EscortStops = dayPattern.EscortStops;
					personDay.PersonalBusinessStops = dayPattern.PersonalBusinessStops;
					personDay.ShoppingStops = dayPattern.ShoppingStops;
					personDay.SocialStops = dayPattern.SocialStops;

				}
			}
		}

		private void InitializeDayPatterns(ActumPersonDayWrapper personDay) {
			//			if (_dayPatterns != null) {
			//				return;
			//			}

			var workTours = personDay.WorkTours > 0 ? 1 : 0;
			var schoolTours = personDay.SchoolTours > 0 ? 1 : 0;
			var businessTours = personDay.BusinessTours > 0 ? 1 : 0;
			var workStops = personDay.WorkStops > 0 ? 1 : 0;
			var schoolStops = personDay.SchoolStops > 0 ? 1 : 0;
			var businessStops = personDay.BusinessStops > 0 ? 1 : 0;
			var priorEscortTours = (personDay.EscortFullHalfTours > 0 || personDay.EscortJointTours > 0) ? 1 : 0;
			var priorPersonalBusinessTours = personDay.PersonalBusinessJointTours > 0 ? 1 : 0;
			var priorShoppingTours = personDay.ShoppingJointTours > 0 ? 1 : 0;
			var priorSocialTours = personDay.SocialJointTours > 0 ? 1 : 0;

			_dayPatterns = new DayPattern[TOTAL_ALTERNATIVES];

			var alternativeIndex = -1;

			for (var escortTours = 0; escortTours <= 1; escortTours++) {
				for (var personalBusinessTours = 0; personalBusinessTours <= 1; personalBusinessTours++) {
					for (var shoppingTours = 0; shoppingTours <= 1; shoppingTours++) {
						for (var socialTours = 0; socialTours <= 1; socialTours++) {
							for (var escortStops = 0; escortStops <= 1; escortStops++) {
								for (var personalBusinessStops = 0; personalBusinessStops <= 1; personalBusinessStops++) {
									for (var shoppingStops = 0; shoppingStops <= 1; shoppingStops++) {
										for (var socialStops = 0; socialStops <= 1; socialStops++) {
											var totalNonMandatoryTourPurposes = escortTours + personalBusinessTours + shoppingTours + socialTours;
											var totalNonMandatoryStopPurposes = escortStops + personalBusinessStops + shoppingStops + socialStops;
											var totalTourPurposes = totalNonMandatoryTourPurposes + workTours + schoolTours + businessTours;
											var totalStopPurposes = totalNonMandatoryStopPurposes + workStops + schoolStops + businessStops;

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
											tours[Constants.Purpose.BUSINESS] = businessTours;
											tours[Constants.Purpose.ESCORT] = escortTours;
											tours[Constants.Purpose.PERSONAL_BUSINESS] = personalBusinessTours;
											tours[Constants.Purpose.SHOPPING] = shoppingTours;
											tours[Constants.Purpose.SOCIAL] = socialTours;

											var stops = new int[Constants.Purpose.TOTAL_PURPOSES];

											stops[Constants.Purpose.WORK] = workStops;
											stops[Constants.Purpose.SCHOOL] = schoolStops;
											stops[Constants.Purpose.BUSINESS] = businessStops;
											stops[Constants.Purpose.ESCORT] = escortStops;
											stops[Constants.Purpose.PERSONAL_BUSINESS] = personalBusinessStops;
											stops[Constants.Purpose.SHOPPING] = shoppingStops;
											stops[Constants.Purpose.SOCIAL] = socialStops;

											bool available = totalNonMandatoryStopPurposes > 0 && totalTourPurposes == 0 ? false :
												priorEscortTours > 0 && escortTours == 0 ? false :
												priorPersonalBusinessTours > 0 && personalBusinessTours == 0 ? false :
												priorShoppingTours > 0 && shoppingTours == 0 ? false :
												priorSocialTours > 0 && socialTours == 0 ? false :
												totalTourPurposes > 3 || totalStopPurposes > 4 || totalTourPurposes + totalStopPurposes > 5 ? false :
												Math.Min(totalStopPurposes, 1) > totalTourPurposes ? false :
												true;

											_dayPatterns[alternativeIndex] = new DayPattern(tours, totalTourPurposes, stops, totalStopPurposes, available);
										}
									}
								}
							}
						}
					}
				}
			}

		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumPersonDayWrapper personDay, ActumHouseholdDayWrapper householdDay, DayPattern choice = null) {
			var household = personDay.Household;
			var residenceParcel = household.ResidenceParcel;
			var person = personDay.Person;

			var carsPerDriver = household.CarsPerDriver;
			var mixedDensity = residenceParcel.MixedUse3Index2();
			var intersectionDensity = residenceParcel.IntersectionDensity34Minus1Buffer2();

			var purposeLogsums = new double[Constants.Purpose.TOTAL_PURPOSES + 2];
			var atUsualLogsums = new double[3];
			//var carOwnership = person.CarOwnershipSegment; //GV: sat car ownership not to impact logsums
			var carOwnership = 0;
			var votSegment = person.Household.VotALSegment;
			var transitAccess = residenceParcel.TransitAccessSegment();

			//GV: input 26. july 2013
			// household inputs
			//var childrenUnder5 = householdTotals.ChildrenUnder5;
			//var childrenAge5Through15 = householdTotals.ChildrenAge5Through15;
			//var nonworkingAdults = householdTotals.NonworkingAdults;
			//var retiredAdults = householdTotals.RetiredAdults;

			var onePersonHouseholdFlag = household.IsOnePersonHousehold.ToFlag();
			var twoPersonHouseholdFlag = household.IsTwoPersonHousehold.ToFlag();

			var householdCars = household.VehiclesAvailable;
			//var noCarsInHouseholdFlag = HouseholdWrapper.GetNoCarsInHouseholdFlag(householdCars);
			//var carsLessThanDriversFlag = household.GetCarsLessThanDriversFlag(householdCars);
			//var carsLessThanWorkersFlag = household.GetCarsLessThanWorkersFlag(householdCars);

			var HHwithChildrenFlag = household.HasChildren.ToFlag();
			var HHwithSmallChildrenFlag = household.HasChildrenUnder5.ToFlag();
			var HHwithLowIncomeFlag = (household.Income >= 300000 && household.Income < 600000).ToFlag();
			var HHwithMidleIncomeFlag = (household.Income >= 600000 && household.Income < 900000).ToFlag();
			var HHwithHighIncomeFlag = (household.Income >= 900000).ToFlag();

			var primaryFamilyTimeFlag = householdDay.PrimaryPriorityTimeFlag;

			//GV: input 26. july 2013
			// person inputs
			var partTimeWorkerFlag = person.IsPartTimeWorker.ToFlag();
			var nonworkingAdultFlag = person.IsNonworkingAdult.ToFlag();
			var universityStudentFlag = person.IsUniversityStudent.ToFlag();
			var retiredAdultFlag = person.IsRetiredAdult.ToFlag();
			var fullTimeWorkerFlag = person.IsFulltimeWorker.ToFlag();
			var childAge5Through15Flag = person.IsChildAge5Through15.ToFlag();
			var childUnder5Flag = person.IsChildUnder5.ToFlag();
			var adultFlag = person.IsAdult.ToFlag();

			var maleFlag = person.IsMale.ToFlag();
			var femaleFlag = person.IsFemale.ToFlag();



			if (person.UsualWorkParcel == null || person.UsualWorkParcelId == household.ResidenceParcelId) {
				purposeLogsums[Constants.Purpose.WORK] = 0;
			}
			else {
				var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.WORK_TOUR_MODE_MODEL);
				var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.WORK_TOUR_MODE_MODEL);
				var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumWorkTourModeModel") as ActumWorkTourModeModel).RunNested(personDay, residenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);

				purposeLogsums[Constants.Purpose.WORK] = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				atUsualLogsums[Constants.Purpose.WORK] = Global.AggregateLogsums[person.UsualWorkParcel.ZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votSegment][person.UsualWorkParcel.TransitAccessSegment()];
			}

			if (person.UsualSchoolParcel == null || person.UsualSchoolParcelId == household.ResidenceParcelId) {
				purposeLogsums[Constants.Purpose.SCHOOL] = 0;
			}
			else {
				var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
				var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
				var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumSchoolTourModeModel") as ActumSchoolTourModeModel).RunNested(personDay, residenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);

				purposeLogsums[Constants.Purpose.SCHOOL] = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				atUsualLogsums[Constants.Purpose.SCHOOL] = Global.AggregateLogsums[person.UsualSchoolParcel.ZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votSegment][person.UsualSchoolParcel.TransitAccessSegment()];
			}

			var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votSegment][transitAccess];

			purposeLogsums[Constants.Purpose.ESCORT] = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.ESCORT][carOwnership][votSegment][transitAccess];
			purposeLogsums[Constants.Purpose.PERSONAL_BUSINESS] = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.PERSONAL_BUSINESS][carOwnership][votSegment][transitAccess];
			purposeLogsums[Constants.Purpose.SHOPPING] = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.SHOPPING][carOwnership][votSegment][transitAccess];
			purposeLogsums[Constants.Purpose.SOCIAL] = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.SOCIAL][carOwnership][votSegment][transitAccess];

			for (var xPurpose = Constants.Purpose.ESCORT; xPurpose <= Constants.Purpose.SOCIAL + 10; xPurpose++) {
				// extra components 1-5 are for 2,3,4,5,6 tour purposes
				// extra components 6-10 are for 2,3,4,5,6 stop puroposes

				// recode purpose to match coefficients
				var purpose = xPurpose <= Constants.Purpose.SOCIAL ? xPurpose :
					xPurpose <= Constants.Purpose.SOCIAL + 5 ? Constants.Purpose.SOCIAL + 1 :
					Constants.Purpose.SOCIAL + 2;

				// get correct multiplier on coefficients.
				var xMultiplier = xPurpose <= Constants.Purpose.SOCIAL ? 1.0 :
					xPurpose <= Constants.Purpose.SOCIAL + 5 ? Math.Log(xPurpose - Constants.Purpose.SOCIAL + 1) :
					Math.Log(xPurpose - Constants.Purpose.SOCIAL - 5 + 1);

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

				component.AddUtilityTerm(100 * purpose + 9, xMultiplier * HHwithLowIncomeFlag);
				component.AddUtilityTerm(100 * purpose + 10, xMultiplier * HHwithMidleIncomeFlag);
				component.AddUtilityTerm(100 * purpose + 11, xMultiplier * HHwithHighIncomeFlag);

				//component.AddUtilityTerm(100 * purpose + 12, xMultiplier * carsPerDriver);
				component.AddUtilityTerm(100 * purpose + 12, xMultiplier * householdCars);

				component.AddUtilityTerm(100 * purpose + 13, xMultiplier * person.IsOnlyAdult.ToFlag());
				component.AddUtilityTerm(100 * purpose + 14, xMultiplier * person.IsOnlyFullOrPartTimeWorker.ToFlag());
				component.AddUtilityTerm(100 * purpose + 15, xMultiplier * 0);
				component.AddUtilityTerm(100 * purpose + 16, xMultiplier * person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * (!household.HasChildrenUnder16).ToFlag());
				component.AddUtilityTerm(100 * purpose + 17, xMultiplier * person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenUnder5.ToFlag());
				component.AddUtilityTerm(100 * purpose + 18, xMultiplier * person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenAge5Through15.ToFlag());
				component.AddUtilityTerm(100 * purpose + 19, xMultiplier * person.IsMale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenUnder5.ToFlag());
				component.AddUtilityTerm(100 * purpose + 20, xMultiplier * person.IsMale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenAge5Through15.ToFlag());

				//component.AddUtilityTerm(100 * purpose + 21, xMultiplier * primaryFamilyTimeFlag); //GV: wrong sign

				//component.AddUtilityTerm(100 * purpose + 21, xMultiplier * person.AgeIsBetween18And25.ToFlag());
				//component.AddUtilityTerm(100 * purpose + 22, xMultiplier * person.AgeIsBetween26And35.ToFlag());
				//component.AddUtilityTerm(100 * purpose + 23, xMultiplier * person.AgeIsBetween51And65.ToFlag());

				component.AddUtilityTerm(100 * purpose + 24, xMultiplier * person.WorksAtHome.ToFlag());
				component.AddUtilityTerm(100 * purpose + 25, xMultiplier * mixedDensity);
				component.AddUtilityTerm(100 * purpose + 26, xMultiplier * intersectionDensity);
				//component.AddUtilityTerm(100 * purpose + 27, xMultiplier * purposeLogsums[purpose]); //GV: 17.08.2013, the logsums are wrong
				//component.AddUtilityTerm(100 * purpose + 28, xMultiplier * person.TransitPassOwnershipFlag);
			}

			// tour utility
			const int tourComponentIndex = 18;
			choiceProbabilityCalculator.CreateUtilityComponent(tourComponentIndex);
			var tourComponent = choiceProbabilityCalculator.GetUtilityComponent(tourComponentIndex);
			//tourComponent.AddUtilityTerm(1701, carsPerDriver);
			tourComponent.AddUtilityTerm(1701, householdCars);

			tourComponent.AddUtilityTerm(1702, person.WorksAtHome.ToFlag());
			tourComponent.AddUtilityTerm(1703, mixedDensity);
			tourComponent.AddUtilityTerm(1704, mixedDensity * person.IsChildAge5Through15.ToFlag());
			tourComponent.AddUtilityTerm(1705, compositeLogsum);

			//tourComponent.AddUtilityTerm(1706, person.TransitPassOwnershipFlag);
			tourComponent.AddUtilityTerm(1706, primaryFamilyTimeFlag);

			// stop utility
			const int stopComponentIndex = 19;
			choiceProbabilityCalculator.CreateUtilityComponent(stopComponentIndex);
			var stopComponent = choiceProbabilityCalculator.GetUtilityComponent(stopComponentIndex);
			//stopComponent.AddUtilityTerm(1711, carsPerDriver);
			stopComponent.AddUtilityTerm(1711, householdCars);

			stopComponent.AddUtilityTerm(1712, person.WorksAtHome.ToFlag());
			stopComponent.AddUtilityTerm(1713, mixedDensity);
			stopComponent.AddUtilityTerm(1714, mixedDensity * person.IsChildAge5Through15.ToFlag());
			stopComponent.AddUtilityTerm(1715, compositeLogsum);

			//stopComponent.AddUtilityTerm(1716, person.TransitPassOwnershipFlag);
			//stopComponent.AddUtilityTerm(1716, primaryFamilyTimeFlag); //GV: 17.08.2013, the logsums are wrong

			for (var alternativeIndex = 0; alternativeIndex < TOTAL_ALTERNATIVES; alternativeIndex++) {

				var dayPattern = _dayPatterns[alternativeIndex];
				var available = dayPattern.Available;
				var alternative = choiceProbabilityCalculator.GetAlternative(alternativeIndex, available, choice != null && choice.Equals(dayPattern));

				if (!Global.Configuration.IsInEstimationMode && !alternative.Available) {
					continue;
				}

				alternative.Choice = dayPattern;

				// components for the purposes
				for (var purpose = Constants.Purpose.ESCORT; purpose <= Constants.Purpose.SOCIAL; purpose++) {
					if (dayPattern.Tours[purpose] > 0 || dayPattern.Stops[purpose] > 0) {
						alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(purpose));

						if (dayPattern.Tours[purpose] > 0) {
							alternative.AddUtilityTerm(100 * purpose + 50, 1); // tour purpose ASC
							//alternative.AddUtilityTerm(100 * purpose + 51, purposeLogsums[purpose]); // tour purpose logsum GV: 17.08.2013, the logsums are wrong
						}

						if (dayPattern.Stops[purpose] > 0) {
							alternative.AddUtilityTerm(100 * purpose + 60, 1); // stop purpose ASC
							//alternative.AddUtilityTerm(100 * purpose + 61, purposeLogsums[purpose]); // stop purpose logsum GV: 17.08.2013, the logsums are wrong
						}
						if (Global.Configuration.IsInEstimationMode) {
							//GV commented out
							//alternative.AddUtilityTerm(100 * purpose + 70, 1 - person.PaperDiary);
							//GV commented out
							//alternative.AddUtilityTerm(100 * purpose + 71, person.ProxyResponse);
						}
					}
				}

				// multiple tour purposes component
				if (dayPattern.TotalTourPurposes > 1) {
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(Constants.Purpose.SOCIAL + (dayPattern.TotalTourPurposes - 1)));
				}

				// multiple stop purposes component
				if (dayPattern.TotalStopPurposes > 1) {
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(Constants.Purpose.SOCIAL + 5 + (dayPattern.TotalStopPurposes - 1)));
				}

				for (var tourPurpose = Constants.Purpose.WORK; tourPurpose <= Constants.Purpose.SOCIAL; tourPurpose++) {
					for (var stopPurpose = Constants.Purpose.WORK; stopPurpose <= Constants.Purpose.SOCIAL - 1; stopPurpose++) {
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
						//alternative.AddUtilityTerm(1300 + 10 * tourPurpose + 2, atUsualLogsums[tourPurpose]); // at usual location aggregate logsum x  presence of stops in work or school pattern GV: commented out as the sign is wrong
					}
				}

				for (var tourPurpose = Constants.Purpose.WORK; tourPurpose <= Constants.Purpose.SOCIAL - 2; tourPurpose++) {
					for (var tourPurpose2 = tourPurpose + 1; tourPurpose2 <= Constants.Purpose.SOCIAL; tourPurpose2++) {
						if (dayPattern.Tours[tourPurpose] > 0 && dayPattern.Tours[tourPurpose2] > 0) {
							alternative.AddUtilityTerm(1400 + 10 * tourPurpose + tourPurpose2, 1); // tour-tour comb. utility
						}
					}
				}

				for (var tourPurpose = Constants.Purpose.BUSINESS; tourPurpose <= Constants.Purpose.BUSINESS; tourPurpose++) {
					for (var tourPurpose2 = Constants.Purpose.ESCORT; tourPurpose2 <= Constants.Purpose.SOCIAL; tourPurpose2++) {
						if (dayPattern.Tours[tourPurpose] > 0 && dayPattern.Tours[tourPurpose2] > 0) {
							alternative.AddUtilityTerm(1461, 1); // tour-tour comb. utility for business combos
						}
					}
				}

				for (var stopPurpose = Constants.Purpose.WORK; stopPurpose <= Constants.Purpose.SOCIAL - 2; stopPurpose++) {
					for (var stopPurpose2 = stopPurpose + 1; stopPurpose2 <= Constants.Purpose.SOCIAL; stopPurpose2++) {
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
				if (dayPattern.TotalTourPurposes - dayPattern.Tours[Constants.Purpose.WORK]
					- dayPattern.Tours[Constants.Purpose.BUSINESS]
					- dayPattern.Tours[Constants.Purpose.SCHOOL] > 0) {
					// tour utility
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(tourComponentIndex));
				}
				if (dayPattern.TotalStopPurposes - dayPattern.Stops[Constants.Purpose.WORK]
					- dayPattern.Stops[Constants.Purpose.BUSINESS]
					- dayPattern.Stops[Constants.Purpose.SCHOOL] > 0) {
					// stop utility
					alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(stopComponentIndex));
				}

			}
		}

		private sealed class DayPattern {
			private readonly int _hashCode;

			public DayPattern(int[] tours, int totalTourPurposes, int[] stops, int totalStopPurposes, bool available) {
				Tours = tours;

				WorkTours = tours[Constants.Purpose.WORK];
				SchoolTours = tours[Constants.Purpose.SCHOOL];
				BusinessTours = tours[Constants.Purpose.BUSINESS];
				EscortTours = tours[Constants.Purpose.ESCORT];
				PersonalBusinessTours = tours[Constants.Purpose.PERSONAL_BUSINESS];
				ShoppingTours = tours[Constants.Purpose.SHOPPING];
				SocialTours = tours[Constants.Purpose.SOCIAL];

				TotalTourPurposes = totalTourPurposes;

				Stops = stops;

				WorkStops = stops[Constants.Purpose.WORK];
				SchoolStops = stops[Constants.Purpose.SCHOOL];
				BusinessStops = stops[Constants.Purpose.BUSINESS];
				EscortStops = stops[Constants.Purpose.ESCORT];
				PersonalBusinessStops = stops[Constants.Purpose.PERSONAL_BUSINESS];
				ShoppingStops = stops[Constants.Purpose.SHOPPING];
				SocialStops = stops[Constants.Purpose.SOCIAL];

				TotalStopPurposes = totalStopPurposes;

				Available = available;

				_hashCode = ComputeHashCode();
			}

			public DayPattern(ActumPersonDayWrapper personDay) {
				Tours = new int[Constants.Purpose.TOTAL_PURPOSES];

				WorkTours = Tours[Constants.Purpose.WORK] = personDay.WorkTours > 0 ? 1 : 0;
				SchoolTours = Tours[Constants.Purpose.SCHOOL] = personDay.SchoolTours > 0 ? 1 : 0;
				BusinessTours = Tours[Constants.Purpose.BUSINESS] = personDay.BusinessTours > 0 ? 1 : 0;
				EscortTours = Tours[Constants.Purpose.ESCORT] = personDay.EscortTours > 0 ? 1 : 0;
				PersonalBusinessTours = Tours[Constants.Purpose.PERSONAL_BUSINESS] = personDay.PersonalBusinessTours > 0 ? 1 : 0;
				ShoppingTours = Tours[Constants.Purpose.SHOPPING] = personDay.ShoppingTours > 0 ? 1 : 0;
				SocialTours = Tours[Constants.Purpose.SOCIAL] = personDay.SocialTours > 0 ? 1 : 0;

				TotalTourPurposes = WorkTours + SchoolTours + BusinessTours + EscortTours + PersonalBusinessTours + ShoppingTours + SocialTours;

				Stops = new int[Constants.Purpose.TOTAL_PURPOSES];

				WorkStops = Stops[Constants.Purpose.WORK] = personDay.WorkStops > 0 ? 1 : 0;
				SchoolStops = Stops[Constants.Purpose.SCHOOL] = personDay.SchoolStops > 0 ? 1 : 0;
				BusinessStops = Stops[Constants.Purpose.BUSINESS] = personDay.BusinessStops > 0 ? 1 : 0;
				EscortStops = Stops[Constants.Purpose.ESCORT] = personDay.EscortStops > 0 ? 1 : 0;
				PersonalBusinessStops = Stops[Constants.Purpose.PERSONAL_BUSINESS] = personDay.PersonalBusinessStops > 0 ? 1 : 0;
				ShoppingStops = Stops[Constants.Purpose.SHOPPING] = personDay.ShoppingStops > 0 ? 1 : 0;
				SocialStops = Stops[Constants.Purpose.SOCIAL] = personDay.SocialStops > 0 ? 1 : 0;

				TotalStopPurposes = WorkStops + SchoolStops + BusinessStops + EscortStops + PersonalBusinessStops + ShoppingStops + SocialStops;

				Available = true;

				_hashCode = ComputeHashCode();
			}

			public int[] Tours { get; private set; }

			public int[] Stops { get; private set; }

			public int WorkTours { get; private set; }

			public int SchoolTours { get; private set; }

			public int BusinessTours { get; private set; }

			public int EscortTours { get; private set; }

			public int PersonalBusinessTours { get; private set; }

			public int ShoppingTours { get; private set; }

			public int SocialTours { get; private set; }

			public int TotalTourPurposes { get; private set; }

			public int WorkStops { get; private set; }

			public int SchoolStops { get; private set; }

			public int BusinessStops { get; private set; }

			public int EscortStops { get; private set; }

			public int PersonalBusinessStops { get; private set; }

			public int ShoppingStops { get; private set; }

			public int SocialStops { get; private set; }

			public int TotalStopPurposes { get; private set; }

			public bool Available { get; private set; }

			private int ComputeHashCode() {
				unchecked {
					var hashCode = (WorkTours > 0).ToFlag();

					hashCode = (hashCode * 397) ^ (SchoolTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (BusinessTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (EscortTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (PersonalBusinessTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (ShoppingTours > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (SocialTours > 0 ? 1 : 0);

					hashCode = (hashCode * 397) ^ (WorkStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (SchoolStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (BusinessStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (EscortStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (PersonalBusinessStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (ShoppingStops > 0 ? 1 : 0);
					hashCode = (hashCode * 397) ^ (SocialStops > 0 ? 1 : 0);

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
				var businessToursFlag = (BusinessTours > 0).ToFlag();
				var escortToursFlag = (EscortTours > 0).ToFlag();
				var personalBusinessToursFlag = (PersonalBusinessTours > 0).ToFlag();
				var shoppingToursFlag = (ShoppingTours > 0).ToFlag();
				var socialToursFlag = (SocialTours > 0).ToFlag();

				var workTours2Flag = (other.WorkTours > 0).ToFlag();
				var schoolTours2Flag = (other.SchoolTours > 0).ToFlag();
				var businessTours2Flag = (other.BusinessTours > 0).ToFlag();
				var escortTours2Flag = (other.EscortTours > 0).ToFlag();
				var personalBusinessTours2Flag = (other.PersonalBusinessTours > 0).ToFlag();
				var shoppingTours2Flag = (other.ShoppingTours > 0).ToFlag();
				var socialTours2Flag = (other.SocialTours > 0).ToFlag();

				var workStopsFlag = (WorkStops > 0).ToFlag();
				var schoolStopsFlag = (SchoolStops > 0).ToFlag();
				var businessStopsFlag = (BusinessStops > 0).ToFlag();
				var escortStopsFlag = (EscortStops > 0).ToFlag();
				var personalBusinessStopsFlag = (PersonalBusinessStops > 0).ToFlag();
				var shoppingStopsFlag = (ShoppingStops > 0).ToFlag();
				var socialStopsFlag = (SocialStops > 0).ToFlag();

				var workStops2Flag = (other.WorkStops > 0).ToFlag();
				var schoolStops2Flag = (other.SchoolStops > 0).ToFlag();
				var businessStops2Flag = (other.BusinessStops > 0).ToFlag();
				var escortStops2Flag = (other.EscortStops > 0).ToFlag();
				var personalBusinessStops2Flag = (other.PersonalBusinessStops > 0).ToFlag();
				var shoppingStops2Flag = (other.ShoppingStops > 0).ToFlag();
				var socialStops2Flag = (other.SocialStops > 0).ToFlag();

				return
					workToursFlag == workTours2Flag &&
					schoolToursFlag == schoolTours2Flag &&
						  businessToursFlag == businessTours2Flag &&
						  escortToursFlag == escortTours2Flag &&
					personalBusinessToursFlag == personalBusinessTours2Flag &&
					shoppingToursFlag == shoppingTours2Flag &&
					socialToursFlag == socialTours2Flag &&
					workStopsFlag == workStops2Flag &&
					schoolStopsFlag == schoolStops2Flag &&
						  businessStopsFlag == businessStops2Flag &&
						  escortStopsFlag == escortStops2Flag &&
					personalBusinessStopsFlag == personalBusinessStops2Flag &&
					shoppingStopsFlag == shoppingStops2Flag &&
					socialStopsFlag == socialStops2Flag;
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