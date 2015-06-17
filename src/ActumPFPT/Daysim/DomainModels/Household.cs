// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System.Runtime.InteropServices;
using Daysim.Attributes;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;

namespace Daysim.DomainModels {
	[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
	public sealed class Household : IHousehold {
		[ColumnName("hhno")]
		public int Id { get; set; }

		[ColumnName("fraction_with_jobs_outside")]
		public double FractionWorkersWithJobsOutsideRegion { get; set; }

		[ColumnName("hhsize")]
		public int Size { get; set; }

		[ColumnName("hhvehs")]
		public int VehiclesAvailable { get; set; }

		[ColumnName("hhwkrs")]
		public int Workers { get; set; }

		[ColumnName("hhftw")]
		public int FulltimeWorkers { get; set; }

		[ColumnName("hhptw")]
		public int PartTimeWorkers { get; set; }

		[ColumnName("hhret")]
		public int RetiredAdults { get; set; }

		[ColumnName("hhoad")]
		public int OtherAdults { get; set; }

		[ColumnName("hhuni")]
		public int CollegeStudents { get; set; }

		[ColumnName("hhhsc")]
		public int HighSchoolStudents { get; set; }

		[ColumnName("hh515")]
		public int KidsBetween5And15 { get; set; }

		[ColumnName("hhcu5")]
		public int KidsBetween0And4 { get; set; }

		[ColumnName("hhincome")]
		public int Income { get; set; }

		[ColumnName("hownrent")]
		public int OwnOrRent { get; set; }

		[ColumnName("hrestype")]
		public int ResidenceType { get; set; }

		[ColumnName("hhparcel")]
		public int ResidenceParcelId { get; set; }

		[ColumnName("zone_id")]
		public int ResidenceZoneId { get; set; }

		[ColumnName("hhtaz")]
		public int ResidenceZoneKey { get; set; }

		[ColumnName("hhexpfac")]
		public double ExpansionFactor { get; set; }

		[ColumnName("samptype")]
		public int SampleType { get; set; }
		
		//public int Random { get; set; }
	}

	public sealed class HouseholdTotals : IHouseholdTotals {
		public int FulltimeWorkers { get; set; }

		public int PartTimeWorkers { get; set; }

		public int RetiredAdults { get; set; }

		public int NonworkingAdults { get; set; }

		public int UniversityStudents { get; set; }

		public int DrivingAgeStudents { get; set; }

		public int ChildrenAge5Through15 { get; set; }

		public int ChildrenUnder5 { get; set; }

		public int ChildrenUnder16 { get; set; }

		public int Adults { get; set; }

		public int DrivingAgeMembers { get; set; }

		public int WorkersPlusStudents { get; set; }

		public int FullAndPartTimeWorkers { get; set; }

		public int AllWorkers { get; set; }

		public int AllStudents { get; set; }

		public double PartTimeWorkersPerDrivingAgeMembers { get; set; }

		public double RetiredAdultsPerDrivingAgeMembers { get; set; }

		public double UniversityStudentsPerDrivingAgeMembers { get; set; }

		public double DrivingAgeStudentsPerDrivingAgeMembers { get; set; }

		public double ChildrenUnder5PerDrivingAgeMembers { get; set; }

		public double HomeBasedPersonsPerDrivingAgeMembers { get; set; }

	}
}