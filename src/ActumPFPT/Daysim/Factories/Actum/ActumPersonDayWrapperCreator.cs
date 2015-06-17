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
using Daysim.Interfaces;

namespace Daysim.Factories.Actum {
	public class ActumPersonDayWrapperCreator : IPersonDayWrapperCreator
	{
		public IPersonDayWrapper CreateWrapper (IPersonDay personDay, IPersonWrapper personWrapper, IHouseholdDayWrapper householdDayWrapper)
		{
			return new ActumPersonDayWrapper(personDay as ActumPersonDay, personWrapper as ActumPersonWrapper, householdDayWrapper as ActumHouseholdDayWrapper);
		}
	}
}
