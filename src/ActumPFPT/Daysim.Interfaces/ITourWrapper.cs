using System.Collections.Generic;

namespace Daysim.Interfaces
{
	public interface ITourWrapper
	{
		IHouseholdWrapper Household { get; }

		IPersonWrapper Person { get; }

		IPersonDayWrapper PersonDay { get; }

		ITourWrapper ParentTour { get; }

		List<ITourWrapper> Subtours { get; }

		IHalfTour HalfTourFromOrigin { get; }

		IHalfTour HalfTourFromDestination { get; }

		ICondensedParcel OriginParcel { get; }

		/*ITour.OriginParcel {
			get { return OriginParcel; }
		}*/

		ICondensedParcel DestinationParcel { get; set; }


		// domain model properies

		int Id { get; }

		int Sequence { get; set; }

		int TotalSubtours { get; }

		int DestinationPurpose { get; set; }

		int OriginDepartureTime { get; }

		int DestinationArrivalTime { get; set; }

		int DestinationDepartureTime { get; set; }


		int DestinationAddressType { get; set; }

		int OriginParcelId { get; }

		int DestinationParcelId { get; set; }

		int DestinationZoneKey { get; set; }

		int HalfTour1Trips { get; set; }

		int HalfTour2Trips { get; set; }

		int Mode { get; set; }

		int PathType { get; set; }

		int PartialHalfTour1Sequence { get; set; }

		int PartialHalfTour2Sequence { get; set; }

		int FullHalfTour1Sequence { get; set; }

		int FullHalfTour2Sequence { get; set; }

		int JointTourSequence { get; set; }


		// flags, choice model properties, etc.

		bool IsHomeBasedTour { get; }

		bool IsWorkPurpose { get; }

		bool IsSchoolPurpose { get; }

		bool IsEscortPurpose { get; }

		bool IsPersonalBusinessPurpose { get; }

		bool IsShoppingPurpose { get; }

		bool IsMealPurpose { get; }

		bool IsSocialPurpose { get; }

		bool IsRecreationPurpose { get; }

		bool IsMedicalPurpose { get; }

		bool IsPersonalBusinessOrMedicalPurpose { get; }

		bool IsSocialOrRecreationPurpose { get; }

		bool IsWalkMode { get; }

		bool IsBikeMode { get; }

		bool IsSovMode { get; }

		bool IsHov2Mode { get; }

		bool IsHov3Mode { get; }

		bool IsTransitMode { get; }

		bool IsParkAndRideMode { get; }

		bool IsSchoolBusMode { get; }

		bool IsWalkOrBikeMode { get; }

		bool HasSubtours { get; }

		bool IsAnHovMode { get; }

		bool IsAnAutoMode { get; }

		bool UsesTransitModes { get; }

		int TotalToursByPurpose { get; }

		int TotalSimulatedToursByPurpose { get; }

		ITimeWindow TimeWindow { get; }

		int TourPurposeSegment { get; }

		int TourCategory { get; }

		double IndicatedTravelTimeToDestination { get; set; }
		double IndicatedTravelTimeFromDestination { get; set; }

		int EarliestOrignDepartureTime { get; set; }
		int LatestOrignArrivalTime { get; set; }

		IMinuteSpan DestinationDepartureBigPeriod { get; set; }
		IMinuteSpan DestinationArrivalBigPeriod { get; set; }

		double TimeCoefficient { get; }

		double CostCoefficient { get; }

		int ParkAndRideNodeId { get; set; }

		bool DestinationModeAndTimeHaveBeenSimulated { get; set; }

		bool HalfTour1HasBeenSimulated { get; set; }

		bool HalfTour2HasBeenSimulated { get; set; }

		bool IsMissingData { get; set; }

		// wrapper methods

		void SetHomeBasedIsSimulated();

		void SetWorkBasedIsSimulated();

		void SetHalfTours(int direction);

		ITimeWindow GetRelevantTimeWindow(IHouseholdDayWrapper householdDay);

		void SetOriginTimes(int direction = 0);

		void UpdateTourValues();

		IHalfTour GetHalfTour(int direction);

		ITourModeImpedance[] GetTourModeImpedances();

		void SetParentTourSequence(int parentTourSequence);
		ITourWrapper CreateSubtour(int originAddressType, int originParcelId, int originZoneKey, int destinationPurpose);

		void SetParkAndRideStay();
		int VotALSegment { get; }
		void Export();
	}
}