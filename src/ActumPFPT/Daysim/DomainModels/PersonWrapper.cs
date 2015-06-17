// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
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
	public class PersonWrapper : IPersonWrapper {
		protected readonly IPerson _person;

		public PersonWrapper(IPerson person, IHouseholdWrapper household) {
			if (person == null) {
				throw new ArgumentNullException("person");
			}

			_person = person;

			Household = household;

			SetParcelRelationships(person);
			SetExpansionFactor(person);

			IsFullOrPartTimeWorker = PersonType <= Constants.PersonType.PART_TIME_WORKER;
			IsFulltimeWorker = PersonType == Constants.PersonType.FULL_TIME_WORKER;
			IsPartTimeWorker = PersonType == Constants.PersonType.PART_TIME_WORKER;
			IsNotFullOrPartTimeWorker = PersonType > Constants.PersonType.PART_TIME_WORKER;
			IsStudentAge = PersonType >= Constants.PersonType.UNIVERSITY_STUDENT;
			IsRetiredAdult = PersonType == Constants.PersonType.RETIRED_ADULT;
			IsNonworkingAdult = PersonType == Constants.PersonType.NON_WORKING_ADULT;
			IsUniversityStudent = PersonType == Constants.PersonType.UNIVERSITY_STUDENT;
			IsDrivingAgeStudent = PersonType == Constants.PersonType.DRIVING_AGE_STUDENT;
			IsChildAge5Through15 = PersonType == Constants.PersonType.CHILD_AGE_5_THROUGH_15;
			IsChildUnder5 = PersonType == Constants.PersonType.CHILD_UNDER_5;
			IsChildUnder16 = PersonType >= Constants.PersonType.CHILD_AGE_5_THROUGH_15;
			IsAdult = PersonType < Constants.PersonType.DRIVING_AGE_STUDENT;
			IsWorker = WorkerType > 0;
			IsStudent = StudentType > 0;
			IsFemale = Gender == Constants.PersonGender.FEMALE;
			IsMale = Gender == Constants.PersonGender.MALE;
			IsAdultFemale = IsFemale && IsAdult;
			IsAdultMale = IsMale && IsAdult;
			IsDrivingAge = PersonType <= Constants.PersonType.DRIVING_AGE_STUDENT;
			AgeIsBetween18And25 = person.Age.IsBetween(18, 25);
			AgeIsBetween26And35 = person.Age.IsBetween(26, 35);
			AgeIsBetween51And65 = person.Age.IsBetween(51, 65);
			AgeIsBetween51And98 = person.Age.IsBetween(51, 98);
			AgeIsLessThan35 = person.Age < 35;
			AgeIsLessThan30 = person.Age < 30;
			WorksAtHome = UsualWorkParcelId == Household.ResidenceParcelId;
			IsYouth = IsChildAge5Through15 || IsDrivingAgeStudent;
		}


		// relations

		public IHouseholdWrapper Household { get; private set; }

		public ICondensedParcel UsualWorkParcel { get; set; }

		public ICondensedParcel UsualSchoolParcel { get; set; }


		// domain model properies

		public int Id {
			get { return _person.Id; }
		}

		public int Sequence {
			get { return _person.Sequence; }
		}

		public int PersonType {
			get { return _person.PersonType; }
		}

		public int Age {
			get { return _person.Age; }
		}

		public int Gender {
			get { return _person.Gender; }
		}

		private int WorkerType {
			get { return _person.WorkerType; }
		}

		public int UsualWorkParcelId {
			get { return _person.UsualWorkParcelId; }
			set { _person.UsualWorkParcelId = value; }
		}

		public int PayToParkAtWorkplaceFlag {
			get { return _person.PaidParkingAtWorkplace; }
			set { _person.PaidParkingAtWorkplace = value; }
		}

		public int TransitPassOwnershipFlag {
			get { return _person.TransitPassOwnership; }
			set { _person.TransitPassOwnership = value; }
		}

		public int PaperDiary {
			get { return _person.PaperDiary; }
			set { _person.PaperDiary = value; }
		}

		public int ProxyResponse {
			get { return _person.ProxyResponse; }
			set { _person.ProxyResponse = value; }
		}

		public int UsualWorkZoneKey {
			get { return _person.UsualWorkZoneKey; }
			set { _person.UsualWorkZoneKey = value; }
		}

		public double AutoTimeToUsualWork {
			get { return _person.AutoTimeToUsualWork; }
			set { _person.AutoTimeToUsualWork = value; }
		}

		public double AutoDistanceToUsualWork {
			get { return _person.AutoDistanceToUsualWork; }
			set { _person.AutoDistanceToUsualWork = value; }
		}

		private int StudentType {
			get { return _person.StudentType; }
		}

		public int UsualSchoolParcelId {
			get { return _person.UsualSchoolParcelId; }
			set { _person.UsualSchoolParcelId = value; }
		}

		public int UsualSchoolZoneKey {
			get { return _person.UsualSchoolZoneKey; }
			set { _person.UsualSchoolZoneKey = value; }
		}

		public double AutoTimeToUsualSchool {
			get { return _person.AutoTimeToUsualSchool; }
			set { _person.AutoTimeToUsualSchool = value; }
		}

		public double AutoDistanceToUsualSchool {
			get { return _person.AutoDistanceToUsualSchool; }
			set { _person.AutoDistanceToUsualSchool = value; }
		}

		public int UsualModeToWork {
			get { return _person.UsualModeToWork; }
			set { _person.UsualModeToWork = value; }
		}

		public int UsualArrivalPeriodToWork {
			get { return _person.UsualArrivalPeriodToWork; }
			set { _person.UsualArrivalPeriodToWork = value; }
		}

		public int UsualDeparturePeriodFromWork {
			get { return _person.UsualDeparturePeriodFromWork; }
			set { _person.UsualDeparturePeriodFromWork = value; }
		}

		// flags, choice model properties, etc.

		public bool IsFullOrPartTimeWorker { get; private set; }

		public bool IsFulltimeWorker { get; private set; }

		public bool IsPartTimeWorker { get; private set; }

		public bool IsNotFullOrPartTimeWorker { get; private set; }

		public bool IsStudentAge { get; private set; }

		public bool IsRetiredAdult { get; private set; }

		public bool IsNonworkingAdult { get; private set; }

		public bool IsUniversityStudent { get; private set; }

		public bool IsDrivingAgeStudent { get; private set; }

		public bool IsChildAge5Through15 { get; private set; }

		public bool IsChildUnder5 { get; private set; }

		public bool IsChildUnder16 { get; private set; }

		public bool IsAdult { get; private set; }

		public bool IsWorker { get; private set; }

		public bool IsStudent { get; private set; }

		public bool IsFemale { get; private set; }

		public bool IsMale { get; private set; }

		public bool IsOnlyFullOrPartTimeWorker {
			get { return (IsFulltimeWorker || IsPartTimeWorker) && Household.HouseholdTotals.FullAndPartTimeWorkers == 1; }
		}

		public bool IsOnlyAdult {
			get { return IsAdult && Household.HouseholdTotals.Adults == 1; }
		}

		public bool IsAdultFemale { get; private set; }

		public bool IsAdultMale { get; private set; }

		public bool IsDrivingAge { get; private set; }

		public bool AgeIsBetween18And25 { get; private set; }

		public bool AgeIsBetween26And35 { get; private set; }

		public bool AgeIsBetween51And65 { get; private set; }

		public bool AgeIsBetween51And98 { get; private set; }

		public bool AgeIsLessThan35 { get; private set; }

		public bool AgeIsLessThan30 { get; private set; }

		public bool WorksAtHome { get; private set; }

		public bool IsYouth { get; private set; }

		public int CarOwnershipSegment {
			get {
				return
					_person.Age < 16
						? Constants.CarOwnership.CHILD
						: Household.VehiclesAvailable == 0
							? Constants.CarOwnership.NO_CARS
							: Household.VehiclesAvailable < Household.HouseholdTotals.DrivingAgeMembers
								? Constants.CarOwnership.LT_ONE_CAR_PER_ADULT
								: Constants.CarOwnership.ONE_OR_MORE_CARS_PER_ADULT;
			}
		}

		public double TransitFareDiscountFraction {
			get {
				return
					!Global.Configuration.PathImpedance_TransitUseFareDiscountFractions
						? 0.0
						: Global.Configuration.IncludeTransitPassOwnershipModel && _person.TransitPassOwnership > 0
							? 1.0
							: Global.Configuration.Policy_UniversalTransitFareDiscountFraction != 0.0 
                                ? Global.Configuration.Policy_UniversalTransitFareDiscountFraction
                                : IsChildUnder5
								    ? Global.Configuration.PathImpedance_TransitFareDiscountFractionChildUnder5
								    : IsChildAge5Through15
									    ? Global.Configuration.PathImpedance_TransitFareDiscountFractionChild5To15
									    : IsDrivingAgeStudent
										    ? Global.Configuration.PathImpedance_TransitFareDiscountFractionHighSchoolStudent
										    : IsUniversityStudent
											    ? Global.Configuration.PathImpedance_TransitFareDiscountFractionUniverityStudent
											    : _person.Age >= 65
												    ? Global.Configuration.PathImpedance_TransitFareDiscountFractionAge65Up
												    : 0.0;
			}
		}

		public int HouseholdDayPatternParticipationPriority {
			get {
				switch (PersonType) {
					case Constants.PersonType.FULL_TIME_WORKER: return 5;
					case Constants.PersonType.PART_TIME_WORKER: return 4;
					case Constants.PersonType.RETIRED_ADULT: return 6;
					case Constants.PersonType.NON_WORKING_ADULT: return 3;
					case Constants.PersonType.UNIVERSITY_STUDENT: return 8;
					case Constants.PersonType.DRIVING_AGE_STUDENT: return 7;
					case Constants.PersonType.CHILD_AGE_5_THROUGH_15: return 2;
					case Constants.PersonType.CHILD_UNDER_5: return 1;
					default: return 9;
				}
			}
		}



		// seed synchronization

		public int[] SeedValues { get; set; }


		// wrapper methods

		public void UpdatePersonValues() {
			if (!Global.Configuration.IsInEstimationMode && UsualWorkParcel != null) {
				var useMode = Constants.Mode.SOV;
				var autoPathRoundTrip = PathTypeModel.Run(Household.RandomUtility, Household.ResidenceParcel, UsualWorkParcel, Constants.Time.SEVEN_AM, Constants.Time.FIVE_PM,
					Constants.Purpose.WORK, Global.Coefficients_BaseCostCoefficientPerMonetaryUnit, Global.Configuration.Coefficients_MeanTimeCoefficient_Work,
					true, 1, 0.0, false, useMode).First();
				_person.AutoTimeToUsualWork = autoPathRoundTrip.PathTime / 2.0;
				_person.AutoDistanceToUsualWork = autoPathRoundTrip.PathDistance / 2.0;
			}

			if (!Global.Configuration.IsInEstimationMode && UsualSchoolParcel != null) {
				var useMode = Constants.Mode.SOV;
				var autoPathRoundTrip = PathTypeModel.Run(Household.RandomUtility, Household.ResidenceParcel, UsualSchoolParcel, Constants.Time.SEVEN_AM, Constants.Time.THREE_PM,
					Constants.Purpose.SCHOOL, Global.Coefficients_BaseCostCoefficientPerMonetaryUnit, Global.Configuration.Coefficients_MeanTimeCoefficient_Other,
					true, 1, 0.0, false, useMode).First();
				_person.AutoTimeToUsualSchool = autoPathRoundTrip.PathTime / 2.0;
				_person.AutoDistanceToUsualSchool = autoPathRoundTrip.PathDistance / 2.0;
			}
		}


		private void SetParcelRelationships(IPerson person) {
			CondensedParcel usualWorkParcel;

			if (person.UsualWorkParcelId != Constants.DEFAULT_VALUE && ChoiceModelFactory.Parcels.TryGetValue(person.UsualWorkParcelId, out usualWorkParcel)) {
				UsualWorkParcel = usualWorkParcel;
			}

			CondensedParcel usualSchoolParcel;

			if (person.UsualSchoolParcelId != Constants.DEFAULT_VALUE && ChoiceModelFactory.Parcels.TryGetValue(person.UsualSchoolParcelId, out usualSchoolParcel)) {
				UsualSchoolParcel = usualSchoolParcel;
			}
		}

		private static void SetExpansionFactor(IPerson person) {
			person.ExpansionFactor *= Global.Configuration.HouseholdSamplingRateOneInX;
		}

		public void SetWorkParcelPredictions() {
			if (UsualWorkParcelId != Constants.DEFAULT_VALUE && UsualWorkParcelId != Constants.OUT_OF_REGION_PARCEL_ID) {
				UsualWorkParcel.EmploymentPrediction += Household.ExpansionFactor;
			}
		}

		public void SetSchoolParcelPredictions() {
			if (UsualSchoolParcelId == Constants.DEFAULT_VALUE || UsualSchoolParcelId == Constants.OUT_OF_REGION_PARCEL_ID) {
				return;
			}

			if (IsAdult) {
				UsualSchoolParcel.StudentsUniversityPrediction += Household.ExpansionFactor;
			}
			else {
				UsualSchoolParcel.StudentsK12Prediction += Household.ExpansionFactor;
			}
		}


		// utility/export methods

		public void Export() {
			Global.Kernel.Get<PersonPersistenceFactory>().PersonPersister.Export(_person);
		}

		public static void Close() {
			Global.Kernel.Get<PersonPersistenceFactory>().PersonPersister.Dispose();
		}

		public override string ToString() {
			var builder = new StringBuilder();

			builder.AppendLine(string.Format("Person ID: {0}",
				_person.Id));

			builder.AppendLine(string.Format("Household ID: {0}, Sequence: {1}",
				_person.HouseholdId,
				_person.Sequence));

			builder.AppendLine(string.Format("Usual Work Parcel ID: {0}, Usual Work Zone Key: {1}, Auto Distance To Usual Work: {2}, Auto Time To Usual Work: {3}",
				_person.UsualWorkParcelId,
				_person.UsualWorkZoneKey,
				_person.AutoDistanceToUsualWork,
				_person.AutoTimeToUsualWork));

			builder.AppendLine(string.Format("Usual School Parcel ID: {0}, Usual School Zone Key: {1}, Auto Distance To Usual School: {2}, Auto Time To Usual School: {3}",
				_person.UsualSchoolParcelId,
				_person.UsualSchoolZoneKey,
				_person.AutoDistanceToUsualSchool,
				_person.AutoTimeToUsualSchool));

			return builder.ToString();
		}
	}
}