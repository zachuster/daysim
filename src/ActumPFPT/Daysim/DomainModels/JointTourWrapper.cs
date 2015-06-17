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
	public class JointTourWrapper : IJointTourWrapper {
		private static PersisterWithHDF5<JointTour> _persister  = null;

		private readonly JointTour _jointTour;

		public JointTourWrapper(JointTour jointTour, IHouseholdDayWrapper householdDay, PersisterWithHDF5<JointTour> persister = null )
		{
			if (persister == null)
			{
				if (_persister == null)
					_persister = new PersisterWithHDF5<JointTour>(Global.GetOutputPath(Global.Configuration.OutputJointTourPath), Global.Configuration.OutputJointTourDelimiter);
			}
			else
			{
				_persister = persister;
			}

			_jointTour = jointTour;

			Household = householdDay.Household;
			HouseholdDay = householdDay;
		}


		// relations

		public IHouseholdWrapper Household { get; private set; }

		public IHouseholdDayWrapper HouseholdDay { get; private set; }


		// domain model properies

		public int Id {
			get { return _jointTour.Id; }
		}

		public int Sequence {
			get { return _jointTour.Sequence; }
			set { _jointTour.Sequence = value; }
		}

		public int MainPurpose {
			get { return _jointTour.MainPurpose; }
			set { _jointTour.MainPurpose = value; }
		}

		public int Participants {
			get { return _jointTour.Participants; }
			set { _jointTour.Participants = value; }
		}

		public int PersonSequence1 { 
			get { return _jointTour.PersonSequence1; }
			set { _jointTour.PersonSequence1 = value; }
		}

		public int TourSequence1 { 
			get { return _jointTour.TourSequence1; }
			set { _jointTour.TourSequence1 = value; }
		}

		public int PersonSequence2 { 
			get { return _jointTour.PersonSequence2; }
			set { _jointTour.PersonSequence2 = value; }
		}

		public int TourSequence2 { 
			get { return _jointTour.TourSequence2; }
			set { _jointTour.TourSequence2 = value; }
		}

		public int PersonSequence3 { 
			get { return _jointTour.PersonSequence3; }
			set { _jointTour.PersonSequence3 = value; }
		}

		public int TourSequence3 { 
			get { return _jointTour.TourSequence3; }
			set { _jointTour.TourSequence3 = value; }
		}

		public int PersonSequence4 { 
			get { return _jointTour.PersonSequence4; }
			set { _jointTour.PersonSequence4 = value; }
		}

		public int TourSequence4 { 
			get { return _jointTour.TourSequence4; }
			set { _jointTour.TourSequence4 = value; }
		}

		public int PersonSequence5 { 
			get { return _jointTour.PersonSequence5; }
			set { _jointTour.PersonSequence5 = value; }
		}

		public int TourSequence5 { 
			get { return _jointTour.TourSequence5; }
			set { _jointTour.TourSequence5 = value; }
		}

		public int PersonSequence6 { 
			get { return _jointTour.PersonSequence6; }
			set { _jointTour.PersonSequence6 = value; }
		}

		public int TourSequence6 { 
			get { return _jointTour.TourSequence6; }
			set { _jointTour.TourSequence6 = value; }
		}

		public int PersonSequence7 { 
			get { return _jointTour.PersonSequence7; }
			set { _jointTour.PersonSequence7 = value; }
		}

		public int TourSequence7 { 
			get { return _jointTour.TourSequence7; }
			set { _jointTour.TourSequence7 = value; }
		}

		public int PersonSequence8 { 
			get { return _jointTour.PersonSequence8; }
			set { _jointTour.PersonSequence8 = value; }
		}

		public int TourSequence8 { 
			get { return _jointTour.TourSequence8; }
			set { _jointTour.TourSequence8 = value; }
		}

		public ITimeWindow TimeWindow {
			get;
			set;
		}

		// wrapper methods

		public void SetParticipantTourSequence(ITourWrapper participantTour) {
			if (_jointTour.PersonSequence1 == participantTour.Person.Sequence) {
			_jointTour.TourSequence1 = participantTour.Sequence;
			}
			else if (_jointTour.PersonSequence2 == participantTour.Person.Sequence) {
			_jointTour.TourSequence2 = participantTour.Sequence;
			}
			else if (_jointTour.PersonSequence3 == participantTour.Person.Sequence) {
			_jointTour.TourSequence3 = participantTour.Sequence;
			}
			else if (_jointTour.PersonSequence4 == participantTour.Person.Sequence) {
			_jointTour.TourSequence4 = participantTour.Sequence;
			}
			else if (_jointTour.PersonSequence5 == participantTour.Person.Sequence) {
			_jointTour.TourSequence5 = participantTour.Sequence;
			}
			else if (_jointTour.PersonSequence6 == participantTour.Person.Sequence) {
			_jointTour.TourSequence6 = participantTour.Sequence;
			}
			else if (_jointTour.PersonSequence7 == participantTour.Person.Sequence) {
			_jointTour.TourSequence7 = participantTour.Sequence;
			}
			else if (_jointTour.PersonSequence8 == participantTour.Person.Sequence) {
			_jointTour.TourSequence8 = participantTour.Sequence;
			}

		}


		// utility/export methods

		public void Export() {
			_persister.Export(_jointTour);
			ChoiceModelFactory.JointTourFileRecordsWritten++;
		}

		public static void Close() {
			if (_persister != null) {
				_persister.Dispose();
			}
		}

		public override string ToString() {
			var builder = new StringBuilder();

			builder.AppendLine(string.Format("Joint Tour ID: {0}",
				_jointTour.Id));

			builder.AppendLine(string.Format("Household ID: {0}, Day: {1}",
				_jointTour.HouseholdId,
				_jointTour.Day));

			return builder.ToString();
		}
	}
}
