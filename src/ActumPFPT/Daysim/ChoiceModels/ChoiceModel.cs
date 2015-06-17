using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.Framework.Core;
using Daysim.Interfaces;

namespace Daysim.ChoiceModels {
	public abstract class ChoiceModel : IChoiceModel
	{
		protected ChoiceModelHelper[] _helpers = new ChoiceModelHelper[ParallelUtility.NBatches];
		protected readonly object _lock = new object();
		protected ICoefficientsReader _reader = null;

		protected void Initialize(string choiceModelName, string coefficientsPath, int totalAlternatives, int totalNestedAlternatives, int totalLevels, int maxParameter, ICoefficientsReader reader = null) {
			lock(_lock)
			{
				if (_helpers[ParallelUtility.GetBatchFromThreadId()] != null)
				{
					return;
				}
				
				_reader = reader;

				ChoiceModelHelper.Initialize(ref _helpers[ParallelUtility.GetBatchFromThreadId()], choiceModelName, Global.GetInputPath(coefficientsPath),
				                             totalAlternatives, totalNestedAlternatives, totalLevels, maxParameter, _reader);
			}
		}

		#region IChoiceModel Members

		//public abstract void Run(IHouseholdWrapper household, ICoefficientsReader reader = null);

		#endregion

	}
}
