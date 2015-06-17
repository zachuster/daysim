using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.Interfaces;

namespace Daysim.Framework.Core {
	public class ChoiceModelDictionary 
	{
		readonly Dictionary<string, IChoiceModel> _choiceModels = new Dictionary<string, IChoiceModel>();
		
		public void Register(string name, IChoiceModel model)
		{
			_choiceModels.Add(name, model);
		}

		public IChoiceModel Get(string name)
		{
			return _choiceModels[name];
		}
	}
}
