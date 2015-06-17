// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Daysim.Attributes;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;

namespace Daysim.DomainModels.Actum {
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	public sealed class ActumTrip : ITrip {
		[ColumnName("id")]
		public int Id { get; set; }

		[ColumnName("tour_id")]
		public int TourId { get; set; }

		[ColumnName("hhno")]
		public int HouseholdId { get; set; }

		[ColumnName("pno")]
		public int PersonSequence { get; set; }

		[ColumnName("day")]
		public int Day { get; set; }

		[ColumnName("tour")]
		public int TourSequence { get; set; }

		[ColumnName("half")]
		public int HalfTour { get; set; }

		[ColumnName("tseg")]
		public int Sequence { get; set; }

		[ColumnName("tsvid")]
		public int SurveyTripSequence { get; set; }

		[ColumnName("opurp")]
		public int OriginPurpose { get; set; }

		[ColumnName("dpurp")]
		public int DestinationPurpose { get; set; }

		[ColumnName("oadtyp")]
		public int OriginAddressType { get; set; }

		[ColumnName("dadtyp")]
		public int DestinationAddressType { get; set; }

		[ColumnName("opcl")]
		public int OriginParcelId { get; set; }

		[ColumnName("otaz")]
		public int OriginZoneKey { get; set; }

		[ColumnName("dpcl")]
		public int DestinationParcelId { get; set; }

		[ColumnName("dtaz")]
		public int DestinationZoneKey { get; set; }

		[ColumnName("mode")]
		public int Mode { get; set; }

		[ColumnName("pathtype")]
		public int PathType { get; set; }

		[ColumnName("dorp")]
		public int DriverType { get; set; }

		[ColumnName("deptm")]
		public int DepartureTime { get; set; }

		[ColumnName("arrtm")]
		public int ArrivalTime { get; set; }

		[ColumnName("endacttm")]
		public int ActivityEndTime { get; set; }

		[ColumnName("travtime")]
		public double TravelTime { get; set; }

		[ColumnName("travcost")]
		public double TravelCost { get; set; }

		[ColumnName("travdist")]
		public double TravelDistance { get; set; }

		[ColumnName("vot")]
		public double ValueOfTime { get; set; }

		[ColumnName("trexpfac")]
		public double ExpansionFactor { get; set; }

		[ColumnName("tdescpur")]
		public int EscortedDestinationPurpose { get; set; }

		[ColumnName("tptbicty")]
		public int BikePTCombination { get; set; }
		


	}
}
