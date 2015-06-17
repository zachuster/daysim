﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using Daysim.Framework.Core;
using Daysim.Framework.Roster;
using Daysim.Interfaces;

namespace Daysim.DomainModels {
	public static class PointExtensions {
		public static double DistanceFromOrigin(this IPoint origin, IPoint destination, int minute) {
			if (origin == null) {
				throw new ArgumentNullException("origin");
			}

			if (destination == null) {
				throw new ArgumentNullException("destination");
			}

			return ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, minute, origin, destination).BlendVariable / 10;
		}

		public static double DistanceFromWorkLog(this IPoint origin, IPoint destination, int minute) {
			return
				origin == null
					? 0
					: Math.Log(1 + ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, minute, origin, destination).BlendVariable / 10);
		}

		public static double DistanceFromSchoolLog(this IPoint origin, IPoint destination, int minute) {
			return
				origin == null
					? 0
					: Math.Log(1 + ImpedanceRoster.GetValue("distance", Constants.Mode.SOV, Constants.PathType.FULL_NETWORK, Constants.ValueOfTime.DEFAULT_VOT, minute, origin, destination).BlendVariable / 10);
		}
	}
}