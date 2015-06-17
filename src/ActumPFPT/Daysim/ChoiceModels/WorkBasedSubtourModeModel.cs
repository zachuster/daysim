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
	public class WorkBasedSubtourModeModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "WorkBasedSubtourModeModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 4;
		private const int TOTAL_LEVELS = 2;
		private const int MAX_PARAMETER = 199;
		private const int THETA_PARAMETER = 99;

		private readonly int[] _nestedAlternativeIds = new[] {0, 19, 19, 20, 21, 21, 22, 0, 0};
		private readonly int[] _nestedAlternativeIndexes = new[] {0, 0, 0, 1, 2, 2, 3, 0, 0};

		public void Run(ITourWrapper subtour) {
			if (subtour == null) {
				throw new ArgumentNullException("subtour");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.WorkBasedSubtourModeModelCoefficients, Constants.Mode.TOTAL_MODES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			
			subtour.PersonDay.ResetRandom(40 + subtour.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(subtour.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (subtour.DestinationParcel == null || subtour.Mode <= Constants.Mode.NONE || subtour.Mode > Constants.Mode.TRANSIT) {
					return;
				}

				var pathTypeModels =
					PathTypeModel.RunAll(
					subtour.Household.RandomUtility,
						subtour.OriginParcel,
						subtour.DestinationParcel,
						subtour.DestinationArrivalTime,
						subtour.DestinationDepartureTime,
						subtour.DestinationPurpose,
						subtour.CostCoefficient,
						subtour.TimeCoefficient,
						subtour.Person.IsDrivingAge,
						subtour.Household.VehiclesAvailable,
						subtour.Person.TransitFareDiscountFraction,
						false);

				var pathTypeModel = pathTypeModels.First(x => x.Mode == subtour.Mode);

				if (!pathTypeModel.Available) {
					return;
				}

				RunModel(choiceProbabilityCalculator, subtour, pathTypeModels, subtour.DestinationParcel, subtour.ParentTour.Mode, subtour.Mode);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				var pathTypeModels =
					PathTypeModel.RunAll(
					subtour.Household.RandomUtility,
						subtour.OriginParcel,
						subtour.DestinationParcel,
						subtour.DestinationArrivalTime,
						subtour.DestinationDepartureTime,
						subtour.DestinationPurpose,
						subtour.CostCoefficient,
						subtour.TimeCoefficient,
						subtour.Person.IsDrivingAge,
						subtour.Household.VehiclesAvailable,
						subtour.Person.TransitFareDiscountFraction,
						false);

				RunModel(choiceProbabilityCalculator, subtour, pathTypeModels, subtour.DestinationParcel, subtour.ParentTour.Mode);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(subtour.Household.RandomUtility);

				if (chosenAlternative == null) {
					Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", subtour.PersonDay.Id);
					subtour.Mode = Constants.Mode.HOV3;
					subtour.PersonDay.IsValid = false;
					return;
				}
				var choice = (int) chosenAlternative.Choice;

				subtour.Mode = choice;
				var chosenPathType = pathTypeModels.First(x => x.Mode == choice);
				subtour.PathType = chosenPathType.PathType;
				subtour.ParkAndRideNodeId = choice == Constants.Mode.PARK_AND_RIDE ? chosenPathType.PathParkAndRideNodeId : 0;
			}
		}


		public ChoiceProbabilityCalculator.Alternative RunNested(ITourWrapper subtour, ICondensedParcel destinationParcel) {
			if (subtour == null) {
				throw new ArgumentNullException("subtour");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.WorkBasedSubtourModeModelCoefficients, Constants.Mode.TOTAL_MODES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			
			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetNestedChoiceProbabilityCalculator();

			var pathTypeModels =
				PathTypeModel.RunAll(
				subtour.Household.RandomUtility,
					subtour.OriginParcel,
					destinationParcel,
					subtour.DestinationArrivalTime,
					subtour.DestinationDepartureTime,
					subtour.DestinationPurpose,
					subtour.CostCoefficient,
					subtour.TimeCoefficient,
					subtour.Person.IsDrivingAge,
					subtour.Household.VehiclesAvailable,
					subtour.Person.TransitFareDiscountFraction,
					false);

			RunModel(choiceProbabilityCalculator, subtour, pathTypeModels, destinationParcel, subtour.ParentTour.Mode);

			return choiceProbabilityCalculator.SimulateChoice(subtour.Household.RandomUtility);
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ITourWrapper subtour, IEnumerable<PathTypeModel> pathTypeModels, ICondensedParcel destinationParcel, int parentTourMode, int choice = Constants.DEFAULT_VALUE) {
			var household = subtour.Household;
			var person = subtour.Person;
			var personDay = subtour.PersonDay;

			// household inputs
			var income0To25KFlag = household.Has0To25KIncome.ToFlag();
			var income25To50KFlag = household.Has25To50KIncome.ToFlag();

			// person inputs
			var maleFlag = person.IsMale.ToFlag();

			// tour inputs
			var sovTourFlag = (parentTourMode == Constants.Mode.SOV).ToFlag();
			var hov2TourFlag = (parentTourMode == Constants.Mode.HOV2).ToFlag();
			var bikeTourFlag = (parentTourMode == Constants.Mode.BIKE).ToFlag();
			var walkTourFlag = (parentTourMode == Constants.Mode.WALK).ToFlag();

			// remaining inputs
//			var originParcel = subtour.OriginParcel;
			var parkingDuration = ChoiceModelUtility.GetParkingDuration(person.IsFulltimeWorker);
			var destinationParkingCost = destinationParcel.ParkingCostBuffer1(parkingDuration);

			double escortPercentage;
			double nonEscortPercentage;

			ChoiceModelUtility.SetEscortPercentages(personDay, out escortPercentage, out nonEscortPercentage, true);

			foreach (var pathTypeModel in pathTypeModels) {
				var mode = pathTypeModel.Mode;
				var available = pathTypeModel.Mode != Constants.Mode.PARK_AND_RIDE && pathTypeModel.Available;
				var generalizedTimeLogsum = pathTypeModel.GeneralizedTimeLogsum;

				var alternative = choiceProbabilityCalculator.GetAlternative(mode, available, choice == mode);
				alternative.Choice = mode;

				alternative.AddNestedAlternative(_nestedAlternativeIds[pathTypeModel.Mode], _nestedAlternativeIndexes[pathTypeModel.Mode], THETA_PARAMETER);

				if (!available) {
					continue;
				}

				alternative.AddUtilityTerm(2, generalizedTimeLogsum * subtour.TimeCoefficient);

				switch (mode) {
					case Constants.Mode.TRANSIT:
						alternative.AddUtilityTerm(20, 1);
//						alternative.AddUtility(129, destinationParcel.MixedUse2Index1());
//						alternative.AddUtility(128, destinationParcel.TotalEmploymentDensity1());
//						alternative.AddUtility(127, destinationParcel.NetIntersectionDensity1());
//						alternative.AddUtility(126, originParcel.NetIntersectionDensity1());
//						alternative.AddUtility(125, originParcel.TotalEmploymentDensity1());
//						alternative.AddUtility(124, originParcel.MixedUse2Index1());
//						alternative.AddUtility(123, Math.Log(destinationParcel.StopsTransitBuffer1+1));
//						alternative.AddUtility(122, Math.Log(originParcel.StopsTransitBuffer1+1));

						break;
					case Constants.Mode.HOV3:
						alternative.AddUtilityTerm(1, (destinationParkingCost * subtour.CostCoefficient / ChoiceModelUtility.CPFACT3));
						alternative.AddUtilityTerm(30, 1);
						alternative.AddUtilityTerm(88, sovTourFlag);
						alternative.AddUtilityTerm(89, hov2TourFlag);

						break;
					case Constants.Mode.HOV2:
						alternative.AddUtilityTerm(1, (destinationParkingCost * subtour.CostCoefficient / ChoiceModelUtility.CPFACT2));
						alternative.AddUtilityTerm(40, 1);
						alternative.AddUtilityTerm(88, sovTourFlag);
						alternative.AddUtilityTerm(89, hov2TourFlag);

						break;
					case Constants.Mode.SOV:
						alternative.AddUtilityTerm(1, (destinationParkingCost * subtour.CostCoefficient));
						alternative.AddUtilityTerm(50, 1);
						alternative.AddUtilityTerm(54, income0To25KFlag);
						alternative.AddUtilityTerm(55, income25To50KFlag);
						alternative.AddUtilityTerm(58, sovTourFlag);
						alternative.AddUtilityTerm(59, hov2TourFlag);

						break;
					case Constants.Mode.BIKE:
						alternative.AddUtilityTerm(60, 1);
						alternative.AddUtilityTerm(61, maleFlag);
						alternative.AddUtilityTerm(69, bikeTourFlag);
//						alternative.AddUtility(169, destinationParcel.MixedUse4Index1());
//						alternative.AddUtility(168, destinationParcel.TotalEmploymentDensity1());
//						alternative.AddUtility(167, destinationParcel.NetIntersectionDensity1());
//						alternative.AddUtility(166, originParcel.NetIntersectionDensity1());
//						alternative.AddUtility(165, originParcel.TotalEmploymentDensity1());
//						alternative.AddUtility(164, originParcel.MixedUse4Index1());

						break;
					case Constants.Mode.WALK:
						alternative.AddUtilityTerm(79, walkTourFlag);
//						alternative.AddUtility(179, destinationParcel.MixedUse4Index1());
//						alternative.AddUtility(178, destinationParcel.TotalEmploymentDensity1());
//						alternative.AddUtility(177, destinationParcel.NetIntersectionDensity1());
//						alternative.AddUtility(176, originParcel.NetIntersectionDensity1());
//						alternative.AddUtility(175, originParcel.TotalEmploymentDensity1());
//						alternative.AddUtility(174, originParcel.MixedUse4Index1());

						break;
				}
			}
		}
	}
}