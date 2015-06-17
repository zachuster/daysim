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
using Daysim.DomainModels;
using Daysim.DomainModels.Actum;
using Daysim.Framework.Core;
using Daysim.Interfaces;

namespace Daysim.Factories.Actum {
	public class ActumTourWrapperCreator : ITourWrapperCreator
	{
		public ITourWrapper CreateWrapper(ITour tour, IPersonDayWrapper personDay, int purpose = Constants.Purpose.PERSONAL_BUSINESS, bool suppressRandomVOT = false)
		{
			return new ActumTourWrapper(tour, personDay, purpose, suppressRandomVOT);
		}

		public ITourWrapper CreateSubWrapper(ITour subtour, ITourWrapper tour, int purpose = Constants.Purpose.PERSONAL_BUSINESS, bool suppressRandomVOT = false)
		{
			return new ActumTourWrapper(subtour, tour, purpose, suppressRandomVOT);
		}

		public ITourWrapper GetEntityForNestedModel(IPersonWrapper person, IPersonDayWrapper personDay, ICondensedParcel originParcel, ICondensedParcel destinationParcel, int destinationArrivalTime, int destinationDepartureTime, int destinationPurpose)
		{
			return new ActumTourWrapper(person as ActumPersonWrapper, personDay as ActumPersonDayWrapper, originParcel, destinationParcel, destinationArrivalTime,
			                            destinationDepartureTime, destinationPurpose);
		}
	}
}
