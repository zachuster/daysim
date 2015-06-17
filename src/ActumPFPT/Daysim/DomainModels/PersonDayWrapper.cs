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
	public class PersonDayWrapper : IPersonDayWrapper {
		//public static int NextTourId;

		protected IPersonDay _personDay;
		protected int _tourSequence;

		public PersonDayWrapper(IPersonDay personDay, IPersonWrapper personWrapper, IHouseholdDayWrapper householdDayWrapper) {
			if (personWrapper == null) {
				throw new ArgumentNullException("personWrapper");
			}

			_personDay = personDay;

			Household = personWrapper.Household;
			Person = personWrapper;
			HouseholdDay = householdDayWrapper;

			_personDay.ExpansionFactor = Household.ExpansionFactor;

			TimeWindow = new TimeWindow();
		}


		// relations

		public IHouseholdWrapper Household { get; private set; }

		public IHouseholdDayWrapper HouseholdDay { get; private set; }

		public IPersonWrapper Person { get; private set; }

		public List<ITourWrapper> Tours { get; private set; }


		// domain model properies

		public int Id {
			get { return _personDay.Id; }
		}

		public int Day {
			get { return _personDay.Day; }
		}

		public int HomeBasedTours {
			get { return _personDay.HomeBasedTours; }
			set { _personDay.HomeBasedTours = value; }
		}

		public int WorkBasedTours {
			get { return _personDay.WorkBasedTours; }
			set { _personDay.WorkBasedTours = value; }
		}

		public int UsualWorkplaceTours {
			get { return _personDay.UsualWorkplaceTours; }
			set { _personDay.UsualWorkplaceTours = value; }
		}

		public int WorkTours {
			get { return _personDay.WorkTours; }
			set { _personDay.WorkTours = value; }
		}

		public int SchoolTours {
			get { return _personDay.SchoolTours; }
			set { _personDay.SchoolTours = value; }
		}

		public int EscortTours {
			get { return _personDay.EscortTours; }
			set { _personDay.EscortTours = value; }
		}

		public int PersonalBusinessTours {
			get { return _personDay.PersonalBusinessTours; }
			set { _personDay.PersonalBusinessTours = value; }
		}

		public int ShoppingTours {
			get { return _personDay.ShoppingTours; }
			set { _personDay.ShoppingTours = value; }
		}

		public int MealTours {
			get { return _personDay.MealTours; }
			set { _personDay.MealTours = value; }
		}

		public int SocialTours {
			get { return _personDay.SocialTours; }
			set { _personDay.SocialTours = value; }
		}

		public int RecreationTours {
			get { return _personDay.RecreationTours; }
			set { _personDay.RecreationTours = value; }
		}

		public int MedicalTours {
			get { return _personDay.MedicalTours; }
			set { _personDay.MedicalTours = value; }
		}

		public int WorkStops {
			get { return _personDay.WorkStops; }
			set { _personDay.WorkStops = value; }
		}

		public int SchoolStops {
			get { return _personDay.SchoolStops; }
			set { _personDay.SchoolStops = value; }
		}

		public int EscortStops {
			get { return _personDay.EscortStops; }
			set { _personDay.EscortStops = value; }
		}

		public int PersonalBusinessStops {
			get { return _personDay.PersonalBusinessStops; }
			set { _personDay.PersonalBusinessStops = value; }
		}

		public int ShoppingStops {
			get { return _personDay.ShoppingStops; }
			set { _personDay.ShoppingStops = value; }
		}

		public int MealStops {
			get { return _personDay.MealStops; }
			set { _personDay.MealStops = value; }
		}

		public int SocialStops {
			get { return _personDay.SocialStops; }
			set { _personDay.SocialStops = value; }
		}

		public int RecreationStops {
			get { return _personDay.RecreationStops; }
			set { _personDay.RecreationStops = value; }
		}

		public int MedicalStops {
			get { return _personDay.MedicalStops; }
			set { _personDay.MedicalStops = value; }
		}

		public int WorkAtHomeDuration { 
			get {	return (_personDay).WorkAtHomeDuration; }
			set { _personDay.WorkAtHomeDuration = value; }
		}

		public double ExpansionFactor	{ 
			get {	return (_personDay).ExpansionFactor; }
			set { _personDay.ExpansionFactor = value; }
		}
	

		public int JointTourParticipationPriority {
			get {
				if (PatternType == Constants.PatternType.HOME || CreatedNonMandatoryTours >= 3
					|| TotalCreatedTourPurposes >= 3) {
					return 9;
				}
				switch (Person.PersonType) {
					case Constants.PersonType.FULL_TIME_WORKER: return 8;
					case Constants.PersonType.PART_TIME_WORKER: return 5;
					case Constants.PersonType.RETIRED_ADULT: return 4;
					case Constants.PersonType.NON_WORKING_ADULT: return 2;
					case Constants.PersonType.UNIVERSITY_STUDENT: return 6;
					case Constants.PersonType.DRIVING_AGE_STUDENT: return 7;
					case Constants.PersonType.CHILD_AGE_5_THROUGH_15: return 3;
					case Constants.PersonType.CHILD_UNDER_5: return 1;
					default: return 9;
				}
			}
		}

		public int JointHalfTourParticipationPriority {
			get {
				if (PatternType == Constants.PatternType.HOME) {
					return 9;
				}
				if (!Person.IsDrivingAge && PatternType == Constants.PatternType.NONMANDATORY) {
					return 9;
				}
				switch (Person.PersonType) {
					case Constants.PersonType.FULL_TIME_WORKER: return 5;
					case Constants.PersonType.PART_TIME_WORKER: return 6;
					case Constants.PersonType.RETIRED_ADULT: return 8;
					case Constants.PersonType.NON_WORKING_ADULT: return 7;
					case Constants.PersonType.UNIVERSITY_STUDENT: return 4;
					case Constants.PersonType.DRIVING_AGE_STUDENT: return 3;
					case Constants.PersonType.CHILD_AGE_5_THROUGH_15: return 1;
					case Constants.PersonType.CHILD_UNDER_5: return 2;
					default: return 9;
				}
			}
		}




		// flags, choice model properties, etc.

		public virtual int TotalTours {
			get { return WorkTours + SchoolTours + EscortTours + PersonalBusinessTours + ShoppingTours + MealTours + SocialTours + RecreationTours + MedicalTours; }
		}

		public int TotalToursExcludingWorkAndSchool {
			get { return EscortTours + PersonalBusinessTours + ShoppingTours + MealTours + SocialTours + RecreationTours + MedicalTours; }
		}

		public int CreatedWorkTours { get; set; }

		public int CreatedSchoolTours { get; set; }

		public int CreatedEscortTours { get; set; }

		public int CreatedPersonalBusinessTours { get; set; }

		public int CreatedShoppingTours { get; set; }

		public int CreatedMealTours { get; set; }

		public int CreatedSocialTours { get; set; }

		public int CreatedRecreationTours { get; set; }

		public int CreatedMedicalTours { get; set; }

		public int CreatedWorkBasedTours { get; set; }

		public int CreatedNonMandatoryTours {
			get { return CreatedEscortTours + CreatedPersonalBusinessTours + CreatedShoppingTours + CreatedMealTours + CreatedSocialTours + CreatedRecreationTours + CreatedMedicalTours; }
		}
		public int TotalCreatedTours {
			get { return CreatedWorkTours + CreatedSchoolTours + CreatedEscortTours + CreatedPersonalBusinessTours 
				+ CreatedShoppingTours + CreatedMealTours + CreatedSocialTours + CreatedRecreationTours + CreatedMedicalTours
				+ CreatedWorkBasedTours; }
		}
		
		public virtual int TotalCreatedTourPurposes {
			get { return Math.Min(1, CreatedWorkTours) + Math.Min(1, CreatedSchoolTours) + Math.Min(1, CreatedEscortTours)
				+ Math.Min(1, CreatedPersonalBusinessTours) + Math.Min(1, CreatedShoppingTours) + Math.Min(1, CreatedMealTours)
				+ Math.Min(1, CreatedSocialTours) + Math.Min(1, CreatedRecreationTours) + Math.Min(1, CreatedMedicalTours);}
		}

		public int SimulatedHomeBasedTours { get; private set; }

		public int SimulatedWorkTours { get; private set; }

		public int SimulatedSchoolTours { get; private set; }

		public int SimulatedEscortTours { get; private set; }

		public int SimulatedPersonalBusinessTours { get; private set; }

		public int SimulatedShoppingTours { get; private set; }

		public int SimulatedMealTours { get; private set; }

		public int SimulatedSocialTours { get; private set; }

		public int SimulatedRecreationTours { get; private set; }

		public int SimulatedMedicalTours { get; private set; }

		public virtual int TotalSimulatedTours {
			get { return SimulatedWorkTours + SimulatedSchoolTours + SimulatedEscortTours + SimulatedPersonalBusinessTours + SimulatedShoppingTours + SimulatedMealTours + SimulatedSocialTours + SimulatedRecreationTours + SimulatedMedicalTours; }
		}

		public virtual int TotalStops {
			get { return WorkStops + SchoolStops + EscortStops + PersonalBusinessStops + ShoppingStops + MealStops + SocialStops + RecreationStops + MedicalStops; }
		}

		public int TotalStopsExcludingWorkAndSchool {
			get { return EscortStops + PersonalBusinessStops + ShoppingStops + MealStops + SocialStops + RecreationStops + MedicalStops; }
		}

		public int SimulatedWorkStops { get; protected set; }

		public int SimulatedSchoolStops { get; protected set; }

		public int SimulatedEscortStops { get; protected set; }

		public int SimulatedPersonalBusinessStops { get; protected set; }

		public int SimulatedShoppingStops { get; protected set; }

		public int SimulatedMealStops { get; protected set; }

		public int SimulatedSocialStops { get; protected set; }

		public int SimulatedRecreationStops { get; protected set; }

		public int SimulatedMedicalStops { get; protected set; }

		public virtual int TotalSimulatedStops {
			get { return SimulatedWorkStops + SimulatedSchoolStops + SimulatedEscortStops + SimulatedPersonalBusinessStops + SimulatedShoppingStops + SimulatedMealStops + SimulatedSocialStops + SimulatedRecreationStops + SimulatedMedicalStops; }
		}

		public bool IsWorkOrSchoolPattern {
			get { return WorkTours + SchoolTours > 0; }
		}

		public bool IsOtherPattern {
			get { return WorkTours + SchoolTours == 0; }
		}

		public bool HasHomeBasedTours {
			get { return HomeBasedTours > 1; }
		}

		public bool HasTwoOrMoreWorkTours {
			get { return WorkTours > 1; }
		}

		public bool HasWorkStops {
			get { return WorkStops >= 1; }
		}

		public bool HasSimulatedTours {
			get { return TotalSimulatedTours > 1; }
		}

		public bool HasHomeBasedToursOnly {
			get { return HomeBasedTours == 1; }
		}

		public bool IsFirstSimulatedHomeBasedTour {
			get { return SimulatedHomeBasedTours == 1; }
		}

		public bool IsLaterSimulatedHomeBasedTour {
			get { return SimulatedHomeBasedTours > 1; }
		}

		public bool HasSimulatedWorkStops {
			get { return SimulatedWorkStops > 0; }
		}

		public bool HasSimulatedSchoolStops {
			get { return SimulatedSchoolStops > 0; }
		}

		public bool HasSimulatedEscortStops {
			get { return SimulatedEscortStops > 0; }
		}

		public bool HasSimulatedPersonalBusinessStops {
			get { return SimulatedPersonalBusinessStops > 0; }
		}

		public bool HasSimulatedShoppingStops {
			get { return SimulatedShoppingStops > 0; }
		}

		public bool HasSimulatedMealStops {
			get { return SimulatedMealStops > 0; }
		}

		public bool HasSimulatedSocialStops {
			get { return SimulatedSocialStops > 0; }
		}

		public bool IsValid { get; set; }

		public ITimeWindow TimeWindow { get; private set; }

		public int AttemptedSimulations { get; set; }

		public int PatternType { get; set; }

		public bool HasMandatoryTourToUsualLocation { get; set; }

		public int EscortFullHalfTours { get; set; }

		public int WorksAtHomeFlag { get; set; }

		public int JointTours { get; set; }

		public int EscortJointTours { get; set; }

		public int PersonalBusinessJointTours { get; set; }

		public int ShoppingJointTours { get; set; }

		public int MealJointTours { get; set; }

		public int SocialJointTours { get; set; }

		public int RecreationJointTours { get; set; }

		public int MedicalJointTours { get; set; }

		public bool IsMissingData { get; set;}

		// wrapper methods

		public void InitializeTours() {
			Tours = 
				Global.Configuration.IsInEstimationMode
					? GetTourSurveyData()
					: new List<ITourWrapper>();
		}

		public void SetTours() {
			Tours =
				Global.Configuration.IsInEstimationMode
					? GetTourSurveyData()
					: GetTourSimulatedData();
		}

		private List<ITourWrapper> GetTourSurveyData() {
			var data = new List<ITourWrapper>();
//			var toursForPersonDay = LoadToursFromFile().OrderBy(tour => tour.OriginDepartureTime).ThenBy(tour => tour.ParentTourSequence).ThenBy(tour => tour.Sequence).ToList();
			var toursForPersonDay = LoadToursFromFile().ToList();
			var tours = toursForPersonDay.Where(t => t.ParentTourSequence == 0);

			foreach (var tour in tours) {
				var wrapper = GetNewWrapper(tour, this, tour.DestinationPurpose, true);

				data.Add(wrapper);

				var parentTourSequence = tour.Sequence;
				var subtours = toursForPersonDay.Where(st => st.ParentTourSequence == parentTourSequence);

				foreach (var subtour in subtours) {
					wrapper.Subtours.Add(Global.Kernel.Get<TourWrapperFactory>().TourWrapperCreator.CreateSubWrapper(subtour, wrapper));
				}
			}

			return data;
		}

		protected virtual TourWrapper GetNewWrapper(ITour tour, PersonDayWrapper personDayWrapper, int destinationPurpose, bool b)
		{
			return new TourWrapper(tour, this, tour.DestinationPurpose, true);
		}

		private List<ITourWrapper> GetTourSimulatedData() {
			var data = new List<ITourWrapper>();

			data.AddRange(CreateToursByPurpose(Constants.Purpose.WORK, WorkTours));
			data.AddRange(CreateToursByPurpose(Constants.Purpose.SCHOOL, SchoolTours));
			data.AddRange(CreateToursByPurpose(Constants.Purpose.ESCORT, EscortTours));
			data.AddRange(CreateToursByPurpose(Constants.Purpose.PERSONAL_BUSINESS, PersonalBusinessTours));
			data.AddRange(CreateToursByPurpose(Constants.Purpose.SHOPPING, ShoppingTours));
			data.AddRange(CreateToursByPurpose(Constants.Purpose.MEAL, MealTours));
			data.AddRange(CreateToursByPurpose(Constants.Purpose.SOCIAL, SocialTours));

			return data;
		}
		public void GetMandatoryTourSimulatedData (IPersonDayWrapper personDay, List<ITourWrapper> tours) {
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.WORK, personDay.UsualWorkplaceTours));
			foreach (var tour in tours) {
				tour.DestinationParcel = personDay.Person.UsualWorkParcel;
 				tour.DestinationParcelId = personDay.Person.UsualWorkParcelId;
				tour.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[personDay.Person.UsualWorkParcel.ZoneId];
				tour.DestinationAddressType = Constants.AddressType.USUAL_WORKPLACE;
			}
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.WORK, personDay.WorkTours - personDay.UsualWorkplaceTours));
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.SCHOOL, personDay.SchoolTours));
			foreach (var tour in tours) {
				if (tour.DestinationPurpose == Constants.Purpose.SCHOOL) {
				tour.DestinationParcel = personDay.Person.UsualSchoolParcel;
 				tour.DestinationParcelId = personDay.Person.UsualSchoolParcelId;
				tour.DestinationZoneKey = ChoiceModelFactory.ZoneKeys[personDay.Person.UsualSchoolParcel.ZoneId];
				tour.DestinationAddressType = Constants.AddressType.USUAL_SCHOOL;
				}
			}
		}

		public void GetIndividualTourSimulatedData (IPersonDayWrapper personDay, List<ITourWrapper> tours) {
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.ESCORT, CreatedEscortTours - EscortFullHalfTours));
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.PERSONAL_BUSINESS, CreatedPersonalBusinessTours));
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.SHOPPING, CreatedShoppingTours));
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.MEAL, CreatedMealTours));
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.SOCIAL, CreatedSocialTours));
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.RECREATION, CreatedRecreationTours));
			tours.AddRange(CreateToursByPurpose(Constants.Purpose.MEDICAL, CreatedMedicalTours));
		}


		public void IncrementSimulatedTours(int destinationPurpose) {
			SimulatedHomeBasedTours++;

			switch (destinationPurpose) {
				case Constants.Purpose.WORK:
					SimulatedWorkTours++;

					break;

				case Constants.Purpose.SCHOOL:
					SimulatedSchoolTours++;

					break;

				case Constants.Purpose.ESCORT:
					SimulatedEscortTours++;

					break;

				case Constants.Purpose.PERSONAL_BUSINESS:
					SimulatedPersonalBusinessTours++;

					break;

				case Constants.Purpose.SHOPPING:
					SimulatedShoppingTours++;

					break;

				case Constants.Purpose.MEAL:
					SimulatedMealTours++;

					break;

				case Constants.Purpose.SOCIAL:
					SimulatedSocialTours++;

					break;

				case Constants.Purpose.RECREATION:
					SimulatedRecreationTours++;

					break;

				case Constants.Purpose.MEDICAL:
					SimulatedMedicalTours++;

					break;
			}
		}

		public virtual void IncrementSimulatedStops(int destinationPurpose) {
			switch (destinationPurpose) {
				case Constants.Purpose.WORK:
					SimulatedWorkStops++;

					break;

				case Constants.Purpose.SCHOOL:
					SimulatedSchoolStops++;

					break;

				case Constants.Purpose.ESCORT:
					SimulatedEscortStops++;

					break;

				case Constants.Purpose.PERSONAL_BUSINESS:
					SimulatedPersonalBusinessStops++;

					break;

				case Constants.Purpose.SHOPPING:
					SimulatedShoppingStops++;

					break;

				case Constants.Purpose.MEAL:
					SimulatedMealStops++;

					break;

				case Constants.Purpose.SOCIAL:
					SimulatedSocialStops++;

					break;
				case Constants.Purpose.RECREATION:
					SimulatedRecreationStops++;

					break;
				case Constants.Purpose.MEDICAL:
					SimulatedMedicalStops++;

					break;
			}
		}

		protected virtual void CreateNewPersonDay(IPersonDay temp)
		{
			_personDay = new PersonDay {
				Id = temp.Id,
				PersonId = temp.PersonId,
				HouseholdDayId = temp.HouseholdDayId,
				HouseholdId = temp.HouseholdId,
				PersonSequence = temp.PersonSequence,
				Day = temp.Day, 
				DayBeginsAtHome = temp.DayBeginsAtHome,
				DayEndsAtHome =  temp.DayEndsAtHome,
				ExpansionFactor = temp.ExpansionFactor
			};
		}

		public virtual void Reset() {
			TimeWindow = new TimeWindow();

			var temp = _personDay;

			CreateNewPersonDay(temp);

			_tourSequence = 0;

			SimulatedHomeBasedTours = 0;

			SimulatedWorkTours = 0;
			SimulatedSchoolTours = 0;
			SimulatedEscortTours = 0;
			SimulatedPersonalBusinessTours = 0;
			SimulatedShoppingTours = 0;
			SimulatedMealTours = 0;
			SimulatedSocialTours = 0;

			SimulatedWorkStops = 0;
			SimulatedSchoolStops = 0;
			SimulatedEscortStops = 0;
			SimulatedPersonalBusinessStops = 0;
			SimulatedShoppingStops = 0;
			SimulatedMealStops = 0;
			SimulatedSocialStops = 0;

			CreatedWorkTours = 0;
			CreatedSchoolTours = 0;
			CreatedEscortTours = 0;
			CreatedPersonalBusinessTours = 0;
			CreatedShoppingTours = 0;
			CreatedMealTours = 0;
			CreatedSocialTours = 0;
			CreatedRecreationTours = 0;
			CreatedMedicalTours = 0;

			WorkTours = 0;
			SchoolTours = 0;
			EscortTours = 0;
			PersonalBusinessTours = 0;
			ShoppingTours = 0;
			MealTours = 0;
			SocialTours = 0;
			RecreationTours = 0;
			MedicalTours = 0;

			WorkStops = 0;
			SchoolStops = 0;
			EscortStops = 0;
			PersonalBusinessStops = 0;
			ShoppingStops = 0;
			MealStops = 0;
			SocialStops = 0;
			RecreationStops = 0;
			MedicalStops = 0;

			UsualWorkplaceTours = 0;
			HomeBasedTours = 0;
			WorkBasedTours = 0;

			JointTours = 0;
			EscortJointTours = 0;
			PersonalBusinessJointTours = 0;
			ShoppingJointTours = 0;
			MealJointTours = 0;
			SocialJointTours = 0;
			RecreationJointTours = 0;
			MedicalJointTours = 0;

			EscortFullHalfTours = 0;

			HasMandatoryTourToUsualLocation = false;
			WorksAtHomeFlag = 0;
		
		}

		private IEnumerable<ITour> LoadToursFromFile() {
			return Global.Kernel.Get<TourPersistenceFactory>().TourPersister.Seek(_personDay.Id, "person_day_fk");
		}

		protected IEnumerable<ITourWrapper> CreateToursByPurpose(int purpose, int totalTours) {
			var data = new List<ITourWrapper>();

			for (var i = 0; i < totalTours; i++) {
				var tour = CreateTour(Constants.AddressType.HOME, Household.ResidenceParcelId, Household.ResidenceZoneKey, purpose);

				//tour.DestinationPurpose = purpose;

				data.Add(tour);
			}

			return data;
		}

		protected virtual ITourWrapper CreateTour(int originAddressType, int originParcelId, int originZoneKey, int purpose) {
			return new TourWrapper(new Tour {
				Id = _personDay.Id*10 + GetNextTourSequence(), // ++NextTourId,
				PersonId = _personDay.PersonId,
				PersonDayId = _personDay.Id,
				HouseholdId = _personDay.HouseholdId,
				PersonSequence = _personDay.PersonSequence,
				Day = _personDay.Day,
				Sequence = GetCurrentTourSequence(),
				OriginAddressType = originAddressType,
				OriginParcelId = originParcelId,
				OriginZoneKey = originZoneKey,
				OriginDepartureTime = 180,
				DestinationArrivalTime = 180,
				DestinationDepartureTime = 180,
				OriginArrivalTime = 180,
				DestinationPurpose = purpose,
				PathType = 1,
				ExpansionFactor = Household.ExpansionFactor
			}, this, purpose);
		}

		public virtual ITourWrapper GetEscortTour(int originAddressType, int originParcelId, int originZoneKey){
			ITourWrapper tour = CreateTour (originAddressType, originParcelId, originZoneKey, Constants.Purpose.ESCORT);
			_personDay.EscortTours++;
			Tours.Add(tour);
			return tour;
		}

		public virtual ITourWrapper GetNewTour(int originAddressType, int originParcelId, int originZoneKey, int purpose){
			ITourWrapper tour = CreateTour (originAddressType, originParcelId, originZoneKey, purpose);
			Tours.Add(tour);
			switch(purpose){
				case Constants.Purpose.ESCORT:
					_personDay.EscortTours++;
					break;
				case Constants.Purpose.PERSONAL_BUSINESS:
					_personDay.PersonalBusinessTours++;
					break;
				case Constants.Purpose.SHOPPING:
					_personDay.ShoppingTours++;
					break;
				case Constants.Purpose.MEAL:
					_personDay.MealTours++;
					break;
				case Constants.Purpose.SOCIAL:
					_personDay.SocialTours++;
					break;
				case Constants.Purpose.RECREATION:
					_personDay.RecreationTours++;
					break;
				case Constants.Purpose.MEDICAL:
					_personDay.MedicalTours++;
					break;
				default:
					break;
			}
			return tour;
		}

		public int GetNextTourSequence() {
			return ++_tourSequence;
		}

		public int GetCurrentTourSequence() {
			return _tourSequence;
		}

		public virtual void SetHomeBasedNonMandatoryTours() {
			HomeBasedTours = TotalCreatedTours;
			EscortTours = CreatedEscortTours;
			PersonalBusinessTours = CreatedPersonalBusinessTours;
			ShoppingTours = CreatedShoppingTours;
			MealTours = CreatedMealTours;
			SocialTours = CreatedSocialTours;
			RecreationTours = CreatedRecreationTours;
			MedicalTours = CreatedMedicalTours;
		}


		// utility/export methods

		public void Export() {
			Global.Kernel.Get<PersonDayPersistenceFactory>().PersonDayPersister.Export(_personDay);
		}

		public static void Close() {
			Global.Kernel.Get<PersonDayPersistenceFactory>().PersonDayPersister.Dispose();
		}

		public override string ToString() {
			var builder = new StringBuilder();

			builder.AppendLine(string.Format("Person Day ID: {0}, Person ID: {1}",
				_personDay.Id,
				_personDay.PersonId));

			builder.AppendLine(string.Format("Household ID: {0}, Person Sequence: {1}, Day: {2}",
				_personDay.HouseholdId,
				_personDay.PersonSequence,
				_personDay.Day));

			builder.AppendLine(string.Format("Work Tours: {0}", _personDay.WorkTours));
			builder.AppendLine(string.Format("School Tours: {0}", _personDay.SchoolTours));
			builder.AppendLine(string.Format("Escort Tours: {0}", _personDay.EscortTours));
			builder.AppendLine(string.Format("Personal Business Tours: {0}", _personDay.PersonalBusinessTours));
			builder.AppendLine(string.Format("Shopping Tours: {0}", _personDay.ShoppingTours));
			builder.AppendLine(string.Format("Meal Tours: {0}", _personDay.MealTours));
			builder.AppendLine(string.Format("Social Tours: {0}", _personDay.SocialTours));

			builder.AppendLine(string.Format("Work Stops: {0}", _personDay.WorkStops));
			builder.AppendLine(string.Format("School Stops: {0}", _personDay.SchoolStops));
			builder.AppendLine(string.Format("Escort Stops: {0}", _personDay.EscortStops));
			builder.AppendLine(string.Format("Personal Business Stops: {0}", _personDay.PersonalBusinessStops));
			builder.AppendLine(string.Format("Shopping Stops: {0}", _personDay.ShoppingStops));
			builder.AppendLine(string.Format("Meal Stops: {0}", _personDay.MealStops));
			builder.AppendLine(string.Format("Social Stops: {0}", _personDay.SocialStops));

			return builder.ToString();
		}
	}
}