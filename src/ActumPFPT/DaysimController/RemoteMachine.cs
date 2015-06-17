﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
using System.Collections.Generic;
using System.IO;
using Daysim.Framework.Core;

namespace DaysimController {
	public class RemoteMachine {
		public string Name { get; private set; }

		public string CurrentDirectory { get; private set; }

		public string Filename { get; private set; }

		public string Arguments { get; private set; }

		public string CommandLine { get; private set; }

		public string ConfigurationPath { get; private set; }
		
		public string PrintFilePath { get; private set; }

		public static List<RemoteMachine> GetAll() {
			var entries = Global.Configuration.RemoteMachines.Split(',');
			var machines = new List<RemoteMachine>();

			foreach (var entry in entries) {
				var tokens = entry.Split('|');

				var machine = new RemoteMachine {
					Name = tokens[0], 
					CurrentDirectory = Path.GetDirectoryName(tokens[1]), 
					Filename = tokens[1],
					CommandLine = tokens[1]
				};

				for (var i = 2; i < tokens.Length; i++) {
					machine.Arguments += ((i == 2 ? null : " ") + tokens[i]);
					machine.CommandLine += (" " + tokens[i]);

					if (tokens[i].StartsWith("/c=")) {
						machine.ConfigurationPath = tokens[i].Substring(3);
					}

					if (tokens[i].StartsWith("/p=")) {
						machine.PrintFilePath= tokens[i].Substring(3);
					}
				}

				if (string.IsNullOrEmpty(machine.ConfigurationPath)) {
					machine.ConfigurationPath = machine.CurrentDirectory + @"\" + ConfigurationManager.DEFAULT_CONFIGURATION_NAME;
				}

				if (string.IsNullOrEmpty(machine.PrintFilePath)) {
					machine.PrintFilePath = machine.CurrentDirectory + @"\" + Daysim.Framework.Core.PrintFile.DEFAULT_PRINT_FILE_NAME;
				}

				machines.Add(machine);
			}

			return machines;
		}
	}
}
