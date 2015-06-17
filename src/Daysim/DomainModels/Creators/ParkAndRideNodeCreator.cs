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
	public class ParkAndRideNodeCreator<TWrapper, TModel> : IParkAndRideNodeCreator where TWrapper : IParkAndRideNodeWrapper where TModel : IParkAndRideNode, new() {
		IParkAndRideNode IParkAndRideNodeCreator.CreateModel() {
			return CreateModel();
		}

		private static TModel CreateModel() {
			return new TModel();
		}

		IParkAndRideNodeWrapper IParkAndRideNodeCreator.CreateWrapper(IParkAndRideNode parkAndRideNode) {
			return CreateWrapper(parkAndRideNode);
		}

		private static TWrapper CreateWrapper(IParkAndRideNode parkAndRideNode) {
			var type = typeof (TWrapper);
			var instance = Activator.CreateInstance(type, parkAndRideNode);

			return (TWrapper) instance;
		}
	}
}