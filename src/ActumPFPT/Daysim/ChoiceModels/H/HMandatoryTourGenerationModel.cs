// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Daysim.Interfaces;


namespace Daysim.ChoiceModels.H {
	public static class HMandatoryTourGenerationModel {
		private const string CHOICE_MODEL_NAME = "HMandatoryTourGenerationModel";
		private const int TOTAL_ALTERNATIVES = 4;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 100;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		private static readonly object _lock = new object();

		private static void Initialize() {
			lock (_lock) {
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null) {
					return;
				}

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME,
													  Global.GetInputPath(Global.Configuration.MandatoryTourGenerationModelCoefficients), TOTAL_ALTERNATIVES,
													  TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static int Run(IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay, int nCallsForTour, int[] simulatedMandatoryTours, int choice = Constants.Purpose.NONE_OR_HOME) {

			// to know what the last choice was for a person on the previous step


			if (personDay == null) {
				throw new ArgumentNullException("personDay");
			}

			Initialize();

			personDay.Person.ResetRandom(904 + nCallsForTour);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return choice;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator((personDay.Person.Id * 397) ^ nCallsForTour);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {

				RunModel(choiceProbabilityCalculator, personDay, householdDay, nCallsForTour, simulatedMandatoryTours, choice);
				choiceProbabilityCalculator.WriteObservation();

			}
			else if (Global.Configuration.TestEstimationModelInApplicationMode)     {
				Global.Configuration.IsInEstimationMode = false;
				RunModel(choiceProbabilityCalculator, personDay, householdDay, nCallsForTour, simulatedMandatoryTours);
				var observedChoice = choice;
				var simulatedChoice = choiceProbabilityCalculator.SimulateChoice(personDay.Household.RandomUtility, personDay.Id, choice);
				Global.Configuration.IsInEstimationMode = true;
           }

//				else if (Global.Configuration.TestEstimationModelInApplicationMode==true){

//				Global.Configuration.IsInEstimationMode = false;

//				if (householdDay.Household.Id == 2015) {
//					bool testbreak = true;
//				}
//				RunModel(choiceProbabilityCalculator, personDay, householdDay, nCallsForTour, simulatedMandatoryTours);
				
				// need to determine the choice on this particular simulated tour
//				int[] totalMandatoryTours = new int[4];

//				totalMandatoryTours[1] = personDay.UsualWorkplaceTours;
//				totalMandatoryTours[2] = personDay.WorkTours - totalMandatoryTours[1];
//				totalMandatoryTours[3] = personDay.SchoolTours;
//				totalMandatoryTours[0] = totalMandatoryTours[1] + totalMandatoryTours[2] + totalMandatoryTours[3];
//				if (personDay.UsualWorkplaceTours + personDay.SchoolTours > 0) {
//							personDay.HasMandatoryTourToUsualLocation = true;
//						}
					
				//using nCallsForTour - 1 will give the choice
//				if (nCallsForTour - 1 < totalMandatoryTours[1]) { choice = 1; }
//				else if (nCallsForTour - 1 < totalMandatoryTours[1] + totalMandatoryTours[2]) { choice = 2; }
//				else if (nCallsForTour - 1 < totalMandatoryTours[0]) { choice = 3; }
//				else { choice = 0; }
					
//				var observedChoice = choice ;
//				var simulatedChoice =choiceProbabilityCalculator.SimulateChoice(personDay.Household.RandomUtility, personDay.Id, observedChoice);

//				int tourPurpose =0;

//					if ( simulatedChoice!= null)
//					{
//					tourPurpose = (int) simulatedChoice.Choice;
//					}
			
//				choice = tourPurpose;

//				Global.Configuration.IsInEstimationMode = true;
//			}

			else {
				if (householdDay.Household.Id == 2015) {
					bool testbreak = true;
				}
				RunModel(choiceProbabilityCalculator, personDay, householdDay, nCallsForTour, simulatedMandatoryTours);
				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(personDay.Household.RandomUtility);
				int tourPurpose = (int) chosenAlternative.Choice;
				if (tourPurpose == 1) {
					personDay.UsualWorkplaceTours++;
					personDay.WorkTours++;
				}
				else if (tourPurpose == 2) {
					personDay.WorkTours++;
				}
				else if (tourPurpose == 3) {
					personDay.SchoolTours++;
				}
				choice = tourPurpose;
			}

			return choice;
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay, int nCallsForTour, int[] simulatedMandatoryTours, int choice = Constants.DEFAULT_VALUE) {
			var household = personDay.Household;

			IEnumerable<PersonDayWrapper> orderedPersonDays = householdDay.PersonDays.OrderBy(p => p.JointTourParticipationPriority).ToList().Cast<PersonDayWrapper>();


			var carOwnership =
							household.VehiclesAvailable == 0
								 ? Constants.CarOwnership.NO_CARS
								 : household.VehiclesAvailable < household.HouseholdTotals.DrivingAgeMembers
									  ? Constants.CarOwnership.LT_ONE_CAR_PER_ADULT
									  : Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT;

			var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);
			var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership);

			var votALSegment = household.VotALSegment;
			var transitAccessSegment = household.ResidenceParcel.TransitAccessSegment();
			var personalBusinessAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.PERSONAL_BUSINESS][carOwnership][votALSegment][transitAccessSegment];
			var shoppingAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.SHOPPING][carOwnership][votALSegment][transitAccessSegment];
			var mealAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.MEAL][carOwnership][votALSegment][transitAccessSegment];
			var socialAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.SOCIAL][carOwnership][votALSegment][transitAccessSegment];

			var totAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];

			Double workTourLogsum;
			Double schoolZoneUniStu;
			Double schoolTourLogsum;

			int noUsualWorkZone = 1;


			if (personDay.Person.UsualWorkParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualWorkParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
				if (personDay.Person.UsualDeparturePeriodFromWork != Constants.DEFAULT_VALUE && personDay.Person.UsualArrivalPeriodToWork != Constants.DEFAULT_VALUE) {
					var nestedAlternative = HWorkTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, personDay.Person.UsualArrivalPeriodToWork, personDay.Person.UsualDeparturePeriodFromWork, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
					workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}
				else {
					var nestedAlternative = HWorkTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
					workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}

				noUsualWorkZone = 0;

			}
			else {
				workTourLogsum = 0;

			}

			if (personDay.Person.UsualSchoolParcelId != 0 && personDay.Person.UsualSchoolParcelId != -1 && personDay.Person.UsualSchoolParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
				schoolZoneUniStu = Math.Log(1 + (personDay.Person.UsualSchoolParcel.StudentsUniversityBuffer1));
				var schoolNestedAlternative = HSchoolTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualSchoolParcel, Constants.Time.EIGHT_AM, Constants.Time.TWO_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
				schoolTourLogsum = schoolNestedAlternative == null ? 0 : schoolNestedAlternative.ComputeLogsum();

			}
			else {
				schoolZoneUniStu = 0;
				schoolTourLogsum = 0;
			}

			int countNonMandatory = (from personDayHH in orderedPersonDays where personDayHH.PatternType == 2 select personDayHH.PatternType).Count();

			bool schoolAvailableFlag = true;
			if (!personDay.Person.IsStudent || (!Global.Configuration.IsInEstimationMode && personDay.Person.UsualSchoolParcel == null)){
				schoolAvailableFlag = false;
			}

			// NONE_OR_HOME

			var alternative = choiceProbabilityCalculator.GetAlternative(Constants.Purpose.NONE_OR_HOME, nCallsForTour > 1, choice == Constants.Purpose.NONE_OR_HOME);

			alternative.Choice = Constants.Purpose.NONE_OR_HOME;
			alternative.AddUtilityTerm(2, (nCallsForTour > 2).ToFlag());
			//alternative.AddUtilityTerm(3, (personDay.Person.WorksAtHome).ToFlag());

			// USUAL WORK
			alternative = choiceProbabilityCalculator.GetAlternative(1, (personDay.Person.UsualWorkParcelId > 0 && simulatedMandatoryTours[2] == 0 && simulatedMandatoryTours[3] == 0), choice == 1);
			alternative.Choice = 1;
			alternative.AddUtilityTerm(21, 1);
			alternative.AddUtilityTerm(23, workTourLogsum);
			alternative.AddUtilityTerm(25, (personDay.Person.IsPartTimeWorker).ToFlag());
			alternative.AddUtilityTerm(26, (personDay.Person.IsUniversityStudent).ToFlag());
			alternative.AddUtilityTerm(27, (personDay.Person.Household.Has0To25KIncome).ToFlag());
			alternative.AddUtilityTerm(28, (personDay.Person.Household.Has100KPlusIncome).ToFlag());
			alternative.AddUtilityTerm(30, (personDay.Person.Age <= 30).ToFlag());
			alternative.AddUtilityTerm(31, personDay.Person.TransitPassOwnershipFlag);
			//alternative.AddUtilityTerm(32, ((simulatedMandatoryTours[1] >0).ToFlag()));
			alternative.AddUtilityTerm(33, personDay.Person.PayToParkAtWorkplaceFlag);

			// OTHER WORK
			alternative = choiceProbabilityCalculator.GetAlternative(2, (personDay.Person.IsWorker && simulatedMandatoryTours[3] == 0), choice == 2);
			alternative.Choice = 2;
			alternative.AddUtilityTerm(41, 1);
			alternative.AddUtilityTerm(42, (personDay.Person.IsPartTimeWorker).ToFlag());
			alternative.AddUtilityTerm(43, (personDay.Person.IsUniversityStudent).ToFlag());
			alternative.AddUtilityTerm(47, (personDay.Person.Age <= 30).ToFlag());
			alternative.AddUtilityTerm(48, noUsualWorkZone);
			alternative.AddUtilityTerm(49, personDay.Person.TransitPassOwnershipFlag);
			alternative.AddUtilityTerm(50, totAggregateLogsum);
			alternative.AddUtilityTerm(51, countNonMandatory);
			alternative.AddUtilityTerm(52, ((simulatedMandatoryTours[2] > 0).ToFlag()));
			alternative.AddUtilityTerm(53, (household.HouseholdTotals.AllWorkers == 1).ToFlag());

			// SCHOOL
			alternative = choiceProbabilityCalculator.GetAlternative(3, schoolAvailableFlag, choice == 3);
			alternative.Choice = 3;
			alternative.AddUtilityTerm(61, 1);
			alternative.AddUtilityTerm(62, schoolTourLogsum);
			alternative.AddUtilityTerm(63, noCarsFlag + carCompetitionFlag);
			alternative.AddUtilityTerm(65, (personDay.Person.IsChildUnder5).ToFlag());
			alternative.AddUtilityTerm(66, (personDay.Person.IsUniversityStudent).ToFlag());
			alternative.AddUtilityTerm(67, (personDay.Person.IsDrivingAgeStudent).ToFlag());
			alternative.AddUtilityTerm(68, schoolZoneUniStu);
			alternative.AddUtilityTerm(70, personDay.Person.TransitPassOwnershipFlag);
			// alternative.AddUtilityTerm(71, ((simulatedMandatoryTours[3] > 0).ToFlag()));

		}
	}
}