// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System.Text;
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;
using Daysim.ModelRunners;

namespace Daysim.DomainModels {
	public class FullHalfTourWrapper : IFullHalfTourWrapper {
		//private static Exporter<FullHalfTour> _exporter;
		private static PersisterWithHDF5<FullHalfTour> _persister;

		private readonly FullHalfTour _fullHalfTour;

		public FullHalfTourWrapper(FullHalfTour fullHalfTour, IHouseholdDayWrapper householdDay, PersisterWithHDF5<FullHalfTour> persister = null )
		{
			if (persister != null)
				_persister = persister;
			if (_persister == null) {
				_persister = new PersisterWithHDF5<FullHalfTour>(Global.GetOutputPath(Global.Configuration.OutputFullHalfTourPath), Global.Configuration.OutputFullHalfTourDelimiter);
			}

			_fullHalfTour = fullHalfTour;

			Household = householdDay.Household;
			HouseholdDay = householdDay;
		}

		public static void SetPersister(PersisterWithHDF5<FullHalfTour> persister)
		{
			_persister = persister;
		}


		// relations

		public IHouseholdWrapper Household { get; private set; }

		public IHouseholdDayWrapper HouseholdDay { get; private set; }


		// domain model properies

		public int Id {
			get { return _fullHalfTour.Id; }
		}

		public int Sequence {
			get { return _fullHalfTour.Sequence; }
			set { _fullHalfTour.Sequence = value; }
		}

		public int Direction {
			get { return _fullHalfTour.Direction; }
			set { _fullHalfTour.Direction = value; }
		}

		public int Participants {
			get { return _fullHalfTour.Participants; }
			set { _fullHalfTour.Participants = value; }
		}

		public int PersonSequence1 { 
			get { return _fullHalfTour.PersonSequence1; }
			set { _fullHalfTour.PersonSequence1 = value; }
		}

		public int TourSequence1 { 
			get { return _fullHalfTour.TourSequence1; }
			set { _fullHalfTour.TourSequence1 = value; }
		}

		public int PersonSequence2 { 
			get { return _fullHalfTour.PersonSequence2; }
			set { _fullHalfTour.PersonSequence2 = value; }
		}

		public int TourSequence2 { 
			get { return _fullHalfTour.TourSequence2; }
			set { _fullHalfTour.TourSequence2 = value; }
		}

		public int PersonSequence3 { 
			get { return _fullHalfTour.PersonSequence3; }
			set { _fullHalfTour.PersonSequence3 = value; }
		}

		public int TourSequence3 { 
			get { return _fullHalfTour.TourSequence3; }
			set { _fullHalfTour.TourSequence3 = value; }
		}

		public int PersonSequence4 { 
			get { return _fullHalfTour.PersonSequence4; }
			set { _fullHalfTour.PersonSequence4 = value; }
		}

		public int TourSequence4 { 
			get { return _fullHalfTour.TourSequence4; }
			set { _fullHalfTour.TourSequence4 = value; }
		}

		public int PersonSequence5 { 
			get { return _fullHalfTour.PersonSequence5; }
			set { _fullHalfTour.PersonSequence5 = value; }
		}

		public int TourSequence5 { 
			get { return _fullHalfTour.TourSequence5; }
			set { _fullHalfTour.TourSequence5 = value; }
		}

		public int PersonSequence6 { 
			get { return _fullHalfTour.PersonSequence6; }
			set { _fullHalfTour.PersonSequence6 = value; }
		}

		public int TourSequence6 { 
			get { return _fullHalfTour.TourSequence6; }
			set { _fullHalfTour.TourSequence6 = value; }
		}

		public int PersonSequence7 { 
			get { return _fullHalfTour.PersonSequence7; }
			set { _fullHalfTour.PersonSequence7 = value; }
		}

		public int TourSequence7 { 
			get { return _fullHalfTour.TourSequence7; }
			set { _fullHalfTour.TourSequence7 = value; }
		}

		public int PersonSequence8 { 
			get { return _fullHalfTour.PersonSequence8; }
			set { _fullHalfTour.PersonSequence8 = value; }
		}

		public int TourSequence8 { 
			get { return _fullHalfTour.TourSequence8; }
			set { _fullHalfTour.TourSequence8 = value; }
		}

		// flags, choice model properties, etc.

		public bool Paired { get; set; }

		// wrapper methods

		public void SetParticipantTourSequence(ITourWrapper participantTour) {
			if (_fullHalfTour.PersonSequence1 == participantTour.Person.Sequence) {
			_fullHalfTour.TourSequence1 = participantTour.Sequence;
			}
			else if (_fullHalfTour.PersonSequence2 == participantTour.Person.Sequence) {
			_fullHalfTour.TourSequence2 = participantTour.Sequence;
			}
			else if (_fullHalfTour.PersonSequence3 == participantTour.Person.Sequence) {
			_fullHalfTour.TourSequence3 = participantTour.Sequence;
			}
			else if (_fullHalfTour.PersonSequence4 == participantTour.Person.Sequence) {
			_fullHalfTour.TourSequence4 = participantTour.Sequence;
			}
			else if (_fullHalfTour.PersonSequence5 == participantTour.Person.Sequence) {
			_fullHalfTour.TourSequence5 = participantTour.Sequence;
			}
			else if (_fullHalfTour.PersonSequence6 == participantTour.Person.Sequence) {
			_fullHalfTour.TourSequence6 = participantTour.Sequence;
			}
			else if (_fullHalfTour.PersonSequence7 == participantTour.Person.Sequence) {
			_fullHalfTour.TourSequence7 = participantTour.Sequence;
			}
			else if (_fullHalfTour.PersonSequence8 == participantTour.Person.Sequence) {
			_fullHalfTour.TourSequence8 = participantTour.Sequence;
			}

		}


		// utility/export methods

		public void Export() {
			_persister.Export(_fullHalfTour);
			ChoiceModelFactory.FullHalfTourFileRecordsWritten++;
		}

		public static void Close() {
			if (_persister != null) {
				_persister.Dispose();
			}
		}

		public override string ToString() {
			var builder = new StringBuilder();

			builder.AppendLine(string.Format("Joint Tour ID: {0}",
				_fullHalfTour.Id));

			builder.AppendLine(string.Format("Household ID: {0}, Day: {1}",
				_fullHalfTour.HouseholdId,
				_fullHalfTour.Day));

			return builder.ToString();
		}
	}
}