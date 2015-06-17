﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using Daysim.Framework.DomainModels.Models;

namespace Daysim.Framework.DomainModels.Wrappers {
	public interface IFullHalfTourWrapper : IFullHalfTour {
		#region relations properties

		IHouseholdWrapper Household { get; }

		IHouseholdDayWrapper HouseholdDay { get; }

		#endregion

		#region flags/choice model/etc. properties

		bool Paired { get; set; }

		#endregion

		#region wrapper methods

		void SetParticipantTourSequence(ITourWrapper participantTour);

		#endregion

		#region init/utility/export methods

		void Export();

		#endregion
	}
}