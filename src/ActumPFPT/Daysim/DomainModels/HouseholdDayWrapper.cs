// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


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
	public class HouseholdDayWrapper : IHouseholdDayWrapper {

		public int AttemptedSimulations { get; set; }

		//public static int NextFullHalfTourId;
		protected int _fullHalfTourSequence;
		//public static int NextPartialHalfTourId;
		protected int _partialHalfTourSequence;
		//public static int NextJointTourId;
		protected int _jointTourSequence;

		protected static Reader<JointTour> _jointTourReader;
		protected static Reader<FullHalfTour> _fullHalfTourReader;
		protected static Reader<PartialHalfTour> _partialHalfTourReader;
		//protected static int _nextPersonDayId;

		//protected readonly IHouseholdDay _householdDay;  TODO:  John M, I needed to change this from read only so I could re-create it in case of isValid == false.  Is this okay?
		protected IHouseholdDay _householdDay;
		protected static PersonDayWrapperFactory _personDayWrapperFactory;

		public HouseholdDayWrapper(IHouseholdDay householdDay, IHouseholdWrapper householdWrapper, PersonDayWrapperFactory factory = null) {

			_personDayWrapperFactory = factory ?? Global.Kernel.Get<PersonDayWrapperFactory>();

			_householdDay = householdDay;

			Household = householdWrapper;

			_householdDay.ExpansionFactor = Household.ExpansionFactor;

			SetPersonDays();
			if (Global.UseJointTours) {
				SetJointTours();
				SetFullHalfTours();
				SetPartialHalfTours();
			}
		}


		// relations

		public IHouseholdWrapper Household { get; private set; }

		public List<IJointTourWrapper> JointToursList { get; private set; }

		public List<IFullHalfTourWrapper> FullHalfToursList { get; private set; }

		public List<IPartialHalfTourWrapper> PartialHalfToursList { get; private set; }

		public List<IPersonDayWrapper> PersonDays { get; private set; }

		// domain model properies

		public int JointTours {
			get { return _householdDay.JointTours; }
			set { _householdDay.JointTours = value; }
		}

		public int FullHalfTours {
			get { return _householdDay.FullHalfTours; }
			set { _householdDay.FullHalfTours = value; }
		}

		public int PartialHalfTours {
			get { return _householdDay.PartialHalfTours; }
			set { _householdDay.PartialHalfTours = value; }
		}

		// flags, choice model properties, etc.

		public bool IsMissingData { get; set; }

		public bool IsValid { get; set; }

		// wrapper methods

		private void SetJointTours() {
			JointToursList =
				Global.Configuration.IsInEstimationMode && Global.UseJointTours
				 ? LoadJointToursFromFile().Select(jointTour => new JointTourWrapper(jointTour, this) as IJointTourWrapper).ToList()
				 : new List<IJointTourWrapper>();
		}

		private IEnumerable<JointTour> LoadJointToursFromFile() {
			if (_jointTourReader == null) {
				_jointTourReader = Global.Kernel.Get<Reader<JointTour>>();
			}

			return _jointTourReader.Seek(_householdDay.Id, "household_day_fk");
		}

		public virtual IJointTourWrapper CreateJointTour(IHouseholdDayWrapper householdDay, IEnumerable<IPersonDayWrapper> orderedPersonDays, int[] participants, int purpose) {
			householdDay.JointTours++;

			int j = 0;
			int[] personSequence = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			int i = 0;
			foreach (PersonDayWrapper personDay in orderedPersonDays) {
				i++;
				if (i <= 5) {
					if (participants[i] == 1) {
						j++;
						personSequence[j] = personDay.Person.Sequence;
					}
				}
			}

			JointTourWrapper jointTour = new JointTourWrapper(new JointTour {
				Sequence = ++_jointTourSequence,
				Id = _householdDay.Id * 10 + _jointTourSequence,  //++NextJointTourId,
				HouseholdDayId = _householdDay.Id,
				HouseholdId = _householdDay.HouseholdId,
				Day = _householdDay.Day,
				MainPurpose = purpose,
				Participants = participants[7],
				PersonSequence1 = personSequence[1],
				TourSequence1 = 0,
				PersonSequence2 = personSequence[2],
				TourSequence2 = 0,
				PersonSequence3 = personSequence[3],
				TourSequence3 = 0,
				PersonSequence4 = personSequence[4],
				TourSequence4 = 0,
				PersonSequence5 = personSequence[5],
				TourSequence5 = 0,
				PersonSequence6 = personSequence[6],
				TourSequence6 = 0,
				PersonSequence7 = personSequence[7],
				TourSequence7 = 0,
				PersonSequence8 = personSequence[8],
				TourSequence8 = 0
			}, householdDay);
			householdDay.JointToursList.Add(jointTour);
			return jointTour;
		}



		private void SetFullHalfTours() {
			FullHalfToursList =
				Global.Configuration.IsInEstimationMode
					? LoadFullHalfToursFromFile().Select(jointTour => new FullHalfTourWrapper(jointTour, this) as IFullHalfTourWrapper).ToList()
					: new List<IFullHalfTourWrapper>();
		}

		private IEnumerable<FullHalfTour> LoadFullHalfToursFromFile() {
			if (_fullHalfTourReader == null) {
				_fullHalfTourReader = Global.Kernel.Get<Reader<FullHalfTour>>();
			}

			return _fullHalfTourReader.Seek(_householdDay.Id, "household_day_fk");
		}


		public virtual IFullHalfTourWrapper CreateFullHalfTour(IHouseholdDayWrapper householdDay, IEnumerable<IPersonDayWrapper> orderedPersonDays, int[] participants, int direction) {
			householdDay.FullHalfTours++;
			int j = 0;
			int[] personSequence = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			int i = 0;
			foreach (PersonDayWrapper personDay in orderedPersonDays) {
				i++;
				if (i <= 5) {
					if (participants[i] == 1) {
						j++;
						personSequence[j] = personDay.Person.Sequence;
					}
				}
			}
			FullHalfTourWrapper fullHalfTour = new FullHalfTourWrapper(new FullHalfTour {
				Sequence = ++_fullHalfTourSequence,
				Id = _householdDay.Id * 10 + _fullHalfTourSequence, // ++NextFullHalfTourId,
				HouseholdDayId = _householdDay.Id,
				HouseholdId = _householdDay.HouseholdId,
				Day = _householdDay.Day,
				Direction = direction,
				Participants = participants[7],
				PersonSequence1 = personSequence[1],
				TourSequence1 = 0,
				PersonSequence2 = personSequence[2],
				TourSequence2 = 0,
				PersonSequence3 = personSequence[3],
				TourSequence3 = 0,
				PersonSequence4 = personSequence[4],
				TourSequence4 = 0,
				PersonSequence5 = personSequence[5],
				TourSequence5 = 0,
				PersonSequence6 = personSequence[6],
				TourSequence6 = 0,
				PersonSequence7 = personSequence[7],
				TourSequence7 = 0,
				PersonSequence8 = personSequence[8],
				TourSequence8 = 0
			}, householdDay);
			householdDay.FullHalfToursList.Add(fullHalfTour);
			return fullHalfTour;
		}


		private void SetPartialHalfTours() {
			PartialHalfToursList =
				Global.Configuration.IsInEstimationMode
					? LoadPartialHalfToursFromFile().Select(jointTour => new PartialHalfTourWrapper(jointTour, this) as IPartialHalfTourWrapper).ToList()
					: new List<IPartialHalfTourWrapper>();
		}

		private IEnumerable<PartialHalfTour> LoadPartialHalfToursFromFile() {
			if (_partialHalfTourReader == null) {
				_partialHalfTourReader = Global.Kernel.Get<Reader<PartialHalfTour>>();
			}

			return _partialHalfTourReader.Seek(_householdDay.Id, "household_day_fk");
		}

		public virtual IPartialHalfTourWrapper CreatePartialHalfTour(IHouseholdDayWrapper householdDay, IEnumerable<IPersonDayWrapper> orderedPersonDays, int[] participants, int[] pickOrder, double[] distanceFromChauffeur, int direction) {
			householdDay.PartialHalfTours++;

			int[] personSequence = new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 };
			int i = 0;
			foreach (PersonDayWrapper personDay in orderedPersonDays) {
				i++;
				for (int i2 = 0; i2 < 5; i2++) {
					if (pickOrder[i2] == i) {
						personSequence[i2] = personDay.Person.Sequence;
					}
				}
			}
			PartialHalfTourWrapper partialHalfTour = new PartialHalfTourWrapper(new PartialHalfTour {
				Sequence = ++_partialHalfTourSequence,
				Id = _householdDay.Id * 10 + _partialHalfTourSequence, // ++NextPartialHalfTourId,
				HouseholdDayId = _householdDay.Id,
				HouseholdId = _householdDay.HouseholdId,
				Day = _householdDay.Day,
				Direction = direction,
				Participants = participants[7],
				PersonSequence1 = distanceFromChauffeur[0] > 998 ? 0 : personSequence[0],
				TourSequence1 = 0,
				PersonSequence2 = distanceFromChauffeur[1] > 998 ? 0 : personSequence[1],
				TourSequence2 = 0,
				PersonSequence3 = distanceFromChauffeur[2] > 998 ? 0 : personSequence[2],
				TourSequence3 = 0,
				PersonSequence4 = distanceFromChauffeur[3] > 998 ? 0 : personSequence[3],
				TourSequence4 = 0,
				PersonSequence5 = distanceFromChauffeur[4] > 998 ? 0 : personSequence[4],
				TourSequence5 = 0,
				PersonSequence6 = 0, //distanceFromChauffeur[5] > 998 ? 0 : personSequence[5],
				TourSequence6 = 0,
				PersonSequence7 = 0, //distanceFromChauffeur[6] > 998 ? 0 : personSequence[6],
				TourSequence7 = 0,
				PersonSequence8 = 0, //distanceFromChauffeur[7] > 998 ? 0 : personSequence[7],
				TourSequence8 = 0,
			}, householdDay);
			householdDay.PartialHalfToursList.Add(partialHalfTour);
			return partialHalfTour;
		}



		private void SetPersonDays() {
			PersonDays =
				Global.Configuration.IsInEstimationMode
					? GetPersonDaySurveyData()
					: GetPersonDaySimulatedData();
		}

		private List<IPersonDayWrapper> GetPersonDaySurveyData() {
			var data = new List<IPersonDayWrapper>();
			var personDaysForHousehold = LoadPersonDaysFromFile().ToList();

			foreach (var person in Household.Persons) {
				var personId = person.Id;
				var personDays = personDaysForHousehold.Where(pd => pd.PersonId == personId);

				data.AddRange(personDays.Select(personDay => _personDayWrapperFactory.PersonDayWrapperCreator.CreateWrapper(personDay, person, this)));
			}

			return data;
		}

		private List<IPersonDayWrapper> GetPersonDaySimulatedData() {
			return Household.Persons.Select(CreatePersonDay).ToList();
		}

		private IEnumerable<IPersonDay> LoadPersonDaysFromFile() {
			return Global.Kernel.Get<PersonDayPersistenceFactory>().PersonDayPersister.Seek(_householdDay.Id, "household_day_fk");
		}

		protected virtual IPersonDayWrapper CreatePersonDay(IPersonWrapper person) {
			return _personDayWrapperFactory.PersonDayWrapperCreator.CreateWrapper(new PersonDay {
				Id = person.Id, // ++_nextPersonDayId,
				PersonId = person.Id,
				HouseholdDayId = _householdDay.Id,
				HouseholdId = _householdDay.HouseholdId,
				PersonSequence = person.Sequence,
				Day = _householdDay.Day
			}, person, this);
		}

		protected virtual void CreateNewHouseholdDay(IHouseholdDay temp) {
			_householdDay = new HouseholdDay {
				Id = temp.Id,
				HouseholdId = temp.HouseholdId,
				Day = temp.Day,
				DayOfWeek = temp.DayOfWeek,
				ExpansionFactor = temp.ExpansionFactor
			};
		}

		public virtual void Reset() {
			//TimeWindow = new TimeWindow();

			var temp = _householdDay;

			CreateNewHouseholdDay(temp);

			_fullHalfTourSequence = 0;
			_partialHalfTourSequence = 0;
			_jointTourSequence = 0;

			SetJointTours();
			SetPartialHalfTours();
			SetFullHalfTours();

			foreach (var personDay in PersonDays)
			{
				personDay.Reset();
			}

		}


		// utility/export methods

		public void Export() {
			Global.Kernel.Get<HouseholdDayPersistenceFactory>().HouseholdDayPersister.Export(_householdDay);
		}

		public static void Close() {
			Global.Kernel.Get<HouseholdDayPersistenceFactory>().HouseholdDayPersister.Dispose();
		}

		public override string ToString() {
			var builder = new StringBuilder();

			builder.AppendLine(string.Format("Household Day ID: {0}",
				_householdDay.Id));

			builder.AppendLine(string.Format("Household ID: {0}, Day: {1}",
				_householdDay.HouseholdId,
				_householdDay.Day));

			return builder.ToString();
		}
	}
}