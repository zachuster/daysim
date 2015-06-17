﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Runtime.Serialization;

namespace Daysim.Framework.Exceptions {
	[Serializable]
	public class PersonDayModelException : Exception {
		public PersonDayModelException() : this("Error running person-day models.") {}

		public PersonDayModelException(string message) : base(message) {}

		public PersonDayModelException(string message, Exception innerException) : base(message, innerException) {}

		protected PersonDayModelException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}
}