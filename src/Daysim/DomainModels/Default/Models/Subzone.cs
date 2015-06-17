﻿// Copyright 2005-2008 Mark A. Bradley and John L. Bowman
// Copyright 2011-2013 John Bowman, Mark Bradley, and RSG, Inc.
// You may not possess or use this file without a License for its use.
// Unless required by applicable law or agreed to in writing, software
// distributed under a License for its use is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.

using Daysim.Framework.Core;
using Daysim.Framework.DomainModels.Models;
using Daysim.Framework.Factories;

namespace Daysim.DomainModels.Default.Models {
	[UsedImplicitly]
	[Factory(Factory.SubzoneFactory, DataType = DataType.Default)]
	public class Subzone : ISubzone {
		private readonly double[] _sizes = new double[Global.Settings.Purposes.TotalPurposes];

		public Subzone(int sequence) {
			Sequence = sequence;
		}

		public int Sequence { get; private set; }

		public double Households { get; set; }

		public double StudentsK8 { get; set; }

		public double StudentsHighSchool { get; set; }

		public double StudentsUniversity { get; set; }

		public double EmploymentEducation { get; set; }

		public double EmploymentFood { get; set; }

		public double EmploymentGovernment { get; set; }

		public double EmploymentIndustrial { get; set; }

		public double EmploymentMedical { get; set; }

		public double EmploymentOffice { get; set; }

		public double EmploymentRetail { get; set; }

		public double EmploymentService { get; set; }

		public double EmploymentTotal { get; set; }

		public double ParkingOffStreetPaidDailySpaces { get; set; }

		public double ParkingOffStreetPaidHourlySpaces { get; set; }

		public double MixedUseMeasure { get; set; }

		public virtual void SetSize(int purpose, double size) {
			_sizes[purpose] = size;
		}

		public virtual double GetSize(int purpose) {
			return _sizes[purpose];
		}
	}
}