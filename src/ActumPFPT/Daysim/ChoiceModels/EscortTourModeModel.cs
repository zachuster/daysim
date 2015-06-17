// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.Linq;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Interfaces;
using Daysim.ModelRunners;

namespace Daysim.ChoiceModels {
	public class EscortTourModeModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "EscortTourModeModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 111;

		public void Run(ITourWrapper tour) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.EscortTourModeModelCoefficients, Constants.Mode.TOTAL_MODES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			
			tour.PersonDay.ResetRandom(40 + tour.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(tour.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (tour.DestinationParcel == null || (tour.Mode > Constants.Mode.HOV3 || tour.Mode < Constants.Mode.WALK)) {
					return;
				}

				var pathTypeModels =
					PathTypeModel.Run(
					tour.Household.RandomUtility,
						tour.OriginParcel,
						tour.DestinationParcel,
						tour.DestinationArrivalTime,
						tour.DestinationDepartureTime,
						tour.DestinationPurpose,
						tour.CostCoefficient,
						tour.TimeCoefficient,
						tour.Person.IsDrivingAge,
						tour.Household.VehiclesAvailable,
						tour.Person.TransitFareDiscountFraction,
						false,
						Constants.Mode.WALK, Constants.Mode.BIKE, Constants.Mode.SOV, Constants.Mode.HOV2, Constants.Mode.HOV3);

				var pathTypeModel = pathTypeModels.First(x => x.Mode == tour.Mode);

				if (!pathTypeModel.Available) {
					return;
				}

				RunModel(choiceProbabilityCalculator, tour, pathTypeModels, tour.DestinationParcel, tour.Mode);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				var pathTypeModels =
					PathTypeModel.Run(
					tour.Household.RandomUtility,
						tour.OriginParcel,
						tour.DestinationParcel,
						tour.DestinationArrivalTime,
						tour.DestinationDepartureTime,
						tour.DestinationPurpose,
						tour.CostCoefficient,
						tour.TimeCoefficient,
						tour.Person.IsDrivingAge,
						tour.Household.VehiclesAvailable,
						tour.Person.TransitFareDiscountFraction,
						false,
						Constants.Mode.WALK, Constants.Mode.BIKE, Constants.Mode.SOV, Constants.Mode.HOV2, Constants.Mode.HOV3);

				RunModel(choiceProbabilityCalculator, tour, pathTypeModels, tour.DestinationParcel);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(tour.Household.RandomUtility);

				if (chosenAlternative == null) {
					Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", tour.PersonDay.Id);
					tour.Mode = Constants.Mode.HOV3;
					tour.PersonDay.IsValid = false;
					return;
				}

				var choice = (int) chosenAlternative.Choice;

				tour.Mode = choice;
				var chosenPathType = pathTypeModels.First(x => x.Mode == choice);
				tour.PathType = chosenPathType.PathType;
				tour.ParkAndRideNodeId = choice == Constants.Mode.PARK_AND_RIDE ? chosenPathType.PathParkAndRideNodeId : 0;
			}
		}

		public ChoiceProbabilityCalculator.Alternative RunNested(ITourWrapper tour, ICondensedParcel destinationParcel) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.EscortTourModeModelCoefficients, Constants.Mode.TOTAL_MODES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			
			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetNestedChoiceProbabilityCalculator();

			var pathTypeModels =
				PathTypeModel.Run(
				tour.Household.RandomUtility,
					tour.OriginParcel,
					destinationParcel,
					tour.DestinationArrivalTime,
					tour.DestinationDepartureTime,
					Constants.Purpose.ESCORT,
					tour.CostCoefficient,
					tour.TimeCoefficient,
					tour.Person.IsDrivingAge,
					tour.Household.VehiclesAvailable,
					tour.Person.TransitFareDiscountFraction,
					false,
					Constants.Mode.WALK, Constants.Mode.BIKE, Constants.Mode.SOV, Constants.Mode.HOV2, Constants.Mode.HOV3);

			RunModel(choiceProbabilityCalculator, tour, pathTypeModels, destinationParcel);
			
			return choiceProbabilityCalculator.SimulateChoice(tour.Household.RandomUtility);
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ITourWrapper tour, IEnumerable<PathTypeModel> pathTypeModels, ICondensedParcel destinationParcel, int choice = Constants.DEFAULT_VALUE) {
			var household = tour.Household;
			var householdTotals = household.HouseholdTotals;
			var person = tour.Person;

			// household inputs
			var childrenUnder5 = householdTotals.ChildrenUnder5;
			var childrenAge5Through15 = householdTotals.ChildrenAge5Through15;
			var drivingAgeStudents = householdTotals.DrivingAgeStudents;
			var noCarsInHouseholdFlag = HouseholdWrapper.GetNoCarsInHouseholdFlag(household.VehiclesAvailable);
			var carsLessThanDriversFlag = household.GetCarsLessThanDriversFlag(household.VehiclesAvailable);

			// person inputs
			var ageBetween51And98Flag = person.AgeIsBetween51And98.ToFlag();

			// other inputs

			foreach (var pathTypeModel in pathTypeModels) {
				var mode = pathTypeModel.Mode;
				var available = (mode <= Constants.Mode.HOV3 && mode >= Constants.Mode.WALK) && pathTypeModel.Available;
				var generalizedTimeLogsum = pathTypeModel.GeneralizedTimeLogsum;

				var alternative = choiceProbabilityCalculator.GetAlternative(mode, available, choice == mode);
				alternative.Choice = mode;

				if (!available) {
					continue;
				}

				switch (mode) {
					case Constants.Mode.HOV3:
						alternative.AddUtilityTerm(2, generalizedTimeLogsum * tour.TimeCoefficient);
						alternative.AddUtilityTerm(30, 1);
						alternative.AddUtilityTerm(31, childrenUnder5);
						alternative.AddUtilityTerm(32, childrenAge5Through15);
						alternative.AddUtilityTerm(33, drivingAgeStudents);
						alternative.AddUtilityTerm(41, noCarsInHouseholdFlag);

						break;

					case Constants.Mode.HOV2:
						alternative.AddUtilityTerm(2, generalizedTimeLogsum * tour.TimeCoefficient);
						alternative.AddUtilityTerm(40, 1);
						alternative.AddUtilityTerm(41, noCarsInHouseholdFlag);
						alternative.AddUtilityTerm(42, carsLessThanDriversFlag);

						break;

					case Constants.Mode.SOV:
						alternative.AddUtilityTerm(50, 1);

						break;

					case Constants.Mode.BIKE:
						alternative.AddUtilityTerm(60, 1);

						break;

					case Constants.Mode.WALK:
						alternative.AddUtilityTerm(2, generalizedTimeLogsum * tour.TimeCoefficient);
						alternative.AddUtilityTerm(73, ageBetween51And98Flag);
						alternative.AddUtilityTerm(76, destinationParcel.NetIntersectionDensity1());
						alternative.AddUtilityTerm(81, childrenUnder5);
						alternative.AddUtilityTerm(82, childrenAge5Through15);
						alternative.AddUtilityTerm(83, drivingAgeStudents);

						break;
				}
			}
		}
	}
}