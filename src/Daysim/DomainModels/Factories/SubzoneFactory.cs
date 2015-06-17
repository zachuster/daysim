﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using System;
using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Models;
using Daysim.Framework.Factories;

namespace Daysim.DomainModels.Factories {
	public class SubzoneFactory {
		private readonly Type _type;

		public SubzoneFactory(Configuration configuration) {
			var helper = new FactoryHelper(configuration);

			_type = helper.Subzone.GetSubzoneType();
		}

		public ISubzone Create(int sequence) {
			return (ISubzone) Activator.CreateInstance(_type, sequence);
		}
	}
}