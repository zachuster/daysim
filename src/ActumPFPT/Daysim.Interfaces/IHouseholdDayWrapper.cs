using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface IHouseholdDayWrapper 
	{
		int AttemptedSimulations { get; set; }

		IHouseholdWrapper Household { get; }

		List<IJointTourWrapper> JointToursList { get; }

		List<IFullHalfTourWrapper> FullHalfToursList { get; }

		List<IPartialHalfTourWrapper> PartialHalfToursList { get; }

		List<IPersonDayWrapper> PersonDays { get; }

		int JointTours
		{
			get; set; }

		int FullHalfTours
		{
			get; set; }

		int PartialHalfTours
		{
			get; set; }

		// flags, choice model properties, etc.

		bool IsMissingData { get; set; }

		bool IsValid { get; set; }
		
		IJointTourWrapper CreateJointTour(IHouseholdDayWrapper householdDay, IEnumerable<IPersonDayWrapper> orderedPersonDays,
		                                  int[] participants, int purpose);
			


		IFullHalfTourWrapper CreateFullHalfTour(IHouseholdDayWrapper householdDay,
		                                                      IEnumerable<IPersonDayWrapper> orderedPersonDays,
		                                                      int[] participants, int direction);

		IPartialHalfTourWrapper CreatePartialHalfTour(IHouseholdDayWrapper householdDay, IEnumerable<IPersonDayWrapper> orderedPersonDays, int[] participants, int[] pickOrder, double[] distanceFromChauffeur, int direction);
			

		void Reset();
		void Export();
	}
}
