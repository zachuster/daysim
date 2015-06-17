using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daysim.Interfaces {
	public interface IImporter<TModel>
	{
		void Import(string path);
		void SetModel(TModel model, string[] row);
	}
}
