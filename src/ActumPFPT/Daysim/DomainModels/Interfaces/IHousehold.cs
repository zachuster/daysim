using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.Framework.Persistence;

namespace Daysim.DomainModels.Interfaces {
	public interface IHousehold : IModel
	{
		IHousehold();

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
	}
}
