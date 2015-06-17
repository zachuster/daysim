using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface IPersonDayWrapper 
	{
		IHouseholdWrapper Household { get; }

		IHouseholdDayWrapper HouseholdDay { get; }

		IPersonWrapper Person { get; }

		List<ITourWrapper> Tours { get; }


		// domain model properies

		int Id { get; }

		int Day { get; }

		int HomeBasedTours { get; set; }

		int WorkBasedTours { get; set; }

		int UsualWorkplaceTours { get; set; }

		int WorkTours { get; set; }

		int SchoolTours { get; set; }

		int EscortTours { get; set; }

		int PersonalBusinessTours { get; set; }

		int ShoppingTours { get; set; }

		int MealTours { get; set; }

		int SocialTours { get; set; }

		int RecreationTours { get; set; }

		int MedicalTours { get; set; }

		int WorkStops { get; set; }

		int SchoolStops { get; set; }

		int EscortStops { get; set; }

		int PersonalBusinessStops { get; set; }

		int ShoppingStops { get; set; }

		int MealStops { get; set; }

		int SocialStops { get; set; }

		int RecreationStops { get; set; }

		int MedicalStops { get; set; }

		int WorkAtHomeDuration { get; set; }

		double ExpansionFactor { get; set; }


		int JointTourParticipationPriority { get; }

		int JointHalfTourParticipationPriority { get; }
		
		// flags, choice model properties, etc.

		int TotalTours { get; }

		int TotalToursExcludingWorkAndSchool { get; }

		int CreatedWorkTours { get; set; }

		int CreatedSchoolTours { get; set; }

		int CreatedEscortTours { get; set; }

		int CreatedPersonalBusinessTours { get; set; }

		int CreatedShoppingTours { get; set; }

		int CreatedMealTours { get; set; }

		int CreatedSocialTours { get; set; }

		int CreatedRecreationTours { get; set; }

		int CreatedMedicalTours { get; set; }

		int CreatedWorkBasedTours { get; set; }

		int CreatedNonMandatoryTours { get; }
		int TotalCreatedTours { get; }
		
		int SimulatedHomeBasedTours { get; }

		int SimulatedWorkTours { get; }

		int SimulatedSchoolTours { get; }

		int SimulatedEscortTours { get; }

		int SimulatedPersonalBusinessTours { get; }

		int SimulatedShoppingTours { get; }

		int SimulatedMealTours { get; }

		int SimulatedSocialTours { get; }

		int SimulatedRecreationTours { get; }

		int SimulatedMedicalTours { get; }

		int TotalSimulatedTours { get; }

		int TotalStops { get; }

		int TotalStopsExcludingWorkAndSchool { get; }

		int SimulatedWorkStops { get; }

		int SimulatedSchoolStops { get; }

		int SimulatedEscortStops { get; }

		int SimulatedPersonalBusinessStops { get; }

		int SimulatedShoppingStops { get; }

		int SimulatedMealStops { get; }

		int SimulatedSocialStops { get; }

		int SimulatedRecreationStops { get; }

		int SimulatedMedicalStops { get; }

		int TotalSimulatedStops { get; }

		bool IsWorkOrSchoolPattern { get; }

		bool IsOtherPattern { get; }

		bool HasHomeBasedTours { get; }

		bool HasTwoOrMoreWorkTours { get; }

		bool HasWorkStops { get; }

		bool HasSimulatedTours { get; }

		bool HasHomeBasedToursOnly { get; }

		bool IsFirstSimulatedHomeBasedTour { get; }

		bool IsLaterSimulatedHomeBasedTour { get; }

		bool HasSimulatedWorkStops { get; }

		bool HasSimulatedSchoolStops { get; }

		bool HasSimulatedEscortStops { get; }

		bool HasSimulatedPersonalBusinessStops { get; }

		bool HasSimulatedShoppingStops { get; }

		bool HasSimulatedMealStops { get; }

		bool HasSimulatedSocialStops { get; }

		bool IsValid { get; set; }

		ITimeWindow TimeWindow { get; }

		int AttemptedSimulations { get; set; }

		int PatternType { get; set; }

		bool HasMandatoryTourToUsualLocation { get; set; }

		int EscortFullHalfTours { get; set; }

		int WorksAtHomeFlag { get; set; }

		int JointTours { get; set; }

		int EscortJointTours { get; set; }

		int PersonalBusinessJointTours { get; set; }

		int ShoppingJointTours { get; set; }

		int MealJointTours { get; set; }

		int SocialJointTours { get; set; }

		int RecreationJointTours { get; set; }

		int MedicalJointTours { get; set; }

		bool IsMissingData { get; set;}

		// wrapper methods

		void InitializeTours();

		void SetTours();

		void GetMandatoryTourSimulatedData(IPersonDayWrapper personDay, List<ITourWrapper> tours);

		void GetIndividualTourSimulatedData(IPersonDayWrapper personDay, List<ITourWrapper> tours);

		void IncrementSimulatedTours(int destinationPurpose);

		void IncrementSimulatedStops(int destinationPurpose);

		void Reset();

		ITourWrapper GetEscortTour(int originAddressType, int originParcelId, int originZoneKey);

		ITourWrapper GetNewTour(int originAddressType, int originParcelId, int originZoneKey, int purpose);

		int GetNextTourSequence();

		int GetCurrentTourSequence();
		void SetHomeBasedNonMandatoryTours();
		void Export();
	}
}
