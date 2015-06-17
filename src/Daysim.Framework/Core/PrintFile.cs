﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.IO;
using System.Reflection;

namespace Daysim.Framework.Core {
	public class PrintFile : IDisposable {
		private readonly Configuration _configuration;
		public const string DEFAULT_PRINT_FILE_NAME = "last-run.log";
		private readonly StreamWriter _writer;
		private int _indent;

		public PrintFile(string path, Configuration configuration = null) {
			_configuration = configuration;

			if (string.IsNullOrWhiteSpace(path)) {
				var location = Assembly.GetExecutingAssembly().Location;
				var directoryName = Path.GetDirectoryName(location);

				path =
					directoryName == null
						? DEFAULT_PRINT_FILE_NAME
						: Path.Combine(directoryName, DEFAULT_PRINT_FILE_NAME);
			}

			var file = new FileInfo(path);

			_writer = new StreamWriter(file.Open(FileMode.Create, FileAccess.Write, FileShare.Read)) {
				AutoFlush = false
			};
		}

		public void IncrementIndent() {
			_indent += 2;
		}

		public void DecrementIndent() {
			_indent -= 2;
		}

		public void WriteLine() {
			_writer.WriteLine();

			_writer.Flush();
		}

		public void WriteLine(string value) {
			_writer.WriteLine(new string(' ', _indent) + value);

			_writer.Flush();
		}

		public void WriteLine(string format, params object[] args) {
			WriteLine(string.Format(format, args));
		}

		public void WriteFileInfo(FileInfo file) {
			WriteFileInfo(file, null, false, null);
		}

		public void WriteFileInfo(FileInfo file, string alternateMessage) {
			WriteFileInfo(file, alternateMessage, false, null);
		}

		public void WriteFileInfo(FileInfo file, bool includeChecksum) {
			WriteFileInfo(file, null, includeChecksum, null);
		}

		public void WriteFileInfo(FileInfo file, bool includeChecksum, string destination) {
			WriteFileInfo(file, null, includeChecksum, destination);
		}

		private void WriteFileInfo(FileInfo file, string alternateMessage, bool includeChecksum, string destination) {
			if (file == null) {
				WriteLine(alternateMessage);

				return;
			}

			var checksum =
				includeChecksum && file.Exists
					? string.Format(", MD5 checksum: {0}", file.ToMD5Checksum())
					: null;

			destination =
				string.IsNullOrWhiteSpace(destination)
					? null
					: string.Format(@" --> ""{0}""", destination);

			var format =
				string.Format(@"* ""{0}""{1}, Size: {2}, Exists? {3}{4}",
					file.FullName,
					destination,
					file.Exists
						? file.Length.ToFileSize()
						: "unknown",
					file.Exists
						? "Yes"
						: "No", checksum);

			WriteLine(format);
		}

		public void WriteArrivalTimeGreaterThanDepartureTimeWarning(string @class, string method, int personDayId, int arrivalTime, int departureTime) {
			if (_configuration == null) {
				throw new InvalidOperationException("No active configuration set.");
			}

			if (_configuration.ReportInvalidPersonDays) {
				WriteLine(string.Format(@"Warning in {0}.{1}: An attempt to simulate a destination arrival time of ""{3}"" greater than the destination departure time of ""{4}"" was made. PersonDay {2} is invalid.", @class, method, personDayId, arrivalTime, departureTime));
			}
		}

		public void WriteSubtourArrivalAndDepartureTimesOutOfRangeWarning(string @class, string method, int personDayId, int subtourArrivalTime, int subtourDepartureTime, int tourArrivalTime, int tourDepartureTime) {
			if (_configuration == null) {
				throw new InvalidOperationException("No active configuration set.");
			}

			if (_configuration.ReportInvalidPersonDays) {
				WriteLine(string.Format(@"Warning in {0}.{1}: The subtour destination times of ""{3}"" and ""{4}"" must be between the parent tour's destination arrival time of ""{5}"" and destination departure time of ""{6}"". PersonDay {2} is invalid.", @class, method, personDayId, subtourArrivalTime, subtourDepartureTime, tourArrivalTime, tourDepartureTime));
			}
		}

		public void WriteNoAlternativesAvailableWarning(string @class, string method, int personDayId) {
			if (_configuration == null) {
				throw new InvalidOperationException("No active configuration set.");
			}

			if (_configuration.ReportInvalidPersonDays) {
				WriteLine(string.Format(@"Warning in {0}.{1}: No alternatives available. PersonDay {2} is invalid.", @class, method, personDayId));
			}
		}

		public void WriteDurationIsInvalidWarning(string @class, string method, int personDayId, double travelTime, double travelCost, double travelDistance) {
			if (_configuration == null) {
				throw new InvalidOperationException("No active configuration set.");
			}

			if (_configuration.ReportInvalidPersonDays) {
				WriteLine(string.Format(@"Warning in {0}.{1}: Duration is invalid. PersonDay {2} is invalid. Travel time: {3}, Travel cost: {4}, Travel distance: {5}", @class, method, personDayId, travelTime, travelCost, travelDistance));
			}
		}

		public void WriteEstimationRecordExclusionMessage(string @class, string method, int houseHoldId, int personId, int personDayId, int tourId, int halfTourId, int tripId, int excludeReason) {
			WriteLine(string.Format(@"Message in {0}.{1}: Record excluded from estimation. HH {2}, Person {3}, Day {4}, Tour {5}, HalfTour {6}, Trip {7}.  Reason {8}", @class, method, houseHoldId, personId, personDayId, tourId, halfTourId, tripId, excludeReason));
		}

		public void WriteInfiniteLoopWarning(int personDayId, int tourId, int tripId, int parcelId, int zoneId, int destZoneId, int mode, int time, double weight, double size) {
			if (_configuration == null) {
				throw new InvalidOperationException("No active configuration set.");
			}

			if (_configuration.ReportInvalidPersonDays) {
				WriteLine(string.Format(@"Warning: Infinite Loop. PersonDay {0}, Tour {1}, Trip {2}, Parcel {3}, Origin Zone {4}, Destination Zone {5}, Mode{6}, Time{7}, Weight {8}, Size {9} is invalid.", personDayId, tourId, tripId, parcelId, zoneId, destZoneId, mode, time, weight, size));
			}
		}

		public void Dispose() {
			if (_writer != null) {
				_writer.Dispose();
			}
		}
	}
}