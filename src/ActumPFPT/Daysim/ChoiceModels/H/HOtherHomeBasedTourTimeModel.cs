// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Linq;
using System.Threading.Tasks;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.ModelRunners;

namespace Daysim.ChoiceModels {
	public static class HOtherHomeBasedTourTimeModel {
		private const string CHOICE_MODEL_NAME = "HOtherHomeBasedTourTimeModel";
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 180;

		private static ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];

		private static void Initialize() {
			if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null) {
				return;
			}

			ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], CHOICE_MODEL_NAME, Global.Configuration.OtherHomeBasedTourTimeModelCoefficients, TourTime.TOTAL_TOUR_TIMES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
		}

		public static void Run(TourWrapper tour) {
			if (tour == null) {
				throw new ArgumentNullException("tour");
			}

			Initialize();
			TourTime.InitializeTourTimes();
			
			tour.PersonDay.ResetRandom(50 + tour.Sequence - 1);

			if (Global.Configuration.IsInEstimationMode) {
				if (Global.Configuration.EstimationModel != CHOICE_MODEL_NAME) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helpers[ParallelUtility.GetBatchFromThreadId()].GetChoiceProbabilityCalculator(tour.Id);

			if (_helpers[ParallelUtility.GetBatchFromThreadId()].ModelIsInEstimationMode) {
				if (tour.DestinationParcel == null || tour.OriginParcel == null || tour.Mode <= Constants.Mode.NONE || tour.Mode >= Constants.Mode.PARK_AND_RIDE) {
					return;
				}

				RunModel(choiceProbabilityCalculator, tour, new TourTime(tour.DestinationArrivalTime, tour.DestinationDepartureTime));
				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, tour);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice(tour.Household.RandomUtility);

				if (chosenAlternative == null) {
					Global.PrintFile.WriteNoAlternativesAvailableWarning(CHOICE_MODEL_NAME, "Run", tour.PersonDay.Id);
					tour.PersonDay.IsValid = false;

					return;
				}

				var choice = (TourTime) chosenAlternative.Choice;
				var destinationTimes = choice.GetDestinationTimes(tour);

				tour.DestinationArrivalTime = destinationTimes.Start;
				tour.DestinationDepartureTime = destinationTimes.End;
			}
		}


		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, TourWrapper tour, TourTime choice = null) {
			var household = tour.Household;
			var person = tour.Person;
			var personDay = tour.PersonDay;

			// household inputs
			var income0To25KFlag = household.Has0To25KIncome.ToFlag();
			var income100KPlusFlag = household.Has100KPlusIncome.ToFlag();

			// person inputs
			var partTimeWorkerFlag = person.IsPartTimeWorker.ToFlag();
			var nonworkingAdultFlag = person.IsNonworkingAdult.ToFlag();
			var universityStudentFlag = person.IsUniversityStudent.ToFlag();
			var retiredAdultFlag = person.IsRetiredAdult.ToFlag();
			var drivingAgeStudentFlag = person.IsDrivingAgeStudent.ToFlag();
			var fulltimeWorkerFlag = person.IsFulltimeWorker.ToFlag();
			var childAge5Through15Flag = person.IsChildAge5Through15.ToFlag();
			var childUnder5Flag = person.IsChildUnder5.ToFlag();

			// person-day inputs
			var homeBasedToursOnlyFlag = personDay.HasHomeBasedToursOnly.ToFlag();
			var firstSimulatedHomeBasedTourFlag = personDay.IsFirstSimulatedHomeBasedTour.ToFlag();
			var laterSimulatedHomeBasedTourFlag = personDay.IsLaterSimulatedHomeBasedTour.ToFlag();
			var totalStops = personDay.TotalStops;
			var totalSimulatedStops = personDay.TotalSimulatedStops;
			var escortStops = personDay.EscortStops;
			var homeBasedTours = personDay.HomeBasedTours;
			var simulatedHomeBasedTours = personDay.SimulatedHomeBasedTours;

			// tour inputs
			var escortTourFlag = tour.IsEscortPurpose.ToFlag();
			var shoppingTourFlag = tour.IsShoppingPurpose.ToFlag();
			var mealTourFlag = tour.IsMealPurpose.ToFlag();
			var socialTourFlag = tour.IsSocialPurpose.ToFlag();
			var personalBusinessTourFlag = tour.IsPersonalBusinessPurpose.ToFlag();

			var sovOrHovTourFlag = tour.IsAnAutoMode.ToFlag();
			var transitTourFlag = tour.UsesTransitModes.ToFlag();

			// remaining inputs
			// Higher priority tour of 2+ tours for the same purpose
			var highPrioritySameFlag = (tour.TotalToursByPurpose > tour.TotalSimulatedToursByPurpose && tour.TotalSimulatedToursByPurpose == 1).ToFlag();

			// Lower priority tour(s) of 2+ tours for the same purpose
			var lowPrioritySameFlag = (tour.TotalSimulatedToursByPurpose > 1).ToFlag();

			// Higher priority tour of 2+ tours for different purposes
			var highPriorityDifferentFlag = (personDay.IsFirstSimulatedHomeBasedTour && personDay.HasHomeBasedTours).ToFlag() * (1 - highPrioritySameFlag);

			// Lower priority tour of 2+ tours for different purposes
			var lowPriorityDifferentFlag = (personDay.IsLaterSimulatedHomeBasedTour && personDay.HasHomeBasedTours).ToFlag() * (1 - lowPrioritySameFlag);

			var timeWindow = tour.ParentTour == null ? personDay.TimeWindow : tour.ParentTour.TimeWindow;
			var impedances = tour.GetTourModeImpedances();
			var period1Middle = 15;
			var smallPeriodCount = DayPeriod.SmallDayPeriods.Count();

			for (var periodIndex = 0; periodIndex < smallPeriodCount; periodIndex++) {
				// set arrival period component
				var arrivalPeriodIndex = periodIndex;
				var arrivalPeriodShift = arrivalPeriodIndex * (48.0 / DayPeriod.SMALL_DAY_PERIOD_TOTAL_TRIP_TIMES); //adjust shift amount if period lengths change
				var arrivalPeriod = DayPeriod.SmallDayPeriods[arrivalPeriodIndex];
				var earlyArriveFlag = arrivalPeriod.Middle.IsBetween(Constants.Time.THREE_AM, Constants.Time.SEVEN_AM).ToFlag();
				var arrivalImpedance = impedances[arrivalPeriodIndex];
				var arrivalPeriodAvailableMinutes = timeWindow.TotalAvailableMinutes(arrivalPeriod.Start, arrivalPeriod.End);

				choiceProbabilityCalculator.CreateUtilityComponent(3 * periodIndex + 0);
				var arrivalComponent = choiceProbabilityCalculator.GetUtilityComponent(3 * periodIndex + 0);

				if (arrivalPeriodAvailableMinutes > 0) {
					arrivalComponent.AddUtilityTerm(11, arrivalPeriod.Middle.IsBetween(Constants.Time.THREE_AM, Constants.Time.SIX_AM).ToFlag());
					arrivalComponent.AddUtilityTerm(12, arrivalPeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SIX_AM, Constants.Time.SEVEN_AM).ToFlag());
					arrivalComponent.AddUtilityTerm(13, arrivalPeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_AM, Constants.Time.EIGHT_AM).ToFlag());
					arrivalComponent.AddUtilityTerm(14, arrivalPeriod.Middle.IsLeftExclusiveBetween(Constants.Time.EIGHT_AM, Constants.Time.NINE_AM).ToFlag());
					arrivalComponent.AddUtilityTerm(15, arrivalPeriod.Middle.IsLeftExclusiveBetween(Constants.Time.NINE_AM, Constants.Time.TEN_AM).ToFlag());
					arrivalComponent.AddUtilityTerm(16, arrivalPeriod.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_AM, Constants.Time.ONE_PM).ToFlag());
					arrivalComponent.AddUtilityTerm(17, arrivalPeriod.Middle.IsLeftExclusiveBetween(Constants.Time.ONE_PM, Constants.Time.FOUR_PM).ToFlag());
					arrivalComponent.AddUtilityTerm(18, arrivalPeriod.Middle.IsLeftExclusiveBetween(Constants.Time.FOUR_PM, Constants.Time.SEVEN_PM).ToFlag());
					arrivalComponent.AddUtilityTerm(19, arrivalPeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_PM, Constants.Time.TEN_PM).ToFlag());
					arrivalComponent.AddUtilityTerm(20, arrivalPeriod.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_PM, Constants.Time.MINUTES_IN_A_DAY).ToFlag());
					arrivalComponent.AddUtilityTerm(41, partTimeWorkerFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(43, nonworkingAdultFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(45, universityStudentFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(47, retiredAdultFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(49, drivingAgeStudentFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(139, fulltimeWorkerFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(141, childAge5Through15Flag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(143, childUnder5Flag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(145, escortTourFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(147, shoppingTourFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(149, mealTourFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(151, socialTourFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(153, personalBusinessTourFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(51, income0To25KFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(53, income100KPlusFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(55, highPrioritySameFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(57, lowPrioritySameFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(155, highPriorityDifferentFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(157, lowPriorityDifferentFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(59, homeBasedToursOnlyFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(61, totalStops * homeBasedToursOnlyFlag * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(63, totalStops * (1 - homeBasedToursOnlyFlag) * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(65, (totalStops - totalSimulatedStops) * (1 - homeBasedToursOnlyFlag) * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(67, escortStops * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(69, tour.TotalSubtours * arrivalPeriodShift);
					arrivalComponent.AddUtilityTerm(72, income100KPlusFlag * earlyArriveFlag);
					arrivalComponent.AddUtilityTerm(175, escortTourFlag * earlyArriveFlag);
					arrivalComponent.AddUtilityTerm(176, shoppingTourFlag * earlyArriveFlag);
					arrivalComponent.AddUtilityTerm(177, mealTourFlag * earlyArriveFlag);
					arrivalComponent.AddUtilityTerm(85, sovOrHovTourFlag * arrivalImpedance.GeneralizedTimeFromOrigin * tour.TimeCoefficient);
					arrivalComponent.AddUtilityTerm(87, transitTourFlag * Math.Max(0, arrivalImpedance.GeneralizedTimeFromOrigin) * tour.TimeCoefficient);
					arrivalComponent.AddUtilityTerm(89, transitTourFlag * (arrivalImpedance.GeneralizedTimeFromOrigin < 0).ToFlag());
					arrivalComponent.AddUtilityTerm(91, Math.Log(arrivalPeriodAvailableMinutes));
					arrivalComponent.AddUtilityTerm(93, arrivalImpedance.AdjacentMinutesBefore * firstSimulatedHomeBasedTourFlag);
					arrivalComponent.AddUtilityTerm(95, arrivalImpedance.AdjacentMinutesBefore * laterSimulatedHomeBasedTourFlag);
					arrivalComponent.AddUtilityTerm(99, (totalStops - totalSimulatedStops) / (1D + arrivalImpedance.AdjacentMinutesBefore));
				}

				// set departure period component
				var departurePeriodIndex = periodIndex;
				var departurePeriod = DayPeriod.SmallDayPeriods[departurePeriodIndex];
				var lateDepartFlag = departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.NINE_PM, Constants.Time.MINUTES_IN_A_DAY).ToFlag();
				var departureImpedance = impedances[departurePeriodIndex];
				var departurePeriodAvailableMinutes = timeWindow.TotalAvailableMinutes(departurePeriod.Start, departurePeriod.End);

				choiceProbabilityCalculator.CreateUtilityComponent(3 * periodIndex + 1);
				var departureComponent = choiceProbabilityCalculator.GetUtilityComponent(3 * periodIndex + 1);

				if (departurePeriodAvailableMinutes > 0) {
					departureComponent.AddUtilityTerm(21, departurePeriod.Middle.IsBetween(Constants.Time.THREE_AM, Constants.Time.SEVEN_AM).ToFlag());
					departureComponent.AddUtilityTerm(22, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_AM, Constants.Time.TEN_AM).ToFlag());
					departureComponent.AddUtilityTerm(23, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.TEN_AM, Constants.Time.ONE_PM).ToFlag());
					departureComponent.AddUtilityTerm(24, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.ONE_PM, Constants.Time.THREE_PM).ToFlag());
					departureComponent.AddUtilityTerm(124, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.THREE_PM, Constants.Time.FOUR_PM).ToFlag());
					departureComponent.AddUtilityTerm(25, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.FOUR_PM, Constants.Time.FIVE_PM).ToFlag());
					departureComponent.AddUtilityTerm(26, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.FIVE_PM, Constants.Time.SIX_PM).ToFlag());
					departureComponent.AddUtilityTerm(27, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SIX_PM, Constants.Time.SEVEN_PM).ToFlag());
					departureComponent.AddUtilityTerm(28, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.SEVEN_PM, Constants.Time.NINE_PM).ToFlag());
					departureComponent.AddUtilityTerm(29, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.NINE_PM, Constants.Time.MIDNIGHT).ToFlag());
					departureComponent.AddUtilityTerm(30, departurePeriod.Middle.IsLeftExclusiveBetween(Constants.Time.MIDNIGHT, Constants.Time.MINUTES_IN_A_DAY).ToFlag());
					departureComponent.AddUtilityTerm(73, income100KPlusFlag * lateDepartFlag);
					departureComponent.AddUtilityTerm(178, escortTourFlag * lateDepartFlag);
					departureComponent.AddUtilityTerm(179, shoppingTourFlag * lateDepartFlag);
					departureComponent.AddUtilityTerm(180, mealTourFlag * lateDepartFlag);
					departureComponent.AddUtilityTerm(86, sovOrHovTourFlag * departureImpedance.GeneralizedTimeFromDestination * tour.TimeCoefficient);
					departureComponent.AddUtilityTerm(88, transitTourFlag * Math.Max(0, departureImpedance.GeneralizedTimeFromDestination) * tour.TimeCoefficient);
					departureComponent.AddUtilityTerm(89, transitTourFlag * (departureImpedance.GeneralizedTimeFromDestination < 0).ToFlag());
					departureComponent.AddUtilityTerm(92, Math.Log(departurePeriodAvailableMinutes));
					departureComponent.AddUtilityTerm(94, departureImpedance.AdjacentMinutesAfter * firstSimulatedHomeBasedTourFlag);
					departureComponent.AddUtilityTerm(96, departureImpedance.AdjacentMinutesAfter * laterSimulatedHomeBasedTourFlag);
					departureComponent.AddUtilityTerm(100, (totalStops - totalSimulatedStops) / (1D + departureImpedance.AdjacentMinutesAfter));
				}

				// set duration component (relative to period 1)
				var periodSpan = periodIndex * (48.0 / DayPeriod.SMALL_DAY_PERIOD_TOTAL_TRIP_TIMES); //adjust shift amount if period lengths change;
				if (arrivalPeriodIndex == 0) {
					period1Middle = arrivalPeriod.Middle;
				}
				var duration = departurePeriod.Middle - period1Middle;
				var durationUnder1HourFlag = ChoiceModelUtility.GetDurationUnder1HourFlag(duration);
				var duration1To2HoursFlag = ChoiceModelUtility.GetDuration1To2HoursFlag(duration);
				var durationUnder4HoursFlag = ChoiceModelUtility.GetDurationUnder4HoursFlag(duration);
				var durationUnder8HoursFlag = ChoiceModelUtility.GetDurationUnder8HoursFlag(duration);
				var durationUnder9HoursFlag = ChoiceModelUtility.GetDurationUnder9HoursFlag(duration);

				choiceProbabilityCalculator.CreateUtilityComponent(3 * periodIndex + 2);
				var durationComponent = choiceProbabilityCalculator.GetUtilityComponent(3 * periodIndex + 2);

				if (tour.IsWorkPurpose || tour.IsSchoolPurpose) {
					durationComponent.AddUtilityTerm(31, duration.IsRightExclusiveBetween(Constants.Time.ZERO_HOURS, Constants.Time.THREE_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(32, duration.IsRightExclusiveBetween(Constants.Time.THREE_HOURS, Constants.Time.FIVE_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(33, duration.IsRightExclusiveBetween(Constants.Time.FIVE_HOURS, Constants.Time.SEVEN_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(34, duration.IsRightExclusiveBetween(Constants.Time.SEVEN_HOURS, Constants.Time.NINE_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(35, duration.IsRightExclusiveBetween(Constants.Time.NINE_HOURS, Constants.Time.TEN_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(36, duration.IsRightExclusiveBetween(Constants.Time.TEN_HOURS, Constants.Time.ELEVEN_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(37, duration.IsRightExclusiveBetween(Constants.Time.ELEVEN_HOURS, Constants.Time.TWELVE_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(38, duration.IsRightExclusiveBetween(Constants.Time.TWELVE_HOURS, Constants.Time.FOURTEEN_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(39, duration.IsRightExclusiveBetween(Constants.Time.FOURTEEN_HOURS, Constants.Time.EIGHTEEN_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(40, (duration >= Constants.Time.EIGHTEEN_HOURS).ToFlag());
				}
				else {
					durationComponent.AddUtilityTerm(31, duration.IsRightExclusiveBetween(Constants.Time.ZERO_HOURS, Constants.Time.ONE_HOUR).ToFlag());
					durationComponent.AddUtilityTerm(32, duration.IsRightExclusiveBetween(Constants.Time.ONE_HOUR, Constants.Time.TWO_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(33, duration.IsRightExclusiveBetween(Constants.Time.TWO_HOURS, Constants.Time.THREE_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(34, duration.IsRightExclusiveBetween(Constants.Time.THREE_HOURS, Constants.Time.FIVE_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(35, duration.IsRightExclusiveBetween(Constants.Time.FIVE_HOURS, Constants.Time.SEVEN_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(36, duration.IsRightExclusiveBetween(Constants.Time.SEVEN_HOURS, Constants.Time.NINE_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(37, duration.IsRightExclusiveBetween(Constants.Time.NINE_HOURS, Constants.Time.TWELVE_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(38, duration.IsRightExclusiveBetween(Constants.Time.TWELVE_HOURS, Constants.Time.FOURTEEN_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(39, duration.IsRightExclusiveBetween(Constants.Time.FOURTEEN_HOURS, Constants.Time.EIGHTEEN_HOURS).ToFlag());
					durationComponent.AddUtilityTerm(40, (duration >= Constants.Time.EIGHTEEN_HOURS).ToFlag());
				}

				durationComponent.AddUtilityTerm(42, partTimeWorkerFlag * periodSpan);
				durationComponent.AddUtilityTerm(44, nonworkingAdultFlag * periodSpan);
				durationComponent.AddUtilityTerm(46, universityStudentFlag * periodSpan);
				durationComponent.AddUtilityTerm(48, retiredAdultFlag * periodSpan);
				durationComponent.AddUtilityTerm(50, drivingAgeStudentFlag * periodSpan);
				durationComponent.AddUtilityTerm(140, fulltimeWorkerFlag * periodSpan);
				durationComponent.AddUtilityTerm(142, childAge5Through15Flag * periodSpan);
				durationComponent.AddUtilityTerm(144, childUnder5Flag * periodSpan);
				durationComponent.AddUtilityTerm(146, escortTourFlag * periodSpan);
				durationComponent.AddUtilityTerm(148, shoppingTourFlag * periodSpan);
				durationComponent.AddUtilityTerm(150, mealTourFlag * periodSpan);
				durationComponent.AddUtilityTerm(152, socialTourFlag * periodSpan);
				durationComponent.AddUtilityTerm(154, personalBusinessTourFlag * periodSpan);
				durationComponent.AddUtilityTerm(52, income0To25KFlag * periodSpan);
				durationComponent.AddUtilityTerm(54, income100KPlusFlag * periodSpan);
				durationComponent.AddUtilityTerm(56, highPrioritySameFlag * periodSpan);
				durationComponent.AddUtilityTerm(58, lowPrioritySameFlag * periodSpan);
				durationComponent.AddUtilityTerm(156, highPriorityDifferentFlag * periodSpan);
				durationComponent.AddUtilityTerm(158, lowPriorityDifferentFlag * periodSpan);
				durationComponent.AddUtilityTerm(60, homeBasedToursOnlyFlag * periodSpan);
				durationComponent.AddUtilityTerm(62, totalStops * homeBasedToursOnlyFlag * periodSpan);
				durationComponent.AddUtilityTerm(64, totalStops * (1 - homeBasedToursOnlyFlag) * periodSpan);
				durationComponent.AddUtilityTerm(66, (totalStops - totalSimulatedStops) * (1 - homeBasedToursOnlyFlag) * periodSpan);
				durationComponent.AddUtilityTerm(68, escortStops * periodSpan);
				durationComponent.AddUtilityTerm(70, tour.TotalSubtours * periodSpan);
				durationComponent.AddUtilityTerm(71, fulltimeWorkerFlag * durationUnder9HoursFlag);
				durationComponent.AddUtilityTerm(169, escortTourFlag * durationUnder1HourFlag);
				durationComponent.AddUtilityTerm(170, shoppingTourFlag * durationUnder1HourFlag);
				durationComponent.AddUtilityTerm(171, mealTourFlag * durationUnder1HourFlag);
				durationComponent.AddUtilityTerm(172, escortTourFlag * duration1To2HoursFlag);
				durationComponent.AddUtilityTerm(173, shoppingTourFlag * duration1To2HoursFlag);
				durationComponent.AddUtilityTerm(174, mealTourFlag * duration1To2HoursFlag);
				durationComponent.AddUtilityTerm(81, highPrioritySameFlag * durationUnder8HoursFlag);
				durationComponent.AddUtilityTerm(82, lowPrioritySameFlag * durationUnder8HoursFlag);
				durationComponent.AddUtilityTerm(83, highPriorityDifferentFlag * durationUnder8HoursFlag);
				durationComponent.AddUtilityTerm(84, lowPriorityDifferentFlag * durationUnder4HoursFlag);

			}

			foreach (var time in TourTime.Times) {
				var available =
					(time.ArrivalPeriod.Index < time.DeparturePeriod.Index &&
					 timeWindow.EntireSpanIsAvailable(time.ArrivalPeriod.End, time.DeparturePeriod.Start)) ||
					(time.ArrivalPeriod.Index == time.DeparturePeriod.Index &&
					 timeWindow.TotalAvailableMinutes(time.ArrivalPeriod.Start, time.ArrivalPeriod.End) > 1);

				var alternative = choiceProbabilityCalculator.GetAlternative(time.Index, available, choice != null && choice.Equals(time));

				if (!alternative.Available) {
					continue;
				}

				var arrivalPeriodIndex = time.ArrivalPeriod.Index;
				var arrivalImpedance = impedances[arrivalPeriodIndex];
				var departurePeriodIndex = time.DeparturePeriod.Index;
				var departureImpedance = impedances[departurePeriodIndex];

				alternative.Choice = time;

				// arrival period utility component
				alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(3 * time.ArrivalPeriod.Index + 0));

				// departure period utility component
				alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(3 * time.DeparturePeriod.Index + 1));

				// duration utility components
				alternative.AddUtilityComponent(choiceProbabilityCalculator.GetUtilityComponent(3 * (time.DeparturePeriod.Index - time.ArrivalPeriod.Index) + 2));

				alternative.AddUtilityTerm(97, Math.Min(0.3, (homeBasedTours - simulatedHomeBasedTours) / (1.0 + arrivalImpedance.TotalMinutesBefore + departureImpedance.TotalMinutesAfter)));
				alternative.AddUtilityTerm(98, Math.Min(0.3, (homeBasedTours - simulatedHomeBasedTours) / (1.0 + Math.Max(arrivalImpedance.MaxMinutesBefore, departureImpedance.MaxMinutesAfter))));

			}
		}
	}
}