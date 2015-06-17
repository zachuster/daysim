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
using Daysim.Framework.Roster;
using Daysim.Interfaces;
using Daysim.ModelRunners;

namespace Daysim.ChoiceModels {
	public sealed class PathTypeModel {
		private const double MAX_UTILITY = 80D;
		private const double MIN_UTILITY = -80D;

		private ICondensedParcel _originParcel;
		private ICondensedParcel _destinationParcel;
		private int _originZoneId;
		private int _destinationZoneId;
		private int _outboundTime;
		private int _returnTime;
		private int _purpose;
		private double _tourCostCoefficient;
		private double _tourTimeCoefficient;
		private bool _isDrivingAge;
		private int _householdCars;
		private double _transitDiscountFraction;
		private bool _randomChoice;
		private int _choice;

		// model variables
		private readonly double[] _utility = new double[Constants.PathType.TOTAL_PATH_TYPES];
		private readonly double[] _expUtility = new double[Constants.PathType.TOTAL_PATH_TYPES];
		private readonly double[] _pathTime = new double[Constants.PathType.TOTAL_PATH_TYPES];
		private readonly double[] _pathDistance = new double[Constants.PathType.TOTAL_PATH_TYPES];
		private readonly double[] _pathCost = new double[Constants.PathType.TOTAL_PATH_TYPES];
		private readonly int[] _pathParkAndRideNodeId = new int[Constants.PathType.TOTAL_PATH_TYPES];

		private PathTypeModel() {
			GeneralizedTimeLogsum = Constants.GENERALIZED_TIME_UNAVAILABLE;
			GeneralizedTimeChosen = Constants.GENERALIZED_TIME_UNAVAILABLE;
		}

		public int Mode { get; private set; }

		public double GeneralizedTimeLogsum { get; private set; }

		public double GeneralizedTimeChosen { get; private set; }

		public double PathTime { get; private set; }

		public double PathDistance { get; private set; }

		public double PathCost { get; private set; }

		public int PathType { get; private set; }

		public int PathParkAndRideNodeId { get; private set; }

		public bool Available { get; private set; }

		public static List<PathTypeModel> RunAllPlusParkAndRide(IRandomUtility randomUtility, ICondensedParcel originParcel, ICondensedParcel destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice) {
			var modes = new List<int>();

			for (var mode = Constants.Mode.WALK; mode <= Constants.Mode.PARK_AND_RIDE; mode++) {
				modes.Add(mode);
			}

			return Run(randomUtility, originParcel, destinationParcel, outboundTime, returnTime, purpose, tourCostCoefficient, tourTimeCoefficient, isDrivingAge, householdCars, transitDiscountFraction, randomChoice, modes.ToArray());
		}

		public static List<PathTypeModel> RunAll(IRandomUtility randomUtility, ICondensedParcel originParcel, ICondensedParcel destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice) {
			var modes = new List<int>();

			for (var mode = Constants.Mode.WALK; mode <= Constants.Mode.TRANSIT; mode++) {
				modes.Add(mode);
			}

			return Run(randomUtility, originParcel, destinationParcel, outboundTime, returnTime, purpose, tourCostCoefficient, tourTimeCoefficient, isDrivingAge, householdCars, transitDiscountFraction, randomChoice, modes.ToArray());
		}

		public static List<PathTypeModel> Run(IRandomUtility randomUtility, ICondensedParcel originParcel, ICondensedParcel destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice, params int[] modes) {
			var list = new List<PathTypeModel>();

			foreach (var pathTypeModel in modes.Select(mode => new PathTypeModel { _originParcel = originParcel, _destinationParcel = destinationParcel, _outboundTime = outboundTime, _returnTime = returnTime, _purpose = purpose, _tourCostCoefficient = tourCostCoefficient, _tourTimeCoefficient = tourTimeCoefficient, _isDrivingAge = isDrivingAge, _householdCars = householdCars, _transitDiscountFraction = transitDiscountFraction, _randomChoice = randomChoice, Mode = mode })) {
				pathTypeModel.RunModel(randomUtility);

				list.Add(pathTypeModel);
			}

			return list;
		}

		public static List<PathTypeModel> Run(IRandomUtility randomUtility, int originZoneId, int destinationZoneId, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice, params int[] modes) {
			var list = new List<PathTypeModel>();

			foreach (var pathTypeModel in modes.Select(mode => new PathTypeModel { _originZoneId = originZoneId, _destinationZoneId = destinationZoneId, _outboundTime = outboundTime, _returnTime = returnTime, _purpose = purpose, _tourCostCoefficient = tourCostCoefficient, _tourTimeCoefficient = tourTimeCoefficient, _isDrivingAge = isDrivingAge, _householdCars = householdCars, _transitDiscountFraction = transitDiscountFraction, _randomChoice = randomChoice, Mode = mode })) {
				pathTypeModel.RunModel(randomUtility, true);

				list.Add(pathTypeModel);
			}

			return list;
		}

		private void RunModel(IRandomUtility randomUtility, bool useZones = false) {
			switch (Mode) {
				case Constants.Mode.HOV2:
					_tourCostCoefficient
						= _tourCostCoefficient /
						  (_purpose == Constants.Purpose.WORK
								? Global.Configuration.Coefficients_HOV2CostDivisor_Work
								: Global.Configuration.Coefficients_HOV2CostDivisor_Other);

					break;
				case Constants.Mode.HOV3:
					_tourCostCoefficient
						= _tourCostCoefficient /
						  (_purpose == Constants.Purpose.WORK
								? Global.Configuration.Coefficients_HOV3CostDivisor_Work
								: Global.Configuration.Coefficients_HOV3CostDivisor_Other);

					break;
			}

			//			 some test code 
			//			 for zone 1063 to 680 in Base_2_SKM_NM.MTX.TXT  - there is no record - zone 680 is not connected, so it shouldn't be a sampled destination 
			//			_originParcel = ChoiceModelRunner.Parcels[478318];
			//			_destinationParcel = ChoiceModelRunner.Parcels[81523];
			//			_outboundTime = 300;
			//			_returnTime = 1020;

			var votValue = (60.0 * _tourTimeCoefficient) / _tourCostCoefficient; // in $/hour

			//ACTUM Begin
			if (useZones == false && (Global.Configuration.PathImpedance_UtilityForm_Auto == 1
				|| Global.Configuration.PathImpedance_UtilityForm_Transit == 1)) {
				if (_purpose == Constants.Purpose.WORK || _purpose == Constants.Purpose.SCHOOL || _purpose == Constants.Purpose.ESCORT) {
					votValue = Global.Configuration.VotMediumHigh - 0.5;
				}
				else if (_purpose == Constants.Purpose.BUSINESS) {
					votValue = Global.Configuration.VotHighVeryHigh - 0.5;
				}
				else votValue = Global.Configuration.VotLowMedium - 0.5;
			}
			//ACTUM End

			var skimMode = (Mode == Constants.Mode.PARK_AND_RIDE) ? Constants.Mode.TRANSIT : Mode;
			var availablePathTypes = 0;
			var expUtilitySum = 0D;
			var bestExpUtility = 0D;
			var bestPathType = Constants.DEFAULT_VALUE;

			// loop on all relevant path types for the mode
			for (var pathType = Constants.PathType.FULL_NETWORK; pathType < Constants.PathType.TOTAL_PATH_TYPES; pathType++) {
				_utility[pathType] = 0D;

				if (!ImpedanceRoster.IsActualCombination(skimMode, pathType)) {
					continue;
				}

				// set path type utility and impedance, depending on the mode
				switch (Mode) {
					case Constants.Mode.BIKE:
					case Constants.Mode.WALK:
						RunWalkBikeModel(skimMode, pathType, votValue, useZones);

						break;
					case Constants.Mode.HOV3:
					case Constants.Mode.HOV2:
					case Constants.Mode.SOV:
						if (Mode != Constants.Mode.SOV || (_isDrivingAge && _householdCars > 0)) {
							RunAutoModel(skimMode, pathType, votValue, useZones);
						}

						break;
					case Constants.Mode.TRANSIT:
						RunTransitModel(skimMode, pathType, votValue, useZones);

						break;
					case Constants.Mode.PARK_AND_RIDE:
						RunParkAndRideModel(skimMode, pathType, votValue, useZones);

						break;
				}

				if (_expUtility[pathType] < Constants.EPSILON) {
					continue;
				}

				// add to total utility and see if it is the best so far
				availablePathTypes++;
				expUtilitySum += _expUtility[pathType];

				if (_expUtility[pathType] <= bestExpUtility) {
					continue;
				}

				// make the best current path type and utility
				bestPathType = pathType;
				bestExpUtility = _expUtility[pathType];
			}

			if (expUtilitySum < Constants.EPSILON) {
				Available = false;

				return;
			}

			// set the generalized time logsum
			var logsum = Math.Log(expUtilitySum);
			var tourTimeCoefficient = (Global.Configuration.PathImpedance_PathChoiceScaleFactor * _tourTimeCoefficient);

			if (Double.IsNaN(expUtilitySum) || Double.IsNaN(logsum) || Double.IsNaN(tourTimeCoefficient)) {
				throw new ValueIsNaNException(string.Format("Value is NaN for utilitySum: {0}, logsum: {1}, tourTimeCoefficient: {2}.", expUtilitySum, logsum, tourTimeCoefficient));
			}

			GeneralizedTimeLogsum = logsum / tourTimeCoefficient; // need to make sure _tourTimeCoefficient is not 0

			if (Double.IsNaN(GeneralizedTimeLogsum)) {
				throw new ValueIsNaNException(string.Format("Value is NaN for GeneralizedTimeLogsum where utilitySum: {0}, logsum: {1}, tourTimeCoefficient: {2}.", expUtilitySum, logsum, tourTimeCoefficient));
			}

			// draw a choice using a random number if requested (and in application mode), otherwise return best utility
			if (_randomChoice && availablePathTypes > 1 && !Global.Configuration.IsInEstimationMode) {
				var random = randomUtility.Uniform01();

				for (var pathType = Constants.PathType.FULL_NETWORK; pathType <= Constants.PathType.TOTAL_PATH_TYPES; pathType++) {
					_choice = pathType;
					random -= _expUtility[pathType] / expUtilitySum;

					if (random < 0) {
						break;
					}
				}
			}
			else {
				_choice = bestPathType;
			}

			Available = true;
			PathType = _choice;
			PathTime = _pathTime[_choice];
			PathDistance = _pathDistance[_choice];
			PathCost = _pathCost[_choice];
			GeneralizedTimeChosen = _utility[_choice] / tourTimeCoefficient;
			PathParkAndRideNodeId = _pathParkAndRideNodeId[_choice];
		}


		private void RunWalkBikeModel(int skimMode, int pathType, double votValue, bool useZones) {
			var zzDist = ImpedanceRoster.GetValue("distance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable;
			var circuityDistance =
				(zzDist > Global.Configuration.MaximumBlendingDistance)
					? Constants.DEFAULT_VALUE
					: (!useZones && Global.Configuration.UseShortDistanceNodeToNodeMeasures)
						? _originParcel.NodeToNodeDistance(_destinationParcel)
						: (!useZones && Global.Configuration.UseShortDistanceCircuityMeasures)
							? _originParcel.CircuityDistance(_destinationParcel)
							: Constants.DEFAULT_VALUE;
			//test output
			//var orth=(Math.Abs(_originParcel.XCoordinate - _destinationParcel.XCoordinate) + Math.Abs(_originParcel.YCoordinate - _destinationParcel.YCoordinate)) / 5280.0;
			//Global.PrintFile.WriteLine("Circuity distance for parcels {0} to {1} is {2} vs {3}",_originParcel.Id, _destinationParcel.Id, circuityDistance, orth);

			var skimValue =
				useZones
					? ImpedanceRoster.GetValue("time", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
					: ImpedanceRoster.GetValue("time", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);

			_pathTime[pathType] = skimValue.Variable;
			_pathDistance[pathType] = skimValue.BlendVariable;
			_pathCost[pathType] = 0;
			_pathParkAndRideNodeId[pathType] = 0;

			if (_returnTime > 0) {

				skimValue =
					useZones
						? ImpedanceRoster.GetValue("time", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId)
						: ImpedanceRoster.GetValue("time", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel, circuityDistance);

				_pathTime[pathType] += skimValue.Variable;
				_pathDistance[pathType] += skimValue.BlendVariable;
			}

			// sacog-specific adjustment of generalized time for bike mode
			if (_pathDistance[pathType] > Constants.EPSILON && skimMode == Constants.Mode.BIKE && Global.Configuration.PathImpedance_BikeUseTypeSpecificDistanceFractions) {
				var d1 =
					Math.Abs(Global.Configuration.PathImpedance_BikeType1DistanceFractionAdditiveWeight) < Constants.EPSILON
						? 0D
						: useZones
							  ? ImpedanceRoster.GetValue("class1distance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
							  : ImpedanceRoster.GetValue("class1distance", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var d2 =
					Math.Abs(Global.Configuration.PathImpedance_BikeType2DistanceFractionAdditiveWeight) < Constants.EPSILON
						? 0D
						: useZones
							  ? ImpedanceRoster.GetValue("class2distance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
							  : ImpedanceRoster.GetValue("class2distance", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var d3 =
					Math.Abs(Global.Configuration.PathImpedance_BikeType3DistanceFractionAdditiveWeight) < Constants.EPSILON
						? 0D
						: useZones
							  ? ImpedanceRoster.GetValue("baddistance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
							  : ImpedanceRoster.GetValue("baddistance", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var d4 = Math.Abs(Global.Configuration.PathImpedance_BikeType4DistanceFractionAdditiveWeight) < Constants.EPSILON
								? 0D
								: useZones
									  ? ImpedanceRoster.GetValue("worstdistance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
									  : ImpedanceRoster.GetValue("worstdistance", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				if (_returnTime > 0) {
					d1 +=
						Math.Abs(Global.Configuration.PathImpedance_BikeType1DistanceFractionAdditiveWeight) < Constants.EPSILON
							? 0D
							: useZones
								  ? ImpedanceRoster.GetValue("class1distance", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
								  : ImpedanceRoster.GetValue("class1distance", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					d2 +=
						Math.Abs(Global.Configuration.PathImpedance_BikeType2DistanceFractionAdditiveWeight) < Constants.EPSILON
							? 0D
							: useZones
								  ? ImpedanceRoster.GetValue("class2distance", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
								  : ImpedanceRoster.GetValue("class2distance", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					d3 +=
						Math.Abs(Global.Configuration.PathImpedance_BikeType3DistanceFractionAdditiveWeight) < Constants.EPSILON
							? 0D
							: useZones
								  ? ImpedanceRoster.GetValue("baddistance", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
								  : ImpedanceRoster.GetValue("baddistance", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					d4 +=
						Math.Abs(Global.Configuration.PathImpedance_BikeType4DistanceFractionAdditiveWeight) < Constants.EPSILON
							? 0D
							: useZones
								  ? ImpedanceRoster.GetValue("worstdistance", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
								  : ImpedanceRoster.GetValue("worstdistance", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;
				}

				var adjFactor =
					1.0
					+ d1 / _pathDistance[pathType] * Global.Configuration.PathImpedance_BikeType1DistanceFractionAdditiveWeight
					+ d2 / _pathDistance[pathType] * Global.Configuration.PathImpedance_BikeType2DistanceFractionAdditiveWeight
					+ d3 / _pathDistance[pathType] * Global.Configuration.PathImpedance_BikeType3DistanceFractionAdditiveWeight
					+ d4 / _pathDistance[pathType] * Global.Configuration.PathImpedance_BikeType4DistanceFractionAdditiveWeight;

				_pathTime[pathType] *= adjFactor;
			}

			// a fix for unconnected parcels/zones (sampling should be fixed to not sample them)
			//			if (_pathTime[pathType] < Constants.EPSILON && _pathDistance[pathType] >= Constants.EPSILON ) {
			//				_pathTime[pathType] = _pathDistance[pathType] * (skimMode == Constants.Mode.WALK ? 20.0 : 6.0) ; 
			//			}

			// a fix for intra-parcels, which happen once in a great while for school
			if (!useZones && _originParcel.Id == _destinationParcel.Id && skimMode == Constants.Mode.WALK
				//JLB 20130628 added destination scale condition because ImpedanceRoster assigns time and cost values for intrazonals 
				&& Global.Configuration.DestinationScale != Constants.DestinationScale.ZONE) {
				_pathTime[pathType] = 1.0;
				_pathDistance[pathType] = 0.01 * Global.DistanceUnitsPerMile;  // JLBscale.  multiplied by distance units per mile
			}



			var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (_returnTime > 0 ? 2 : 1);

			if (_pathTime[pathType] > pathTimeLimit || _pathTime[pathType] < Constants.EPSILON) {
				return;
			}

			_utility[pathType] =
				Global.Configuration.PathImpedance_PathChoiceScaleFactor *
				(_tourTimeCoefficient * _pathTime[pathType]
				 * (skimMode == Constants.Mode.WALK
						 ? Global.Configuration.PathImpedance_WalkTimeWeight
						 : Global.Configuration.PathImpedance_BikeTimeWeight));

			_expUtility[pathType] = _utility[pathType] > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : _utility[pathType] < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(_utility[pathType]);
		}

		private void RunAutoModel(int skimMode, int pathType, double votValue, bool useZones) {
			_pathCost[pathType] =
				useZones
					? ImpedanceRoster.GetValue("toll", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
					: ImpedanceRoster.GetValue("toll", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

			if (_returnTime > 0) {
				_pathCost[pathType] +=
					useZones
						? ImpedanceRoster.GetValue("toll", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
						: ImpedanceRoster.GetValue("toll", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;
			}

			//if full network path and no-tolls path exists check for duplicate
			var tollConstant = 0D;
			if (pathType == Constants.PathType.FULL_NETWORK && ImpedanceRoster.IsActualCombination(skimMode, Constants.PathType.NO_TOLLS)) {
				var noTollCost =
					useZones
						? ImpedanceRoster.GetValue("toll", skimMode, Constants.PathType.NO_TOLLS, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("toll", skimMode, Constants.PathType.NO_TOLLS, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				if (_returnTime > 0) {
					noTollCost +=
						useZones
							? ImpedanceRoster.GetValue("toll", skimMode, Constants.PathType.NO_TOLLS, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("toll", skimMode, Constants.PathType.NO_TOLLS, votValue, _returnTime, _destinationParcel, _originParcel).Variable;
				}
				// if the toll route doesn't have a higher cost than no toll route, than make it unavailable
				if (_pathCost[pathType] - noTollCost < Constants.EPSILON) {
					return;
				}
				// else it is a toll route with a higher cost than no toll route, add a toll constant also
				tollConstant = Global.Configuration.PathImpedance_AutoTolledPathConstant;
			}

			var zzDist = ImpedanceRoster.GetValue("distance", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable;
			var circuityDistance =
				(zzDist > Global.Configuration.MaximumBlendingDistance)
					? Constants.DEFAULT_VALUE
					: (!useZones && Global.Configuration.UseShortDistanceNodeToNodeMeasures)
						? _originParcel.NodeToNodeDistance(_destinationParcel)
						: (!useZones && Global.Configuration.UseShortDistanceCircuityMeasures)
							? _originParcel.CircuityDistance(_destinationParcel)
							: Constants.DEFAULT_VALUE;

			var skimValue =
				useZones
					? ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
					: ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);

			_pathParkAndRideNodeId[pathType] = 0;
			_pathTime[pathType] = skimValue.Variable;
			_pathDistance[pathType] = skimValue.BlendVariable;

			//implement mileage-based pricing policy
			if (Global.Configuration.Policy_TestMilageBasedPricing) {
				var minutesAfterMidnight = _outboundTime + 180;
				var centsPerMile = (minutesAfterMidnight >= Global.Configuration.Policy_AMPricingPeriodStart && minutesAfterMidnight <= Global.Configuration.Policy_AMPricingPeriodEnd)
					 ? Global.Configuration.Policy_CentsPerMileInAMPeak :
						(minutesAfterMidnight >= Global.Configuration.Policy_PMPricingPeriodStart && minutesAfterMidnight <= Global.Configuration.Policy_PMPricingPeriodEnd)
						  ? Global.Configuration.Policy_CentsPerMileInPMPeak :
							 (minutesAfterMidnight > Global.Configuration.Policy_AMPricingPeriodEnd && minutesAfterMidnight < Global.Configuration.Policy_PMPricingPeriodStart)
								? Global.Configuration.Policy_CentsPerMileBetweenPeaks : Global.Configuration.Policy_CentsPerMileOutsidePeaks;
				_pathCost[pathType] += skimValue.BlendVariable * centsPerMile / 100.0;
			}
			if (_returnTime > 0) {

				skimValue =
					useZones
						? ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId)
						: ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel, circuityDistance);

				_pathTime[pathType] += skimValue.Variable;
				_pathDistance[pathType] += skimValue.BlendVariable;

				//implement mileage-based pricing policy
				if (Global.Configuration.Policy_TestMilageBasedPricing) {
					var minutesAfterMidnight = _returnTime + 180;
					var centsPerMile = (minutesAfterMidnight >= Global.Configuration.Policy_AMPricingPeriodStart && minutesAfterMidnight <= Global.Configuration.Policy_AMPricingPeriodEnd)
						 ? Global.Configuration.Policy_CentsPerMileInAMPeak :
							(minutesAfterMidnight >= Global.Configuration.Policy_PMPricingPeriodStart && minutesAfterMidnight <= Global.Configuration.Policy_PMPricingPeriodEnd)
							  ? Global.Configuration.Policy_CentsPerMileInPMPeak :
								 (minutesAfterMidnight > Global.Configuration.Policy_AMPricingPeriodEnd && minutesAfterMidnight < Global.Configuration.Policy_PMPricingPeriodStart)
									? Global.Configuration.Policy_CentsPerMileBetweenPeaks : Global.Configuration.Policy_CentsPerMileOutsidePeaks;
					_pathCost[pathType] += skimValue.BlendVariable * centsPerMile / 100.0;
				}
			}

			// a fix for unconnected parcels/zones (sampling should be fixed to not sample them in the first place)
			//			if (_pathTime[pathType] < Constants.EPSILON && _pathDistance[pathType] >= Constants.EPSILON ) {
			//				_pathTime[pathType] = _pathDistance[pathType] * 2.0 ;  // correct missing time with speed of 30 mph 
			//			}
			//			else if (_pathTime[pathType] < Constants.EPSILON && _pathDistance[pathType] < Constants.EPSILON ) {
			//				_pathDistance[pathType] = (Math.Abs(_originParcel.XCoordinate - _destinationParcel.XCoordinate) 
			//					                      + Math.Abs(_originParcel.YCoordinate - _destinationParcel.YCoordinate))/5280D;
			//				_pathTime[pathType] = _pathDistance[pathType] * 2.0 ;  // correct missing time with speed of 30 mph 
			//			}

			var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (_returnTime > 0 ? 2 : 1);

			if (_pathTime[pathType] > pathTimeLimit || _pathTime[pathType] < Constants.EPSILON) {
				return;
			}

			_pathCost[pathType] += _pathDistance[pathType] * Global.PathImpedance_AutoOperatingCostPerDistanceUnit;

			//ACTUM Begin
			if (useZones == false && Global.Configuration.PathImpedance_UtilityForm_Auto == 1) {
				//calculate time utility
				var freeFlowSkimValue =
					useZones
						? ImpedanceRoster.GetValue("ivtfree", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
						: ImpedanceRoster.GetValue("ivtfree", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);
				var freeFlowTime = freeFlowSkimValue.Variable;
				var extraSkimValue =
				useZones
					? ImpedanceRoster.GetValue("ivtextra", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
					: ImpedanceRoster.GetValue("ivtextra", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);
				var extraTime = extraSkimValue.Variable;
				extraTime += _destinationParcel.ParkingOffStreetPaidHourlyPrice; //this property represents average search time per trip in Actum data
				if (_returnTime > 0) {
					freeFlowSkimValue =
						useZones
							? ImpedanceRoster.GetValue("ivtfree", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
							: ImpedanceRoster.GetValue("ivtfree", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);
					freeFlowTime += freeFlowSkimValue.Variable;
					extraSkimValue =
					useZones
						? ImpedanceRoster.GetValue("ivtextra", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId)
						: ImpedanceRoster.GetValue("ivtextra", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel, circuityDistance);
					extraTime += extraSkimValue.Variable;
				}
				var gammaFreeFlowTime = GammaFunction(freeFlowTime, Global.Configuration.PathImpedance_Gamma_InVehicleTime);
				
				extraTime = Global.Configuration.Policy_CongestedTravelTimeMultiplier != 0 ? extraTime * Global.Configuration.Policy_CongestedTravelTimeMultiplier : extraTime;
				var gammaExtraTime = GammaFunction(extraTime, Global.Configuration.PathImpedance_Gamma_ExtraTime);
				//determine time weight
				//extra time weight for driver and passenger
				double inVehicleExtraTimeWeight;
				if (skimMode == Constants.Mode.HOVPASSENGER) {
					inVehicleExtraTimeWeight = Global.Configuration.PathImpedance_InVehicleExtraTimeWeight_Passenger;
				}
				else {
					inVehicleExtraTimeWeight = Global.Configuration.PathImpedance_InVehicleExtraTimeWeight_Driver;
				}
				//weights for purpose x mode
				int aggregatePurpose = 0;
				double inVehicleTimeWeight;
				if (_purpose == Constants.Purpose.WORK || _purpose == Constants.Purpose.SCHOOL || _purpose == Constants.Purpose.ESCORT) {
					aggregatePurpose = 1;
				}
				else if (_purpose == Constants.Purpose.BUSINESS) {
					aggregatePurpose = 2;
				}
				if (skimMode == Constants.Mode.SOV) {
					if (aggregatePurpose == 1) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_SOV;
					}
					else if (aggregatePurpose == 2) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_SOV;
					}
					else {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_SOV;
					}
				}
				else if (skimMode == Constants.Mode.HOVDRIVER) {
					if (aggregatePurpose == 1) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_HOVDriver;
					}
					else if (aggregatePurpose == 2) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_HOVDriver;
					}
					else {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_HOVDriver;
					}
				}
				else {
					if (aggregatePurpose == 1) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_HOVPassenger;
					}
					else if (aggregatePurpose == 2) {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_HOVPassenger;
					}
					else {
						inVehicleTimeWeight = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_HOVPassenger;
					}
				}
				//calculate cost utility
					_pathCost[pathType] += _destinationParcel.ParkingOffStreetPaidDailyPrice; // this property represents avg parking cost per trip in Actum
		
				double gammaCost;
				double distanceCoefficient;
				double modeConstant;
				if (skimMode == Constants.Mode.HOVPASSENGER && !Global.Configuration.HOVPassengersIncurCosts) {
					gammaCost = 0;
				}
				else {
					gammaCost = GammaFunction(_pathCost[pathType], Global.Configuration.PathImpedance_Gamma_Cost);
				}
				//calculate distance utility
				//calculate utility
				_utility[pathType] = Global.Configuration.PathImpedance_PathChoiceScaleFactor
					* (_tourCostCoefficient * gammaCost
					+ _tourTimeCoefficient * inVehicleTimeWeight
					* (gammaFreeFlowTime + gammaExtraTime * inVehicleExtraTimeWeight)
					+ tollConstant);
			}
			//ACTUM End
			else {
				_utility[pathType] = Global.Configuration.PathImpedance_PathChoiceScaleFactor *
				(_tourCostCoefficient * _pathCost[pathType] +
				 _tourTimeCoefficient * _pathTime[pathType] +
				 tollConstant);
			}

			_expUtility[pathType] = _utility[pathType] > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : _utility[pathType] < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(_utility[pathType]);
		}

		private void RunTransitModel(int skimMode, int pathType, double votValue, bool useZones) {
			// check for presence of valid path
			var outboundInVehicleTime =
					useZones
						? ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;
			var returnInVehicleTime =
				_returnTime > 0
					? useZones
						  ? ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
						  : ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable
					: 0;

			if (outboundInVehicleTime < Constants.EPSILON || (_returnTime > 0 && returnInVehicleTime < Constants.EPSILON)) {
				return;
			}
			// valid path(s).  Proceed.

			var pathTypeConstant =
					pathType == Constants.PathType.LOCAL_BUS
						? Global.Configuration.PathImpedance_TransitLocalBusPathConstant
						: pathType == Constants.PathType.LIGHT_RAIL
							  ? Global.Configuration.PathImpedance_TransitLightRailPathConstant
							  : pathType == Constants.PathType.PREMIUM_BUS
									 ? Global.Configuration.PathImpedance_TransitPremiumBusPathConstant
									 : pathType == Constants.PathType.COMMUTER_RAIL
											? Global.Configuration.PathImpedance_TransitCommuterRailPathConstant
											: pathType == Constants.PathType.FERRY
												  ? Global.Configuration.PathImpedance_TransitFerryPathConstant
												  : 0;

			var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (_returnTime > 0 ? 2 : 1);


			//ACTUM Begin
			if (useZones == false && Global.Configuration.PathImpedance_UtilityForm_Transit == 1) {
				//Actum:  nonlinear utility with submode-speciifc time components in a generic transit mode (labeled local bus)
				//get skim values
				var firstWaitTime =
					useZones
						? ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var transferWaitTime =
					useZones
						? ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var fare =
					useZones
						? ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var distance =
					useZones
						? ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var accessEgressTime =
					useZones
						? ImpedanceRoster.GetValue("accegrtime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("accegrtime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var fTime =
					useZones
						? ImpedanceRoster.GetValue("ftime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("ftime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var gTime =
					useZones
						? ImpedanceRoster.GetValue("gtime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("gtime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var bTime =
					useZones
						? ImpedanceRoster.GetValue("btime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("btime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var pTime =
					useZones
						? ImpedanceRoster.GetValue("ptime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("ptime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var rTime =
					useZones
						? ImpedanceRoster.GetValue("rtime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("rtime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var sTime =
					useZones
						? ImpedanceRoster.GetValue("stime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("stime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var xTime =
					useZones
						? ImpedanceRoster.GetValue("xtime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("xtime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var yTime =
					useZones
						? ImpedanceRoster.GetValue("ytime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("ytime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var zTime =
					useZones
						? ImpedanceRoster.GetValue("ztime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("ztime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				// add return LOS, if valid _departureTime passed			
				if (_returnTime > 0) {
					firstWaitTime +=
						useZones
							? ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					transferWaitTime +=
						useZones
							? ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					fare +=
						useZones
							? ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					accessEgressTime +=
						useZones
							? ImpedanceRoster.GetValue("accegrtime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("accegrtime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					fTime +=
						useZones
							? ImpedanceRoster.GetValue("ftime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("ftime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					gTime +=
						useZones
							? ImpedanceRoster.GetValue("gtime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("gtime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					bTime +=
						useZones
							? ImpedanceRoster.GetValue("btime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("btime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					pTime +=
						useZones
							? ImpedanceRoster.GetValue("ptime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("ptime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					rTime +=
						useZones
							? ImpedanceRoster.GetValue("rtime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("rtime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					sTime +=
						useZones
							? ImpedanceRoster.GetValue("stime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("stime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					xTime +=
						useZones
							? ImpedanceRoster.GetValue("xtime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("xtime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					yTime +=
						useZones
							? ImpedanceRoster.GetValue("ytime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("ytime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					zTime +=
						useZones
							? ImpedanceRoster.GetValue("ztime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("ztime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					distance *= 2;
				}

				fare = fare * (1.0 - _transitDiscountFraction); //fare adjustment

				//determine utility components 
				//accessEgressTime = accessEgressTime
				var trainTime = pTime + rTime + sTime;
				var busTime = bTime + xTime + yTime + zTime;
				var metroTime = gTime;
				var lightRailTime = fTime;

				_pathParkAndRideNodeId[pathType] = 0;
				_pathTime[pathType] = accessEgressTime + firstWaitTime + transferWaitTime + trainTime + busTime + metroTime + lightRailTime;
				_pathDistance[pathType] = distance;
				_pathCost[pathType] = fare;

				if (_pathTime[pathType] > pathTimeLimit) {
					return;
				}

				//determine submode ivt time weights, and weighted IVT 
				int aggregatePurpose = 0;
				if (_purpose == Constants.Purpose.WORK || _purpose == Constants.Purpose.SCHOOL || _purpose == Constants.Purpose.ESCORT) {
					aggregatePurpose = 1;
				}
				else if (_purpose == Constants.Purpose.BUSINESS) {
					aggregatePurpose = 2;
				}

				double inVehicleTimeWeight_Train;
				double inVehicleTimeWeight_Bus;
				double inVehicleTimeWeight_Metro;
				double inVehicleTimeWeight_LightRail;

				if (aggregatePurpose == 1) {
					inVehicleTimeWeight_Train = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_Train;
					inVehicleTimeWeight_Bus = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_Bus;
					inVehicleTimeWeight_Metro = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_Metro;
					inVehicleTimeWeight_LightRail = Global.Configuration.PathImpedance_InVehicleTimeWeight_Commute_LightRail;
				}
				else if (aggregatePurpose == 2) {
					inVehicleTimeWeight_Train = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_Train;
					inVehicleTimeWeight_Bus = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_Bus;
					inVehicleTimeWeight_Metro = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_Metro;
					inVehicleTimeWeight_LightRail = Global.Configuration.PathImpedance_InVehicleTimeWeight_Business_LightRail;
				}
				else {
					inVehicleTimeWeight_Train = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_Train;
					inVehicleTimeWeight_Bus = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_Bus;
					inVehicleTimeWeight_Metro = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_Metro;
					inVehicleTimeWeight_LightRail = Global.Configuration.PathImpedance_InVehicleTimeWeight_Personal_LightRail;
				}
				var weightedInVehicleTime = trainTime * inVehicleTimeWeight_Train
					+ busTime * inVehicleTimeWeight_Bus
					+ metroTime * inVehicleTimeWeight_Metro
					+ lightRailTime * inVehicleTimeWeight_LightRail;

				//calculate time utility

				_utility[pathType] =
					Global.Configuration.PathImpedance_PathChoiceScaleFactor *
					(_tourCostCoefficient * GammaFunction(fare, Global.Configuration.PathImpedance_Gamma_Cost)
					+ _tourTimeCoefficient *
					(Global.Configuration.PathImpedance_TransitInVehicleTimeWeight * GammaFunction(weightedInVehicleTime, Global.Configuration.PathImpedance_Gamma_InVehicleTime)
					+ Global.Configuration.PathImpedance_TransitFirstWaitTimeWeight * firstWaitTime
					+ Global.Configuration.PathImpedance_TransitTransferWaitTimeWeight * transferWaitTime
					+ Global.Configuration.PathImpedance_TransitAccessEgressTimeWeight * accessEgressTime
					  ));

				_expUtility[pathType] = _utility[pathType] > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : _utility[pathType] < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(_utility[pathType]);
			} //ACTUM End

			else {
				//all cases other than Actum
				// get outbound los
				var initialWaitTime =
					useZones
						? ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var transferWaitTime =
					useZones
						? ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var numberOfBoards =
					useZones
						? ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var fare =
					useZones
						? ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var distance =
					useZones
						? ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var originWalkTime = useZones ? 5.0 : GetTransitWalkTime(_originParcel, pathType, numberOfBoards);
				var destinationWalkTime = useZones ? 5.0 : GetTransitWalkTime(_destinationParcel, pathType, numberOfBoards);

				if (originWalkTime < -1 * Constants.EPSILON || destinationWalkTime < -1 * Constants.EPSILON) {
					return;
				}

				// add return LOS, if valid _departureTime passed			
				if (_returnTime > 0) {
					initialWaitTime +=
						useZones
							? ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					transferWaitTime +=
						useZones
							? ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					numberOfBoards +=
						useZones
							? ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					fare +=
						useZones
							? ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
							: ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

					distance *= 2;
					originWalkTime *= 2;
					destinationWalkTime *= 2;
				}

				fare = fare * (1.0 - _transitDiscountFraction); //fare adjustment

				// set utility
				_pathParkAndRideNodeId[pathType] = 0;
				_pathTime[pathType] = outboundInVehicleTime + returnInVehicleTime + initialWaitTime + transferWaitTime + originWalkTime + destinationWalkTime;
				_pathDistance[pathType] = distance;
				_pathCost[pathType] = fare;

				if (_pathTime[pathType] > pathTimeLimit) {
					return;
				}

				// for sacog, use pathtype-specific time skims and weights
				var pathTypeSpecificTime = 0D;
				var pathTypeSpecificTimeWeight = 0D;

				if (Global.Configuration.PathImpedance_TransitUsePathTypeSpecificTime) {
					pathTypeSpecificTimeWeight =
						pathType == Constants.PathType.LIGHT_RAIL
							? Global.Configuration.PathImpedance_TransitLightRailTimeAdditiveWeight
							: pathType == Constants.PathType.PREMIUM_BUS
								  ? Global.Configuration.PathImpedance_TransitPremiumBusTimeAdditiveWeight
								  : pathType == Constants.PathType.COMMUTER_RAIL
										 ? Global.Configuration.PathImpedance_TransitCommuterRailTimeAdditiveWeight
										 : pathType == Constants.PathType.FERRY
												? Global.Configuration.PathImpedance_TransitFerryTimeAdditiveWeight
												: 0D;

					if (Math.Abs(pathTypeSpecificTimeWeight) > Constants.EPSILON) {
						pathTypeSpecificTime =
							pathType == Constants.PathType.LIGHT_RAIL
								? useZones
									  ? ImpedanceRoster.GetValue("lrttime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
									  : ImpedanceRoster.GetValue("lrttime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable
								: pathType == Constants.PathType.PREMIUM_BUS
									  ? useZones
											 ? ImpedanceRoster.GetValue("comtime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
											 : ImpedanceRoster.GetValue("comtime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable
									  : 0D;

						if (_returnTime > 0) {
							pathTypeSpecificTime +=
								pathType == Constants.PathType.LIGHT_RAIL
									? useZones
										  ? ImpedanceRoster.GetValue("lrttime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
										  : ImpedanceRoster.GetValue("lrttime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable
									: pathType == Constants.PathType.PREMIUM_BUS
										  ? useZones
												 ? ImpedanceRoster.GetValue("comtime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
												 : ImpedanceRoster.GetValue("comtime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable
										  : 0D;
						}
					}
				}

				var totalInVehicleTime = outboundInVehicleTime + returnInVehicleTime;
				var totalWalkTime = originWalkTime + destinationWalkTime;

				_utility[pathType] =
					Global.Configuration.PathImpedance_PathChoiceScaleFactor *
					(pathTypeConstant +
					 _tourCostCoefficient * fare +
					 _tourTimeCoefficient *
					 (Global.Configuration.PathImpedance_TransitInVehicleTimeWeight * totalInVehicleTime +
					  Global.Configuration.PathImpedance_TransitFirstWaitTimeWeight * initialWaitTime +
					  Global.Configuration.PathImpedance_TransitTransferWaitTimeWeight * transferWaitTime +
					  Global.Configuration.PathImpedance_TransitNumberBoardingsWeight * numberOfBoards +
					  Global.Configuration.PathImpedance_TransitWalkAccessTimeWeight * totalWalkTime +
					  pathTypeSpecificTime * pathTypeSpecificTimeWeight));

				_expUtility[pathType] = _utility[pathType] > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : _utility[pathType] < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(_utility[pathType]);
			}
		}

		private void RunParkAndRideModel(int skimMode, int pathType, double votValue, bool useZones) {
			if (ChoiceModelFactory.ParkAndRideNodeDao == null || _returnTime <= 0) {
				return;
			}

			IEnumerable<ParkAndRideNodeWrapper> parkAndRideNodes;

			if (Global.Configuration.ShouldReadParkAndRideNodeSkim) {
				var nodeId =
					useZones
						? (int) ImpedanceRoster.GetValue("przone", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
						: (int) ImpedanceRoster.GetValue("przone", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

				var node = ChoiceModelFactory.ParkAndRideNodeDao.Get(nodeId);

				parkAndRideNodes = new List<ParkAndRideNodeWrapper> { node };
			}
			else {
				parkAndRideNodes = ChoiceModelFactory.ParkAndRideNodeDao.Nodes.Where(n => n.Capacity > 0);
			}

			// valid node(s), and tour-level call  
			var bestNodeExpUtility = 0D;
			var destinationZoneId = useZones ? _destinationZoneId : _destinationParcel.ZoneId;

			var pathTypeConstant =
				pathType == Constants.PathType.LOCAL_BUS
					? Global.Configuration.PathImpedance_TransitLocalBusPathConstant
					: pathType == Constants.PathType.LIGHT_RAIL
						  ? Global.Configuration.PathImpedance_TransitLightRailPathConstant
						  : pathType == Constants.PathType.PREMIUM_BUS
								 ? Global.Configuration.PathImpedance_TransitPremiumBusPathConstant
								 : pathType == Constants.PathType.COMMUTER_RAIL
										? Global.Configuration.PathImpedance_TransitCommuterRailPathConstant
										: pathType == Constants.PathType.FERRY
											  ? Global.Configuration.PathImpedance_TransitFerryPathConstant
											  : 0;

			foreach (var node in parkAndRideNodes) {
				// only look at nodes with positive capacity
				if (node.Capacity < Constants.EPSILON) {
					continue;
				}

				// use the node rather than the nearest parcel for transit LOS, becuase more accurate, and distance blending is not relevant 
				var parkAndRideParcel = ChoiceModelFactory.Parcels[node.NearestParcelId];
				var parkAndRideZoneId = node.ZoneId;
				var parkAndRideParkingCost = node.Cost / 100.0; // converts hundredths of Monetary Units to Monetary Units  // JLBscale: changed comment from cents and dollars
				var outboundInVehicleTime = ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _outboundTime, parkAndRideZoneId, destinationZoneId).Variable;
				var returnInVehicleTime = ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _returnTime, destinationZoneId, parkAndRideZoneId).Variable;

				if (outboundInVehicleTime < Constants.EPSILON || returnInVehicleTime < Constants.EPSILON) {
					continue;
				}

				// valid path(s), get outbound los
				var zzDist = ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable;
				var circuityDistance =
					(zzDist > Global.Configuration.MaximumBlendingDistance)
						? Constants.DEFAULT_VALUE
						: (!useZones && Global.Configuration.UseShortDistanceNodeToNodeMeasures)
							? _originParcel.NodeToNodeDistance(parkAndRideParcel)
							: (!useZones && Global.Configuration.UseShortDistanceCircuityMeasures)
								? _originParcel.CircuityDistance(parkAndRideParcel)
								: Constants.DEFAULT_VALUE;

				var skimValue
					= useZones
						  ? ImpedanceRoster.GetValue("ivtime", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _outboundTime, _originZoneId, parkAndRideZoneId)
						  : ImpedanceRoster.GetValue("ivtime", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _outboundTime, _originParcel, parkAndRideParcel, circuityDistance);

				var driveTime = skimValue.Variable;
				var driveDistance = skimValue.BlendVariable;
				var initialWaitTime = ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _outboundTime, parkAndRideZoneId, destinationZoneId).Variable;
				var transferWaitTime = ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _outboundTime, parkAndRideZoneId, destinationZoneId).Variable;
				var numberOfBoards = ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _outboundTime, parkAndRideZoneId, destinationZoneId).Variable;
				var fare = ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _outboundTime, parkAndRideZoneId, destinationZoneId).Variable;
				var parkMinute = (int) (_outboundTime - initialWaitTime - outboundInVehicleTime - 3); // subtract 3 is estimate of change mode activity time, same as assumed when setting trip departure time in ChoiceModelRunner.

				var transitDistance =
					useZones
						? ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _outboundTime, parkAndRideZoneId, _destinationZoneId).Variable
						: ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _outboundTime, parkAndRideParcel, _destinationParcel).Variable;

				var destinationWalkTime = useZones ? 5.0 : GetTransitWalkTime(_destinationParcel, pathType, numberOfBoards);

				// add return LOS
				if (destinationWalkTime < -1 * Constants.EPSILON) {
					continue;
				}

				skimValue =
					useZones
						? ImpedanceRoster.GetValue("ivtime", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _returnTime, parkAndRideZoneId, _originZoneId)
						: ImpedanceRoster.GetValue("ivtime", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, votValue, _returnTime, parkAndRideParcel, _originParcel, circuityDistance);

				driveTime += skimValue.Variable;
				driveDistance += skimValue.BlendVariable;
				initialWaitTime += ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _returnTime, destinationZoneId, parkAndRideZoneId).Variable;
				transferWaitTime += ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _returnTime, destinationZoneId, parkAndRideZoneId).Variable;
				numberOfBoards += ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _returnTime, destinationZoneId, parkAndRideZoneId).Variable;
				fare += ImpedanceRoster.GetValue("fare", skimMode, pathType, votValue, _returnTime, destinationZoneId, parkAndRideZoneId).Variable;
				transitDistance *= 2;
				destinationWalkTime *= 2;
				fare = fare * (1.0 - _transitDiscountFraction); //fare adjustment

				// set utility
				var nodePathTime = outboundInVehicleTime + returnInVehicleTime + initialWaitTime + transferWaitTime + driveTime + destinationWalkTime;
				var nodePathDistance = driveDistance + transitDistance;
				var nodePathCost = fare;
				var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (_returnTime > 0 ? 2 : 1);

				if (nodePathTime > pathTimeLimit) {
					continue;
				}

				// for sacog, use pathtype-specific time skims and weights
				var pathTypeSpecificTime = 0D;
				var pathTypeSpecificTimeWeight = 0D;

				if (Global.Configuration.PathImpedance_TransitUsePathTypeSpecificTime) {
					pathTypeSpecificTimeWeight
						= pathType == Constants.PathType.LIGHT_RAIL
							  ? Global.Configuration.PathImpedance_TransitLightRailTimeAdditiveWeight
							  : pathType == Constants.PathType.PREMIUM_BUS
									 ? Global.Configuration.PathImpedance_TransitPremiumBusTimeAdditiveWeight
									 : pathType == Constants.PathType.COMMUTER_RAIL
											? Global.Configuration.PathImpedance_TransitCommuterRailTimeAdditiveWeight
											: pathType == Constants.PathType.FERRY
												  ? Global.Configuration.PathImpedance_TransitFerryTimeAdditiveWeight
												  : 0D;

					if (Math.Abs(pathTypeSpecificTimeWeight) > Constants.EPSILON) {
						pathTypeSpecificTime =
							pathType == Constants.PathType.LIGHT_RAIL
								? ImpedanceRoster.GetValue("lrttime", skimMode, pathType, votValue, _outboundTime, parkAndRideZoneId, destinationZoneId).Variable +
								  ImpedanceRoster.GetValue("lrttime", skimMode, pathType, votValue, _returnTime, destinationZoneId, parkAndRideZoneId).Variable
								: pathType == Constants.PathType.PREMIUM_BUS
									  ? ImpedanceRoster.GetValue("comtime", skimMode, pathType, votValue, _outboundTime, parkAndRideZoneId, destinationZoneId).Variable +
										 ImpedanceRoster.GetValue("comtime", skimMode, pathType, votValue, _returnTime, destinationZoneId, parkAndRideZoneId).Variable
									  : 0D;
					}
				}

				var totalInVehicleTime = outboundInVehicleTime + returnInVehicleTime;
				var nodeUtility =
					Global.Configuration.PathImpedance_PathChoiceScaleFactor *
					(pathTypeConstant +
					 _tourCostCoefficient * (fare + parkAndRideParkingCost) +
					 _tourTimeCoefficient *
					 (Global.Configuration.PathImpedance_TransitInVehicleTimeWeight * totalInVehicleTime +
					  Global.Configuration.PathImpedance_TransitFirstWaitTimeWeight * initialWaitTime +
					  Global.Configuration.PathImpedance_TransitTransferWaitTimeWeight * transferWaitTime +
					  Global.Configuration.PathImpedance_TransitNumberBoardingsWeight * numberOfBoards +
					  Global.Configuration.PathImpedance_TransitDriveAccessTimeWeight * driveTime +
					  Global.Configuration.PathImpedance_TransitWalkAccessTimeWeight * destinationWalkTime +
					  pathTypeSpecificTime * pathTypeSpecificTimeWeight));

				if (Global.Configuration.ShouldUseParkAndRideShadowPricing && !Global.Configuration.IsInEstimationMode) {
					nodeUtility += node.ShadowPrice[parkMinute];
				}

				var nodeExpUtility = nodeUtility > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : nodeUtility < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(nodeUtility);

				// if the best path so far, reset pathType properties
				if (nodeExpUtility <= bestNodeExpUtility) {
					continue;
				}

				bestNodeExpUtility = nodeExpUtility;

				_utility[pathType] = nodeUtility;
				_expUtility[pathType] = nodeExpUtility;
				_pathTime[pathType] = nodePathTime;
				_pathDistance[pathType] = nodePathDistance;
				_pathCost[pathType] = nodePathCost;
				_pathParkAndRideNodeId[pathType] = node.Id;
			}
		}

		private static double GetTransitWalkTime(ICondensedParcel parcel, int pathType, double boardings) {
			var walkDist = parcel.DistanceToLocalBus; // default is local bus (feeder), for any submode

			double altDist;

			switch (pathType) {
				case Constants.PathType.LIGHT_RAIL:
					altDist = parcel.DistanceToLightRail;

					break;
				case Constants.PathType.PREMIUM_BUS:
					altDist = parcel.DistanceToExpressBus;

					break;
				case Constants.PathType.COMMUTER_RAIL:
					altDist = parcel.DistanceToCommuterRail;

					break;
				case Constants.PathType.FERRY:
					altDist = parcel.DistanceToFerry;

					break;
				default:
					altDist = Constants.DEFAULT_VALUE;

					break;
			}

			if ((altDist >= 0 && altDist < walkDist) || (boardings < Global.Configuration.PathImpedance_TransitSingleBoardingLimit && altDist >= 0 && altDist < Global.Configuration.PathImpedance_TransitWalkAccessDirectLimit)) {
				walkDist = altDist;
			}

			return
				(walkDist >= 0 && walkDist < Global.Configuration.PathImpedance_TransitWalkAccessDistanceLimit)
					? walkDist * Global.PathImpedance_WalkMinutesPerDistanceUnit
					: Constants.DEFAULT_VALUE; // -1 is "missing" value
		}

		double GammaFunction(double x, double gamma) {
			double xGamma;
			xGamma = gamma * x + (1 - gamma) * Math.Log(Math.Max(x, 1.0));
			return xGamma;
		}
	}

}
