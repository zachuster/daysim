﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Daysim.Framework.Core;
using Daysim.Interfaces;
using Ninject;

namespace Daysim.Framework.Roster {
	public static class ImpedanceRoster {

		private static int[] _variableKeys;

		private static bool[][] _possibleCombinations;
		private static bool[][] _actualCombinations;
		private static RosterEntry[][][][][] _rosterEntries;
		private static SkimMatrix[] _skimMatrices;
		private static List<VotRange> _votRanges;
		private static ImpedanceRosterLoader _loader;

		public static void Initialize(Dictionary<int, int> zoneMapping, Dictionary<int, int> transitStopAreaMapping, ImpedanceRosterLoader loader = null) {
			if ( loader == null )
				_loader = new ImpedanceRosterLoader();
			else
				_loader = loader;
			BeginLoadRosterCombinations();
			var entries = BeginLoadRoster();
			BeginLoadSkimMatrices(entries, zoneMapping, transitStopAreaMapping);
		}

		private static void BeginLoadRosterCombinations() {
			Global.PrintFile.WriteLine("Roster combinations file:");
			Global.PrintFile.IncrementIndent();

			_loader.LoadRosterCombinations();
			_possibleCombinations = _loader.PossibleCombinations;
			_actualCombinations = _loader.ActualCombinations;

			Global.PrintFile.DecrementIndent();
		}

		

		private static IEnumerable<RosterEntry> BeginLoadRoster() {
			Global.PrintFile.WriteLine("Roster file:");
			Global.PrintFile.IncrementIndent();

			var entries = _loader.LoadRoster();
			_votRanges = _loader.VotRanges;
			_rosterEntries = _loader.RosterEntries;
			_variableKeys = _loader.VariableKeys;
			
			Global.PrintFile.DecrementIndent();

			return entries;
		}

		

		private static void BeginLoadSkimMatrices(IEnumerable<RosterEntry> entries, Dictionary<int, int> zoneMapping, Dictionary<int, int> transitStopAreaMapping) {
			Global.PrintFile.WriteLine("Skim matrices files:");
			Global.PrintFile.IncrementIndent();

			_loader.LoadSkimMatrices(entries, zoneMapping, transitStopAreaMapping);
			_skimMatrices = _loader.SkimMatrices;
			//LoadSkimMatrices(entries, zoneMapping, transitStopAreaMapping);

			Global.PrintFile.DecrementIndent();
		}


		public static bool IsPossibleCombination(int mode, int pathType) {
			return _possibleCombinations[mode][pathType];
		}

		public static bool IsActualCombination(int mode, int pathType) {
			return _actualCombinations[mode][pathType];
		}

		public static SkimValue GetValue(string variable, int mode, int pathType, double vot, int minute, int origin, int destination) {
			var votGroup = GetVotGroup(vot);
			var entry = GetEntry(variable, mode, pathType, votGroup, minute);

			return GetValue(origin, destination, entry, minute);
		}

		public static SkimValue GetValue(string variable, int mode, int pathType, double vot, int minute, IPoint origin, IPoint destination, double circuityDistance = Constants.DEFAULT_VALUE) {
			var votGroup = GetVotGroup(vot);
			var entry = GetEntry(variable, mode, pathType, votGroup, minute);
			var skimValue = GetValue(origin.ZoneId, destination.ZoneId, entry, minute);

			//mb fix for 0 intrazonals
            if (Global.Configuration.DestinationScale == Constants.DestinationScale.ZONE && origin.ZoneId == destination.ZoneId && skimValue.Variable < Constants.EPSILON) {
                if (variable == "distance")  {
                    skimValue.Variable = 0.25 * Global.Configuration.DistanceUnitsPerMile;
                }
                else if (variable == "ivtime" || variable == "time" || variable == "ivtfree")  {
                    skimValue.Variable =
                        (mode == Constants.Mode.WALK) ? 5 :
                        (mode == Constants.Mode.BIKE) ? 2 :
                        (mode > Constants.Mode.BIKE && mode < Constants.Mode.TRANSIT) ? 1 : 0;
                }
            }
            
            
            if (string.IsNullOrEmpty(entry.BlendVariable)) {
				return skimValue;
			}

            
			var blendEntry =
				entry.BlendPathType == Constants.PathType.NONE
					? GetEntry(entry.BlendVariable, entry.Mode, entry.PathType, votGroup, minute)
					: GetEntry(entry.BlendVariable, entry.Mode, entry.BlendPathType, votGroup, minute);
			var blendSkimValue = GetValue(origin.ZoneId, destination.ZoneId, blendEntry, minute);

			if (Global.Configuration.DestinationScale == Constants.DestinationScale.ZONE) {
                //skimValue.BlendVariable = blendSkimValue.BlendVariable;
                skimValue.BlendVariable = blendSkimValue.Variable;  //JLB replaced above line 20130628

                //mb fix for 0 intrazonals. Assumes blend variable is distance
                if (origin.ZoneId == destination.ZoneId && skimValue.BlendVariable < Constants.EPSILON)  {
                    skimValue.BlendVariable = 0.25 * Global.Configuration.DistanceUnitsPerMile;
                }

                return skimValue;
			}

			var networkDistance = blendSkimValue.Variable;
			var networkFraction = networkDistance / Global.Configuration.MaximumBlendingDistance;

			if (networkFraction > 1) {
				networkFraction = 1;
			}
			//intrazonals - use network fraction = 0, so blend distance is XY distance
			if (origin.ZoneId == destination.ZoneId) {
				networkFraction = 0;
			}

			double xyDistance;

			if (networkFraction >= 1) {
				// no blending
				xyDistance = networkDistance;
			}
			else if (circuityDistance > Constants.DEFAULT_VALUE + Constants.EPSILON) {
				// blending with circuity value
				//Global.PrintFile.WriteLine("Distances: Network {0} Circuity {1} Orthogonal {2}", networkDistance, circuityDistance,
				//	(Math.Abs(origin.XCoordinate - destination.XCoordinate) + Math.Abs(origin.YCoordinate - destination.YCoordinate)) / 5280D);
				xyDistance = circuityDistance;
			}
			else {
				// default is orthogonal distance
				xyDistance = (Math.Abs(origin.XCoordinate - destination.XCoordinate) + Math.Abs(origin.YCoordinate - destination.YCoordinate)) / 5280D;
			}

			if (networkDistance >= Constants.EPSILON && xyDistance >= Constants.EPSILON) {
				skimValue.BlendVariable = networkFraction * networkDistance + (1 - networkFraction) * xyDistance;
			}
			else if (xyDistance >= Constants.EPSILON) {
				skimValue.BlendVariable = xyDistance;
			}
			else if (networkDistance >= Constants.EPSILON) {
				skimValue.BlendVariable = networkDistance;
			}
			else {
				skimValue.BlendVariable = 0;
			}

			if (!skimValue.BlendVariable.AlmostEquals(Constants.DEFAULT_VALUE)) {
				if (networkDistance >= Constants.EPSILON) {
					skimValue.Variable = skimValue.Variable * skimValue.BlendVariable / networkDistance;
				}
					// if networkDistance is 0 or tiny, cannot use pivot method, multiply blend distance by default speed depending on mode
				else {
					// TODO: Make these constants for minutesPerMile configurable.
					var minutesPerMile = entry.Mode == Constants.Mode.WALK ? 20.0 :
					                                                              	entry.Mode == Constants.Mode.BIKE ? 6.0 : 3.0;
					skimValue.Variable = skimValue.BlendVariable * minutesPerMile;
				}
			}

			return skimValue;
		}

		private static SkimValue GetValue(int origin, int destination, RosterEntry entry, int minute) {
			if (entry.Name == null) {
				return new SkimValue();
			}

			var skimMatrix = _skimMatrices[entry.MatrixIndex];
			if (skimMatrix.IsEmpty())
			{
				return new SkimValue {Variable = 0, BlendVariable = 0};
			}
			if (skimMatrix == null) {
				throw new SkimMatrixNotFoundException(string.Format("There is not a skim matrix defined for the combination of variable: {0}, mode: {1}, path type: {2}, and minute: {3}. Please adjust the roster accordingly.", entry.Variable, entry.Mode, entry.PathType, minute));
			}

			var skimValue = new SkimValue {
				Variable = entry.Transpose ? skimMatrix.GetValue(destination, origin) : skimMatrix.GetValue(origin, destination)
			};

			skimValue.Variable = skimValue.Variable / entry.Scaling;

			skimValue.Variable = skimValue.Variable * entry.Factor;

			return skimValue;
		}

		private static int GetVariableIndex(string variable) {
			var variableIndex = _variableKeys.GetIndex(variable);

			if (variableIndex == -1) {
				throw new VariableNotFoundException(string.Format("The variable \"{0}\" was not found in the roster configuration file. Please correct the problem and run the program again.", variable));
			}

			return variableIndex;
		}

		private static int GetVotGroup(double vot)
		{
			foreach (var votRange in _votRanges)			{
				if (Math.Max(vot,Constants.EPSILON).IsLeftExclusiveBetween(votRange.Min, votRange.Max))				{
					return votRange.VotGroup;
				}
			}

			throw new VotGroupNotFoundException(string.Format("The vot group for vot \"{0}\" was not found in the roster configuration file. Please correct the problem and run the program again.", vot));
		}

		private static RosterEntry GetEntry(string variable, int mode, int pathType, int votGroup, int minute) {
			var variableIndex = GetVariableIndex(variable);

			if (!minute.IsBetween(1, Constants.Time.MINUTES_IN_A_DAY)) {
				throw new ArgumentOutOfRangeException("minute", string.Format("The value of \"{0}\" used for minute is outside the allowable range. It should be between 1 and {1}.", minute, Constants.Time.MINUTES_IN_A_DAY));
			}

			return GetEntry(variable, variableIndex, mode, pathType, votGroup, minute);
		}

		private static RosterEntry GetEntry(string variable, int variableIndex, int mode, int pathType, int votGroup, int minute) {
			var entry = _rosterEntries[variableIndex][mode][pathType][votGroup][minute];

			if (entry == null) {
				throw new RosterEntryNotFoundException(string.Format("There was not a roster entry that matched the combination of variable: {0}, mode: {1}, path type: {2}, vot group: {3}, and minute: {4}. Please adjust the roster accordingly.", variable, mode, pathType, votGroup, minute));
			}

			return entry;
		}

		public static List<VotRange> GetVotRanges() {
			const int MAX_VOT = 10000;
			var range = new double[6];

			if (Global.Configuration.VotVeryLowLow <= 0) {
				range[0] = Global.Configuration.VotVeryLowLow - 1;
			}
			else {
				range[0] = 0;
			}

			range[1] = Global.Configuration.VotVeryLowLow;
			range[2] = Global.Configuration.VotLowMedium;
			range[3] = Global.Configuration.VotMediumHigh;
			range[4] = Global.Configuration.VotHighVeryHigh;

			if (Global.Configuration.VotHighVeryHigh >= MAX_VOT) {
				range[5] = Global.Configuration.VotHighVeryHigh + 1;
			}
			else {
				range[5] = MAX_VOT;
			}

			var votRanges = new List<VotRange>();

			for (var i = 0; i < range.Length - 1; i++) {
				var votGroup = i + 1;
				var min = range[i];
				var max = range[i + 1];

				if (min >= 0 && max <= MAX_VOT) {
					votRanges.Add(new VotRange(votGroup, min, max));
				}
			}

			return votRanges;
		}

		public sealed class VotRange {
			public VotRange(int votGroup, double min, double max) {
				VotGroup = votGroup;
				Min = min;
				Max = max;
			}

			public int VotGroup { get; private set; }

			public double Min { get; private set; }

			public double Max { get; private set; }
		}
	}
}