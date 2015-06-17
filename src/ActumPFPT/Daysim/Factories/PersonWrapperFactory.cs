// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using System.Collections.Generic;
using Daysim.Framework.Core;
using Daysim.Interfaces;

namespace Daysim.Factories {
	public class PersonWrapperFactory 
	{
		private readonly string _key = Global.DataType;

		private readonly Dictionary<String, IPersonWrapperCreator> _creators = new Dictionary<string, IPersonWrapperCreator>();

		public IPersonWrapperCreator PersonWrapperCreator { get; private set; }

		public void Register(String key, IPersonWrapperCreator value)
		{
			_creators.Add(key, value);
		}

		public void Initialize()
		{
			PersonWrapperCreator = _creators[_key];
		}
	}
}
