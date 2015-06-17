using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface IPersonWrapper 
	{
		IHouseholdWrapper Household { get; }

		ICondensedParcel UsualWorkParcel { get; set; }

		ICondensedParcel UsualSchoolParcel { get; set; }


		// domain model properies

		int Id { get; }

		int Sequence { get; }

		int PersonType { get; }

		int Age { get; }

		int Gender { get; }

		int UsualWorkParcelId { get; set; }

		int PayToParkAtWorkplaceFlag { get; set; }

		int TransitPassOwnershipFlag { get; set; }

		int PaperDiary { get; set; }

		int ProxyResponse { get; set; }

		int UsualWorkZoneKey { get; set; }

		double AutoTimeToUsualWork { get; set; }

		double AutoDistanceToUsualWork { get; set; }

		int UsualSchoolParcelId { get; set; }

		int UsualSchoolZoneKey { get; set; }

		double AutoTimeToUsualSchool { get; set; }

		double AutoDistanceToUsualSchool { get; set; }

		int UsualModeToWork { get; set; }

		int UsualArrivalPeriodToWork { get; set; }

		int UsualDeparturePeriodFromWork { get; set; }

		// flags, choice model properties, etc.

		bool IsFullOrPartTimeWorker { get; }

		bool IsFulltimeWorker { get; }

		bool IsPartTimeWorker { get; }

		bool IsNotFullOrPartTimeWorker { get; }

		bool IsStudentAge { get; }

		bool IsRetiredAdult { get; }

		bool IsNonworkingAdult { get; }

		bool IsUniversityStudent { get; }

		bool IsDrivingAgeStudent { get; }

		bool IsChildAge5Through15 { get; }

		bool IsChildUnder5 { get; }

		bool IsChildUnder16 { get; }

		bool IsAdult { get; }

		bool IsWorker { get; }

		bool IsStudent { get; }

		bool IsFemale { get; }

		bool IsMale { get; }

		bool IsOnlyFullOrPartTimeWorker { get; }

		bool IsOnlyAdult { get; }

		bool IsAdultFemale { get; }

		bool IsAdultMale { get; }

		bool IsDrivingAge { get; }

		bool AgeIsBetween18And25 { get; }

		bool AgeIsBetween26And35 { get; }

		bool AgeIsBetween51And65 { get; }

		bool AgeIsBetween51And98 { get; }

		bool AgeIsLessThan35 { get; }

		bool AgeIsLessThan30 { get; }

		bool WorksAtHome { get; }

		bool IsYouth { get; }

		int CarOwnershipSegment { get; }

		double TransitFareDiscountFraction { get; }

		int HouseholdDayPatternParticipationPriority { get; }



		// seed synchronization

		int[] SeedValues { get; set; }


		// wrapper methods

		void UpdatePersonValues();
		void SetWorkParcelPredictions();

		void SetSchoolParcelPredictions();
		void Export();
	}
}
