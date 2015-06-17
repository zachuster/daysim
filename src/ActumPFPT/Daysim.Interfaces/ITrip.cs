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

namespace Daysim.Interfaces {
	public interface ITrip : IModel {
		[ColumnName("tour_id")]
		int TourId { get; set; }

		[ColumnName("hhno")]
		int HouseholdId { get; set; }

		[ColumnName("pno")]
		int PersonSequence { get; set; }

		[ColumnName("day")]
		int Day { get; set; }

		[ColumnName("tour")]
		int TourSequence { get; set; }

		[ColumnName("half")]
		int HalfTour { get; set; }

		[ColumnName("tseg")]
		int Sequence { get; set; }

		[ColumnName("tsvid")]
		int SurveyTripSequence { get; set; }

		[ColumnName("opurp")]
		int OriginPurpose { get; set; }

		[ColumnName("dpurp")]
		int DestinationPurpose { get; set; }

		[ColumnName("oadtyp")]
		int OriginAddressType { get; set; }

		[ColumnName("dadtyp")]
		int DestinationAddressType { get; set; }

		[ColumnName("opcl")]
		int OriginParcelId { get; set; }

		[ColumnName("otaz")]
		int OriginZoneKey { get; set; }

		[ColumnName("dpcl")]
		int DestinationParcelId { get; set; }

		[ColumnName("dtaz")]
		int DestinationZoneKey { get; set; }

		[ColumnName("mode")]
		int Mode { get; set; }

		[ColumnName("pathtype")]
		int PathType { get; set; }

		[ColumnName("dorp")]
		int DriverType { get; set; }

		[ColumnName("deptm")]
		int DepartureTime { get; set; }

		[ColumnName("arrtm")]
		int ArrivalTime { get; set; }

		[ColumnName("endacttm")]
		int ActivityEndTime { get; set; }

		[ColumnName("travtime")]
		double TravelTime { get; set; }

		[ColumnName("travcost")]
		double TravelCost { get; set; }

		[ColumnName("travdist")]
		double TravelDistance { get; set; }

		[ColumnName("vot")]
		double ValueOfTime { get; set; }

		[ColumnName("trexpfac")]
		double ExpansionFactor { get; set; }
	}
}
