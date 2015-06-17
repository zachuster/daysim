using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface IFullHalfTourWrapper 
	{
		IHouseholdWrapper Household { get; }
		IHouseholdDayWrapper HouseholdDay { get; }
		int Id { get; }

		int Sequence { get; set; }

		int Direction { get; set; }

		int Participants { get; set; }

		int PersonSequence1 { get; set; }

		int TourSequence1 { get; set; }

		int PersonSequence2 { get; set; }

		int TourSequence2 { get; set; }

		int PersonSequence3 { get; set; }

		int TourSequence3 { get; set; }

		int PersonSequence4 { get; set; }

		int TourSequence4 { get; set; }

		int PersonSequence5 { get; set; }

		int TourSequence5 { get; set; }

		int PersonSequence6 { get; set; }

		int TourSequence6 { get; set; }

		int PersonSequence7 { get; set; }

		int TourSequence7 { get; set; }

		int PersonSequence8 { get; set; }

		int TourSequence8 { get; set; }

		// flags, choice model properties, etc.

		bool Paired { get; set; }
		void SetParticipantTourSequence(ITourWrapper participantTour);
		void Export();
	}
}
