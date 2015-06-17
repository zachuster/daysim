// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace Daysim.Framework.Core {
	public static class ConfigurationManager {
		public const string DEFAULT_CONFIGURATION_NAME = "Configuration.xml";

		public static FileInfo XmlFile { get; private set; }

		public static Configuration OpenConfiguration(string path = null) {
			if (string.IsNullOrEmpty(path)) {
				var location = Assembly.GetExecutingAssembly().Location;
				var directoryName = Path.GetDirectoryName(location);

				path = directoryName == null ? DEFAULT_CONFIGURATION_NAME : Path.Combine(directoryName, DEFAULT_CONFIGURATION_NAME);
			}

			var serializer = new XmlSerializer(typeof (Configuration));
			var file = new FileInfo(path);

			XmlFile = file;

			try {
				using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read)) {
					return (Configuration) serializer.Deserialize(stream);
				}
			}
			catch (Exception e) {
				
				Console.WriteLine("There was an error reading the configuration file, " + path);
				if (e.Message.StartsWith("There is an error in XML document"))
				{
					try
					{
						char[] split1 = {'('};
						char[] split2 = {','};
						int line = int.Parse(e.Message.Split(split1)[1].Split(split2)[0]);
						Console.WriteLine("The error is on line " + line);
					}
					catch (Exception)
					{
						Console.WriteLine(e.Message);
					}
					
				}
				else
				{
					Console.WriteLine(e.Message);
				}

				if (e.InnerException != null) {
					Console.WriteLine(e.InnerException.Message);
				}

				
				Global.PrintFile.WriteLine(e.ToString());
				if (e.InnerException != null)
				{
					Global.PrintFile.WriteLine(e.InnerException.Message);
				}
				Console.WriteLine("Please press any key to exit");
				Console.ReadKey();
				Environment.Exit(2);
				return null;
			}
		}

		public static void SaveConfiguration(Configuration configuration, string path) {
			if (path == null) {
				var location = Assembly.GetExecutingAssembly().Location;
				var directoryName = Path.GetDirectoryName(location);

				path = directoryName == null ? "Configuration.xml" : Path.Combine(directoryName, "Configuration.xml");
			}

			var serializer = new XmlSerializer(typeof (Configuration));
			var file = new FileInfo(path);

			using (var stream = file.Open(FileMode.Create, FileAccess.Write, FileShare.Read)) {
				serializer.Serialize(stream, configuration);
			}
		}
	}
}