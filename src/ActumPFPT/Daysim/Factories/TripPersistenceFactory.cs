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

namespace Daysim.Factories {
	public class TripPersistenceFactory 
	{
		private readonly string _key = Global.DataType;

		public ITripPersister TripPersister { get; private set; }

		private readonly Dictionary<String, ITripPersister> _persisters = new Dictionary<string, ITripPersister>(); 

		public void Register(String key, ITripPersister value)
		{
			_persisters.Add(key, value);
		}

		public void Initialize()
		{
			TripPersister = _persisters[_key];
		}
	}
}
