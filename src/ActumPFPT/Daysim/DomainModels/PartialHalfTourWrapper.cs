// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System.Text;
using Daysim.ChoiceModels;
using Daysim.Factories;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;
using Daysim.ModelRunners;

namespace Daysim.DomainModels {
	public class PartialHalfTourWrapper : IPartialHalfTourWrapper {
		private static PersisterWithHDF5<PartialHalfTour> _persister;

		private readonly PartialHalfTour _partialHalfTour;

		public PartialHalfTourWrapper(PartialHalfTour partialHalfTour, IHouseholdDayWrapper householdDay, PersisterWithHDF5<PartialHalfTour> persister = null ) 
		{
			if (persister == null)
			{
				if (_persister == null)
					_persister = new PersisterWithHDF5<PartialHalfTour>(Global.GetOutputPath(Global.Configuration.OutputPartialHalfTourPath), Global.Configuration.OutputPartialHalfTourDelimiter);
			}
			else
			{
				_persister = persister;
			}
			_partialHalfTour = partialHalfTour;

			Household = householdDay.Household;
			HouseholdDay = householdDay;
		}


		// relations

		public IHouseholdWrapper Household { get; private set; }

		public IHouseholdDayWrapper HouseholdDay { get; private set; }

		// domain model properies

		public int Id {
			get { return _partialHalfTour.Id; }
		}

		public int Sequence {
			get { return _partialHalfTour.Sequence; }
			set { _partialHalfTour.Sequence = value; }
		}

		public int Direction {
			get { return _partialHalfTour.Direction; }
			set { _partialHalfTour.Direction = value; }
		}

		public int Participants {
			get { return _partialHalfTour.Participants; }
			set { _partialHalfTour.Participants = value; }
		}

		public int PersonSequence1 { 
			get { return _partialHalfTour.PersonSequence1; }
			set { _partialHalfTour.PersonSequence1 = value; }
		}

		public int TourSequence1 { 
			get { return _partialHalfTour.TourSequence1; }
			set { _partialHalfTour.TourSequence1 = value; }
		}

		public int PersonSequence2 { 
			get { return _partialHalfTour.PersonSequence2; }
			set { _partialHalfTour.PersonSequence2 = value; }
		}

		public int TourSequence2 { 
			get { return _partialHalfTour.TourSequence2; }
			set { _partialHalfTour.TourSequence2 = value; }
		}

		public int PersonSequence3 { 
			get { return _partialHalfTour.PersonSequence3; }
			set { _partialHalfTour.PersonSequence3 = value; }
		}

		public int TourSequence3 { 
			get { return _partialHalfTour.TourSequence3; }
			set { _partialHalfTour.TourSequence3 = value; }
		}

		public int PersonSequence4 { 
			get { return _partialHalfTour.PersonSequence4; }
			set { _partialHalfTour.PersonSequence4 = value; }
		}

		public int TourSequence4 { 
			get { return _partialHalfTour.TourSequence4; }
			set { _partialHalfTour.TourSequence4 = value; }
		}

		public int PersonSequence5 { 
			get { return _partialHalfTour.PersonSequence5; }
			set { _partialHalfTour.PersonSequence5 = value; }
		}

		public int TourSequence5 { 
			get { return _partialHalfTour.TourSequence5; }
			set { _partialHalfTour.TourSequence5 = value; }
		}

		public int PersonSequence6 { 
			get { return _partialHalfTour.PersonSequence6; }
			set { _partialHalfTour.PersonSequence6 = value; }
		}

		public int TourSequence6 { 
			get { return _partialHalfTour.TourSequence6; }
			set { _partialHalfTour.TourSequence6 = value; }
		}

		public int PersonSequence7 { 
			get { return _partialHalfTour.PersonSequence7; }
			set { _partialHalfTour.PersonSequence7 = value; }
		}

		public int TourSequence7 { 
			get { return _partialHalfTour.TourSequence7; }
			set { _partialHalfTour.TourSequence7 = value; }
		}

		public int PersonSequence8 { 
			get { return _partialHalfTour.PersonSequence8; }
			set { _partialHalfTour.PersonSequence8 = value; }
		}

		public int TourSequence8 { 
			get { return _partialHalfTour.TourSequence8; }
			set { _partialHalfTour.TourSequence8 = value; }
		}

		// wrapper methods

		public void SetParticipantTourSequence(ITourWrapper participantTour) {
			if (_partialHalfTour.PersonSequence1 == participantTour.Person.Sequence) {
			_partialHalfTour.TourSequence1 = participantTour.Sequence;
			}
			else if (_partialHalfTour.PersonSequence2 == participantTour.Person.Sequence) {
			_partialHalfTour.TourSequence2 = participantTour.Sequence;
			}
			else if (_partialHalfTour.PersonSequence3 == participantTour.Person.Sequence) {
			_partialHalfTour.TourSequence3 = participantTour.Sequence;
			}
			else if (_partialHalfTour.PersonSequence4 == participantTour.Person.Sequence) {
			_partialHalfTour.TourSequence4 = participantTour.Sequence;
			}
			else if (_partialHalfTour.PersonSequence5 == participantTour.Person.Sequence) {
			_partialHalfTour.TourSequence5 = participantTour.Sequence;
			}
			else if (_partialHalfTour.PersonSequence6 == participantTour.Person.Sequence) {
			_partialHalfTour.TourSequence6 = participantTour.Sequence;
			}
			else if (_partialHalfTour.PersonSequence7 == participantTour.Person.Sequence) {
			_partialHalfTour.TourSequence7 = participantTour.Sequence;
			}
			else if (_partialHalfTour.PersonSequence8 == participantTour.Person.Sequence) {
			_partialHalfTour.TourSequence8 = participantTour.Sequence;
			}

		}

		// flags, choice model properties, etc.

		public bool Paired { get; set; }

		// utility/export methods

		public void Export() {
			_persister.Export(_partialHalfTour);
			ChoiceModelFactory.PartialHalfTourFileRecordsWritten++;
		}

		public static void Close() {
			if (_persister != null) {
				_persister.Dispose();
			}
		}

		public override string ToString() {
			var builder = new StringBuilder();

			builder.AppendLine(string.Format("Joint Tour ID: {0}",
				_partialHalfTour.Id));

			builder.AppendLine(string.Format("Household ID: {0}, Day: {1}",
				_partialHalfTour.HouseholdId,
				_partialHalfTour.Day));

			return builder.ToString();
		}
	}
}
