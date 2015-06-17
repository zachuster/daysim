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
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Framework.Roster;
using Daysim.Interfaces;
using Daysim.ModelRunners;
using Ninject;

namespace Daysim.ChoiceModels {
	public class WorkTourModeModel : ChoiceModel {
		private const string CHOICE_MODEL_NAME = "WorkTourModeModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 4;
		private const int TOTAL_LEVELS = 2;
		private const int MAX_PARAMETER = 199;
		private const int THETA_PARAMETER = 99;

		private readonly int[] _nestedAlternativeIds = new[] {0, 19, 19, 20, 21, 21, 22, 22, 0};
		private readonly int[] _nestedAlternativeIndexes = new[] {0, 0, 0, 1, 2, 2, 3, 3, 0};
		private TourWrapperCreator _creator = Global.Kernel.Get<TourWrapperCreator>();

		public void Run(ITourWrapper tour) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			Initialize(CHOICE_MODEL_NAME, Global.Configuration.WorkTourModeModelCoefficients, Constants.Mode.TOTAL_MODES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			
			tour.PersonDay.ResetRandom(40 + tour.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(tour.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (tour.DestinationParcel == null || tour.Mode <= Constants.Mode.NONE || tour.Mode > Constants.Mode.PARK_AND_RIDE) {
					return;
				}

				var pathTypeModels =
					PathTypeModel.RunAllPlusParkAndRide(
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
						false);

				var pathTypeModel = pathTypeModels.First(x => x.Mode == tour.Mode);

				if (!pathTypeModel.Available) {
					return;
				}

				RunModel(choiceProbabilityCalculator, tour, pathTypeModels, tour.DestinationParcel, tour.Household.VehiclesAvailable, tour.Mode);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				var pathTypeModels =
					PathTypeModel.RunAllPlusParkAndRide(
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
						false);

				RunModel(choiceProbabilityCalculator, tour, pathTypeModels, tour.DestinationParcel, tour.Household.VehiclesAvailable);

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

		public ChoiceProbabilityCalculator.Alternative RunNested(IPersonWrapper person, ICondensedParcel originParcel, ICondensedParcel destinationParcel, int destinationArrivalTime, int destinationDepartureTime, int householdCars) {
			if (person == null) {
				throw new ArgumentNullException("person");
			}

			var tour = _creator.GetEntityForNestedModel(person, null, originParcel, destinationParcel, destinationArrivalTime, destinationDepartureTime, Constants.Purpose.WORK);

			return RunNested(tour, destinationParcel, householdCars, 0.0);
		}

		public ChoiceProbabilityCalculator.Alternative RunNested(IPersonDayWrapper personDay, ICondensedParcel originParcel, ICondensedParcel destinationParcel, int destinationArrivalTime, int destinationDepartureTime, int householdCars) {
			if (personDay == null) {
				throw new ArgumentNullException("personDay");
			}

			var tour = _creator.GetEntityForNestedModel(personDay.Person, personDay, originParcel, destinationParcel, destinationArrivalTime, destinationDepartureTime, Constants.Purpose.WORK);

			return RunNested(tour, destinationParcel, householdCars, personDay.Person.TransitFareDiscountFraction);
		}

		private ChoiceProbabilityCalculator.Alternative RunNested(ITourWrapper tour, ICondensedParcel destinationParcel, int householdCars, double transitDiscountFraction) {
			Initialize(CHOICE_MODEL_NAME, Global.Configuration.WorkTourModeModelCoefficients, Constants.Mode.TOTAL_MODES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
			
			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetNestedChoiceProbabilityCalculator();

			var pathTypeModels =
				PathTypeModel.RunAll(
				tour.Household.RandomUtility,
					tour.OriginParcel,
					destinationParcel,
					tour.DestinationArrivalTime,
					tour.DestinationDepartureTime,
					tour.DestinationPurpose,
					tour.CostCoefficient,
					tour.TimeCoefficient,
					tour.Person.IsDrivingAge,
					householdCars,
					transitDiscountFraction,
					false);

			RunModel(choiceProbabilityCalculator, tour, pathTypeModels, destinationParcel, householdCars);

			return choiceProbabilityCalculator.SimulateChoice(tour.Household.RandomUtility);
		}

		private void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, ITourWrapper tour, IEnumerable<PathTypeModel> pathTypeModels, ICondensedParcel destinationParcel, int householdCars, int choice = Constants.DEFAULT_VALUE) {
			var household = tour.Household;
			var householdTotals = household.HouseholdTotals;
			var personDay = tour.PersonDay;
			var person = tour.Person;

			// household inputs
			var childrenUnder5 = householdTotals.ChildrenUnder5;
			var childrenAge5Through15 = householdTotals.ChildrenAge5Through15;
//			var nonworkingAdults = householdTotals.NonworkingAdults;
//			var retiredAdults = householdTotals.RetiredAdults;
			var onePersonHouseholdFlag = household.IsOnePersonHousehold.ToFlag();
			var twoPersonHouseholdFlag = household.IsTwoPersonHousehold.ToFlag();
			var noCarsInHouseholdFlag = HouseholdWrapper.GetNoCarsInHouseholdFlag(householdCars);
			var carsLessThanDriversFlag = household.GetCarsLessThanDriversFlag(householdCars);
			var carsLessThanWorkersFlag = household.GetCarsLessThanWorkersFlag(householdCars);
			var income0To25KFlag = household.Has0To25KIncome.ToFlag();

			// person inputs
			var maleFlag = person.IsMale.ToFlag();
			var ageBetween51And98Flag = person.AgeIsBetween51And98.ToFlag();

			var originParcel = tour.OriginParcel;
			var parkingDuration = ChoiceModelUtility.GetParkingDuration(person.IsFulltimeWorker);
			// parking at work is free if no paid parking at work and tour goes to usual workplace
			var destinationParkingCost = (Global.Configuration.ShouldRunPayToParkAtWorkplaceModel && tour.Person.UsualWorkParcel != null
			                              && destinationParcel == tour.Person.UsualWorkParcel && person.PayToParkAtWorkplaceFlag == 0) ? 0.0 : destinationParcel.ParkingCostBuffer1(parkingDuration);

			double escortPercentage;
			double nonEscortPercentage;

			ChoiceModelUtility.SetEscortPercentages(personDay, out escortPercentage, out nonEscortPercentage);

//			var timeWindow = (originParcel == tour.Household.ResidenceParcel) ? personDay.TimeWindow : tour.ParentTour.TimeWindow;
//			var longestWindow = timeWindow.MaxAvailableMinutesAfter(1);
//			var totalWindow = timeWindow.TotalAvailableMinutesAfter(1);
//			var expectedDurationCurrentTour = person.IsFulltimeWorker ? Constants.Time.EIGHT_HOURS : Constants.Time.FOUR_HOURS;
//			var expectedDurationOtherTours = (personDay.TotalTours - personDay.TotalSimulatedTours) * Constants.Time.TWO_HOURS;
//			var expectedDurationStops = (Math.Min(personDay.TotalStops,1) - Math.Min(personDay.TotalSimulatedStops,1)) * Constants.Time.ONE_HOUR;
//			var totalExpectedDuration = expectedDurationCurrentTour + expectedDurationOtherTours + expectedDurationStops;

			foreach (var pathTypeModel in pathTypeModels) {
				var mode = pathTypeModel.Mode;
				var generalizedTime = pathTypeModel.GeneralizedTimeLogsum;
//				var travelTime = pathTypeModel.PathTime;
//				var travelCost = pathTypeModel.PathCost;

				var available = pathTypeModel.Available; //&& (travelTime < longestWindow);

				var alternative = choiceProbabilityCalculator.GetAlternative(mode, available, choice == mode);
				alternative.Choice = mode;

				alternative.AddNestedAlternative(_nestedAlternativeIds[pathTypeModel.Mode], _nestedAlternativeIndexes[pathTypeModel.Mode], THETA_PARAMETER);

//				if (mode == Constants.Mode.PARK_AND_RIDE) {
//					Console.WriteLine("Park and ride logsum = {0}", generalizedTimeLogsum);
//				}

				if (!available) {
					continue;
				}

				alternative.AddUtilityTerm(2, generalizedTime * tour.TimeCoefficient);
//				alternative.AddUtility(3, Math.Log(1.0 - travelTime / longestWindow));
//				alternative.AddUtility(4, travelTime < longestWindow - expectedDurationCurrentTour ? Math.Log(1.0 - travelTime / (longestWindow - expectedDurationCurrentTour)) : 0); 
//				alternative.AddUtility(5, travelTime < longestWindow - expectedDurationCurrentTour ? 0 : 1); 
//				alternative.AddUtility(6, travelTime < totalWindow - totalExpectedDuration ? Math.Log(1.0 - travelTime / (totalWindow - totalExpectedDuration)) : 0); 
//				alternative.AddUtility(7, travelTime < totalWindow - totalExpectedDuration ? 0 : 1); 
//				var vot = tour.TimeCoefficient / tour.CostCoefficient; 

				switch (mode) {
					case Constants.Mode.PARK_AND_RIDE:
						alternative.AddUtilityTerm(10, 1);
						alternative.AddUtilityTerm(11, noCarsInHouseholdFlag);
						alternative.AddUtilityTerm(13, carsLessThanWorkersFlag);
//						alternative.AddUtility(129, destinationParcel.MixedUse2Index1());
						alternative.AddUtilityTerm(128, destinationParcel.TotalEmploymentDensity1());
						alternative.AddUtilityTerm(127, destinationParcel.NetIntersectionDensity1());
//						alternative.AddUtility(123, Math.Log(destinationParcel.StopsTransitBuffer1+1));

						break;
					case Constants.Mode.TRANSIT:
						alternative.AddUtilityTerm(20, 1);
//						alternative.AddUtility(129, destinationParcel.MixedUse2Index1());
						alternative.AddUtilityTerm(128, destinationParcel.TotalEmploymentDensity1());
						alternative.AddUtilityTerm(127, destinationParcel.NetIntersectionDensity1());
//						alternative.AddUtility(126, originParcel.NetIntersectionDensity1());
//						alternative.AddUtility(125, originParcel.HouseholdDensity1());
						alternative.AddUtilityTerm(124, originParcel.MixedUse2Index1());
//						alternative.AddUtility(123, Math.Log(destinationParcel.StopsTransitBuffer1+1));
//						alternative.AddUtility(122, Math.Log(originParcel.StopsTransitBuffer1+1));

						break;
					case Constants.Mode.HOV3:
						alternative.AddUtilityTerm(1, (destinationParkingCost * tour.CostCoefficient / Global.Configuration.Coefficients_HOV3CostDivisor_Work));
						alternative.AddUtilityTerm(30, 1);
						alternative.AddUtilityTerm(31, childrenUnder5);
						alternative.AddUtilityTerm(32, childrenAge5Through15);
//						alternative.AddUtility(34, nonworkingAdults + retiredAdults);
						alternative.AddUtilityTerm(35, pathTypeModel.PathDistance.AlmostEquals(0) ? 0 : Math.Log(pathTypeModel.PathDistance));
						alternative.AddUtilityTerm(38, onePersonHouseholdFlag);
						alternative.AddUtilityTerm(39, twoPersonHouseholdFlag);
						alternative.AddUtilityTerm(41, noCarsInHouseholdFlag);
						alternative.AddUtilityTerm(42, carsLessThanDriversFlag);
						alternative.AddUtilityTerm(133, escortPercentage);
						alternative.AddUtilityTerm(134, nonEscortPercentage);

						break;
					case Constants.Mode.HOV2:
						alternative.AddUtilityTerm(1, (destinationParkingCost * tour.CostCoefficient / Global.Configuration.Coefficients_HOV2CostDivisor_Work));
						alternative.AddUtilityTerm(31, childrenUnder5);
						alternative.AddUtilityTerm(32, childrenAge5Through15);
//						alternative.AddUtility(34, nonworkingAdults + retiredAdults);
						alternative.AddUtilityTerm(35, pathTypeModel.PathDistance.AlmostEquals(0) ? 0 : Math.Log(pathTypeModel.PathDistance));
						alternative.AddUtilityTerm(40, 1);
						alternative.AddUtilityTerm(41, noCarsInHouseholdFlag);
						alternative.AddUtilityTerm(42, carsLessThanDriversFlag);
						alternative.AddUtilityTerm(48, onePersonHouseholdFlag);
						alternative.AddUtilityTerm(133, escortPercentage);
						alternative.AddUtilityTerm(134, nonEscortPercentage);

						break;
					case Constants.Mode.SOV:
						alternative.AddUtilityTerm(1, (destinationParkingCost) * tour.CostCoefficient);
						alternative.AddUtilityTerm(50, 1);
						alternative.AddUtilityTerm(53, carsLessThanWorkersFlag);
						alternative.AddUtilityTerm(54, income0To25KFlag);
						alternative.AddUtilityTerm(131, escortPercentage);
						alternative.AddUtilityTerm(132, nonEscortPercentage);

						break;
					case Constants.Mode.BIKE:
						var class1Dist
							= Global.Configuration.PathImpedance_BikeUseTypeSpecificDistanceFractions
								  ? ImpedanceRoster.GetValue("class1distance", mode, Constants.PathType.FULL_NETWORK,
									  Constants.ValueOfTime.DEFAULT_VOT, tour.DestinationArrivalTime, originParcel, destinationParcel).Variable
								  : 0;

						var class2Dist =
							Global.Configuration.PathImpedance_BikeUseTypeSpecificDistanceFractions
								? ImpedanceRoster.GetValue("class2distance", mode, Constants.PathType.FULL_NETWORK,
									Constants.ValueOfTime.DEFAULT_VOT, tour.DestinationArrivalTime, originParcel, destinationParcel).Variable
								: 0;

//						var worstDist = Global.Configuration.PathImpedance_BikeUseTypeSpecificDistanceFractions ?
//						 ImpedanceRoster.GetValue("worstdistance", mode, Constants.PathType.FULL_NETWORK, 
//							Constants.VotGroup.MEDIUM, tour.DestinationArrivalTime,originParcel, destinationParcel).Variable : 0;

						alternative.AddUtilityTerm(60, 1);
						alternative.AddUtilityTerm(61, maleFlag);
						alternative.AddUtilityTerm(63, ageBetween51And98Flag);
						alternative.AddUtilityTerm(169, destinationParcel.MixedUse4Index1());
						alternative.AddUtilityTerm(168, destinationParcel.TotalEmploymentDensity1());
//						alternative.AddUtility(167, destinationParcel.NetIntersectionDensity1());
//						alternative.AddUtility(166, originParcel.NetIntersectionDensity1());
//						alternative.AddUtility(165, originParcel.HouseholdDensity1());
						alternative.AddUtilityTerm(164, originParcel.MixedUse4Index1());
						alternative.AddUtilityTerm(162, (class1Dist > 0).ToFlag());
						alternative.AddUtilityTerm(162, (class2Dist > 0).ToFlag());
//						alternative.AddUtility(163, (worstDist > 0).ToFlag());

						break;
					case Constants.Mode.WALK:
						alternative.AddUtilityTerm(71, maleFlag);
//						alternative.AddUtility(73, ageBetween51And98Flag);
						alternative.AddUtilityTerm(179, destinationParcel.MixedUse4Index1());
//						alternative.AddUtility(178, destinationParcel.TotalEmploymentDensity1());
//						alternative.AddUtility(177, destinationParcel.NetIntersectionDensity1());
//						alternative.AddUtility(176, originParcel.NetIntersectionDensity1());
//						alternative.AddUtility(175, originParcel.HouseholdDensity1());
						alternative.AddUtilityTerm(179, originParcel.MixedUse4Index1());

						break;
				}
			}
		}
	}
}