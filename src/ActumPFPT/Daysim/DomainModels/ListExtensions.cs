// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.Collections.Generic;
using Daysim.Interfaces;

namespace Daysim.DomainModels {
	public static class ListExtensions {
		public static List<ITripWrapper> Invert(this List<ITripWrapper> trips) {
			if (trips == null) {
				throw new ArgumentNullException("trips");
			}

			var list = new List<ITripWrapper>();
			var sequence = trips.Count;

			foreach (var trip in trips) {
				trip.Invert(sequence--);

				list.Insert(0, trip);
			}

			return list;
		}
	}
}