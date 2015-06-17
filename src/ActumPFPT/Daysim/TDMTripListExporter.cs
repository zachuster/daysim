﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.


using System;
using System.IO;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Interfaces;

namespace Daysim {
	public sealed class TDMTripListExporter : IDisposable {
		private int _current;
		private readonly char _delimiter;
		private readonly StreamWriter _writer;

		public TDMTripListExporter(string outputPath, char delimiter) {
			var outputFile = new FileInfo(outputPath);

			_writer = new StreamWriter(outputFile.Open(FileMode.Create, FileAccess.Write, FileShare.Read)) {AutoFlush = false};
			_delimiter = delimiter;

			WriteHeader();
		}

		private void WriteHeader() {
			if (Global.Configuration.UseTransimsTDMTripListFormat) {
				_writer.Write("HHOLD");
				_writer.Write(_delimiter);

				_writer.Write("PERSON");
				_writer.Write(_delimiter);

				_writer.Write("TOUR");
				_writer.Write(_delimiter);

				_writer.Write("TRIP");
				_writer.Write(_delimiter);

				_writer.Write("START");
				_writer.Write(_delimiter);

				_writer.Write("END");
				_writer.Write(_delimiter);

				_writer.Write("DURATION");
				_writer.Write(_delimiter);

				_writer.Write("ORIGIN");
				_writer.Write(_delimiter);

				_writer.Write("DESTINATION");
				_writer.Write(_delimiter);

				_writer.Write("PURPOSE");
				_writer.Write(_delimiter);

				_writer.Write("MODE");
				_writer.Write(_delimiter);

				_writer.Write("CONSTRAINT");
				_writer.Write(_delimiter);

				_writer.Write("PRIORITY");
				_writer.Write(_delimiter);

				_writer.Write("VEHICLE");
				_writer.Write(_delimiter);

				_writer.Write("PASSENGERS");
				_writer.Write(_delimiter);

				_writer.WriteLine("TYPE");
			}
			else {
				_writer.Write(";id");
				_writer.Write(_delimiter);

				_writer.Write("otaz");
				_writer.Write(_delimiter);

				_writer.Write("dtaz");
				_writer.Write(_delimiter);

				_writer.Write("mode");
				_writer.Write(_delimiter);

				_writer.Write("deptm");
				_writer.Write(_delimiter);

				_writer.Write("arrtm");
				_writer.Write(_delimiter);

				_writer.Write("duration");
				_writer.Write(_delimiter);

				_writer.Write("dpurp");
				_writer.Write(_delimiter);

				_writer.WriteLine("vot");
			}
		}

		public void Export(ITripWrapper trip) {
			if (trip == null) {
				throw new ArgumentNullException("trip");
			}
			if (Global.Configuration.UseTransimsTDMTripListFormat) {
				_writer.Write(trip.Household.Id);
				_writer.Write(_delimiter);

				_writer.Write(trip.Person.Sequence);
				_writer.Write(_delimiter);

				_writer.Write(trip.Tour.Sequence);
				_writer.Write(_delimiter);

				_writer.Write(trip.IsHalfTourFromOrigin ? trip.Sequence : trip.Tour.HalfTour1Trips + trip.Sequence);
				_writer.Write(_delimiter);

				var departureTime24Hour = trip.DepartureTime.ToMinutesAfterMidnight();
                if (departureTime24Hour < 180) { departureTime24Hour += 1440; } // range becomes 180-1619 instead of 0-1439 

				_writer.Write(departureTime24Hour);
				_writer.Write(_delimiter);

				var arrivalTime24Hour = trip.ArrivalTime.ToMinutesAfterMidnight();
                if (arrivalTime24Hour < 180) { arrivalTime24Hour += 1440; } // range becomes 180-1619 instead of 0-1439

				_writer.Write(arrivalTime24Hour);
				_writer.Write(_delimiter);

				_writer.Write(trip.ActivityEndTime - trip.ArrivalTime);
				_writer.Write(_delimiter);

				_writer.Write(trip.OriginParcel.TransimsActivityLocation);
				_writer.Write(_delimiter);

				_writer.Write(trip.DestinationParcel.TransimsActivityLocation);
				_writer.Write(_delimiter);

				_writer.Write(trip.DestinationPurpose);
				_writer.Write(_delimiter);

				var tsMode = trip.Mode == Constants.Mode.WALK
				             	? 1
				             	: trip.Mode == Constants.Mode.BIKE
				             	  	? 2
				             	  	: trip.Mode >= Constants.Mode.SOV && trip.Mode <= Constants.Mode.HOV3 && trip.DriverType == Constants.DriverType.DRIVER
				             	  	  	? 3
				             	  	  	: trip.Mode >= Constants.Mode.SOV && trip.Mode <= Constants.Mode.HOV3 && trip.DriverType != Constants.DriverType.DRIVER
				             	  	  	  	? 4
				             	  	  	  	: trip.Mode == Constants.Mode.TRANSIT
				             	  	  	  	  	? 5
				             	  	  	  	  	: trip.Mode == Constants.Mode.SCHOOL_BUS
				             	  	  	  	  	  	? 11
				             	  	  	  	  	  	: 6;

				var tsPass = trip.Mode == Constants.Mode.HOV2
				             	? 1
				             	: trip.Mode == Constants.Mode.HOV3
				             	  	? 2
				             	  	: 0;

				_writer.Write(tsMode);
				_writer.Write(_delimiter);

				const int zero = 0;
				_writer.Write(zero);
				_writer.Write(_delimiter);
				_writer.Write(zero);
				_writer.Write(_delimiter);

				_writer.Write(trip.Household.Id * 100 + trip.Person.Sequence);
				_writer.Write(_delimiter);

				_writer.Write(tsPass);
				_writer.Write(_delimiter);

				var tripVOT = trip.ValueOfTime;
				var votType = (trip.Mode < Constants.Mode.SOV || trip.Mode > Constants.Mode.HOV3)
				              	? 0
				              	: ((tripVOT < 30
				              	    	? 1 + (int) tripVOT
				              	    	: tripVOT < 100
				              	    	  	? 31 + (int) ((tripVOT - 30) / 5)
				              	    	  	: 45)
				              	   + (trip.PathType == Constants.PathType.NO_TOLLS ? 0 : 50));

				_writer.WriteLine(votType);
			}
			else {
				_writer.Write(trip.Id);
				_writer.Write(_delimiter);

				_writer.Write(trip.OriginZoneKey);
				_writer.Write(_delimiter);

				_writer.Write(trip.DestinationZoneKey);
				_writer.Write(_delimiter);

				_writer.Write(trip.Mode);
				_writer.Write(_delimiter);

				var departureTime24Hour = trip.DepartureTime.ToMinutesAfterMidnight();

				_writer.Write(departureTime24Hour / 60D);
				_writer.Write(_delimiter);

				var arrivalTime24Hour = trip.ArrivalTime.ToMinutesAfterMidnight();

				_writer.Write(arrivalTime24Hour / 60D);
				_writer.Write(_delimiter);

				_writer.Write(trip.ArrivalTime - trip.DepartureTime);
				_writer.Write(_delimiter);

				_writer.Write(trip.DestinationPurpose);
				_writer.Write(_delimiter);

				var valueOfTime =
					trip.Household.Income < (trip.Tour.DestinationPurpose == Constants.Purpose.WORK ? 20000 : 40000)
						? Constants.ValueOfTime.LOW
						: trip.Household.Income < (trip.Tour.DestinationPurpose == Constants.Purpose.WORK ? 45000 : 110000)
						  	? Constants.ValueOfTime.MEDIUM
						  	: Constants.ValueOfTime.HIGH;

				_writer.WriteLine(valueOfTime);
			}

			_current++;

			if (_current % 1000 == 0) {
				_writer.Flush();
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				_writer.Dispose();
			}
		}
	}
}