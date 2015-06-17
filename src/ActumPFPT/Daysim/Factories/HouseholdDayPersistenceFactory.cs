﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
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

namespace Daysim.Factories {
	public class HouseholdDayPersistenceFactory 
	{
		private readonly string _key = Global.DataType;

		public IHouseholdDayPersister HouseholdDayPersister { get; private set; }

		private readonly Dictionary<String, IHouseholdDayPersister> _persisters = new Dictionary<string, IHouseholdDayPersister>(); 

		public void Register(String key, IHouseholdDayPersister value)
		{
			_persisters.Add(key, value);
		}

		public void Initialize()
		{
			HouseholdDayPersister = _persisters[_key];
		}
	}
}