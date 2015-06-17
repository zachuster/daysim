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
using Daysim.DomainModels.Actum;
using System.Collections.Generic;


namespace Daysim.ChoiceModels.Actum {
	public class ActumPrimaryPriorityTimeModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "ActumPrimaryPriorityTimeModel";
		private const int TOTAL_ALTERNATIVES = 4;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 99;

		public void Run(ActumHouseholdDayWrapper householdDay) {
			if (householdDay == null) {
				throw new ArgumentNullException("householdDay");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.ActumPrimaryPriorityTimeModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);

			householdDay.ResetRandom(901);

			// IEnumerable<PersonWrapper> personTypeOrderedPersons = householdDay.Household.Persons.OrderBy(p=> p.PersonType).ToList();



			if (Global.Configuration.IsInEstimationMode) {

				if (householdDay.SharedActivityHomeStays >= 1
					//&& householdDay.DurationMinutesSharedHomeStay >=60 
					 && householdDay.AdultsInSharedHomeStay >= 1
					 && householdDay.NumberInLargestSharedHomeStay >= (householdDay.Household.Size)
					 ) {
					householdDay.PrimaryPriorityTimeFlag = 1;
				}
				else householdDay.PrimaryPriorityTimeFlag = 0;
				if (householdDay.JointTours > 0) {
					householdDay.JointTourFlag = 1;
				}
				else {
					householdDay.JointTourFlag = 0;
				}

				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(householdDay.Household.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {

				//				// set choice variable here  (derive from available household properties)
				//				if (householdDay.SharedActivityHomeStays >= 1 
				//					//&& householdDay.DurationMinutesSharedHomeStay >=60 
				//					&& householdDay.AdultsInSharedHomeStay >= 1 
				//					&& householdDay.NumberInLargestSharedHomeStay >= (householdDay.Household.Size)
				//                   )
				//				{
				//					householdDay.PrimaryPriorityTimeFlag = 1;  
				//				}
				//				else 	householdDay.PrimaryPriorityTimeFlag = 0;

				int choice = householdDay.PrimaryPriorityTimeFlag + 2 * (householdDay.JointTours > 0 ? 1 : 0);

				RunModel(choiceProbabilityCalculator, householdDay, choice);

				choiceProbabilityCalculator.WriteObservation();
			}

			else if (Global.Configuration.TestEstimationModelInApplicationMode) {
				Global.Configuration.IsInEstimationMode = false;

				RunModel(choiceProbabilityCalculator, householdDay);

				//var observedChoice = new HTourModeTime(tour.Mode, tour.DestinationArrivalTime, tour.DestinationDepartureTime);

				var simulatedChoice = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility, householdDay.Household.Id, householdDay.PrimaryPriorityTimeFlag);

				Global.Configuration.IsInEstimationMode = true;
			}

			else {
				RunModel(choiceProbabilityCalculator, householdDay);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
				var choice = (int) chosenAlternative.Choice;

				if (choice == 0) {
					householdDay.PrimaryPriorityTimeFlag = 0;
					householdDay.JointTourFlag = 0;
				}
				else if (choice == 1) {
					householdDay.PrimaryPriorityTimeFlag = 1;
					householdDay.JointTourFlag = 0;
				}
				else if (choice == 2) {
					householdDay.PrimaryPriorityTimeFlag = 0;
					householdDay.JointTourFlag = 1;
				}
				else { // if (choice == 3) {
					householdDay.PrimaryPriorityTimeFlag = 1;
					householdDay.JointTourFlag = 1;
				}
			}
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ActumHouseholdDayWrapper householdDay, int choice = Constants.DEFAULT_VALUE) {

			var household = householdDay.Household;
			var residenceParcel = household.ResidenceParcel;

			// set household characteristics here that don't depend on person characteristics

			int hasAdultEducLevel12 = 0;
			int allAdultEducLevel12 = 1;
			int youngestAge = 999;


			double firstWorkLogsum = 0;
			double secondWorkLogsum = 0;
			bool firstWorkLogsumIsSet = false;
			bool secondWorkLogsumIsSet = false;
			int numberWorkers = 0;
			int numberAdults = 0;
			int numberChildren = 0;
			int numberChildrenUnder5 = 0;


			// set characteristics here that depend on person characteristics
			foreach (ActumPersonDayWrapper personDay in householdDay.PersonDays) {
				double workLogsum = 0;
				var person = (ActumPersonWrapper) personDay.Person;
				if (person.UsualWorkParcel == null || person.UsualWorkParcelId == household.ResidenceParcelId
					|| (person.PersonType != Constants.PersonType.FULL_TIME_WORKER
					&& person.PersonType != Constants.PersonType.PART_TIME_WORKER))
				//	|| household.VehiclesAvailable == 0) 
				{
				}
				else {
					var destinationArrivalTime = ChoiceModelUtility.GetDestinationArrivalTime(Constants.Model.WORK_TOUR_MODE_MODEL);
					var destinationDepartureTime = ChoiceModelUtility.GetDestinationDepartureTime(Constants.Model.WORK_TOUR_MODE_MODEL);
					var nestedAlternative = (Global.ChoiceModelDictionary.Get("ActumWorkTourModeModel") as ActumWorkTourModeModel).RunNested(personDay, residenceParcel, person.UsualWorkParcel, destinationArrivalTime, destinationDepartureTime, household.VehiclesAvailable);
					workLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
				}
				if (person.Age >= 18 && person.EducationLevel >= 12) hasAdultEducLevel12 = 1;
				if (person.Age >= 18 && person.EducationLevel < 12) allAdultEducLevel12 = 0;
				if (person.Age < youngestAge) youngestAge = person.Age;
				if (workLogsum != 0 && !firstWorkLogsumIsSet) {
					firstWorkLogsum = workLogsum;
					firstWorkLogsumIsSet = true;
				}
				else if (workLogsum != 0 && !secondWorkLogsumIsSet) {
					secondWorkLogsum = workLogsum;
					secondWorkLogsumIsSet = true;
				}
				if (person.Age >= 18) {
					numberAdults++;
					if (person.PersonType == Constants.PersonType.FULL_TIME_WORKER
						//|| person.PersonType == Constants.PersonType.PART_TIME_WORKER
						) {
						numberWorkers++;
					}
				}
				else {
					numberChildren++;
					if (person.PersonType == Constants.PersonType.CHILD_UNDER_5) {
						numberChildrenUnder5++;
					}
				}
			}
			var singleWorkerWithChildUnder5 = (numberAdults == numberWorkers && numberAdults == 1
				&& numberChildrenUnder5 > 0) ? true : false;
			var workingCoupleNoChildren = (numberAdults == numberWorkers && numberAdults == 2
				&& numberChildren == 0) ? true : false;
			var workingCoupleAllChildrenUnder5 = (numberAdults == numberWorkers && numberAdults == 2
				&& numberChildren > 0 && numberChildren == numberChildrenUnder5) ? true : false;
			var otherHouseholdWithPTFTWorkers = false;
			if (!workingCoupleNoChildren && !workingCoupleAllChildrenUnder5 && firstWorkLogsum != 0) {
				otherHouseholdWithPTFTWorkers = true;
			}
			var nonWorkingHousehold = false;
			if (!workingCoupleNoChildren && !workingCoupleAllChildrenUnder5 && !otherHouseholdWithPTFTWorkers) {
				nonWorkingHousehold = true;
			}


			// var votSegment = household.VotALSegment;
			//var taSegment = household.ResidenceParcel.TransitAccessSegment();

			//var aggregateLogsumDifference = // full car ownership vs. no car ownership
			//	Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT][votSegment][taSegment] -
			//	Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.NO_CARS][votSegment][taSegment];


			//var householdDay = (ActumHouseholdDayWrapper)tour.HouseholdDay;

			var carOwnership =
							household.VehiclesAvailable == 0
								 ? Constants.CarOwnership.NO_CARS
								 : household.VehiclesAvailable < household.HouseholdTotals.DrivingAgeMembers
									  ? Constants.CarOwnership.LT_ONE_CAR_PER_ADULT
									  : Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT;

			var noCarsFlag = AggregateLogsumsCalculator.GetNoCarsFlag(carOwnership);
			var carCompetitionFlag = AggregateLogsumsCalculator.GetCarCompetitionFlag(carOwnership);

			var votALSegment = Constants.VotALSegment.MEDIUM;  // TODO:  calculate a VOT segment that depends on household income
			var transitAccessSegment = household.ResidenceParcel.TransitAccessSegment();
			var personalBusinessAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.PERSONAL_BUSINESS][carOwnership][votALSegment][transitAccessSegment];
			var shoppingAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.SHOPPING][carOwnership][votALSegment][transitAccessSegment];
			var mealAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.MEAL][carOwnership][votALSegment][transitAccessSegment];
			var socialAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
				 [Constants.Purpose.SOCIAL][carOwnership][votALSegment][transitAccessSegment];
			//var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment];
			var compositeLogsum = Global.AggregateLogsums[household.ResidenceZoneId][Constants.Purpose.HOME_BASED_COMPOSITE][Constants.CarOwnership.NO_CARS][votALSegment][transitAccessSegment];

			var componentIndex = 0;
			for (int pfpt = 0; pfpt < 2; pfpt++) {
				if (pfpt == 1) {
					componentIndex = 1;
					choiceProbabilityCalculator.CreateUtilityComponent(componentIndex);
					var pfptComponent = choiceProbabilityCalculator.GetUtilityComponent(componentIndex);
					pfptComponent.AddUtilityTerm(1, (householdDay.Household.Size == 3).ToFlag());
					pfptComponent.AddUtilityTerm(2, (householdDay.Household.Size >= 4).ToFlag());
					pfptComponent.AddUtilityTerm(3, householdDay.Household.HasChildrenUnder5.ToFlag());
					pfptComponent.AddUtilityTerm(4, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildrenAge5Through15).ToFlag());
					pfptComponent.AddUtilityTerm(5, (householdDay.AdultsInSharedHomeStay == 2 && householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
					pfptComponent.AddUtilityTerm(6, (householdDay.AdultsInSharedHomeStay == 2 && hasAdultEducLevel12 == 1).ToFlag());
					pfptComponent.AddUtilityTerm(7, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());
					pfptComponent.AddUtilityTerm(8, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());

					pfptComponent.AddUtilityTerm(11, (householdDay.Household.Income >= 300000 && householdDay.Household.Income < 600000).ToFlag());
					pfptComponent.AddUtilityTerm(12, (householdDay.Household.Income >= 600000 && householdDay.Household.Income < 900000).ToFlag());
					pfptComponent.AddUtilityTerm(13, (householdDay.Household.Income >= 900000).ToFlag());

					// OBS; 27. aug., work tour mode logsum does not work - see what happens in the old PFPT model 
					// GV, sep. 1st - it is not significant                    
					pfptComponent.AddUtilityTerm(15, (firstWorkLogsum + secondWorkLogsum) * 
					    (workingCoupleNoChildren || workingCoupleAllChildrenUnder5).ToFlag());
					pfptComponent.AddUtilityTerm(15, (firstWorkLogsum + secondWorkLogsum) * otherHouseholdWithPTFTWorkers.ToFlag());

					// dette er gamle at-work logsum - it should be plus and significant
					//alternative.AddUtilityTerm(31, (firstWorkLogsum + secondWorkLogsum) *
					//(workingCoupleNoChildren || workingCoupleAllChildrenUnder5).ToFlag());
					//alternative.AddUtilityTerm(31, (firstWorkLogsum + secondWorkLogsum) * otherHouseholdWithPTFTWorkers.ToFlag());

					// at-home logsum works
					pfptComponent.AddUtilityTerm(16, compositeLogsum);

				}
			}
			for (var jointTourFlag = 0; jointTourFlag < 2; jointTourFlag++) {
				if (jointTourFlag == 1) {
					componentIndex = 2;
					choiceProbabilityCalculator.CreateUtilityComponent(componentIndex);
					var jointComponent = choiceProbabilityCalculator.GetUtilityComponent(componentIndex);

					jointComponent.AddUtilityTerm(21, (householdDay.Household.Size == 3).ToFlag());
					jointComponent.AddUtilityTerm(22, (householdDay.Household.Size >= 4).ToFlag());

					// GV: 1st sep.
					//jointComponent.AddUtilityTerm(23, (householdDay.Household.Size == 2 && householdDay.Household.HasChildren).ToFlag());
					//jointComponent.AddUtilityTerm(23, (householdDay.Household.Size >= 2 && householdDay.Household.HasChildren).ToFlag());
					jointComponent.AddUtilityTerm(23, (householdDay.Household.HasChildren).ToFlag());

					//jointComponent.AddUtilityTerm(21, householdDay.Household.HasChildrenUnder5.ToFlag());
					//jointComponent.AddUtilityTerm(22, householdDay.Household.HasChildrenAge5Through15.ToFlag());

					//jointComponent.AddUtilityTerm(23, householdDay.Household.HasChildren.ToFlag());

					jointComponent.AddUtilityTerm(24, (householdDay.Household.Size == 2 && householdDay.AdultsInSharedHomeStay == 2).ToFlag());
					//jointComponent.AddUtilityTerm(25, (householdDay.AdultsInSharedHomeStay == 2 && householdDay.Household.HouseholdTotals.FullAndPartTimeWorkers >= 2).ToFlag());
					jointComponent.AddUtilityTerm(26, (householdDay.AdultsInSharedHomeStay == 2 && hasAdultEducLevel12 == 1).ToFlag());

					//jointComponent.AddUtilityTerm(27, (householdDay.Household.Size == 2 && householdDay.Household.HasChildrenUnder5).ToFlag());
					//jointComponent.AddUtilityTerm(28, (householdDay.Household.Size == 2 && householdDay.Household.HasChildrenAge5Through15).ToFlag());
					//jointComponent.AddUtilityTerm(27, (householdDay.Household.Size == 2 && householdDay.Household.HasChildrenUnder16).ToFlag());

					jointComponent.AddUtilityTerm(29, (householdDay.AdultsInSharedHomeStay == 1 && householdDay.Household.HasChildrenUnder16).ToFlag());

					//jointComponent.AddUtilityTerm(37, (householdDay.Household.VehiclesAvailable == 1 && household.Has2Drivers).ToFlag());
					//jointComponent.AddUtilityTerm(38, (householdDay.Household.VehiclesAvailable >= 2 && household.Has2Drivers).ToFlag());
					jointComponent.AddUtilityTerm(30, (householdDay.Household.VehiclesAvailable >= 1 && household.Has2Drivers).ToFlag());

					jointComponent.AddUtilityTerm(31, (householdDay.Household.Income >= 300000 && householdDay.Household.Income < 600000).ToFlag());
					jointComponent.AddUtilityTerm(32, (householdDay.Household.Income >= 600000 && householdDay.Household.Income < 900000).ToFlag());
					jointComponent.AddUtilityTerm(33, (householdDay.Household.Income >= 900000).ToFlag());

					// GV, sep. 1st - it is not significant 
					//jointComponent.AddUtilityTerm(41, compositeLogsum);

				}
			}

			var available = true;
			for (int pfpt = 0; pfpt < 2; pfpt++) {
				for (var jointTourFlag = 0; jointTourFlag < 2; jointTourFlag++) {

					var altIndex = pfpt + jointTourFlag * 2;
					if (household.Size < 2 && altIndex > 0) {
						available = false;
					}
					else {
						available = true;
					}
					var alternative = choiceProbabilityCalculator.GetAlternative(altIndex, available, choice != null && choice == altIndex);

					alternative.Choice = altIndex;

					//NESTING WAS REJECTED BY TESTS
					//alternative.AddNestedAlternative(5 + pfpt,          pfpt, THETA_PARAMETER);  // pfpt on top
					//alternative.AddNestedAlternative(5 + jointTourFlag, jointTourFlag, THETA_PARAMETER); //jointTourFlag on top

					if (pfpt == 1) {
						alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(1));
						//alternative.AddUtilityTerm(20, 1);

					}
					if (jointTourFlag == 1) {
						alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(2));
						//alternative.AddUtilityTerm(40, 1);
					}

					if (pfpt == 0 && jointTourFlag == 0) {
					}
					else if (pfpt == 1 && jointTourFlag == 0) {
						alternative.AddUtilityTerm(51, 1);
					}
					else if (pfpt == 0 && jointTourFlag == 1) {
						alternative.AddUtilityTerm(61, 1);
					}
					else if (pfpt == 1 && jointTourFlag == 1) {
						alternative.AddUtilityTerm(71, 1);
						//alternative.AddUtilityTerm(72, (householdDay.Household.Size == 2 && householdDay.AdultsInSharedHomeStay == 2).ToFlag());

						// GV: comented out sep. 1st    
						//alternative.AddUtilityTerm(73, householdDay.Household.HasChildren.ToFlag());
						//alternative.AddUtilityTerm(73, householdDay.Household.HasChildrenUnder16.ToFlag());

					}
				}
			}
		}
	}
}
