using System;
using System.Collections.Generic;
using System.Threading;
using Daysim.ChoiceModels;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Interfaces;
using Xunit;

namespace Daysim.Tests 
{
	public class AutoOwnershipModelTest 
	{
		[Fact]
		public void TestAutoOwnershipModel()
		{
			Global.Configuration = new Configuration {NBatches = 1};
			Global.Configuration.AutoOwnershipModelCoefficients = "c:\\a.txt";
			ParallelUtility.Register(Thread.CurrentThread.ManagedThreadId, 0);
			List<IPerson> persons = new List<IPerson>{new Person()};
			CondensedParcel residenceParcel = new CondensedParcel();
			HouseholdWrapper household = TestHelper.GetHouseholdWrapper(persons, residenceParcel: residenceParcel);
			household.Init();
			AutoOwnershipModel model = new AutoOwnershipModel();
			model.Run(household, new TestCoefficientsReader());

		}
	}

	public class TestCoefficientsReader : CoefficientsReader
	{
		public TestCoefficientsReader()
		{

		}

		public override ICoefficient[] Read(string path, out string title,
		                                   out ICoefficient sizeFunctionMultiplier,
		                                   out ICoefficient nestCoefficient)
		{
			title = null;
			sizeFunctionMultiplier = null;
			nestCoefficient = null;
			return new Coefficient[100];
		}
	}
}
