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
using Daysim.ModelRunners;

namespace Daysim.ChoiceModels {
	public sealed class ActumPathTypeModel {
		private const double MAX_UTILITY = 80D;
		private const double MIN_UTILITY = -80D;

		private CondensedParcel _originParcel;
		private CondensedParcel _destinationParcel;
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

		private ActumPathTypeModel() {
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

		public static List<ActumPathTypeModel> RunAllPlusParkAndRide(RandomUtility randomUtility, CondensedParcel originParcel, CondensedParcel destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice) {
			var modes = new List<int>();

			for (var mode = Constants.Mode.WALK; mode <= Constants.Mode.PARK_AND_RIDE; mode++) {
				modes.Add(mode);
			}

			return Run(randomUtility, originParcel, destinationParcel, outboundTime, returnTime, purpose, tourCostCoefficient, tourTimeCoefficient, isDrivingAge, householdCars, transitDiscountFraction, randomChoice, modes.ToArray());
		}

		public static List<ActumPathTypeModel> RunAll(RandomUtility randomUtility, CondensedParcel originParcel, CondensedParcel destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice) {
			var modes = new List<int>();

			for (var mode = Constants.Mode.WALK; mode <= Constants.Mode.TRANSIT; mode++) {
				modes.Add(mode);
			}

			return Run(randomUtility, originParcel, destinationParcel, outboundTime, returnTime, purpose, tourCostCoefficient, tourTimeCoefficient, isDrivingAge, householdCars, transitDiscountFraction, randomChoice, modes.ToArray());
		}

		public static List<ActumPathTypeModel> Run(RandomUtility randomUtility, CondensedParcel originParcel, CondensedParcel destinationParcel, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice, params int[] modes) {
			var list = new List<ActumPathTypeModel>();

			foreach (var pathTypeModel in modes.Select(mode => new ActumPathTypeModel {_originParcel = originParcel, _destinationParcel = destinationParcel, _outboundTime = outboundTime, _returnTime = returnTime, _purpose = purpose, _tourCostCoefficient = tourCostCoefficient, _tourTimeCoefficient = tourTimeCoefficient, _isDrivingAge = isDrivingAge, _householdCars = householdCars, _transitDiscountFraction = transitDiscountFraction, _randomChoice = randomChoice, Mode = mode})) {
				pathTypeModel.RunModel(randomUtility);

				list.Add(pathTypeModel);
			}

			return list;
		}

		public static List<ActumPathTypeModel> Run(RandomUtility randomUtility, int originZoneId, int destinationZoneId, int outboundTime, int returnTime, int purpose, double tourCostCoefficient, double tourTimeCoefficient, bool isDrivingAge, int householdCars, double transitDiscountFraction, bool randomChoice, params int[] modes) {
			var list = new List<ActumPathTypeModel>();

			foreach (var pathTypeModel in modes.Select(mode => new ActumPathTypeModel {_originZoneId = originZoneId, _destinationZoneId = destinationZoneId, _outboundTime = outboundTime, _returnTime = returnTime, _purpose = purpose, _tourCostCoefficient = tourCostCoefficient, _tourTimeCoefficient = tourTimeCoefficient, _isDrivingAge = isDrivingAge, _householdCars = householdCars, _transitDiscountFraction = transitDiscountFraction, _randomChoice = randomChoice, Mode = mode})) {
				pathTypeModel.RunModel(randomUtility, true);

				list.Add(pathTypeModel);
			}

			return list;
		}

		private void RunModel(RandomUtility randomUtility, bool useZones = false) {
			switch (Mode) {
				case Constants.Mode.HOVDRIVER:
					_tourCostCoefficient
						= _tourCostCoefficient /
						  ((_purpose == Constants.Purpose.WORK || _purpose == Constants.Purpose.BUSINESS)
							   ? Global.Configuration.Coefficients_HOV2CostDivisor_Work
							   : Global.Configuration.Coefficients_HOV2CostDivisor_Other);

					break;
				case Constants.Mode.HOVPASSENGER:
					_tourCostCoefficient
						= _tourCostCoefficient /
						  ((_purpose == Constants.Purpose.WORK || _purpose == Constants.Purpose.BUSINESS)
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
					case Constants.Mode.HOVPASSENGER:
					case Constants.Mode.HOVDRIVER:
					case Constants.Mode.SOV:
						if (Mode == Constants.Mode.HOVPASSENGER || (_isDrivingAge && _householdCars > 0)) {
							RunAutoModel(skimMode, pathType, votValue, useZones);
						}

						break;
					case Constants.Mode.TRANSIT:
						RunTransitModel(skimMode, pathType, votValue, useZones);

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
				(zzDist>Global.Configuration.MaximumBlendingDistance)
					? Constants.DEFAULT_VALUE
					: (!useZones && Global.Configuration.UseShortDistanceNodeToNodeMeasures)
						?  _originParcel.NodeToNodeDistance(_destinationParcel)
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
			if (!useZones && _originParcel.Id == _destinationParcel.Id && skimMode == Constants.Mode.WALK) {
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
				(zzDist>Global.Configuration.MaximumBlendingDistance)
					? Constants.DEFAULT_VALUE
					: (!useZones && Global.Configuration.UseShortDistanceNodeToNodeMeasures)
						?  _originParcel.NodeToNodeDistance(_destinationParcel)
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

			if (_returnTime > 0) {

				skimValue =
					useZones
						? ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId)
						: ImpedanceRoster.GetValue("ivtime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel, circuityDistance);

				_pathTime[pathType] += skimValue.Variable;
				_pathDistance[pathType] += skimValue.BlendVariable;
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

			_utility[pathType] =
				Global.Configuration.PathImpedance_PathChoiceScaleFactor *
				(_tourCostCoefficient * _pathCost[pathType] +
				 _tourTimeCoefficient * _pathTime[pathType] +
				 tollConstant);

			_expUtility[pathType] = _utility[pathType] > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : _utility[pathType] < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(_utility[pathType]);
		}

		private void RunTransitModel(int skimMode, int pathType, double votValue, bool useZones) {
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

			// valid path(s), get outbound los
			var initialWaitTime =
				useZones
					? ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
					: ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

			var transferWaitTime =
				useZones
					? ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
					: ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

			//var numberOfBoards =
			//	useZones
			//		? ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _outboundTime, _originZoneId, _destinationZoneId).Variable
			//		: ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _outboundTime, _originParcel, _destinationParcel).Variable;

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
				initialWaitTime +=
					useZones
						? ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
						: ImpedanceRoster.GetValue("iwaittime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

				transferWaitTime +=
					useZones
						? ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
						: ImpedanceRoster.GetValue("xwaittime", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

				//numberOfBoards +=
				//	useZones
				//		? ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _returnTime, _destinationZoneId, _originZoneId).Variable
				//		: ImpedanceRoster.GetValue("nboard", skimMode, pathType, votValue, _returnTime, _destinationParcel, _originParcel).Variable;

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

			// set utility
			_pathParkAndRideNodeId[pathType] = 0;
			_pathTime[pathType] = outboundInVehicleTime + returnInVehicleTime + initialWaitTime + transferWaitTime + accessEgressTime;
			_pathDistance[pathType] = distance;
			_pathCost[pathType] = fare;

			var pathTimeLimit = Global.Configuration.PathImpedance_AvailablePathUpperTimeLimit * (_returnTime > 0 ? 2 : 1);

			if (_pathTime[pathType] > pathTimeLimit) {
				return;
			}

			var totalInVehicleTime = outboundInVehicleTime + returnInVehicleTime;

			_utility[pathType] =
				Global.Configuration.PathImpedance_PathChoiceScaleFactor *
				(_tourCostCoefficient * fare +
				 _tourTimeCoefficient *
			  (Global.Configuration.PathImpedance_Transit_F_InVehicleTimeWeight * fTime +
				Global.Configuration.PathImpedance_Transit_G_InVehicleTimeWeight * gTime +
				Global.Configuration.PathImpedance_Transit_B_InVehicleTimeWeight * bTime +
				Global.Configuration.PathImpedance_Transit_P_InVehicleTimeWeight * pTime +
				Global.Configuration.PathImpedance_Transit_R_InVehicleTimeWeight * rTime +
				Global.Configuration.PathImpedance_Transit_S_InVehicleTimeWeight * sTime +
				Global.Configuration.PathImpedance_Transit_X_InVehicleTimeWeight * xTime +
				Global.Configuration.PathImpedance_Transit_Y_InVehicleTimeWeight * yTime +
				Global.Configuration.PathImpedance_Transit_Z_InVehicleTimeWeight * zTime +
				Global.Configuration.PathImpedance_TransitAccessEgressTimeWeight * accessEgressTime +
				Global.Configuration.PathImpedance_TransitFirstWaitTimeWeight * initialWaitTime +
				Global.Configuration.PathImpedance_TransitTransferWaitTimeWeight * transferWaitTime
				  ));

			_expUtility[pathType] = _utility[pathType] > MAX_UTILITY ? Math.Exp(MAX_UTILITY) : _utility[pathType] < MIN_UTILITY ? Math.Exp(MIN_UTILITY) : Math.Exp(_utility[pathType]);
		}
	}
}