// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.Linq;
using Daysim.Framework.Core;

namespace Daysim.Framework.Factories {
	public abstract class TypeLocator {
		protected TypeLocator(Configuration configuration) {
			ChoiceModelRunner = GetChoiceModelRunner(configuration.ChoiceModelRunner);
			DataType = GetDataType(configuration.DataType);
		}

		protected ChoiceModelRunner ChoiceModelRunner { get; private set; }

		protected DataType DataType { get; private set; }

		private static ChoiceModelRunner GetChoiceModelRunner(string choiceModelRunner) {
			ChoiceModelRunner result;

			if (Enum.TryParse(choiceModelRunner, out result)) {
				return result;
			}

			var values =
				Enum
					.GetValues(typeof (ChoiceModelRunner))
					.Cast<ChoiceModelRunner>()
					.ToList();

			throw new Exception(string.Format("Unable to determine type. The choice model runner set to \"{0}\" is not valid. Valid values are {1}", choiceModelRunner, string.Join(", ", values)));
		}

		private static DataType GetDataType(string dataType) {
			DataType result;

			if (Enum.TryParse(dataType, out result)) {
				return result;
			}

			var values =
				Enum
					.GetValues(typeof (DataType))
					.Cast<DataType>()
					.ToList();

			throw new Exception(string.Format("Unable to determine type. The data type set to \"{0}\" is not valid. Valid values are {1}", dataType, string.Join(", ", values)));
		}
	}
}