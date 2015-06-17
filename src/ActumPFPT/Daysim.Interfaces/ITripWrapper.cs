using System.Collections.Generic;

namespace Daysim.Interfaces
{
	public interface ITripWrapper
	{
		 IHouseholdWrapper Household { get; }

		IPersonWrapper Person { get; }

		IPersonDayWrapper PersonDay { get; }

		ITourWrapper Tour { get; }

		IHalfTour HalfTour { get; }

		ICondensedParcel OriginParcel { get; set; }

		ICondensedParcel DestinationParcel { get; set; }


		int Id { get; }

		int Day { get; }

		int Direction { get; }

		int Sequence { get; }

		int DestinationPurpose { get; set; }

		int OriginPurpose { get; set; }

		int DestinationAddressType { get; set; }

		int OriginParcelId { get; set; }

		int OriginZoneKey { get; set; }

		int DestinationParcelId { get; set; }

		int DestinationZoneKey { get; set; }

		int Mode { get; set; }

		int DriverType { get; set; }

		int PathType { get; set; }

		int DepartureTime { get; set; }

		int ArrivalTime { get; }

		int ActivityEndTime { get; }

		int EarliestDepartureTime { get; set; }
		int LatestDepartureTime { get; set; }
		int ArrivalTimeLimit { get; set; }

		double ValueOfTime { get; }

		// flags, choice model properties, etc.

		bool IsHalfTourFromOrigin { get; }

		bool IsToTourOrigin { get; set; }

		bool IsNoneOrHomePurposeByOrigin { get; }

		bool IsWorkPurposeByOrigin { get; }

		bool IsEscortPurposeByOrigin { get; }

		bool IsNoneOrHomePurposeByDestination { get; }

		bool IsWorkPurposeByDestination { get; }

		bool IsEscortPurposeByDestination { get; }

		bool IsWorkDestinationPurpose { get; }

		bool IsSchoolDestinationPurpose { get; }

		bool IsEscortDestinationPurpose { get; }

		bool IsPersonalBusinessDestinationPurpose { get; }

		bool IsShoppingDestinationPurpose { get; }

		bool IsMealDestinationPurpose { get; }

		bool IsSocialDestinationPurpose { get; }

		bool IsRecreationDestinationPurpose { get; }

		bool IsMedicalDestinationPurpose { get; }

		bool IsPersonalBusinessOrMedicalDestinationPurpose { get; }

		bool IsWorkOrSchoolDestinationPurpose { get; }

		bool IsPersonalReasonsDestinationPurpose { get; }

		bool IsSchoolOriginPurpose { get; }

		bool IsEscortOriginPurpose { get; }

		bool IsShoppingOriginPurpose { get; }

		bool IsPersonalBusinessOriginPurpose { get; }

		bool IsMealOriginPurpose { get; }

		bool IsSocialOriginPurpose { get; }

		bool UsesSovOrHovModes { get; }

		bool IsTransitMode { get; }

		bool IsBeforeMandatoryDestination { get; }

		ITripWrapper PreviousTrip { get; }

		ITripWrapper NextTrip { get; }

		int StartTime { get; }

		bool IsMissingData { get; set; }

		void SetDriverOrPassenger(List<ITripWrapper> trips);

		void UpdateTripValues();

		void HUpdateTripValues();

		void Invert(int sequence);

		ITripModeImpedance[] GetTripModeImpedances();

		void SetActivityEndTime(int activityEndTime);

		void SetOriginAddressType(int originAddressType);

		void SetTourSequence(int tourSequence);

		void SetTripValueOfTime();

		// utility/export methods

		void Export();
	}
}