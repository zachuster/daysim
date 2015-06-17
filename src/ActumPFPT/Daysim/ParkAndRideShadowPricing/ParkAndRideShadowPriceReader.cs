// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.IO;
using Daysim.Framework.Core;

namespace Daysim.ParkAndRideShadowPricing {
	public static class ParkAndRideShadowPriceReader {
		public static Dictionary<int, ParkAndRideShadowPriceNode> ReadParkAndRideShadowPrices() {
			var shadowPrices = new Dictionary<int, ParkAndRideShadowPriceNode>();
			var shadowPriceFile = new FileInfo(Global.ParkAndRideShadowPricesPath);

			if (!Global.ParkAndRideNodeIsEnabled || !shadowPriceFile.Exists || !Global.Configuration.ShouldUseParkAndRideShadowPricing || Global.Configuration.IsInEstimationMode) {
				return shadowPrices;
			}

			using (var reader = new StreamReader(shadowPriceFile.Open(FileMode.Open, FileAccess.Read, FileShare.Read))) {
				reader.ReadLine();

				string line;

				while ((line = reader.ReadLine()) != null) {
					var tokens = line.Split(new[] {Global.Configuration.ParkAndRideShadowPriceDelimiter}, StringSplitOptions.RemoveEmptyEntries);

					var shadowPriceNode = new ParkAndRideShadowPriceNode {
						NodeId = Convert.ToInt32(tokens[0]),
					};

					shadowPriceNode.ShadowPriceDifference = new double[Constants.Time.MINUTES_IN_A_DAY];
					shadowPriceNode.ShadowPrice = new double[Constants.Time.MINUTES_IN_A_DAY];
					shadowPriceNode.ExogenousLoad = new double[Constants.Time.MINUTES_IN_A_DAY];
					shadowPriceNode.ParkAndRideLoad = new double[Constants.Time.MINUTES_IN_A_DAY];

					for (var i = 1; i <= Constants.Time.MINUTES_IN_A_DAY; i++) {
						shadowPriceNode.ShadowPriceDifference[i - 1] = Convert.ToDouble(tokens[i]);
						shadowPriceNode.ShadowPrice[i - 1] = Convert.ToDouble(tokens[Constants.Time.MINUTES_IN_A_DAY + i]);
						shadowPriceNode.ExogenousLoad[i - 1] = Convert.ToDouble(tokens[2 * Constants.Time.MINUTES_IN_A_DAY + i]);
						shadowPriceNode.ParkAndRideLoad[i - 1] = Convert.ToDouble(tokens[3 * Constants.Time.MINUTES_IN_A_DAY + i]);
					}

					shadowPrices.Add(shadowPriceNode.NodeId, shadowPriceNode);
				}
			}

			return shadowPrices;
		}
	}
}