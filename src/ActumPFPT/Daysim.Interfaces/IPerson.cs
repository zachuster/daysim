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
	public interface IPerson : IModel
	{
		[ColumnName("hhno")]
		int HouseholdId { get; set; }

		[ColumnName("pno")]
		int Sequence { get; set; }

		[ColumnName("pptyp")]
		int PersonType { get; set; }

		[ColumnName("pagey")]
		int Age { get; set; }

		[ColumnName("pgend")]
		int Gender { get; set; }

		[ColumnName("pwtyp")]
		int WorkerType { get; set; }

		[ColumnName("pwpcl")]
		int UsualWorkParcelId { get; set; }

		[ColumnName("pwtaz")]
		int UsualWorkZoneKey { get; set; }

		[ColumnName("pwautime")]
		double AutoTimeToUsualWork { get; set; }

		[ColumnName("pwaudist")]
		double AutoDistanceToUsualWork { get; set; }

		[ColumnName("pstyp")]
		int StudentType { get; set; }

		[ColumnName("pspcl")]
		int UsualSchoolParcelId { get; set; }

		[ColumnName("pstaz")]
		int UsualSchoolZoneKey { get; set; }

		[ColumnName("psautime")]
		double AutoTimeToUsualSchool { get; set; }

		[ColumnName("psaudist")]
		double AutoDistanceToUsualSchool { get; set; }

		[ColumnName("puwmode")]
		int UsualModeToWork { get; set; }

		[ColumnName("puwarrp")]
		int UsualArrivalPeriodToWork { get; set; }

		[ColumnName("puwdepp")]
		int UsualDeparturePeriodFromWork { get; set; }

		[ColumnName("ptpass")]
		int TransitPassOwnership { get; set; }

		[ColumnName("ppaidprk")]
		int PaidParkingAtWorkplace { get; set; }

		[ColumnName("pdiary")]
		int PaperDiary { get; set; }

		[ColumnName("pproxy")]
		int ProxyResponse { get; set; }

		[ColumnName("psexpfac")]
		double ExpansionFactor { get; set; }
	}
}
