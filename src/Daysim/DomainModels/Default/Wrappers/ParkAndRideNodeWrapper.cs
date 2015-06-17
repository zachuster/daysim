﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.Collections.Generic;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Models;
using Daysim.Framework.DomainModels.Wrappers;
using Daysim.Framework.Factories;
using Daysim.Framework.ShadowPricing;

namespace Daysim.DomainModels.Default.Wrappers {
	[Factory(Factory.WrapperFactory, Category = Category.Wrapper, DataType = DataType.Default)]
	public class ParkAndRideNodeWrapper : IParkAndRideNodeWrapper {
		private readonly IParkAndRideNode _parkAndRideNode;

		[UsedImplicitly]
		public ParkAndRideNodeWrapper(IParkAndRideNode parkAndRideNode) {
			_parkAndRideNode = parkAndRideNode;
		}

		#region domain model properies

		public int Id {
			get { return _parkAndRideNode.Id; }
			set { _parkAndRideNode.Id = value; }
		}

		public int ZoneId {
			get { return _parkAndRideNode.ZoneId; }
			set { _parkAndRideNode.ZoneId = value; }
		}

		public int XCoordinate {
			get { return _parkAndRideNode.XCoordinate; }
			set { _parkAndRideNode.XCoordinate = value; }
		}

		public int YCoordinate {
			get { return _parkAndRideNode.YCoordinate; }
			set { _parkAndRideNode.YCoordinate = value; }
		}

		public int Capacity {
			get { return _parkAndRideNode.Capacity; }
			set { _parkAndRideNode.Capacity = value; }
		}

		public int Cost {
			get { return _parkAndRideNode.Cost; }
			set { _parkAndRideNode.Cost = value; }
		}

		public int NearestParcelId {
			get { return _parkAndRideNode.NearestParcelId; }
			set { _parkAndRideNode.NearestParcelId = value; }
		}

		public int NearestStopAreaId {
			get { return _parkAndRideNode.NearestStopAreaId; }
			set { _parkAndRideNode.NearestStopAreaId = value; }
		}

		#endregion

		#region flags/choice model/etc. properties

		public double[] ShadowPriceDifference { get; set; }

		public double[] ShadowPrice { get; set; }

		public double[] ExogenousLoad { get; set; }

		public double[] ParkAndRideLoad { get; set; }

		#endregion

		#region wrapper methods

		public virtual void SetParkAndRideShadowPricing(Dictionary<int, IParkAndRideShadowPriceNode> parkAndRideShadowPrices) {
			if (parkAndRideShadowPrices == null) {
				throw new ArgumentNullException("parkAndRideShadowPrices");
			}

			if (!Global.ParkAndRideNodeIsEnabled || !Global.Configuration.ShouldUseParkAndRideShadowPricing || Global.Configuration.IsInEstimationMode) {
				return;
			}

			IParkAndRideShadowPriceNode parkAndRideShadowPriceNode;

			ShadowPriceDifference = new double[Global.Settings.Times.MinutesInADay];
			ShadowPrice = new double[Global.Settings.Times.MinutesInADay];
			ExogenousLoad = new double[Global.Settings.Times.MinutesInADay];
			ParkAndRideLoad = new double[Global.Settings.Times.MinutesInADay];

			if (!parkAndRideShadowPrices.TryGetValue(Id, out parkAndRideShadowPriceNode)) {
				return;
			}

			ShadowPriceDifference = parkAndRideShadowPrices[Id].ShadowPriceDifference;
			ShadowPrice = parkAndRideShadowPrices[Id].ShadowPrice;
			ExogenousLoad = parkAndRideShadowPrices[Id].ExogenousLoad;
			// ParkAndRideLoad = parkAndRideShadowPrices[Id].ParkAndRideLoad; {JLB 20121001 commented out this line so that initial values of load are zero for any run}
		}

		#endregion
	}
}