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
using Daysim.Framework.Core;
using Daysim.Interfaces;

namespace Daysim.DomainModels.Actum {
	public class ActumTourWrapper : TourWrapper
	{
		public ActumTourWrapper(ITour subtour, ITourWrapper tour, int purpose = Constants.Purpose.PERSONAL_BUSINESS, bool suppressRandomVOT = false) : base(subtour, tour, purpose, suppressRandomVOT)
		{
		}

		public ActumTourWrapper(ITour tour, IPersonDayWrapper personDay, int purpose = Constants.Purpose.PERSONAL_BUSINESS, bool suppressRandomVOT = false) : base(tour, personDay, purpose, suppressRandomVOT)
		{
		}

		public ActumTourWrapper(PersonWrapper person, PersonDayWrapper personDay, ICondensedParcel originParcel, ICondensedParcel destinationParcel, int destinationArrivalTime, int destinationDepartureTime, int destinationPurpose) : base(person, personDay, originParcel, destinationParcel, destinationArrivalTime, destinationDepartureTime, destinationPurpose)
		{
		}


		public bool IsBusinessPurpose {
			get { return DestinationPurpose == Constants.Purpose.BUSINESS; }
		}

		public bool IsHovDriverMode {
			get { return Mode == Constants.Mode.HOVDRIVER; }
		}

		public bool IsHovPassengerMode {
			get { return Mode == Constants.Mode.HOVPASSENGER; }
		}


		public override ITourWrapper CreateSubtour(int originAddressType, int originParcelId, int originZoneKey, int destinationPurpose) {
			TotalSubtours++;

			return new ActumTourWrapper(new Tour {
				Id =_tour.PersonDayId*100 + PersonDay.GetNextTourSequence(),//++PersonDayWrapper.NextTourId,
				PersonId = _tour.PersonId,
				PersonDayId = _tour.PersonDayId,
				HouseholdId = _tour.HouseholdId,
				PersonSequence = _tour.PersonSequence,
				Day = _tour.Day,
				Sequence = PersonDay.GetCurrentTourSequence(),
				OriginAddressType = originAddressType,
				OriginParcelId = originParcelId,
				OriginZoneKey = originZoneKey,
				DestinationPurpose = destinationPurpose,
				OriginDepartureTime = 180,
				DestinationArrivalTime = 180,
				DestinationDepartureTime = 180,
				OriginArrivalTime = 180,
				PathType = 1,
				ExpansionFactor = Household.ExpansionFactor
			}, this, destinationPurpose);
		}

			public override int VotALSegment {
			get {
				var segment = (DestinationPurpose == Constants.Purpose.WORK || DestinationPurpose == Constants.Purpose.SCHOOL || DestinationPurpose == Constants.Purpose.ESCORT)
				              	? Constants.VotALSegment.MEDIUM
				              	: (DestinationPurpose == Constants.Purpose.BUSINESS)
				              	  	? Constants.VotALSegment.HIGH
				              	  	: Constants.VotALSegment.LOW;
				return segment;
			}
		}

	}
}
