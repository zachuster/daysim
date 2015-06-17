﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
using System;
using Daysim;
using Daysim.Framework.Core;
using NDesk.Options;
using Ninject;

namespace DaysimController {
	public class Program {
		private static string _configurationPath;
		private static bool _showHelp;

		static void Main(string[] args) {
			var options = new OptionSet {
				{"c|configuration=", "Path to configuration file", v => _configurationPath = v},
				{"h|?|help", "Show help and syntax summary", v => _showHelp = v != null}
			};

			options.Parse(args);

			if (_showHelp) {
				options.WriteOptionDescriptions(Console.Out);

				Console.WriteLine();
				Console.WriteLine("If you do not provide a configuration then the default is to use {0}, in the same directory as the executable.", ConfigurationManager.DEFAULT_CONFIGURATION_NAME);

				Console.WriteLine();
				Console.WriteLine("If you do not provide a printfile then the default is to create {0}, in the same directory as the executable.", PrintFile.DEFAULT_PRINT_FILE_NAME);

				Environment.Exit(0);
			}

			Global.Configuration = ConfigurationManager.OpenConfiguration(_configurationPath);

			Engine.Start = -1;
			Engine.End = -1;
			Engine.Index = -1;

			using (var daysimModule = new DaysimModule()) {
				Global.Kernel = new StandardKernel(daysimModule);

				Controller.BeginProgram();
			}
		}
	}
}
