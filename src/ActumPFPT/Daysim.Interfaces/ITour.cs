// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.Attributes;

namespace Daysim.Interfaces {
	public interface ITour : IModel
	{
		[ColumnName("person_id")]
		int PersonId { get; set; }

		[ColumnName("person_day_id")]
		int PersonDayId { get; set; }

		[ColumnName("hhno")]
		int HouseholdId { get; set; }

		[ColumnName("pno")]
		int PersonSequence { get; set; }

		[ColumnName("day")]
		int Day { get; set; }

		[ColumnName("tour")]
		int Sequence { get; set; }

		[ColumnName("jtindex")]
		int JointTourSequence { get; set; }

		[ColumnName("parent")]
		int ParentTourSequence { get; set; }

		[ColumnName("subtrs")]
		int Subtours { get; set; }

		[ColumnName("pdpurp")]
		int DestinationPurpose { get; set; }

		[ColumnName("tlvorig")]
		int OriginDepartureTime { get; set; }

		[ColumnName("tardest")]
		int DestinationArrivalTime { get; set; }

		[ColumnName("tlvdest")]
		int DestinationDepartureTime { get; set; }

		[ColumnName("tarorig")]
		int OriginArrivalTime { get; set; }

		[ColumnName("toadtyp")]
		int OriginAddressType { get; set; }

		[ColumnName("tdadtyp")]
		int DestinationAddressType { get; set; }

		[ColumnName("topcl")]
		int OriginParcelId { get; set; }

		[ColumnName("totaz")]
		int OriginZoneKey { get; set; }

		[ColumnName("tdpcl")]
		int DestinationParcelId { get; set; }

		[ColumnName("tdtaz")]
		int DestinationZoneKey { get; set; }

		[ColumnName("tmodetp")]
		int Mode { get; set; }

		[ColumnName("tpathtp")]
		int PathType { get; set; }

		[ColumnName("tautotime")]
		double AutoTimeOneWay { get; set; }

		[ColumnName("tautocost")]
		double AutoCostOneWay { get; set; }

		[ColumnName("tautodist")]
		double AutoDistanceOneWay { get; set; }

		[ColumnName("tripsh1")]
		int HalfTour1Trips { get; set; }

		[ColumnName("tripsh2")]
		int HalfTour2Trips { get; set; }

		[ColumnName("phtindx1")]
		int PartialHalfTour1Sequence { get; set; }

		[ColumnName("phtindx2")]
		int PartialHalfTour2Sequence { get; set; }

		[ColumnName("fhtindx1")]
		int FullHalfTour1Sequence { get; set; }

		[ColumnName("fhtindx2")]
		int FullHalfTour2Sequence { get; set; }

		[ColumnName("toexpfac")]
		double ExpansionFactor { get; set; }
	}
}
