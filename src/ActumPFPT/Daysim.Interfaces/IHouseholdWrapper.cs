using System.Collections.Generic;

namespace Daysim.Interfaces 
{
	public interface IHouseholdWrapper
	{
		IRandomUtility RandomUtility { get; set; }
		IPersonPersister PersonPersister { get; set; }
		IHouseholdPersister HouseholdPersister { get; set; }
		IHouseholdDayPersister HouseholdDayPersister { get; set; }
		IHouseholdDayWrapperCreator HouseholdDayWrapperCreator { get; set; }
		IPersonWrapperCreator PersonWrapperCreator { get; set; }
		
		void Init();
		
		List<IPersonWrapper> Persons { get; }

		List<IHouseholdDayWrapper> HouseholdDays { get; }

		IHouseholdTotals HouseholdTotals { get; }

		ICondensedParcel ResidenceParcel { get; }
		

		// domain model properies

		int Id { get; }

		double FractionWorkersWithJobsOutsideRegion { get; }
		int Size { get; }
		int VehiclesAvailable { get; set; }

		int Income { get; }

		int ResidenceParcelId { get; }

		int ResidenceZoneId { get; }

		int ResidenceZoneKey { get; }

		double ExpansionFactor { get; }


		// flags, choice model properties, etc.

		bool IsOnePersonHousehold { get; }

		bool IsTwoPersonHousehold { get; }

		bool Has0To15KIncome { get; }

		bool Has0To25KIncome { get; }

		bool Has25To45KIncome { get; }

		bool Has25To50KIncome { get; }

		bool Has50To75KIncome { get; }

		bool Has75To100KIncome { get; }

		bool Has75KPlusIncome { get; }

		bool Has100KPlusIncome { get; }

		bool HasIncomeUnder50K { get; }

		bool HasIncomeOver50K { get; }

		bool HasValidIncome { get; }

		bool HasMissingIncome { get; }

		bool Has1Driver { get; }

		bool Has2Drivers { get; }

		bool Has3Drivers { get; }

		bool Has4OrMoreDrivers { get; }

		bool HasMoreDriversThan1 { get; }

		bool HasMoreDriversThan2 { get; }

		bool HasMoreDriversThan3 { get; }

		bool HasMoreDriversThan4 { get; }

		bool HasNoFullOrPartTimeWorker { get; }

		bool Has1OrLessFullOrPartTimeWorkers { get; }

		bool Has2OrLessFullOrPartTimeWorkers { get; }

		bool Has3OrLessFullOrPartTimeWorkers { get; }

		bool Has4OrLessFullOrPartTimeWorkers { get; }

		bool HasChildrenUnder16 { get; }

		bool HasChildrenUnder5 { get; }

		bool HasChildrenAge5Through15 { get; }

		bool HasChildren { get; }

		int HouseholdType { get; }

		int VotALSegment { get; }

		/// <summary>
		/// Auto IVT value of money in minutes/$ 
		/// </summary>
		// double AutoIVT { get; set; }

		// int TimeWindowSegment { get; set; }

		int CarsPerDriver { get; }


		// seed synchronization

		int[] SeedValues { get; }


		// wrapper methods


		int GetCarsLessThanDriversFlag(int householdCars);

		int GetCarsLessThanWorkersFlag(int householdCars);


		// utility/export methods

		void Export();
		

	}
}
