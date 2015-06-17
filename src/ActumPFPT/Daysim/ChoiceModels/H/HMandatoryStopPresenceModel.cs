// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Linq;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;
using System.Collections.Generic;
using Daysim.Interfaces;


namespace Daysim.ChoiceModels.H {
	public static class HMandatoryStopPresenceModel {
		private const string CHOICE_MODEL_NAME = "HMandatoryStopPresenceModel";
		private const int TOTAL_ALTERNATIVES = 4;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 80;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		private static readonly object _lock = new object();

		private static void Initialize() {
			lock (_lock)
			{
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null)
				{
					return;
				}

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME,
				                             Global.GetInputPath(Global.Configuration.MandatoryStopPresenceModelCoefficients), TOTAL_ALTERNATIVES,
				                             TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			}
		}

		public static void Run(IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay) {
			if (personDay == null) {
				throw new ArgumentNullException("personDay");
			}

			Initialize();
			
			personDay.Person.ResetRandom(961); 

			int choice = 0;

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(personDay.Person.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {

				choice = Math.Min(personDay.WorkStops,1) + 2 * Math.Min(personDay.SchoolStops, 1);

				
				RunModel(choiceProbabilityCalculator, personDay, householdDay, choice);

				choiceProbabilityCalculator.WriteObservation();
			}
				else if (Global.Configuration.TestEstimationModelInApplicationMode){
				
				Global.Configuration.IsInEstimationMode = false;

                RunModel(choiceProbabilityCalculator, personDay, householdDay);

				var observedChoice = Math.Min(personDay.WorkStops,1) + 2 * Math.Min(personDay.SchoolStops, 1);
            
				var simulatedChoice = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility, personDay.Id, observedChoice);
				
				Global.Configuration.IsInEstimationMode = true;
			}
			else {
				RunModel(choiceProbabilityCalculator, personDay, householdDay);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(householdDay.Household.RandomUtility);
				choice = (int) chosenAlternative.Choice;

				if (choice == 1 || choice == 3) {
					personDay.WorkStops = 1;
				}
				if (choice == 2 || choice == 3) {
					personDay.SchoolStops = 1;
				}
			}
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, IPersonDayWrapper personDay, IHouseholdDayWrapper householdDay, int choice = Constants.DEFAULT_VALUE) {
            var household = personDay.Household;
            var person = personDay.Person;

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
            var totAggregateLogsum = Global.AggregateLogsums[household.ResidenceParcel.ZoneId]
                [Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment]; 
          
            //Double workTourLogsum=0;
            //Double schoolTourLogsum=0;
            //Double workParcelEmp=0;
            Double schoolParcelEmp = 0;
            //Double logDistToWork=0;
				//Double totAggregateLogsum_work =0;
				//Double totAggregateLogsum_school=0;

           /* if (personDay.Person.UsualWorkParcelId != Constants.DEFAULT_VALUE && personDay.Person.UsualWorkParcelId != Constants.OUT_OF_REGION_PARCEL_ID)
            {
                var nestedAlternative = WorkTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualWorkParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
                workTourLogsum = nestedAlternative == null ? 0 : nestedAlternative.ComputeLogsum();
					 totAggregateLogsum_work = Global.AggregateLogsums[person.UsualWorkParcel.ZoneId]
                [Constants.Purpose.HOME_BASED_COMPOSITE][carOwnership][votALSegment][transitAccessSegment]; 
					workParcelEmp =Math.Log(1+person.UsualWorkParcel.EmploymentTotalBuffer2);
            }
            else
            {
                workTourLogsum = 0;
            }
			   * */

            if (personDay.Person.UsualSchoolParcelId != 0 && personDay.Person.UsualSchoolParcelId != -1 && personDay.Person.UsualSchoolParcelId != Constants.OUT_OF_REGION_PARCEL_ID)
            {
                //var schoolNestedAlternative = WorkTourModeModel.RunNested(personDay.Person, personDay.Person.Household.ResidenceParcel, personDay.Person.UsualSchoolParcel, Constants.Time.EIGHT_AM, Constants.Time.FIVE_PM, personDay.Person.Household.HouseholdTotals.DrivingAgeMembers);
                //schoolTourLogsum = schoolNestedAlternative == null ? 0 : schoolNestedAlternative.ComputeLogsum();
                schoolParcelEmp = Math.Log(1 + person.UsualSchoolParcel.EmploymentTotalBuffer2);
            }
			  
      
            // No mandatory stops
            var alternative = choiceProbabilityCalculator.GetAlternative(0, true, choice == 0);
            alternative.Choice = 0;

			  // Work stop(s)
            alternative = choiceProbabilityCalculator.GetAlternative(1, personDay.Person.IsWorker, choice == 1);
            alternative.Choice = 1;
            alternative.AddUtilityTerm(21, 1);
				alternative.AddUtilityTerm(22, (personDay.WorkTours==1).ToFlag());
				alternative.AddUtilityTerm(23, (personDay.WorkTours>1).ToFlag());
           // alternative.AddUtilityTerm(24, household.HasChildrenUnder5.ToFlag() * (person.IsAdultFemale).ToFlag());
            alternative.AddUtilityTerm(25, household.HasChildrenAge5Through15.ToFlag());
           // alternative.AddUtilityTerm(26, (person.AgeIsBetween51And65).ToFlag());
           // alternative.AddUtilityTerm(27, (person.Age>65).ToFlag());
				alternative.AddUtilityTerm(28, (personDay.SchoolTours>0).ToFlag());
            alternative.AddUtilityTerm(29, person.TransitPassOwnershipFlag);
				alternative.AddUtilityTerm(30, totAggregateLogsum);
            alternative.AddUtilityTerm(31, person.IsPartTimeWorker.ToFlag());
            alternative.AddUtilityTerm(33, household.Has100KPlusIncome.ToFlag());
            alternative.AddUtilityTerm(35, person.PayToParkAtWorkplaceFlag);
            alternative.AddUtilityTerm(36, (household.HouseholdTotals.AllWorkers==2).ToFlag());
				alternative.AddUtilityTerm(37, (household.HouseholdTotals.AllWorkers>2).ToFlag());

            // School stop(s)
            alternative = choiceProbabilityCalculator.GetAlternative(2, personDay.Person.IsStudent, choice == 2);
            alternative.Choice = 2;
            alternative.AddUtilityTerm(41, 1);
            alternative.AddUtilityTerm(42, (personDay.SchoolTours==0).ToFlag());
            alternative.AddUtilityTerm(45, person.IsChildUnder5.ToFlag());
            alternative.AddUtilityTerm(46, person.IsUniversityStudent.ToFlag());
            alternative.AddUtilityTerm(47, schoolParcelEmp);
            //alternative.AddUtilityTerm(48, household.Size);
				 alternative.AddUtilityTerm(49, (household.HouseholdTotals.AllWorkers>=2).ToFlag());
            //alternative.AddUtilityTerm(50, carCompetitionFlag + noCarsFlag);
            alternative.AddUtilityTerm(51, person.TransitPassOwnershipFlag);
				//alternative.AddUtilityTerm(52, (household.HouseholdTotals.ChildrenUnder16==2).ToFlag());
				alternative.AddUtilityTerm(53, (household.HouseholdTotals.ChildrenUnder16>2).ToFlag());

            // Work and school stops
            alternative = choiceProbabilityCalculator.GetAlternative(3, (personDay.Person.IsWorker && personDay.Person.IsStudent), choice == 3);
            alternative.Choice = 3;
            alternative.AddUtilityTerm(61, 1);
            
  

		}
	}
}