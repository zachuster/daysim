﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Creators;
using Daysim.Framework.DomainModels.Models;
using Daysim.Framework.DomainModels.Wrappers;
using Daysim.Framework.Factories;

namespace Daysim.DomainModels.Creators {
	[UsedImplicitly]
	[Factory(Factory.WrapperFactory, Category = Category.Creator)]
	public class TourCreator<TWrapper, TModel> : ITourCreator where TWrapper : ITourWrapper where TModel : ITour, new() {
		ITour ITourCreator.CreateModel() {
			return CreateModel();
		}

		private static TModel CreateModel() {
			return new TModel();
		}

		ITourWrapper ITourCreator.CreateWrapper(IPersonWrapper personWrapper, IPersonDayWrapper personDayWrapper, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int destinationArrivalTime, int destinationDepartureTime, int destinationPurpose) {
			return CreateWrapper(personWrapper, personDayWrapper, originParcel, destinationParcel, destinationArrivalTime, destinationDepartureTime, destinationPurpose);
		}

		private static TWrapper CreateWrapper(IPersonWrapper personWrapper, IPersonDayWrapper personDayWrapper, IParcelWrapper originParcel, IParcelWrapper destinationParcel, int destinationArrivalTime, int destinationDepartureTime, int destinationPurpose) {
			var type = typeof (TWrapper);
			var instance = Activator.CreateInstance(type, new TModel(), personWrapper, personDayWrapper, originParcel, destinationParcel, destinationArrivalTime, destinationDepartureTime, destinationPurpose);

			return (TWrapper) instance;
		}

		ITourWrapper ITourCreator.CreateWrapper(ITour subtour, ITourWrapper tourWrapper, int purpose, bool suppressRandomVOT) {
			return CreateWrapper(subtour, tourWrapper, purpose, suppressRandomVOT);
		}

		private static TWrapper CreateWrapper(ITour subtour, ITourWrapper tourWrapper, int purpose, bool suppressRandomVOT) {
			var type = typeof (TWrapper);
			var instance = Activator.CreateInstance(type, subtour, tourWrapper, purpose, suppressRandomVOT);

			return (TWrapper) instance;
		}

		ITourWrapper ITourCreator.CreateWrapper(ITour tour, IPersonDayWrapper personDayWrapper, int purpose, bool suppressRandomVOT) {
			return CreateWrapper(tour, personDayWrapper, purpose, suppressRandomVOT);
		}

		private static TWrapper CreateWrapper(ITour tour, IPersonDayWrapper personDayWrapper, int purpose, bool suppressRandomVOT) {
			var type = typeof (TWrapper);
			var instance = Activator.CreateInstance(type, tour, personDayWrapper, purpose, suppressRandomVOT);

			return (TWrapper) instance;
		}
	}
}