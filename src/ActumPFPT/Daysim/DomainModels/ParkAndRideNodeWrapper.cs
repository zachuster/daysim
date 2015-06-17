// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using Daysim.Framework.Core;
using Daysim.ParkAndRideShadowPricing;

namespace Daysim.DomainModels {
	public class ParkAndRideNodeWrapper {
		private readonly ParkAndRideNode _parkAndRideNode;

		public ParkAndRideNodeWrapper(ParkAndRideNode parkAndRideNode) {
			_parkAndRideNode = parkAndRideNode;
		}

		public int Id {
			get { return _parkAndRideNode.Id; }
		}

		public int ZoneId {
			get { return _parkAndRideNode.ZoneId; }
		}

		public int XCoordinate {
			get { return _parkAndRideNode.XCoordinate; }
		}

		public int YCoordinate {
			get { return _parkAndRideNode.YCoordinate; }
		}

		public int Capacity {
			get { return _parkAndRideNode.Capacity; }
		}

		public int Cost {
			get { return _parkAndRideNode.Cost; }
		}

		public int NearestParcelId {
			get { return _parkAndRideNode.NearestParcelId; }
		}

		public double[] ShadowPriceDifference { get; set; }

		public double[] ShadowPrice { get; set; }

		public double[] ExogenousLoad { get; set; }

		public double[] ParkAndRideLoad { get; set; }

		public void SetParkAndRideShadowPricing(Dictionary<int, ParkAndRideShadowPriceNode> parkAndRideShadowPrices) {
			if (parkAndRideShadowPrices == null) {
				throw new ArgumentNullException("parkAndRideShadowPrices");
			}

			if (!Global.ParkAndRideNodeIsEnabled || !Global.Configuration.ShouldUseParkAndRideShadowPricing || Global.Configuration.IsInEstimationMode) {
				return;
			}

			ParkAndRideShadowPriceNode parkAndRideShadowPriceNode;

			ShadowPriceDifference = new double[Constants.Time.MINUTES_IN_A_DAY];
			ShadowPrice = new double[Constants.Time.MINUTES_IN_A_DAY];
			ExogenousLoad = new double[Constants.Time.MINUTES_IN_A_DAY];
			ParkAndRideLoad = new double[Constants.Time.MINUTES_IN_A_DAY];

			if (parkAndRideShadowPrices.TryGetValue(Id, out parkAndRideShadowPriceNode)) {
				ShadowPriceDifference = parkAndRideShadowPrices[Id].ShadowPriceDifference;
				ShadowPrice = parkAndRideShadowPrices[Id].ShadowPrice;
				ExogenousLoad = parkAndRideShadowPrices[Id].ExogenousLoad;
				// ParkAndRideLoad = parkAndRideShadowPrices[Id].ParkAndRideLoad; {JLB 20121001 commented out this line so that initial values of load are zero for any run}
			}
		}
	}
}