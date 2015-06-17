// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using Daysim.Framework.Persistence;
using Daysim.Framework.Roster;
using Daysim.Interfaces;

namespace Daysim.DomainModels {
	public sealed class CondensedParcel : ICondensedParcel {
		public int Id { get; set; }

		public int ZoneId { get; set; }

		public int Sequence { get; set; }

		public int XCoordinate { get; set; }

		public int YCoordinate { get; set; }

		public int TransimsActivityLocation { get; set; }

		public int LandUseCode19 { get; set; }

		public double Households { get; set; }

		public double StudentsK8 { get; set; }

		public double StudentsHighSchool { get; set; }

		public double StudentsK12 { get; set; }

		public double StudentsUniversity { get; set; }

		public double EmploymentEducation { get; set; }

		public double EmploymentFood { get; set; }

		public double EmploymentGovernment { get; set; }

		public double EmploymentIndustrial { get; set; }

		public double EmploymentMedical { get; set; }

		public double EmploymentOffice { get; set; }

		public double EmploymentRetail { get; set; }

		public double EmploymentService { get; set; }

		public double EmploymentAgricultureConstruction { get; set; }

		public double EmploymentTotal { get; set; }

		public double ParkingOffStreetPaidDailySpaces { get; set; }

		public double StudentsK8Buffer1 { get; set; }

		public double StudentsHighSchoolBuffer1 { get; set; }

		public double StudentsK8Buffer2 { get; set; }

		public double StudentsHighSchoolBuffer2 { get; set; }

		public double EmploymentFoodBuffer1 { get; set; }

		public double EmploymentMedicalBuffer1 { get; set; }

		public double EmploymentMedicalBuffer2 { get; set; }

		public double EmploymentRetailBuffer1 { get; set; }

		public double EmploymentServiceBuffer1 { get; set; }

		public double ParkingOffStreetPaidDailyPriceBuffer1 { get; set; }

		public double ParkingOffStreetPaidHourlyPriceBuffer1 { get; set; }

		public double ParkingOffStreetPaidDailyPriceBuffer2 { get; set; }

		public double ParkingOffStreetPaidHourlyPriceBuffer2 { get; set; }

		public double StopsTransitBuffer1 { get; set; }

		public double StopsTransitBuffer2 { get; set; }

		public double NodesSingleLinkBuffer1 { get; set; }

		public double NodesThreeLinksBuffer1 { get; set; }

		public double NodesFourLinksBuffer1 { get; set; }

		public double OpenSpaceType1Buffer1 { get; set; }

		public double OpenSpaceType2Buffer1 { get; set; }

		public double OpenSpaceType1Buffer2 { get; set; }

		public double OpenSpaceType2Buffer2 { get; set; }

		public double EmploymentFoodBuffer2 { get; set; }

		public double EmploymentRetailBuffer2 { get; set; }

		public double EmploymentServiceBuffer2 { get; set; }

		public double HouseholdsBuffer2 { get; set; }

		public double NodesSingleLinkBuffer2 { get; set; }

		public double NodesThreeLinksBuffer2 { get; set; }

		public double NodesFourLinksBuffer2 { get; set; }

		public double DistanceToLocalBus { get; set; }

		public double DistanceToLightRail { get; set; }

		public double DistanceToExpressBus { get; set; }

		public double DistanceToCommuterRail { get; set; }

		public double DistanceToFerry { get; set; }

		public double DistanceToTransit { get; set; }

		public double ShadowPriceForEmployment { get; set; }

		public double ShadowPriceForStudentsK12 { get; set; }

		public double ShadowPriceForStudentsUniversity { get; set; }

		public double ExternalEmploymentTotal { get; set; }

		public double EmploymentDifference { get; set; }

		public double EmploymentPrediction { get; set; }

		public double ExternalStudentsK12 { get; set; }

		public double StudentsK12Difference { get; set; }

		public double StudentsK12Prediction { get; set; }

		public double ExternalStudentsUniversity { get; set; }

		public double StudentsUniversityDifference { get; set; }

		public double StudentsUniversityPrediction { get; set; }

		public double ParkingOffStreetPaidDailyPrice { get; set; }

		public double ParkingOffStreetPaidHourlyPrice { get; set; }

		public double ParkingOffStreetPaidHourlySpaces { get; set; }

		public double EmploymentGovernmentBuffer1 { get; set; }

		public double EmploymentOfficeBuffer1 { get; set; }

		public double EmploymentGovernmentBuffer2 { get; set; }

		public double EmploymentOfficeBuffer2 { get; set; }

		public double EmploymentEducationBuffer1 { get; set; }

		public double EmploymentEducationBuffer2 { get; set; }

		public double EmploymentAgricultureConstructionBuffer1 { get; set; }

		public double EmploymentIndustrialBuffer1 { get; set; }

		public double EmploymentAgricultureConstructionBuffer2 { get; set; }

		public double EmploymentIndustrialBuffer2 { get; set; }

		public double EmploymentTotalBuffer1 { get; set; }

		public double EmploymentTotalBuffer2 { get; set; }

		public double HouseholdsBuffer1 { get; set; }

		public double StudentsUniversityBuffer1 { get; set; }

		public double StudentsUniversityBuffer2 { get; set; }

		public double ParkingOffStreetPaidHourlySpacesBuffer1 { get; set; }

		public double ParkingOffStreetPaidDailySpacesBuffer1 { get; set; }

		public double ParkingOffStreetPaidHourlySpacesBuffer2 { get; set; }

		public double ParkingOffStreetPaidDailySpacesBuffer2 { get; set; }

		public double CircuityRatio_E1 { get; set; }

		public double CircuityRatio_E2 { get; set; }

		public double CircuityRatio_E3 { get; set; }

		public double CircuityRatio_NE1 { get; set; }

		public double CircuityRatio_NE2 { get; set; }

		public double CircuityRatio_NE3 { get; set; }

		public double CircuityRatio_N1 { get; set; }

		public double CircuityRatio_N2 { get; set; }

		public double CircuityRatio_N3 { get; set; }

		public double CircuityRatio_NW1 { get; set; }

		public double CircuityRatio_NW2 { get; set; }

		public double CircuityRatio_NW3 { get; set; }

		public double CircuityRatio_W1 { get; set; }

		public double CircuityRatio_W2 { get; set; }

		public double CircuityRatio_W3 { get; set; }

		public double CircuityRatio_SW1 { get; set; }

		public double CircuityRatio_SW2 { get; set; }

		public double CircuityRatio_SW3 { get; set; }

		public double CircuityRatio_S1 { get; set; }

		public double CircuityRatio_S2 { get; set; }

		public double CircuityRatio_S3 { get; set; }

		public double CircuityRatio_SE1 { get; set; }

		public double CircuityRatio_SE2 { get; set; }

		public double CircuityRatio_SE3 { get; set; }

		public override string ToString() {
			return string.Format("Parcel: {0}", Id);
		}
	}
}