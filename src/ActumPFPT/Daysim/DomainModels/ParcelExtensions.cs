// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using Daysim.Framework.Core;

namespace Daysim.DomainModels {
	public static class ParcelExtensions {
		public static double GetDistanceToTransit(this Parcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			double distance = Constants.DEFAULT_VALUE;

			if (parcel.DistanceToFerry >= 0) {
				distance = parcel.DistanceToFerry;
			}

			if (parcel.DistanceToCommuterRail >= 0 && (distance < 0 || parcel.DistanceToCommuterRail < distance)) {
				distance = parcel.DistanceToCommuterRail;
			}

			if (parcel.DistanceToLightRail >= 0 && (distance < 0 || parcel.DistanceToLightRail < distance)) {
				distance = parcel.DistanceToLightRail;
			}

			if (parcel.DistanceToExpressBus >= 0 && (distance < 0 || parcel.DistanceToExpressBus < distance)) {
				distance = parcel.DistanceToExpressBus;
			}

			if (parcel.DistanceToLocalBus >= 0 && (distance < 0 || parcel.DistanceToLocalBus < distance)) {
				distance = parcel.DistanceToLocalBus;
			}

			return distance;
		}

		public static CondensedParcel GetCondensedParcel(this Parcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return new CondensedParcel {
				Id = parcel.Id,
				Sequence = parcel.Sequence,
				ZoneId = parcel.ZoneId,
				XCoordinate = parcel.XCoordinate,
				YCoordinate = parcel.YCoordinate,
				TransimsActivityLocation = parcel.LandUseCode,
				LandUseCode19 = parcel.LandUseCode == 19 ? 1 : 0,
				Households = parcel.Households,
				StudentsK8 = parcel.StudentsK8,
				StudentsHighSchool = parcel.StudentsHighSchool,
				StudentsK12 = parcel.StudentsK8 + parcel.StudentsHighSchool,
				StudentsUniversity = parcel.StudentsUniversity,
				EmploymentEducation = parcel.EmploymentEducation,
				EmploymentFood = parcel.EmploymentFood,
				EmploymentGovernment = parcel.EmploymentGovernment,
				EmploymentIndustrial = parcel.EmploymentIndustrial,
				EmploymentMedical = parcel.EmploymentMedical,
				EmploymentOffice = parcel.EmploymentOffice,
				EmploymentRetail = parcel.EmploymentRetail,
				EmploymentService = parcel.EmploymentService,
				EmploymentAgricultureConstruction = parcel.EmploymentAgricultureConstruction,
				EmploymentTotal = parcel.EmploymentTotal,
				ParkingOffStreetPaidHourlyPrice = parcel.ParkingOffStreetPaidHourlyPrice,
				ParkingOffStreetPaidDailyPrice = parcel.ParkingOffStreetPaidDailyPrice,
				ParkingOffStreetPaidDailySpaces = parcel.ParkingOffStreetPaidDailySpaces,
				ParkingOffStreetPaidHourlySpaces = parcel.ParkingOffStreetPaidHourlySpaces,
				HouseholdsBuffer1 = parcel.HouseholdsBuffer1,
				StudentsK8Buffer1 = parcel.StudentsK8Buffer1,
				StudentsHighSchoolBuffer1 = parcel.StudentsHighSchoolBuffer1,
				StudentsUniversityBuffer1 = parcel.StudentsUniversityBuffer1,
				EmploymentEducationBuffer1 = parcel.EmploymentEducationBuffer1,
				EmploymentFoodBuffer1 = parcel.EmploymentFoodBuffer1,
				EmploymentGovernmentBuffer1 = parcel.EmploymentGovernmentBuffer1,
				EmploymentIndustrialBuffer1 = parcel.EmploymentIndustrialBuffer1,
				EmploymentOfficeBuffer1 = parcel.EmploymentOfficeBuffer1,
				EmploymentMedicalBuffer1 = parcel.EmploymentMedicalBuffer1,
				EmploymentRetailBuffer1 = parcel.EmploymentRetailBuffer1,
				EmploymentServiceBuffer1 = parcel.EmploymentServiceBuffer1,
				EmploymentAgricultureConstructionBuffer1 = parcel.EmploymentAgricultureConstructionBuffer1,
				EmploymentTotalBuffer1 = parcel.EmploymentTotalBuffer1,
				ParkingOffStreetPaidHourlyPriceBuffer1 = parcel.ParkingOffStreetPaidHourlyPriceBuffer1,
				ParkingOffStreetPaidDailyPriceBuffer1 = parcel.ParkingOffStreetPaidDailyPriceBuffer1,
				ParkingOffStreetPaidDailySpacesBuffer1 = parcel.ParkingOffStreetPaidDailySpacesBuffer1,
				ParkingOffStreetPaidHourlySpacesBuffer1 = parcel.ParkingOffStreetPaidHourlySpacesBuffer1,
				NodesFourLinksBuffer1 = parcel.NodesFourLinksBuffer1,
				NodesSingleLinkBuffer1 = parcel.NodesSingleLinkBuffer1,
				NodesThreeLinksBuffer1 = parcel.NodesThreeLinksBuffer1,
				OpenSpaceType1Buffer1 = parcel.OpenSpaceType1Buffer1,
				OpenSpaceType2Buffer1 = parcel.OpenSpaceType2Buffer1,
				StopsTransitBuffer1 = parcel.StopsTransitBuffer1,
				HouseholdsBuffer2 = parcel.HouseholdsBuffer2,
				StudentsK8Buffer2 = parcel.StudentsK8Buffer2,
				StudentsHighSchoolBuffer2 = parcel.StudentsHighSchoolBuffer2,
				StudentsUniversityBuffer2 = parcel.StudentsUniversityBuffer2,
				EmploymentEducationBuffer2 = parcel.EmploymentEducationBuffer2,
				EmploymentFoodBuffer2 = parcel.EmploymentFoodBuffer2,
				EmploymentGovernmentBuffer2 = parcel.EmploymentGovernmentBuffer2,
				EmploymentIndustrialBuffer2 = parcel.EmploymentIndustrialBuffer2,
				EmploymentOfficeBuffer2 = parcel.EmploymentOfficeBuffer2,
				EmploymentMedicalBuffer2 = parcel.EmploymentMedicalBuffer2,
				EmploymentRetailBuffer2 = parcel.EmploymentRetailBuffer2,
				EmploymentServiceBuffer2 = parcel.EmploymentServiceBuffer2,
				EmploymentAgricultureConstructionBuffer2 = parcel.EmploymentAgricultureConstructionBuffer2,
				EmploymentTotalBuffer2 = parcel.EmploymentTotalBuffer2,
				ParkingOffStreetPaidHourlyPriceBuffer2 = parcel.ParkingOffStreetPaidHourlyPriceBuffer2,
				ParkingOffStreetPaidDailyPriceBuffer2 = parcel.ParkingOffStreetPaidDailyPriceBuffer2,
				ParkingOffStreetPaidDailySpacesBuffer2 = parcel.ParkingOffStreetPaidDailySpacesBuffer2,
				ParkingOffStreetPaidHourlySpacesBuffer2 = parcel.ParkingOffStreetPaidHourlySpacesBuffer2,
				NodesFourLinksBuffer2 = parcel.NodesFourLinksBuffer2,
				NodesSingleLinkBuffer2 = parcel.NodesSingleLinkBuffer2,
				NodesThreeLinksBuffer2 = parcel.NodesThreeLinksBuffer2,
				OpenSpaceType1Buffer2 = parcel.OpenSpaceType1Buffer2,
				OpenSpaceType2Buffer2 = parcel.OpenSpaceType2Buffer2,
				StopsTransitBuffer2 = parcel.StopsTransitBuffer2,
				DistanceToLocalBus = parcel.DistanceToLocalBus,
				DistanceToLightRail = parcel.DistanceToLightRail,
				DistanceToExpressBus = parcel.DistanceToExpressBus,
				DistanceToCommuterRail = parcel.DistanceToCommuterRail,
				DistanceToFerry = parcel.DistanceToFerry,
				DistanceToTransit = parcel.GetDistanceToTransit(),
				CircuityRatio_E1 = parcel.CircuityRatio_E1,
				CircuityRatio_E2 = parcel.CircuityRatio_E2,
				CircuityRatio_E3 = parcel.CircuityRatio_E3,
				CircuityRatio_NE1 = parcel.CircuityRatio_NE1,
				CircuityRatio_NE2 = parcel.CircuityRatio_NE2,
				CircuityRatio_NE3 = parcel.CircuityRatio_NE3,
				CircuityRatio_N1 = parcel.CircuityRatio_N1,
				CircuityRatio_N2 = parcel.CircuityRatio_N2,
				CircuityRatio_N3 = parcel.CircuityRatio_N3,
				CircuityRatio_NW1 = parcel.CircuityRatio_NW1,
				CircuityRatio_NW2 = parcel.CircuityRatio_NW2,
				CircuityRatio_NW3 = parcel.CircuityRatio_NW3,
				CircuityRatio_W1 = parcel.CircuityRatio_W1,
				CircuityRatio_W2 = parcel.CircuityRatio_W2,
				CircuityRatio_W3 = parcel.CircuityRatio_W3,
				CircuityRatio_SW1 = parcel.CircuityRatio_SW1,
				CircuityRatio_SW2 = parcel.CircuityRatio_SW2,
				CircuityRatio_SW3 = parcel.CircuityRatio_SW3,
				CircuityRatio_S1 = parcel.CircuityRatio_S1,
				CircuityRatio_S2 = parcel.CircuityRatio_S2,
				CircuityRatio_S3 = parcel.CircuityRatio_S3,
				CircuityRatio_SE1 = parcel.CircuityRatio_SE1,
				CircuityRatio_SE2 = parcel.CircuityRatio_SE2,
				CircuityRatio_SE3 = parcel.CircuityRatio_SE3,
			};
		}
	}
}