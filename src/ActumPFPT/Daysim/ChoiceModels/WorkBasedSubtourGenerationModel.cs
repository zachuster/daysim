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
	public class WorkBasedSubtourGenerationModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "WorkBasedSubtourGenerationModel";
		private const int TOTAL_ALTERNATIVES = Constants.Purpose.SOCIAL + 1;
		private const int TOTAL_NESTED_ALTERNATIVES = 2;
		private const int TOTAL_LEVELS = 2;
		private const int MAX_PARAMETER = 50;

		public int Run(ITourWrapper tour, int nCallsForTour, int choice = Constants.Purpose.NONE_OR_HOME) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.WorkBasedSubtourGenerationModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			
			tour.PersonDay.ResetRandom(30 + tour.Sequence + nCallsForTour - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return choice;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator((tour.Id * 397) ^ nCallsForTour);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (tour.PersonDay.TotalStops > 0) {
					RunModel(choiceProbabilityCalculator, tour, nCallsForTour, choice);

					choiceProbabilityCalculator.WriteObservation();
				}
			}
			else {
				if (tour.PersonDay.TotalStops > 0) {
					RunModel(choiceProbabilityCalculator, tour, nCallsForTour);

					var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(tour.Household.RandomUtility);
					choice = (int) chosenAlternative.Choice;
				}
				else {
					choice = Constants.Purpose.NONE_OR_HOME;
				}
			}
			
			return choice;
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ITourWrapper tour, int nCallsForTour, int choice = Constants.DEFAULT_VALUE) {
			var person = tour.Person;
			var personDay = tour.PersonDay;

//			var foodRetailServiceMedicalQtrMileLog = tour.DestinationParcel.FoodRetailServiceMedicalQtrMileLogBuffer1();
//			var mixedUseIndex = tour.DestinationParcel.MixedUse4Index1();
			var k8HighSchoolQtrMileLog = tour.DestinationParcel.K8HighSchoolQtrMileLogBuffer1();
			var carOwnership = person.CarOwnershipSegment;

			var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);
			var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership);
//			var notUsualWorkParcelFlag = tour.DestinationParcel.NotUsualWorkParcelFlag(person.UsualWorkParcelId);

			var votALSegment = tour.VotALSegment;

			var workTaSegment = tour.DestinationParcel.TransitAccessSegment();
			var workAggregateLogsum = Global.AggregateLogsums[tour.DestinationParcel.ZoneId]
				[Constants.Purpose.WORK_BASED][carOwnership][votALSegment][workTaSegment];

			// NONE_OR_HOME

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, true, choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;

			alternative.AddUtilityTerm(15, (nCallsForTour > 1).ToFlag());
			alternative.AddUtilityTerm(16, Math.Log(personDay.HomeBasedTours));
			alternative.AddUtilityTerm(18, personDay.HasTwoOrMoreWorkTours.ToFlag());
//			alternative.AddUtility(19, notUsualWorkParcelFlag);
			alternative.AddUtilityTerm(22, noCarsFlag);
			alternative.AddUtilityTerm(23, carCompetitionFlag);
			alternative.AddUtilityTerm(32, workAggregateLogsum);
//			alternative.AddUtility(32, mixedUseIndex);

			alternative.AddNestedAlternative(11, 0, 50);

			// WORK

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.WORK, personDay.WorkStops > 0, choice == Constants.Purpose.WORK);

			alternative.Choice = Constants.Purpose.WORK;

			alternative.AddUtilityTerm(1, 1);

			alternative.AddNestedAlternative(12, 1, 50);

			// SCHOOL

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SCHOOL, personDay.SchoolStops > 0, choice == Constants.Purpose.SCHOOL);

			alternative.Choice = Constants.Purpose.SCHOOL;

			alternative.AddUtilityTerm(3, 1);

			alternative.AddNestedAlternative(12, 1, 50);

			// ESCORT

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.ESCORT, personDay.EscortStops > 0, choice == Constants.Purpose.ESCORT);

			alternative.Choice = Constants.Purpose.ESCORT;

			alternative.AddUtilityTerm(4, 1);
			alternative.AddUtilityTerm(39, k8HighSchoolQtrMileLog);

			alternative.AddNestedAlternative(12, 1, 50);

			// PERSONAL_BUSINESS

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.PERSONAL_BUSINESS, personDay.PersonalBusinessStops > 0, choice == Constants.Purpose.PERSONAL_BUSINESS);

			alternative.Choice = Constants.Purpose.PERSONAL_BUSINESS;

			alternative.AddUtilityTerm(6, 1);

			alternative.AddNestedAlternative(12, 1, 50);

			// SHOPPING

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SHOPPING, personDay.ShoppingStops > 0, choice == Constants.Purpose.SHOPPING);

			alternative.Choice = Constants.Purpose.SHOPPING;

			alternative.AddUtilityTerm(8, 1);

			alternative.AddNestedAlternative(12, 1, 50);

			// MEAL

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.MEAL, personDay.MealStops > 0, choice == Constants.Purpose.MEAL);

			alternative.Choice = Constants.Purpose.MEAL;

			alternative.AddUtilityTerm(10, 1);

			alternative.AddNestedAlternative(12, 1, 50);

			// SOCIAL

			alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.SOCIAL, personDay.SocialStops > 0, choice == Constants.Purpose.SOCIAL);

			alternative.Choice = Constants.Purpose.SOCIAL;

			alternative.AddUtilityTerm(13, 1);

			alternative.AddNestedAlternative(12, 1, 50);
		}
	}
}