// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections;
using System.Collections.Generic;
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Framework.Roster;
using Daysim.Interfaces;
using Daysim.ShadowPricing;
using Ninject;

namespace Daysim.DomainModels {
	public static class ICondensedParcelExtensions {
		private static Reader<ParcelNode> _reader;
		private static Dictionary<int, int> _nodeIndex = null; 


		public static void InitializeIndex(Reader<ParcelNode> reader = null )
		{
			if (reader == null)
				reader = Global.Kernel.Get<Reader<ParcelNode>>();
			_nodeIndex = new Dictionary<int, int>();
			foreach (var node in reader)
			{
				_nodeIndex.Add(node.Id, node.NodeId);
			}

			Global.NodeNodePreviousOriginParcelId = Constants.DEFAULT_VALUE;
			Global.NodeNodePreviousDestinationParcelId = Constants.DEFAULT_VALUE;
			Global.NodeNodePreviousDistance = Constants.DEFAULT_VALUE;
		}

		public static double NetIntersectionDensity1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.NodesFourLinksBuffer1 + parcel.NodesThreeLinksBuffer1 - parcel.NodesSingleLinkBuffer1;
		}

		public static double NetIntersectionDensity2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.NodesFourLinksBuffer2 + parcel.NodesThreeLinksBuffer2 - parcel.NodesSingleLinkBuffer2;
		}

		public static double OpenSpaceDensity1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.OpenSpaceType1Buffer1; //converted to million square feet
		}

		public static double OpenSpaceDensity2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.OpenSpaceType1Buffer2; //converted to million square feet
		}

		public static double OpenSpaceMillionSqFtBuffer1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.OpenSpaceType1Buffer1 * parcel.OpenSpaceType2Buffer1 / 1000000.0; //converted to million square feet
		}

		public static double OpenSpaceMillionSqFtBuffer2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.OpenSpaceType1Buffer2 * parcel.OpenSpaceType2Buffer2 / 1000000.0; //converted to million square feet
		}

		public static double RetailEmploymentDensity1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.EmploymentRetailBuffer1 + parcel.EmploymentFoodBuffer1;
		}

		public static double RetailEmploymentDensity2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.EmploymentRetailBuffer2 + parcel.EmploymentFoodBuffer2;
		}

		public static double ServiceEmploymentDensity1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.EmploymentServiceBuffer1 + parcel.EmploymentMedicalBuffer1;
		}

		public static double ServiceEmploymentDensity2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.EmploymentServiceBuffer2 + parcel.EmploymentMedicalBuffer2;
		}

		public static double OfficeEmploymentDensity1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.EmploymentOfficeBuffer1 + parcel.EmploymentGovernmentBuffer1;
		}

		public static double OfficeEmploymentDensity2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.EmploymentOfficeBuffer2 + parcel.EmploymentGovernmentBuffer2;
		}

		public static double TotalEmploymentDensity1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.EmploymentTotalBuffer1;
		}

		public static double TotalEmploymentDensity2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.EmploymentTotalBuffer2;
		}

		public static double StudentEnrolmentDensity1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.StudentsHighSchoolBuffer1 + parcel.StudentsUniversityBuffer1 + parcel.StudentsK8Buffer1;
		}

		public static double StudentEnrolmentDensity2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.StudentsHighSchoolBuffer2 + parcel.StudentsUniversityBuffer2 + parcel.StudentsK8Buffer2;
		}

		public static double HouseholdDensity1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.HouseholdsBuffer1;
		}

		public static double HouseholdDensity2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.HouseholdsBuffer2;
		}

		public static double MixedUse2Index1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var hh = parcel.HouseholdDensity1();
			var emp = parcel.TotalEmploymentDensity1();

			return Log2(hh, emp);
		}

		public static double MixedUse2Index2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var hh = parcel.HouseholdDensity2();
			var emp = parcel.TotalEmploymentDensity2();

			return Log2(hh, emp);
		}

		public static double MixedUse3Index2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var hh = parcel.HouseholdDensity2();
			var ret = parcel.RetailEmploymentDensity2();
			var svc = parcel.ServiceEmploymentDensity2();

			return Log3(hh, ret, svc);

		}
		
		public static double MixedUse4Index1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var hh = parcel.HouseholdDensity1();
			var ret = parcel.RetailEmploymentDensity1();
			var svc = parcel.ServiceEmploymentDensity1();
			var ofc = parcel.OfficeEmploymentDensity1();
			return Log4(hh, ret, svc, ofc);
		}

		public static double MixedUse4Index2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var hh = parcel.HouseholdDensity2();
			var ret = parcel.RetailEmploymentDensity2();
			var svc = parcel.ServiceEmploymentDensity2();
			var ofc = parcel.OfficeEmploymentDensity2();

			return Log4(hh, ret, svc, ofc);
		}

		public static int TransitAccessSegment(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return
				// JLBscal:  divided by DistanceUnitsPerMile in following four lines to convert to miles scale
				parcel.DistanceToTransit / Global.DistanceUnitsPerMile >= 0 && parcel.DistanceToTransit / Global.DistanceUnitsPerMile <= .25
					? Constants.TransitAccess.GT_0_AND_LTE_QTR_MI
					: parcel.DistanceToTransit / Global.DistanceUnitsPerMile > .25 && parcel.DistanceToTransit / Global.DistanceUnitsPerMile <= .5
					  	? Constants.TransitAccess.GT_QTR_MI_AND_LTE_H_MI
					  	: Constants.TransitAccess.NONE;
		}

		public static double ParcelParkingPerTotalEmployment(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var spaces = parcel.ParkingOffStreetPaidDailySpaces;
			var total = parcel.EmploymentTotal;

			return MaxLog(spaces, total);
		}

		public static double ParkingHourlyEmploymentCommercialMixInParcel(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var spaces = parcel.ParkingOffStreetPaidHourlySpaces;
			var emp = parcel.EmploymentFood + parcel.EmploymentRetail + parcel.EmploymentService + parcel.EmploymentMedical;

			return Log2(spaces, emp);
		}

		public static double ParkingHourlyEmploymentCommercialMixBuffer1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var spaces = parcel.ParkingOffStreetPaidHourlySpacesBuffer1;
			var emp = parcel.EmploymentFoodBuffer1 + parcel.EmploymentRetailBuffer1 + parcel.EmploymentServiceBuffer1 + parcel.EmploymentMedicalBuffer1;

			return Log2(spaces, emp);
		}

		public static double ParkingDailyEmploymentTotalMixInParcel(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var spaces = parcel.ParkingOffStreetPaidDailySpaces;
			var emp = parcel.EmploymentTotal;

			return Log2(spaces, emp);
		}

		public static double ParkingDailyEmploymentTotalMixBuffer1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var spaces = parcel.ParkingOffStreetPaidDailySpacesBuffer1;
			var emp = parcel.EmploymentTotalBuffer1;

			return Log2(spaces, emp);
		}

		public static double ParcelParkingPerFoodRetailServiceMedicalEmployment(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var spaces = parcel.ParkingOffStreetPaidDailySpaces;
			var total = parcel.EmploymentFood + parcel.EmploymentRetail + parcel.EmploymentService + parcel.EmploymentMedical;

			return MaxLog(spaces, total);
		}

		public static double ZoneParkingPerTotalEmploymentAndK12UniversityStudents(this ICondensedParcel parcel, ZoneTotals zoneTotals, double millionsSquareLengthUnits) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			if (zoneTotals == null) {
				throw new ArgumentNullException("zoneTotals");
			}

			var spaces = parcel.ParkingOffStreetPaidDailySpaces / millionsSquareLengthUnits;
			var total = (parcel.EmploymentTotal + zoneTotals.StudentsK12 + zoneTotals.StudentsUniversity) * 100 / millionsSquareLengthUnits;

			return MaxLog(spaces, total);
		}

		public static double ZoneParkingPerFoodRetailServiceMedicalEmployment(this ICondensedParcel parcel, double millionsSquareLengthUnits) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var spaces = parcel.ParkingOffStreetPaidDailySpaces / millionsSquareLengthUnits;
			var total = (parcel.EmploymentFood + parcel.EmploymentRetail + parcel.EmploymentService + parcel.EmploymentMedical) * 100 / millionsSquareLengthUnits;

			return MaxLog(spaces, total);
		}

		public static double C34RatioBuffer1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return (parcel.NodesThreeLinksBuffer1 + parcel.NodesFourLinksBuffer1) / (Math.Max(1, parcel.NodesSingleLinkBuffer1 + parcel.NodesThreeLinksBuffer1 + parcel.NodesFourLinksBuffer1));
		}

		public static double ParcelHouseholdsPerRetailServiceEmploymentBuffer2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var households = parcel.HouseholdsBuffer2 * 100;
			var total = (parcel.EmploymentRetailBuffer2 + parcel.EmploymentServiceBuffer2) * 100;

			return Math.Min(100000, .001 * households * total / (households + total + .1));
		}

		public static double ParcelHouseholdsPerRetailServiceFoodEmploymentBuffer2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var households = parcel.HouseholdsBuffer2 * 100;
			var total = (parcel.EmploymentRetailBuffer2 + parcel.EmploymentServiceBuffer2 + parcel.EmploymentFoodBuffer2) * 100;

			return households * total / (households + total + 1);
		}

		public static double IntersectionDensity34Buffer2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.NodesThreeLinksBuffer2 * .5 + parcel.NodesFourLinksBuffer2;
		}

		public static double IntersectionDensity34Minus1Buffer2(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return parcel.IntersectionDensity34Buffer2() - parcel.NodesSingleLinkBuffer2;
		}

		public static double ParkingCostBuffer1(this ICondensedParcel parcel, double parkingDuration) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			var parkingCost =
				parcel.ParkingOffStreetPaidDailyPriceBuffer1 < parkingDuration * parcel.ParkingOffStreetPaidHourlyPriceBuffer1
					? parcel.ParkingOffStreetPaidDailyPriceBuffer1
					: parkingDuration * parcel.ParkingOffStreetPaidHourlyPriceBuffer1;

			return parkingCost / 100; // convert to Monetary units from hundredths of monetary units
		}

		/*public static double DistanceToTransitCappedUnderQtrMile(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return
				// JLBscale:  adjust formula to convert from distance units to miles
				parcel.DistanceToTransit / Global.DistanceUnitsPerMile >= 0
					? Math.Min(.25, Math.Max(0, .25 - parcel.DistanceToTransit / Global.DistanceUnitsPerMile))
					: 0;
		}*/

		/*public static double DistanceToTransitQtrToHalfMile(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return
				// JLBscale:  adjust formula to convert from distance units to miles
				parcel.DistanceToTransit / Global.DistanceUnitsPerMile >= 0
					? Math.Min(.25, Math.Max(0, .5 - parcel.DistanceToTransit / Global.DistanceUnitsPerMile))
					: 0;
		}*/

		public static double FoodRetailServiceMedicalLogBuffer1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return Math.Log(1 + parcel.EmploymentFoodBuffer1 + parcel.EmploymentRetailBuffer1 + parcel.EmploymentServiceBuffer1 + parcel.EmploymentMedicalBuffer1);
		}

		public static double K8HighSchoolQtrMileLogBuffer1(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return Math.Log(1 + parcel.StudentsK8Buffer1 + parcel.StudentsHighSchoolBuffer1);
		}

		public static int UsualWorkParcelFlag(this ICondensedParcel parcel, int usualWorkParcelId) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return (parcel.Id == usualWorkParcelId).ToFlag();
		}

		public static int NotUsualWorkParcelFlag(this ICondensedParcel parcel, int usualWorkParcelId) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return (parcel.Id != usualWorkParcelId).ToFlag();
		}

		public static int RuralFlag(this ICondensedParcel parcel) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			return (parcel.TotalEmploymentDensity1() + parcel.HouseholdDensity1() < Global.Configuration.UrbanThreshold).ToFlag();
		}

		public static void SetShadowPricing(this ICondensedParcel parcel, Dictionary<int, Zone> zones, Dictionary<int, ShadowPriceParcel> shadowPrices) {
			if (parcel == null) {
				throw new ArgumentNullException("parcel");
			}

			if (zones == null) {
				throw new ArgumentNullException("zones");
			}

			if (shadowPrices == null) {
				throw new ArgumentNullException("shadowPrices");
			}

			if (!Global.Configuration.ShouldUseShadowPricing || (!Global.Configuration.ShouldRunWorkLocationModel && !Global.Configuration.ShouldRunSchoolLocationModel)) {
				return;
			}

			ShadowPriceParcel shadowPriceParcel;

			if (shadowPrices.TryGetValue(parcel.Id, out shadowPriceParcel)) {
				parcel.ShadowPriceForEmployment = shadowPrices[parcel.Id].ShadowPriceForEmployment;
				parcel.ShadowPriceForStudentsK12 = shadowPrices[parcel.Id].ShadowPriceForStudentsK12;
				parcel.ShadowPriceForStudentsUniversity = shadowPrices[parcel.Id].ShadowPriceForStudentsUniversity;
			}

			Zone zone;

			if (zones.TryGetValue(parcel.ZoneId, out zone)) {
				parcel.ExternalEmploymentTotal = parcel.EmploymentTotal * (1 - zone.FractionJobsFilledByWorkersFromOutsideRegion);
				// TODO: Missing information about external students. Zero is the placeholder for university student fraction.
				parcel.ExternalStudentsK12 = 0;
				parcel.ExternalStudentsUniversity = parcel.StudentsUniversity * (1 - 0);
			}
		}

		public static double NodeToNodeDistance(this IPoint origin, IPoint destination) {
			var o = (CondensedParcel) origin;
			var d = (CondensedParcel) destination;

			return o.NodeToNodeDistance(d);
		}

		public static double NodeToNodeDistance(this ICondensedParcel origin, ICondensedParcel destination) {
			if (origin == null) {
				throw new ArgumentNullException("origin");
			}

			if (destination == null) {
				throw new ArgumentNullException("destination");
			}

			if (origin.Id == Global.NodeNodePreviousOriginParcelId && destination.Id == Global.NodeNodePreviousDestinationParcelId) {
				return Global.NodeNodePreviousDistance;
			}
			Global.NodeNodePreviousOriginParcelId = origin.Id;
			Global.NodeNodePreviousDestinationParcelId = destination.Id;
			Global.NodeNodePreviousDistance = Constants.DEFAULT_VALUE;

			// this is a 2-stage search through a partial matrix with many millions of cells...
			// get record for aNode_Id in node index arrays

			int oNode_Id;
			int dNode_Id;
			bool foundOrigin = _nodeIndex.TryGetValue(origin.Id, out oNode_Id);
			bool foundDestination = _nodeIndex.TryGetValue(destination.Id, out dNode_Id);
				
			if ( !foundDestination || !foundOrigin) {
				return Constants.DEFAULT_VALUE;
			}


			if (oNode_Id == dNode_Id || oNode_Id <1 || dNode_Id <1 || oNode_Id > Global.ANodeId.Length || dNode_Id > Global.ANodeId.Length) {
				return Constants.DEFAULT_VALUE;
			}
			// symmetry assumed - used smaller node # as aNode
			int aNode_Id = Math.Min(oNode_Id,dNode_Id);
			int bNode_Id = Math.Max(oNode_Id,dNode_Id);
		
			// no longer necessary - nodeIds are already indexed
			//int index = -1;
			//do {
			//	index++;
			//} while (Global.ANodeId[index] != aNode_Id && index < Global.ANodeId.Length-1);
			//
			//if (Global.ANodeId[index] != aNode_Id) { 
			//	return Constants.DEFAULT_VALUE; //a node not in file
			//}
			
			int firstRecord = Global.ANodeFirstRecord[aNode_Id-1];
			int lastRecord  = Global.ANodeLastRecord[aNode_Id-1];

			if (firstRecord <= 0 || lastRecord <= 0) {
				return Constants.DEFAULT_VALUE; //there are no b nodes for a node			
			}

			// binary search for bnode_Id in relevant records in node-node distance arrays
			var minIndex = firstRecord - 1;
			var maxIndex = lastRecord - 1;

			int index = 0;
			int bNodeComp = 0;
			do {
				index = (maxIndex + minIndex) / 2;
				bNodeComp = Global.NodePairBNodeId[index]; 
				
				if (bNodeComp < bNode_Id) {
					minIndex = index + 1;
				} 
				else if (bNodeComp > bNode_Id) {
					maxIndex = index - 1;
				}
			} while (bNodeComp != bNode_Id && maxIndex >= minIndex) ;

			if (bNodeComp != bNode_Id) {
				return Constants.DEFAULT_VALUE; //there are no b nodes for a node			
			}
			else {
				var distance = Global.NodePairDistance[index] / 5280.0; // convert feet to miles
				Global.NodeNodePreviousDistance = distance;
				return distance;
			}
		}

		public static double CircuityDistance(this IPoint origin, IPoint destination) {
			var o = (CondensedParcel) origin;
			var d = (CondensedParcel) destination;

			return o.CircuityDistance(d);
		}

		public static double CircuityDistance(this ICondensedParcel origin, ICondensedParcel destination) {
			if (origin == null) {
				throw new ArgumentNullException("origin");
			}

			if (destination == null) {
				throw new ArgumentNullException("destination");
			}

			// JLBscale:  change so calculations work in length units instead of ft.
			double maxCircLength = 10560.0 * Global.LengthUnitsPerFoot ; // only apply circuity multiplier out to 2 miles = 10560 feet
			double lengthLimit1 = 2640.0 * Global.LengthUnitsPerFoot ; // circuity distance 1 = 1/2 mile
			double lengthLimit2 = 5280.0 * Global.LengthUnitsPerFoot ; // circuity distance 2 = 1 mile
			double lengthLimit3 = 7920.0 * Global.LengthUnitsPerFoot ; // circuity distance 3 = 1 1/2 mile
			const double defaultCircuity = 1.4;

			var ox = origin.XCoordinate;
			var oy = origin.YCoordinate;
			var dx = destination.XCoordinate;
			var dy = destination.YCoordinate;

			double circuityRatio;
			var dWeight1 = 0.0;
			var dWeight2 = 0.0;
			var dWeight3 = 0.0;

			double xDiff = Math.Abs(dx - ox);
			double yDiff = Math.Abs(dy - oy);
			var xyLength = Math.Sqrt(xDiff * xDiff + yDiff * yDiff);

			// JLBscale.  rescale from miles to distance units
			if  (((xyLength / Global.LengthUnitsPerFoot) / 5280D) * Global.DistanceUnitsPerMile > Global.Configuration.MaximumBlendingDistance) {  
				return ((xyLength / Global.LengthUnitsPerFoot) / 5280D) * Global.DistanceUnitsPerMile * defaultCircuity;  
			}

			if (xyLength < lengthLimit1) {
				dWeight1 = 1.0;
			}
			else if (xyLength < lengthLimit2) {
				dWeight2 = (xyLength - lengthLimit1) / (lengthLimit2 - lengthLimit1);
				dWeight1 = 1.0 - dWeight2;
			}
			else if (xyLength < lengthLimit3) {
				dWeight3 = (xyLength - lengthLimit2) / (lengthLimit3 - lengthLimit2);
				dWeight2 = 1.0 - dWeight3;
			}
			else {
				dWeight3 = 1.0;
			}

// Octant  dx-ox  dy-oy  Xdiff vs. Ydiff
//1  E-NE   pos    pos     greater
//2  N-NE   pos    pos     less
//3  N-NW   neg    pos     less
//4  W-NW   neg    pos     greater
//5  W-SW   neg    neg     greater
//6  S-SW   neg    neg     leYass
//7  S-SE   pos    neg     less
//8  E-SE   pos    neg     greater}

			if (xDiff == 0 && yDiff == 0) {
				// same point
				circuityRatio = 1.0;
			}
			else if (yDiff == 0) {
				// due E or W
				if (dx > ox) {
					// due E
					circuityRatio =
						dWeight1 * origin.CircuityRatio_E1 +
						dWeight2 * origin.CircuityRatio_E2 +
						dWeight3 * origin.CircuityRatio_E3;
				}
				else {
					// due W
					circuityRatio =
						dWeight1 * origin.CircuityRatio_W1 +
						dWeight2 * origin.CircuityRatio_W2 +
						dWeight3 * origin.CircuityRatio_W3;
				}
			}
			else if (xDiff == 0) {
				// due N or S
				if (dy > oy) {
					// due N
					circuityRatio =
						dWeight1 * origin.CircuityRatio_N1 +
						dWeight2 * origin.CircuityRatio_N2 +
						dWeight3 * origin.CircuityRatio_N3;
				}
				else {
					// due S
					circuityRatio =
						dWeight1 * origin.CircuityRatio_S1 +
						dWeight2 * origin.CircuityRatio_S2 +
						dWeight3 * origin.CircuityRatio_S3;
				}
			}
			else if (dy > oy) {
				// towards N
				if (dx > ox) {
					// NE quadrant
					if (xDiff > yDiff) {
						// E-NE
						var odAngle = yDiff / xDiff;
						circuityRatio =
							dWeight1 * (odAngle * origin.CircuityRatio_NE1 + (1 - odAngle) * origin.CircuityRatio_E1) +
							dWeight2 * (odAngle * origin.CircuityRatio_NE2 + (1 - odAngle) * origin.CircuityRatio_E2) +
							dWeight3 * (odAngle * origin.CircuityRatio_NE3 + (1 - odAngle) * origin.CircuityRatio_E3);
					}
					else {
						// N-NE
						var odAngle = xDiff / yDiff;
						circuityRatio =
							dWeight1 * (odAngle * origin.CircuityRatio_NE1 + (1 - odAngle) * origin.CircuityRatio_N1) +
							dWeight2 * (odAngle * origin.CircuityRatio_NE2 + (1 - odAngle) * origin.CircuityRatio_N2) +
							dWeight3 * (odAngle * origin.CircuityRatio_NE3 + (1 - odAngle) * origin.CircuityRatio_N3);
					}
				}
				else {
					if (xDiff < yDiff) {
						// N-NW
						var odAngle = xDiff / yDiff;
						circuityRatio =
							dWeight1 * (odAngle * origin.CircuityRatio_NW1 + (1 - odAngle) * origin.CircuityRatio_N1) +
							dWeight2 * (odAngle * origin.CircuityRatio_NW2 + (1 - odAngle) * origin.CircuityRatio_N2) +
							dWeight3 * (odAngle * origin.CircuityRatio_NW3 + (1 - odAngle) * origin.CircuityRatio_N3);
					}
					else {
						// W-NW
						var odAngle = yDiff / xDiff;
						circuityRatio =
							dWeight1 * (odAngle * origin.CircuityRatio_NW1 + (1 - odAngle) * origin.CircuityRatio_W1) +
							dWeight2 * (odAngle * origin.CircuityRatio_NW2 + (1 - odAngle) * origin.CircuityRatio_W2) +
							dWeight3 * (odAngle * origin.CircuityRatio_NW3 + (1 - odAngle) * origin.CircuityRatio_W3);
					}
				}
			}
			else {
				// toward South
				if (dx < ox) {
					// SW quadrant
					if (xDiff > yDiff) {
						// W-SW
						var odAngle = yDiff / xDiff;
						circuityRatio =
							dWeight1 * (odAngle * origin.CircuityRatio_SW1 + (1 - odAngle) * origin.CircuityRatio_W1) +
							dWeight2 * (odAngle * origin.CircuityRatio_SW2 + (1 - odAngle) * origin.CircuityRatio_W2) +
							dWeight3 * (odAngle * origin.CircuityRatio_SW3 + (1 - odAngle) * origin.CircuityRatio_W3);
					}
					else {
						// S-SW
						var odAngle = xDiff / yDiff;
						circuityRatio =
							dWeight1 * (odAngle * origin.CircuityRatio_SW1 + (1 - odAngle) * origin.CircuityRatio_S1) +
							dWeight2 * (odAngle * origin.CircuityRatio_SW2 + (1 - odAngle) * origin.CircuityRatio_S2) +
							dWeight3 * (odAngle * origin.CircuityRatio_SW3 + (1 - odAngle) * origin.CircuityRatio_S3);
					}
				}
				else {
					if (xDiff < yDiff) {
						// S-SE
						var odAngle = xDiff / yDiff;
						circuityRatio =
							dWeight1 * (odAngle * origin.CircuityRatio_SE1 + (1 - odAngle) * origin.CircuityRatio_S1) +
							dWeight2 * (odAngle * origin.CircuityRatio_SE2 + (1 - odAngle) * origin.CircuityRatio_S2) +
							dWeight3 * (odAngle * origin.CircuityRatio_SE3 + (1 - odAngle) * origin.CircuityRatio_S3);
					}
					else {
						// E-SE
						var odAngle = yDiff / xDiff;
						circuityRatio =
							dWeight1 * (odAngle * origin.CircuityRatio_SE1 + (1 - odAngle) * origin.CircuityRatio_E1) +
							dWeight2 * (odAngle * origin.CircuityRatio_SE2 + (1 - odAngle) * origin.CircuityRatio_E2) +
							dWeight3 * (odAngle * origin.CircuityRatio_SE3 + (1 - odAngle) * origin.CircuityRatio_E3);
					}
				}
			}

			if (xyLength < maxCircLength) {
				// JLBscale.  rescale from miles to distance units
				// return (xyLength * circuityRatio) / 5280D;
				return ((xyLength * circuityRatio / Global.LengthUnitsPerFoot) / 5280D) * Global.DistanceUnitsPerMile;
			}

			// default adjustment applied to portion of distance over maxCircDist
			// return (maxCircLength * circuityRatio + (xyLength - maxCircLength) * defaultCircuity) / 5280D;
			return (((maxCircLength * circuityRatio + (xyLength - maxCircLength) * defaultCircuity) / Global.LengthUnitsPerFoot) / 5280D) * Global.DistanceUnitsPerMile ;
		}

		public static IList<ParcelNode> Seek(int id, string parcelFk)
		{
				if (_reader == null)
				{
					_reader = Global.Kernel.Get<Reader<ParcelNode>>();
				}
				
				return _reader.Seek(id, parcelFk);
		}

		private static double Log2(double var1, double var2) 
		{
			if (var1 < Constants.EPSILON || var2 < Constants.EPSILON) {
				return 0.0;
			}

			var total = var1 + var2;

			return -1.0 * (var1 / total * Math.Log(var1 / total)
			               + var2 / total * Math.Log(var2 / total)) / Math.Log(2.0);
		}

		
		private static double Log3(double var1, double var2, double var3) 
		{
			if (var1 < Constants.EPSILON || var2 < Constants.EPSILON || var3 < Constants.EPSILON) {
				return 0.0;
			}

			var total = var1 + var2 + var3;

			return -1.0 * (var1 / total * Math.Log(var1 / total)
			               + var2 / total * Math.Log(var2 / total)
			               + var3 / total * Math.Log(var3 / total)) / Math.Log(4.0);
		}

		private static double Log4(double var1, double var2, double var3, double var4)
		{
			
			if (var1 < Constants.EPSILON || var2 < Constants.EPSILON || var3 < Constants.EPSILON || var4 < Constants.EPSILON) {
				return 0.0;
			}

			var total = var1 + var2 + var3 + var4;

			return -1.0 * (var1 / total * Math.Log(var1 / total)
			               + var2 / total * Math.Log(var2 / total)
			               + var3 / total * Math.Log(var3 / total)
			               + var4 / total * Math.Log(var4 / total)) / Math.Log(4.0);
		}

		private static double MaxLog(double spaces, double total) 
		{
			return Math.Log(1 + spaces * total / Math.Max(.001, spaces + total));
		}

		
	}
}