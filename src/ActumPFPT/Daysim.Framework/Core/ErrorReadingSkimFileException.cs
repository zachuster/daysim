﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Runtime.Serialization;

namespace Daysim.Framework.Core {
	[Serializable]
	public class ErrorReadingSkimFileException : Exception {
		public ErrorReadingSkimFileException() : this("An error occurred when trying to read the skim file.") {}

		public ErrorReadingSkimFileException(string message) : base(message) {}

		public ErrorReadingSkimFileException(string message, Exception innerException) : base(message, innerException) {}

		protected ErrorReadingSkimFileException(SerializationInfo info, StreamingContext context) : base(info, context) {}
	}
}