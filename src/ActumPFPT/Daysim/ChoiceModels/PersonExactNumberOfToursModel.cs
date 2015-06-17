// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
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

namespace Daysim.ChoiceModels {
	public class PersonExactNumberOfToursModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "PersonExactNumberOfToursModel";
		private const int TOTAL_ALTERNATIVES = 3;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 753;

		public void Run(IPersonDayWrapper personDay, int purpose) {
			if (personDay == null) {
				throw new ArgumentNullException("personDay");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.PersonExactNumberOfToursModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			
			personDay.ResetRandom(890 + purpose);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator((personDay.Id * 397) ^ purpose);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				int tours;

				switch (purpose) {
					case Constants.Purpose.WORK:
						tours = personDay.WorkTours;

						break;
					case Constants.Purpose.SCHOOL:
						tours = personDay.SchoolTours;

						break;
					case Constants.Purpose.ESCORT:
						tours = personDay.EscortTours;

						break;
					case Constants.Purpose.PERSONAL_BUSINESS:
						tours = personDay.PersonalBusinessTours;

						break;
					case Constants.Purpose.SHOPPING:
						tours = personDay.ShoppingTours;

						break;
					case Constants.Purpose.MEAL:
						tours = personDay.MealTours;
						break;

					case Constants.Purpose.SOCIAL:
						tours = personDay.SocialTours;

						break;
					default:
						tours = Constants.DEFAULT_VALUE;

						break;
				}

				RunModel(choiceProbabilityCalculator, personDay, purpose, tours);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, personDay, purpose);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(personDay.Household.RandomUtility);
				var choice = (int) chosenAlternative.Choice;

				personDay.HomeBasedTours += choice;

				switch (purpose) {
					case Constants.Purpose.WORK:
						personDay.WorkTours = choice;

						break;
					case Constants.Purpose.SCHOOL:
						personDay.SchoolTours = choice;

						break;
					case Constants.Purpose.ESCORT:
						personDay.EscortTours = choice;

						break;
					case Constants.Purpose.PERSONAL_BUSINESS:
						personDay.PersonalBusinessTours = choice;

						break;
					case Constants.Purpose.SHOPPING:
						personDay.ShoppingTours = choice;

						break;
					case Constants.Purpose.MEAL:
						personDay.MealTours = choice;

						break;
					case Constants.Purpose.SOCIAL:
						personDay.SocialTours = choice;

						break;
				}
			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, IPersonDayWrapper personDay, int purpose, int choice = Constants.DEFAULT_VALUE) {
			var household = personDay.Household;
			var residenceParcel = household.ResidenceParcel;
			var person = personDay.Person;

			var carsPerDriver = household.CarsPerDriver;
			var mixedDensity = residenceParcel.ParcelHouseholdsPerRetailServiceFoodEmploymentBuffer2();
			var intersectionDensity = residenceParcel.IntersectionDensity34Minus1Buffer2();

			double purposeLogsum;

			switch (purpose) {
				case Constants.Purpose.WORK:
					if (person.UsualWorkParcel == null || person.UsualWorkParcelId == household.ResidenceParcelId) {
						purposeLogsum = 0;
					}
					else {
						var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.WORK_TOUR_MODE_MODEL);
						var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.WORK_TOUR_MODE_MODEL);
						var nestedAlternative = (Global.ChoiceModelDictionary.Get("WorkTourModeModel") as WorkTourModeModel).RunNested(personDay, residenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);

						purposeLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
					}

					break;
				case Constants.Purpose.SCHOOL:
					if (person.UsualSchoolParcel == null || person.UsualSchoolParcelId == household.ResidenceParcelId) {
						purposeLogsum = 0;
					}
					else {
						var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
						var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.SCHOOL_TOUR_MODE_MODEL);
						var nestedAlternative = (Global.ChoiceModelDictionary.Get("SchoolTourModeModel") as SchoolTourModeModel).RunNested(personDay, residenceParcel, person.UsualSchoolParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);

						purposeLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
					}

					break;
				default: {
					var carOwnership = person.CarOwnershipSegment;
					var votSegment = person.Household.VotALSegment;
					var transitAccess = residenceParcel.TransitAccessSegment();

					purposeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][purpose][carOwnership][votSegment][transitAccess];

					break;
				}
			}

			// 1 TOUR

			var alternative = choiceProbabilityCalculator.GetAlternative(0, true, choice == 1);

			alternative.Choice = 1;

			alternative.AddUtilityTerm(1, purpose);

			// 2 TOURS

			alternative = choiceProbabilityCalculator.GetAlternative(1, true, choice == 2);

			const int two = 2;
			alternative.Choice = two;

			alternative.AddUtilityTerm(100 * purpose + 1, person.IsFulltimeWorker.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 2, person.IsPartTimeWorker.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 3, person.IsRetiredAdult.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 4, person.IsNonworkingAdult.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 5, person.IsUniversityStudent.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 6, person.IsDrivingAgeStudent.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 7, person.IsChildAge5Through15.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 8, person.IsChildUnder5.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 9, household.Has0To25KIncome.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 10, household.Has25To45KIncome.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 11, household.Has75KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 12, carsPerDriver);
			alternative.AddUtilityTerm(100 * purpose + 13, person.IsOnlyAdult.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 14, person.IsOnlyFullOrPartTimeWorker.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 15, 0);
			alternative.AddUtilityTerm(100 * purpose + 16, person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * (!household.HasChildrenUnder16).ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 17, person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenUnder5.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 18, person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenAge5Through15.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 19, person.IsMale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenUnder5.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 20, person.IsMale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenAge5Through15.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 21, person.AgeIsBetween18And25.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 22, person.AgeIsBetween26And35.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 23, person.AgeIsBetween51And65.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 24, person.WorksAtHome.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 25, mixedDensity);
			alternative.AddUtilityTerm(100 * purpose + 26, intersectionDensity);
			alternative.AddUtilityTerm(100 * purpose + 31, personDay.WorkTours);
			alternative.AddUtilityTerm(100 * purpose + 32, personDay.SchoolTours);
			alternative.AddUtilityTerm(100 * purpose + 33, personDay.EscortTours);
			alternative.AddUtilityTerm(100 * purpose + 34, personDay.PersonalBusinessTours);
			alternative.AddUtilityTerm(100 * purpose + 35, personDay.ShoppingTours);
			alternative.AddUtilityTerm(100 * purpose + 36, personDay.MealTours);
			alternative.AddUtilityTerm(100 * purpose + 37, personDay.SocialTours);
			alternative.AddUtilityTerm(100 * purpose + 41, personDay.WorkStops);

			if (purpose <= Constants.Purpose.ESCORT) {
				alternative.AddUtilityTerm(100 * purpose + 42, personDay.SchoolStops);
			}

			alternative.AddUtilityTerm(100 * purpose + 43, personDay.EscortStops);
			alternative.AddUtilityTerm(100 * purpose + 44, personDay.PersonalBusinessStops);
			alternative.AddUtilityTerm(100 * purpose + 45, personDay.ShoppingStops);
			alternative.AddUtilityTerm(100 * purpose + 46, personDay.MealStops);
			alternative.AddUtilityTerm(100 * purpose + 47, personDay.SocialStops);
			alternative.AddUtilityTerm(100 * purpose + 50 + two, 1); // ASC
			alternative.AddUtilityTerm(100 * purpose + 23 + 2 * two, purposeLogsum); // accessibility effect has different coefficient for 2 and 3+

			// 3+ TOURS

			alternative = choiceProbabilityCalculator.GetAlternative(2, true, choice == 3);

			const int three = 3;
			alternative.Choice = three;

			alternative.AddUtilityTerm(100 * purpose + 1, person.IsFulltimeWorker.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 2, person.IsPartTimeWorker.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 3, person.IsRetiredAdult.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 4, person.IsNonworkingAdult.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 5, person.IsUniversityStudent.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 6, person.IsDrivingAgeStudent.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 7, person.IsChildAge5Through15.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 8, person.IsChildUnder5.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 9, household.Has0To25KIncome.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 10, household.Has25To45KIncome.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 11, household.Has75KPlusIncome.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 12, carsPerDriver);
			alternative.AddUtilityTerm(100 * purpose + 13, person.IsOnlyAdult.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 14, person.IsOnlyFullOrPartTimeWorker.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 15, 0);
			alternative.AddUtilityTerm(100 * purpose + 16, person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * (!household.HasChildrenUnder16).ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 17, person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenUnder5.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 18, person.IsFemale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenAge5Through15.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 19, person.IsMale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenUnder5.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 20, person.IsMale.ToFlag() * person.IsAdult.ToFlag() * household.HasChildrenAge5Through15.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 21, person.AgeIsBetween18And25.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 22, person.AgeIsBetween26And35.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 23, person.AgeIsBetween51And65.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 24, person.WorksAtHome.ToFlag());
			alternative.AddUtilityTerm(100 * purpose + 25, mixedDensity);
			alternative.AddUtilityTerm(100 * purpose + 26, intersectionDensity);
			alternative.AddUtilityTerm(100 * purpose + 31, personDay.WorkTours);
			alternative.AddUtilityTerm(100 * purpose + 32, personDay.SchoolTours);
			alternative.AddUtilityTerm(100 * purpose + 33, personDay.EscortTours);
			alternative.AddUtilityTerm(100 * purpose + 34, personDay.PersonalBusinessTours);
			alternative.AddUtilityTerm(100 * purpose + 35, personDay.ShoppingTours);
			alternative.AddUtilityTerm(100 * purpose + 36, personDay.MealTours);
			alternative.AddUtilityTerm(100 * purpose + 37, personDay.SocialTours);
			alternative.AddUtilityTerm(100 * purpose + 41, personDay.WorkStops);

			if (purpose <= Constants.Purpose.ESCORT) {
				alternative.AddUtilityTerm(100 * purpose + 42, personDay.SchoolStops);
			}

			alternative.AddUtilityTerm(100 * purpose + 43, personDay.EscortStops);
			alternative.AddUtilityTerm(100 * purpose + 44, personDay.PersonalBusinessStops);
			alternative.AddUtilityTerm(100 * purpose + 45, personDay.ShoppingStops);
			alternative.AddUtilityTerm(100 * purpose + 46, personDay.MealStops);
			alternative.AddUtilityTerm(100 * purpose + 47, personDay.SocialStops);
			alternative.AddUtilityTerm(100 * purpose + 50 + three, 1); // ASC
			alternative.AddUtilityTerm(100 * purpose + 23 + 2 * three, purposeLogsum); // accessibility effect has different coefficient for 2 and 3+
		}
	}
}