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
	public interface IHousehold : IModel 
	{

		[ColumnName("fraction_with_jobs_outside")]
		double FractionWorkersWithJobsOutsideRegion { get; set; }

		[ColumnName("hhsize")]
		int Size { get; set; }

		[ColumnName("hhvehs")]
		int VehiclesAvailable { get; set; }

		[ColumnName("hhwkrs")]
		int Workers { get; set; }

		[ColumnName("hhftw")]
		int FulltimeWorkers { get; set; }

		[ColumnName("hhptw")]
		int PartTimeWorkers { get; set; }

		[ColumnName("hhret")]
		int RetiredAdults { get; set; }

		[ColumnName("hhoad")]
		int OtherAdults { get; set; }

		[ColumnName("hhuni")]
		int CollegeStudents { get; set; }

		[ColumnName("hhhsc")]
		int HighSchoolStudents { get; set; }

		[ColumnName("hh515")]
		int KidsBetween5And15 { get; set; }

		[ColumnName("hhcu5")]
		int KidsBetween0And4 { get; set; }

		[ColumnName("hhincome")]
		int Income { get; set; }

		[ColumnName("hownrent")]
		int OwnOrRent { get; set; }

		[ColumnName("hrestype")]
		int ResidenceType { get; set; }

		[ColumnName("hhparcel")]
		int ResidenceParcelId { get; set; }

		[ColumnName("zone_id")]
		int ResidenceZoneId { get; set; }

		[ColumnName("hhtaz")]
		int ResidenceZoneKey { get; set; }

		[ColumnName("hhexpfac")]
		double ExpansionFactor { get; set; }

		[ColumnName("samptype")]
		int SampleType { get; set; }

		//int Random { get; set; }
	}
}
