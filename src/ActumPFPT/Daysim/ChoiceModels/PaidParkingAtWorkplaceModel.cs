// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// 
// Copyright 2011-2012 John Bowman, Mark Bradley, and RSG, Inc.
// 
// This file is part of Daysim.
// 
// Daysim is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Daysim is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Daysim. If not, see <http://www.gnu.org/licenses/>.

using System;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Framework.Roster;
using Daysim.Framework.Sampling;

namespace Daysim.ChoiceModels {
	public static class PaidParkingAtWorkplaceModel {
		private const string CHOICE_MODEL_NAME = "PaidParkingAtWorkplaceModel";
		private const int TOTAL_ALTERNATIVES = 2;
		private const int TOTAL_NESTED_ALTERNATIVES = 0;
		private const int TOTAL_LEVELS = 1;
		private const int MAX_PARAMETER = 99;

		private static ChoiceModelHelper _helper;

		private static void Initialize() {
			if (_helper != null) {
				return;
			}

			ChoiceModelHelper.Initialize(ref _helper, CHOICE_MODEL_NAME, Global.Configuration.PaidParkingAtWorkplaceModelCoefficients, TOTAL_ALTERNATIVES, TOTAL_NESTED_ALTERNATIVES, TOTAL_LEVELS, MAX_PARAMETER);
		}

		public static void Run(PersonWrapper person) {
			Initialize();
			_helper.OpenTrace();

			if (person == null) {
				throw new ArgumentNullException("person");
			}

			if (Global.Configuration.IsInEstimationMode) {
				if (!_helper.ModelIsInEstimationMode) {
					return;
				}
			}

			var choiceProbabilityCalculator = _helper.GetChoiceProbabilityCalculator(person.Id);

			if (_helper.ModelIsInEstimationMode) {
				if (person.PaidParkingAtWorkplace < 0 || person.PaidParkingAtWorkplace > 1 || person.UsualWorkParcel == null) {
					return;
				}

				RunModel(choiceProbabilityCalculator, person, person.PaidParkingAtWorkplace);

				choiceProbabilityCalculator.WriteObservation();
			}
			else {
				RunModel(choiceProbabilityCalculator, person);

				var chosenAlternative = choiceProbabilityCalculator.SimulateChoice();
				var choice = (int) chosenAlternative.Choice;

				person.PaidParkingAtWorkplace = choice;
			}

			_helper.CloseTrace(person);
		}

		private static void RunModel(ChoiceProbabilityCalculator choiceProbabilityCalculator, PersonWrapper person, int choice = Constants.DEFAULT_VALUE) {

			// 0 No paid parking at work

			var alternative = choiceProbabilityCalculator.GetAlternative(0, true, choice == 0);

			alternative.Choice = 0;

			alternative.AddUtility(1, 1.0);

			// 1 Paid parking at work

			alternative = choiceProbabilityCalculator.GetAlternative(1, true, choice == 1);

			alternative.Choice = 1;

			alternative.AddUtility(2, 1.0);
			alternative.AddUtility(3, person.IsPartTimeWorker.ToFlag());
			alternative.AddUtility(4, person.IsNotFullOrPartTimeWorker.ToFlag());
			alternative.AddUtility(5, Math.Max(1,person.Household.Income)/1000.0);
			alternative.AddUtility(6, person.Household.HasMissingIncome.ToFlag());
			alternative.AddUtility(7, Math.Log(person.UsualWorkParcel.EmploymentTotalBuffer1+1.0));
			alternative.AddUtility(8, Math.Log((person.UsualWorkParcel.ParkingOffStreetPaidDailySpacesBuffer1+1.0) / (person.UsualWorkParcel.EmploymentTotalBuffer1+1.0)));
			if (!Global.Configuration.IsInEstimationMode && Global.Configuration.PathImpedance_ParkingUsePercentPaidChange) {
				alternative.AddUtility(11,  Global.Configuration.PathImpedance_ParkingPercentPaidCalibrationCoefficient);
			}

		}
	}
}