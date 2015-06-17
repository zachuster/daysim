// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Daysim.Framework.Core {
	public class PrintFile : IDisposable {
		public const string DEFAULT_PRINT_FILE_NAME = "last-run.log";
		private readonly StreamWriter _writer;
		private int _indent;

		public PrintFile(string path = null) {
			if (string.IsNullOrEmpty(path)) {
				var location = Assembly.GetExecutingAssembly().Location;
				var directoryName = Path.GetDirectoryName(location);

				path = directoryName == null ? DEFAULT_PRINT_FILE_NAME : Path.Combine(directoryName, DEFAULT_PRINT_FILE_NAME);
			}

			var file = new FileInfo(path);

			try {
				_writer = new StreamWriter(file.Open(FileMode.Create, FileAccess.Write, FileShare.Read)) {AutoFlush = false};
			}
			catch (Exception) {
				Console.WriteLine("The path to the print file, {0}, is invalid. Please enter a valid path.", file.FullName);
				Console.WriteLine("Please press any key to exit");
				Console.ReadKey();
				Environment.Exit(2);
			}

			//WriteConfiguration();
		}

		public void IncrementIndent() {
			_indent += 2;
		}

		public void DecrementIndent() {
			_indent -= 2;
		}

		public void WriteConfiguration() {
			var properties = typeof (Configuration).GetProperties(BindingFlags.Public | BindingFlags.Instance);

			WriteFromXml(properties);

			var list = properties.Select(p => {
				var value = p.GetValue(Global.Configuration, null);
				var metadata = p.GetCustomAttributes(typeof (MetadataAttribute), true).Cast<MetadataAttribute>().SingleOrDefault();
				var description = metadata == null || string.IsNullOrEmpty(metadata.Value) ? p.Name.ToSentenceCase() : metadata.Value;

				string s;

				if (value == null) {
					s = string.Empty;
				}
				else {
					if (value is char) {
						var b = (byte) (char) value;

						s = string.Format("{0} - {1}", b, AsciiTable.GetDescription(b));
					}
					else {
						s = value.ToString().Trim();
					}
				}

				return new {p.Name, Value = s, Description = description};
			}).ToList();

			var maxKeyLength = list.Select(p => p.Name.Length).Max();
			var maxValueLength = list.Select(p => p.Value.Length).Max();

			foreach (var property in list) {
				_writer.WriteLine("{0}> {1} // {2}.", property.Name.PadLeft(maxKeyLength), property.Value.PadRight(maxValueLength), property.Description);
			}

			_writer.WriteLine();
			_writer.Flush();
		}

		private void WriteFromXml(PropertyInfo[] properties) {
			using (var stream = ConfigurationManager.XmlFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read)) {
				var document = XDocument.Load(stream);
				var attributes =
					document.Root == null
						? new List<string>()
						: document.Root.Attributes().Select(x => x.Name.LocalName).ToList();

				WriteUnusedSettings(properties, attributes);
				WriteInvalidSettings(properties, attributes);
			}
		}

		private void WriteUnusedSettings(IEnumerable<PropertyInfo> properties, IEnumerable<string> attributes) {
			var list = (from property in properties where attributes.All(x => x != property.Name) select property.Name).ToList();

			if (list.Count == 0) {
				return;
			}

			WriteLine("The following properties where not set in the configuration file:");
			IncrementIndent();

			foreach (var item in list) {
				WriteLine("* {0}", item);
			}

			DecrementIndent();
			WriteLine();
		}

		private void WriteInvalidSettings(IEnumerable<PropertyInfo> properties, IEnumerable<string> attributes) {
			var list = (from attribute in attributes.Where(x => x != "xsd" && x != "xsi") where properties.All(x => x.Name != attribute) select attribute).ToList();

			if (list.Count == 0) {
				return;
			}

			WriteLine("The following attributes in the configuration file are invalid:");
			IncrementIndent();

			foreach (var item in list) {
				WriteLine("* {0}", item);
			}

			DecrementIndent();
			WriteLine();
		}

		public virtual void WriteLine(string value = null) {
			if (string.IsNullOrEmpty(value)) {
				_writer.WriteLine();
			}
			else {
				_writer.WriteLine(new string(' ', _indent) + value);
			}

			_writer.Flush();
		}

		public void WriteLine(string format, object arg0) {
			WriteLine(string.Format(format, arg0));
		}

		public void WriteLine(string format, object arg0, object arg1) {
			WriteLine(string.Format(format, arg0, arg1));
		}

		public void WriteLine(string format, object arg0, object arg1, object arg2) {
			WriteLine(string.Format(format, arg0, arg1, arg2));
		}

		public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3) {
			WriteLine(string.Format(format, arg0, arg1, arg2, arg3));
		}

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            WriteLine(string.Format(format, arg0, arg1, arg2, arg3, arg4));
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5)
        {
            WriteLine(string.Format(format, arg0, arg1, arg2, arg3, arg4, arg5));
        }

        public void WriteLine(string format, object arg0, object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
        {
            WriteLine(string.Format(format, arg0, arg1, arg2, arg3, arg4, arg5, arg6));
        }
        
        public void WriteFileInfo(FileInfo file)
        {
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

			var checksum = includeChecksum && file.Exists ? string.Format(", MD5 checksum: {0}", file.ToMD5Checksum()) : null;
			destination = string.IsNullOrEmpty(destination) ? "" : string.Format(@" --> ""{0}""", destination);
			var s = string.Format(@"* ""{0}""{1}, Size: {2}, Exists? {3}{4}", file.FullName, destination, file.Exists ? file.Length.ToFileSize() : "unknown", file.Exists ? "Yes" : "No", checksum);

			WriteLine(s);
		}

		public void WriteArrivalTimeGreaterThanDepartureTimeWarning(string @class, string method, int personDayId, int arrivalTime, int departureTime) {
			if (Global.Configuration.ReportInvalidPersonDays) WriteLine(string.Format(@"Warning in {0}.{1}: An attempt to simulate a destination arrival time of ""{3}"" greater than the destination departure time of ""{4}"" was made. PersonDay {2} is invalid.", @class, method, personDayId, arrivalTime, departureTime));
		}

		public void WriteSubtourArrivalAndDepartureTimesOutOfRangeWarning(string @class, string method, int personDayId, int subtourArrivalTime, int subtourDepartureTime, int tourArrivalTime, int tourDepartureTime) {
			if (Global.Configuration.ReportInvalidPersonDays) WriteLine(string.Format(@"Warning in {0}.{1}: The subtour destination times of ""{3}"" and ""{4}"" must be between the parent tour's destination arrival time of ""{5}"" and destination departure time of ""{6}"". PersonDay {2} is invalid.", @class, method, personDayId, subtourArrivalTime, subtourDepartureTime, tourArrivalTime, tourDepartureTime));
		}

		public void WriteNoAlternativesAvailableWarning(string @class, string method, int personDayId) {
//#if DEBUG		
			if (Global.Configuration.ReportInvalidPersonDays) WriteLine(string.Format(@"Warning in {0}.{1}: No alternatives available. PersonDay {2} is invalid.", @class, method, personDayId));
//#endif
		}

		public void WriteDurationIsInvalidWarning(string @class, string method, int personDayId, double travelTime, double travelCost, double travelDistance) {
			if (Global.Configuration.ReportInvalidPersonDays) WriteLine(string.Format(@"Warning in {0}.{1}: Duration is invalid. PersonDay {2} is invalid. Travel time: {3}, Travel cost: {4}, Travel distance: {5}", @class, method, personDayId, travelTime, travelCost, travelDistance));
		}

		public void WriteEstimationRecordExclusionMessage(string @class, string method, int houseHoldId, int personId, int personDayId, int tourId, int halfTourId, int tripId, int excludeReason) {
			WriteLine(string.Format(@"Message in {0}.{1}: Record excluded from estimation. HH {2}, Person {3}, Day {4}, Tour {5}, HalfTour {6}, Trip {7}.  Reason {8}", @class, method, houseHoldId, personId, personDayId, tourId, halfTourId, tripId, excludeReason));
		}

		public void WriteInfiniteLoopWarning(int personDayId, int tourId, int tripId, int parcelID, int zoneId, int destZoneID, int mode, int time, double weight, double size) {
			if (Global.Configuration.ReportInvalidPersonDays) WriteLine(string.Format(@"Warning: Infinite Loop. PersonDay {0}, Tour {1}, Trip {2}, Parcel {3}, Origin Zone {4}, Destination Zone {5}, Mode{6}, Time{7}, Weight {8}, Size {9} is invalid.", personDayId, tourId, tripId, parcelID, zoneId, destZoneID, mode, time, weight, size));
		}

		public void Dispose() {
			if (_writer != null) {
				_writer.Dispose();
			}
		}
	}
}