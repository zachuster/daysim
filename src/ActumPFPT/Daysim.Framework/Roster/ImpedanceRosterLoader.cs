using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Daysim.Framework.Core;
using Ninject;

namespace Daysim.Framework.Roster {
	public class ImpedanceRosterLoader 
	{

		private string _path;

		public int[] VariableKeys { get; protected set; }
		public int[] MatrixKeys { get; protected set; }
		public bool[][] PossibleCombinations { get; protected set; }
		public bool[][] ActualCombinations { get; protected set; }
		public RosterEntry[][][][][] RosterEntries { get; protected set; }
		public List<ImpedanceRoster.VotRange> VotRanges { get; protected set; }
		public SkimMatrix[] SkimMatrices { get; set; }

		public virtual void LoadRosterCombinations() {
			var file = Global.GetInputPath(Global.Configuration.RosterCombinationsPath).ToFile();

			Global.PrintFile.WriteFileInfo(file, true);

			PossibleCombinations = new bool[Constants.Mode.TOTAL_MODES][];
			ActualCombinations = new bool[Constants.Mode.TOTAL_MODES][];

			for (var mode = Constants.Mode.WALK; mode < Constants.Mode.TOTAL_MODES; mode++) {
				PossibleCombinations[mode] = new bool[Constants.PathType.TOTAL_PATH_TYPES];
				ActualCombinations[mode] = new bool[Constants.PathType.TOTAL_PATH_TYPES];
			}

			using (var reader = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))) {
				string line;

				while ((line = reader.ReadLine()) != null) {
					if (line.StartsWith("#")) {
						continue;
					}

					var tokens = line.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();

					if (tokens.Length == 0) {
						continue;
					}

					int pathType;

					switch (tokens[0]) {
						case "full-network":
							pathType = Constants.PathType.FULL_NETWORK;

							break;
						case "no-tolls":
							pathType = Constants.PathType.NO_TOLLS;

							break;
						case "local-bus":
							pathType = Constants.PathType.LOCAL_BUS;

							break;
						case "light-rail":
							pathType = Constants.PathType.LIGHT_RAIL;

							break;
						case "premium-bus":
							pathType = Constants.PathType.PREMIUM_BUS;

							break;
						case "commuter-rail":
							pathType = Constants.PathType.COMMUTER_RAIL;

							break;
						case "ferry":
							pathType = Constants.PathType.FERRY;

							break;
						default:
							throw new InvalidPathTypeException(string.Format("The value of \"{0}\" used for path type is invalid. Please adjust the roster accordingly.", tokens[0]));
					}

					for (var mode = Constants.Mode.WALK; mode < Constants.Mode.TOTAL_MODES; mode++) {
						PossibleCombinations[mode][pathType] = bool.Parse(tokens[mode]);
					}
				}
			}
		}

		public virtual IEnumerable<RosterEntry> LoadRoster() {
			var file = Global.GetInputPath(Global.Configuration.RosterPath).ToFile();

			Global.PrintFile.WriteFileInfo(file, true);

			_path = file.DirectoryName;

			var entries = new List<RosterEntry>();

			using (var reader = new StreamReader(file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))) {
				string line;

				while ((line = reader.ReadLine()) != null) {
					if (line.StartsWith("#")) {
						continue;
					}

					var tokens = line.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToArray();

					if (tokens.Length == 0) {
						continue;
					}

					var entry = new RosterEntry {
						Variable = tokens[0].Clean(),
						Mode = tokens[1].ToMode(),
						PathType = tokens[2].ToPathType(),
						VotGroup = tokens[3].ToVotGroup(),
						StartMinute = int.Parse(tokens[4]).ToMinutesAfter3AM(),
						EndMinute = int.Parse(tokens[5]).ToMinutesAfter3AM(),
						Length = tokens[6].Clean(),
						FileType = tokens[7].Clean(),
						Name = tokens[8],
						Field = int.Parse(tokens[9]),
						Transpose = bool.Parse(tokens[10]),
						BlendVariable = tokens[11].Clean(),
						BlendPathType = tokens[12].ToPathType(),
						Factor = tokens[13].ToFactor(),
						Scaling = ParseScaling(tokens[14])
					};

					if (!IsPossibleCombination(entry.Mode, entry.PathType)) {
						throw new InvalidCombinationException(string.Format("The combination of mode: {0} and path type: {1} is invalid. Please adjust the roster accordingly.", entry.Mode, entry.PathType));
					}

					ActualCombinations[entry.Mode][entry.PathType] = true;

					entries.Add(entry);
				}
			}

			VariableKeys = entries.Select(x => x.Variable.GetHashCode()).Distinct().OrderBy(x => x).ToArray();
			MatrixKeys = entries.Select(x => x.MatrixKey).Distinct().OrderBy(x => x).ToArray();

			foreach (var entry in entries) {
				entry.VariableIndex = GetVariableIndex(entry.Variable);
				entry.MatrixIndex = MatrixKeys.GetIndex(entry.MatrixKey);
			}

			RosterEntries = new RosterEntry[VariableKeys.Length][][][][];

			for (var variableIndex = 0; variableIndex < VariableKeys.Length; variableIndex++) {
				RosterEntries[variableIndex] = new RosterEntry[Constants.Mode.TOTAL_MODES][][][]; // Initialize the mode array

				for (var mode = Constants.Mode.WALK; mode < Constants.Mode.TOTAL_MODES; mode++) {
					RosterEntries[variableIndex][mode] = new RosterEntry[Constants.PathType.TOTAL_PATH_TYPES][][]; // Initialize the path type array

					for (var pathType = Constants.PathType.FULL_NETWORK; pathType < Constants.PathType.TOTAL_PATH_TYPES; pathType++) {
						RosterEntries[variableIndex][mode][pathType] = new RosterEntry[Constants.VotGroup.TOTAL_VOT_GROUPS][]; // Initialize the vot groups

						for (var votGroup = Constants.VotGroup.VERY_LOW; votGroup < Constants.VotGroup.TOTAL_VOT_GROUPS; votGroup++) {
							RosterEntries[variableIndex][mode][pathType][votGroup] = new RosterEntry[Constants.Time.MINUTES_IN_A_DAY + 1]; // Initialize the minute array	
						}
					}
				}
			}

			foreach (var entry in entries) {
				var startMinute = entry.StartMinute;
				var endMinute = entry.EndMinute;

				// if roster entry for vot group is any or all or default, apply it to all vot groups
				var lowestVotGroup = entry.VotGroup == Constants.VotGroup.DEFAULT ? Constants.VotGroup.VERY_LOW : entry.VotGroup;
				var highestVotGroup = entry.VotGroup == Constants.VotGroup.DEFAULT ? Constants.VotGroup.VERY_HIGH : entry.VotGroup;

				for (var votGroup = lowestVotGroup; votGroup <= highestVotGroup; votGroup++) {
					if (startMinute > endMinute) {
						for (var minute = 1; minute <= endMinute; minute++) {
							RosterEntries[entry.VariableIndex][entry.Mode][entry.PathType][votGroup][minute] = entry;
						}

						for (var minute = startMinute; minute <= Constants.Time.MINUTES_IN_A_DAY; minute++) {
							RosterEntries[entry.VariableIndex][entry.Mode][entry.PathType][votGroup][minute] = entry;
						}
					}
					else {
						for (var minute = startMinute; minute <= endMinute; minute++) {
							RosterEntries[entry.VariableIndex][entry.Mode][entry.PathType][votGroup][minute] = entry;
						}
					}
				}
			}

			VotRanges = ImpedanceRoster.GetVotRanges();

			return entries;
		}

		private int GetVariableIndex(string variable) {
			var variableIndex = VariableKeys.GetIndex(variable);

			if (variableIndex == -1) {
				throw new VariableNotFoundException(string.Format("The variable \"{0}\" was not found in the roster configuration file. Please correct the problem and run the program again.", variable));
			}

			return variableIndex;
		}

		private double ParseScaling(string s)
		{
			bool scale;
			if (bool.TryParse(s, out scale))
			{
				if (scale)
					return 100;
				return 1;
			}
			return double.Parse(s);
		}

		public bool IsPossibleCombination(int mode, int pathType) {
			return PossibleCombinations[mode][pathType];
		}

		
		public virtual void LoadSkimMatrices(IEnumerable<RosterEntry> entries, Dictionary<int, int> zoneMapping, Dictionary<int, int> transitStopAreaMapping) {
			SkimMatrices = new SkimMatrix[MatrixKeys.Length];

			var cache = new Dictionary<string, List<float[]>>();

			foreach (var entry in entries.Where(x => x.FileType != null).Select(x => new {x.Name, x.Field, x.FileType, x.MatrixIndex, x.Scaling, x.Length}).Distinct().OrderBy(x => x.Name)) {
				ISkimFileReader skimFileReader = null;

				IFileReaderCreator creator = Global.Kernel.Get<SkimFileReaderFactory>().GetFileReaderCreator(entry.FileType);

				/*switch (entry.FileType) {
					case "text_ij":
						skimFileReader = new TextIJSkimFileReader(cache, _path, mapping);
						break;
					case "bin":
						skimFileReader = new BinarySkimFileReader(_path, mapping);
						break;
				}*/

				if (creator == null) {
					if (entry.FileType == "deferred") {
						continue;
					}

					throw new SkimFileTypeNotSupportedException(string.Format("The specified skim file type of \"{0}\" is not supported.", entry.FileType));
				}
				Dictionary<int, int> mapping = zoneMapping;

				bool useTransitStopAreaMapping = (entry.Length == "transitstop");

				if (useTransitStopAreaMapping)
					mapping = transitStopAreaMapping;
				skimFileReader = creator.CreateReader(cache, _path, mapping);

				var skimMatrix = skimFileReader.Read(entry.Name, entry.Field, (float)entry.Scaling);

				SkimMatrices[entry.MatrixIndex] = skimMatrix;
			}

			foreach (
				var entry in
					entries.Where(x => x.FileType == null)
					       .Select(x => new {x.Name, x.Field, x.FileType, x.MatrixIndex, x.Scaling, x.Length})
					       .Distinct()
					       .OrderBy(x => x.Name))
			{
				var skimMatrix = new SkimMatrix(null);
				SkimMatrices[entry.MatrixIndex] = skimMatrix;
			}

		}
	}
}
