// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.ChoiceModels;
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;
using Daysim.ModelRunners;
using Ninject;

namespace Daysim.DomainModels {
	public class HouseholdWrapper : IHouseholdWrapper {
		//protected static int _nextHouseholdDayId;

		protected readonly IHousehold _household;
		public IRandomUtility RandomUtility { get; set; }
		public IPersonPersister PersonPersister { get; set; }
		public IHouseholdPersister HouseholdPersister { get; set; }
		public IHouseholdDayPersister HouseholdDayPersister { get; set; }
		public IHouseholdDayWrapperCreator HouseholdDayWrapperCreator { get; set; }
		public IPersonWrapperCreator PersonWrapperCreator { get; set; }

		public HouseholdWrapper(IHousehold household)
		{
			RandomUtility = new RandomUtility();

			_household = household;
		}
		public void Init()
		{
			if ( PersonPersister == null )
				PersonPersister = Global.Kernel.Get<PersonPersistenceFactory>().PersonPersister;
			if ( HouseholdDayPersister == null )
				HouseholdDayPersister = Global.Kernel.Get<HouseholdDayPersistenceFactory>().HouseholdDayPersister;
			if ( HouseholdDayWrapperCreator == null )
				HouseholdDayWrapperCreator = Global.Kernel.Get<HouseholdDayWrapperFactory>().HouseholdDayWrapperCreator;
			if ( HouseholdPersister == null )
				HouseholdPersister = Global.Kernel.Get<HouseholdPersistenceFactory>().HouseholdPersister;
			if ( PersonWrapperCreator == null )
				PersonWrapperCreator = Global.Kernel.Get<PersonWrapperFactory>().PersonWrapperCreator;

			SetExpansionFactor(_household);
			SetPersons();
			SetHouseholdDays();
			SetHouseholdTotals();
			SetParcelRelationships(_household);

			IsOnePersonHousehold = _household.Size == 1;
			IsTwoPersonHousehold = _household.Size == 2;
			Has0To15KIncome = _household.Income.IsRightExclusiveBetween(0, 15000);
			Has0To25KIncome = _household.Income.IsRightExclusiveBetween(0, 25000);
			Has25To45KIncome = _household.Income.IsRightExclusiveBetween(25000, 45000);
			Has25To50KIncome = _household.Income.IsRightExclusiveBetween(25000, 50000);
			Has50To75KIncome = _household.Income.IsRightExclusiveBetween(50000, 75000);
			Has75To100KIncome = _household.Income.IsRightExclusiveBetween(75000, 100000);
			Has75KPlusIncome = _household.Income >= 75000;
			Has100KPlusIncome = _household.Income >= 100000;
			HasIncomeUnder50K = _household.Income.IsRightExclusiveBetween(0, 50000);
			HasIncomeOver50K = _household.Income >= 50000;
			HasValidIncome = _household.Income >= 0;
			HasMissingIncome = _household.Income < 0;
			Has1Driver = HouseholdTotals.DrivingAgeMembers == 1;
			Has2Drivers = HouseholdTotals.DrivingAgeMembers == 2;
			Has3Drivers = HouseholdTotals.DrivingAgeMembers == 3;
			Has4OrMoreDrivers = HouseholdTotals.DrivingAgeMembers >= 4;
			HasMoreDriversThan1 = HouseholdTotals.DrivingAgeMembers > 1;
			HasMoreDriversThan2 = HouseholdTotals.DrivingAgeMembers > 2;
			HasMoreDriversThan3 = HouseholdTotals.DrivingAgeMembers > 3;
			HasMoreDriversThan4 = HouseholdTotals.DrivingAgeMembers > 4;
			HasNoFullOrPartTimeWorker = HouseholdTotals.FullAndPartTimeWorkers <= 0;
			Has1OrLessFullOrPartTimeWorkers = HouseholdTotals.FullAndPartTimeWorkers <= 1;
			Has2OrLessFullOrPartTimeWorkers = HouseholdTotals.FullAndPartTimeWorkers <= 2;
			Has3OrLessFullOrPartTimeWorkers = HouseholdTotals.FullAndPartTimeWorkers <= 3;
			Has4OrLessFullOrPartTimeWorkers = HouseholdTotals.FullAndPartTimeWorkers <= 4;
			HasChildrenUnder16 = HouseholdTotals.ChildrenUnder16 > 0;
			HasChildrenUnder5 = HouseholdTotals.ChildrenUnder5 > 0;
			HasChildrenAge5Through15 = HouseholdTotals.ChildrenAge5Through15 > 0;
			HasChildren = HouseholdTotals.DrivingAgeStudents > 0 || HouseholdTotals.ChildrenUnder16 > 0;
			// AutoIVT = GetAutoIVT(_household.Income);
			// TimeWindowSegment = GetTimeWindowSegment(_household.Income);

			HouseholdType = 0;
			if (_household.Size == 1 && (HouseholdTotals.AllWorkers > 0 || HouseholdTotals.AllStudents > 0)) {
				HouseholdType = Constants.HouseholdType.INDIVIDUAL_WORKER_STUDENT;
			}
			else if (_household.Size == 1) {
				HouseholdType = Constants.HouseholdType.INDIVIDUAL_NONWORKER_NONSTUDENT;
			}
			else if (HouseholdTotals.Adults == 1) {
				HouseholdType = Constants.HouseholdType.ONE_ADULT_WITH_CHILDREN;
			}
			else if (HouseholdTotals.RetiredAdults == 0 && HouseholdTotals.NonworkingAdults == 0  
				&& (HouseholdTotals.DrivingAgeStudents > 0 || HouseholdTotals.ChildrenUnder16 > 0)){
				HouseholdType = Constants.HouseholdType.TWO_PLUS_WORKER_STUDENT_ADULTS_WITH_CHILDREN;
			}
			else if (HouseholdTotals.DrivingAgeStudents > 0 || HouseholdTotals.ChildrenUnder16 > 0) {
				HouseholdType = Constants.HouseholdType.TWO_PLUS_ADULTS_ONE_PLUS_WORKERS_STUDENTS_WITH_CHILDREN;
			}
			else if (HouseholdTotals.RetiredAdults == 0 && HouseholdTotals.NonworkingAdults == 0) {
				HouseholdType = Constants.HouseholdType.TWO_PLUS_WORKER_STUDENT_ADULTS_WITHOUT_CHILDREN;
			}
			else if (HouseholdTotals.FullAndPartTimeWorkers > 0 || HouseholdTotals.UniversityStudents > 0){
				HouseholdType = Constants.HouseholdType.ONE_PLUS_WORKER_STUDENT_ADULTS_AND_ONE_PLUS_NONWORKER_NONSTUDENT_ADULTS_WITHOUT_CHILDREN;
			}
			else {
				HouseholdType = Constants.HouseholdType.TWO_PLUS_NONWORKER_NONSTUDENT_ADULTS_WITHOUT_CHILDREN;
			}

		}


		// relations

		public List<IPersonWrapper> Persons { get; private set; }

		public List<IHouseholdDayWrapper> HouseholdDays { get; private set; }

		public IHouseholdTotals HouseholdTotals { get; private set; }

		public ICondensedParcel ResidenceParcel { get; private set; }


		// domain model properies

		public int Id {
			get { return _household.Id; }
		}

		public double FractionWorkersWithJobsOutsideRegion {
			get { return _household.FractionWorkersWithJobsOutsideRegion; }
		}

		public int Size {
			get { return _household.Size; }
		}

		public int VehiclesAvailable {
			get { return _household.VehiclesAvailable; }
			set { _household.VehiclesAvailable = value; }
		}

		public int Income {
			get { return _household.Income; }
		}

		public int ResidenceParcelId {
			get { return _household.ResidenceParcelId; }
		}

		public int ResidenceZoneId {
			get { return _household.ResidenceZoneId; }
		}

		public int ResidenceZoneKey {
			get { return _household.ResidenceZoneKey; }
		}

		public double ExpansionFactor {
			get { return _household.ExpansionFactor; }
		}


		// flags, choice model properties, etc.

		public bool IsOnePersonHousehold { get; private set; }

		public bool IsTwoPersonHousehold { get; private set; }

		public bool Has0To15KIncome { get; private set; }

		public bool Has0To25KIncome { get; private set; }

		public bool Has25To45KIncome { get; private set; }

		public bool Has25To50KIncome { get; private set; }

		public bool Has50To75KIncome { get; private set; }

		public bool Has75To100KIncome { get; private set; }

		public bool Has75KPlusIncome { get; private set; }

		public bool Has100KPlusIncome { get; private set; }

		public bool HasIncomeUnder50K { get; private set; }

		public bool HasIncomeOver50K { get; private set; }

		public bool HasValidIncome { get; private set; }

		public bool HasMissingIncome { get; private set; }

		public bool Has1Driver { get; private set; }

		public bool Has2Drivers { get; private set; }

		public bool Has3Drivers { get; private set; }

		public bool Has4OrMoreDrivers { get; private set; }

		public bool HasMoreDriversThan1 { get; private set; }

		public bool HasMoreDriversThan2 { get; private set; }

		public bool HasMoreDriversThan3 { get; private set; }

		public bool HasMoreDriversThan4 { get; private set; }

		public bool HasNoFullOrPartTimeWorker { get; private set; }

		public bool Has1OrLessFullOrPartTimeWorkers { get; private set; }

		public bool Has2OrLessFullOrPartTimeWorkers { get; private set; }

		public bool Has3OrLessFullOrPartTimeWorkers { get; private set; }

		public bool Has4OrLessFullOrPartTimeWorkers { get; private set; }

		public bool HasChildrenUnder16 { get; private set; }

		public bool HasChildrenUnder5 { get; private set; }

		public bool HasChildrenAge5Through15 { get; private set; }

		public bool HasChildren { get; private set; }

		public int HouseholdType { get; private set; }

		public int VotALSegment {
			get {
				var segment = (Income < Constants.VotALSegment.INCOME_LOW_MEDIUM * Global.MonetaryUnitsPerDollar)
				              	? Constants.VotALSegment.LOW
				              	: (Income < Constants.VotALSegment.INCOME_MEDIUM_HIGH * Global.MonetaryUnitsPerDollar)
				              	  	? Constants.VotALSegment.MEDIUM
				              	  	: Constants.VotALSegment.HIGH;
				return segment;
			}
		}

		/// <summary>
		/// Auto IVT value of money in minutes/$ 
		/// </summary>
		// public double AutoIVT { get; private set; }

		// public int TimeWindowSegment { get; private set; }

		public int CarsPerDriver {
			get { return Math.Min(VehiclesAvailable / Math.Max(HouseholdTotals.DrivingAgeMembers, 1), 1); }
		}


		// seed synchronization

		public int[] SeedValues { get; private set; }


		// wrapper methods

		private void SetPersons() {
			Persons = LoadPersonsFromFile().Select(person => PersonWrapperCreator.CreateWrapper(person, this)).ToList();

			if (!Global.Configuration.ShouldSynchronizeRandomSeed) {
				return;
			}

			foreach (var person in Persons) {
				person.SeedValues = RandomUtility.GetSeedValues(Constants.NUMBER_OF_RANDOM_SEEDS);
			}

			SeedValues = Persons[0].SeedValues;
		}

		private void SetHouseholdDays() {
			HouseholdDays =
				Global.Configuration.IsInEstimationMode
					? LoadHouseholdDaysFromFile().Select(householdDay => Global.Kernel.Get<HouseholdDayWrapperFactory>().HouseholdDayWrapperCreator.CreateWrapper(householdDay, this)).ToList()
					: new List<IHouseholdDayWrapper> {CreateHouseholdDay()};
		}

		private void SetHouseholdTotals() {
			HouseholdTotals = new HouseholdTotals();

			foreach (var person in Persons) {
				HouseholdTotals.FullAndPartTimeWorkers += person.IsFullOrPartTimeWorker.ToFlag();
				HouseholdTotals.FulltimeWorkers += person.IsFulltimeWorker.ToFlag();
				HouseholdTotals.PartTimeWorkers += person.IsPartTimeWorker.ToFlag();
				HouseholdTotals.RetiredAdults += person.IsRetiredAdult.ToFlag();
				HouseholdTotals.NonworkingAdults += person.IsNonworkingAdult.ToFlag();
				HouseholdTotals.UniversityStudents += person.IsUniversityStudent.ToFlag();
				HouseholdTotals.DrivingAgeStudents += person.IsDrivingAgeStudent.ToFlag();
				HouseholdTotals.ChildrenAge5Through15 += person.IsChildAge5Through15.ToFlag();
				HouseholdTotals.ChildrenUnder5 += person.IsChildUnder5.ToFlag();
				HouseholdTotals.ChildrenUnder16 += person.IsChildUnder16.ToFlag();
				HouseholdTotals.Adults += person.IsAdult.ToFlag();
				HouseholdTotals.AllWorkers += person.IsWorker.ToFlag();
				HouseholdTotals.AllStudents += person.IsStudent.ToFlag();
				HouseholdTotals.DrivingAgeMembers += person.IsDrivingAge.ToFlag();
				HouseholdTotals.WorkersPlusStudents += (person.IsFulltimeWorker.ToFlag() + person.IsPartTimeWorker.ToFlag() + person.IsUniversityStudent.ToFlag() + person.IsDrivingAgeStudent.ToFlag());
			}

			var homeBasedPersons = // home-based workers and students in household
				Persons.Count(p => (p.IsWorker && p.UsualWorkParcelId == ResidenceParcelId) || (p.IsStudent && p.UsualSchoolParcelId == ResidenceParcelId));

			HouseholdTotals.PartTimeWorkersPerDrivingAgeMembers = HouseholdTotals.PartTimeWorkers / Math.Max(HouseholdTotals.DrivingAgeMembers, 1D);
			HouseholdTotals.RetiredAdultsPerDrivingAgeMembers = HouseholdTotals.RetiredAdults / Math.Max(HouseholdTotals.DrivingAgeMembers, 1D);
			HouseholdTotals.UniversityStudentsPerDrivingAgeMembers = HouseholdTotals.UniversityStudents / Math.Max(HouseholdTotals.DrivingAgeMembers, 1D);
			HouseholdTotals.DrivingAgeStudentsPerDrivingAgeMembers = HouseholdTotals.DrivingAgeStudents / Math.Max(HouseholdTotals.DrivingAgeMembers, 1D);
			HouseholdTotals.ChildrenUnder5PerDrivingAgeMembers = HouseholdTotals.ChildrenUnder5 / Math.Max(HouseholdTotals.DrivingAgeMembers, 1D);
			HouseholdTotals.HomeBasedPersonsPerDrivingAgeMembers = homeBasedPersons / Math.Max(HouseholdTotals.DrivingAgeMembers, 1D);
		}

		private void SetParcelRelationships(IHousehold household) {
			ResidenceParcel = ChoiceModelFactory.Parcels[household.ResidenceParcelId];
		}

		private static void SetExpansionFactor(IHousehold household) {
			household.ExpansionFactor *= Global.Configuration.HouseholdSamplingRateOneInX;
		}

//		private static double GetAutoIVT(int income) {
//			switch (income) {
//				case 5000:
//					return 20D;
//				case 15000:
//					return 11D;
//				case 25000:
//					return 8D;
//				case 35000:
//					return 5.8;
//				case 45000:
//					return 5D;
//				case 55000:
//					return 4.3;
//				case 65000:
//					return 3.8;
//				case 75000:
//					return 3.1;
//				case 85000:
//					return 2.7;
//				case 95000:
//					return 2.4;
//				case 105000:
//					return 2.1;
//				case 115000:
//					return 1.8;
//				case 125000:
//					return 1.6;
//				case 135000:
//					return 1.5;
//				case 145000:
//					return 1.4;
//				case 175000:
//					return 1.2;
//				default:
//					return 3.1;
//			}
//		}

//		private static int GetTimeWindowSegment(int income) {
//			if (income < 30000) {
//				return 1;
//			}
//
//			if (income < 60000) {
//				return 2;
//			}
//
//			if (income < 90000) {
//				return 3;
//			}
//
//			return 4;
//		}

		private IEnumerable<IPerson> LoadPersonsFromFile() {
			return PersonPersister.Seek(Id, "household_fk");
		}

		private IEnumerable<IHouseholdDay> LoadHouseholdDaysFromFile() {
			

			return HouseholdDayPersister.Seek(Id, "household_fk");
		}

		protected virtual IHouseholdDayWrapper CreateHouseholdDay() {
			return HouseholdDayWrapperCreator.CreateWrapper(new HouseholdDay {
				Id = Id, //++_nextHouseholdDayId,
				HouseholdId = Id,
				Day = 1,
			}, this);
		}

		public static int GetNoCarsInHouseholdFlag(int householdCars) {
			return (householdCars == 0).ToFlag();
		}

		public int GetCarsLessThanDriversFlag(int householdCars) {
			return (householdCars > 0 && householdCars < HouseholdTotals.DrivingAgeMembers).ToFlag();
		}

		public int GetCarsLessThanWorkersFlag(int householdCars) {
			return (householdCars > 0 && householdCars < HouseholdTotals.FullAndPartTimeWorkers).ToFlag();
		}


		// utility/export methods

		public void Export() {
			HouseholdPersister.Export(_household);
		}

		public static void Close() {
				Global.Kernel.Get<HouseholdPersistenceFactory>().HouseholdPersister.Dispose();
		}

		public override string ToString() {
			var builder = new StringBuilder();

			builder.AppendLine(String.Format("Household ID: {0}",
				_household.Id));

			builder.AppendLine(String.Format("Residence Parcel ID: {0}: Residence Zone Key: {1}",
				_household.ResidenceParcelId,
				_household.ResidenceZoneKey));

			builder.AppendLine(String.Format("Vehicles Available: {0}",
				_household.VehiclesAvailable));

			return builder.ToString();
		}
	}
}