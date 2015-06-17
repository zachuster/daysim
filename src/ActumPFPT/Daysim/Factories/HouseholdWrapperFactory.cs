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
using Daysim.Framework.Core;
using Daysim.Interfaces;

namespace Daysim.Factories {
	public class HouseholdWrapperFactory 
	{
		private readonly string _key = Global.DataType;

		private readonly Dictionary<String, IHouseholdWrapperCreator> _creators = new Dictionary<string, IHouseholdWrapperCreator>();

		public IHouseholdWrapperCreator HouseholdWrapperCreator { get; private set; }

		public void Register(String key, IHouseholdWrapperCreator value)
		{
			_creators.Add(key, value);
		}

		public void Initialize()
		{
			HouseholdWrapperCreator = _creators[_key];
		}
	}
}
