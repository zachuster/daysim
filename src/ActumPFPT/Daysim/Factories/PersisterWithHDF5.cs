using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.DomainModels;
using Daysim.Framework.Core;
using Daysim.Framework.Persistence;
using Daysim.Interfaces;
using Daysim.ModelRunners;

namespace Daysim.Factories 
{
	public class PersisterWithHDF5<TModel> where TModel : class, IModel, new()
	{
		protected Exporter<TModel> _exporter;
		private Hdf5Persister<TModel> _hdf5Exporter;

		public PersisterWithHDF5(string path, char delimiter)
		{
			_exporter = ChoiceModelFactory.ExporterFactory.GetExporter<TModel>(path, delimiter);
		}

		public PersisterWithHDF5(Exporter<TModel> exporter)
		{
			_exporter = exporter;
		}

		public void Export(TModel model) 
		{
			_exporter.Export(model);
			if (Global.Configuration.WriteTripsToHDF5)
			{
				if (_hdf5Exporter == null)
				{
					_hdf5Exporter = new Hdf5Persister<TModel>();
				}
				_hdf5Exporter.Export((TModel) model);
			}
		}

		public void Dispose() {
			if (Global.Configuration.WriteTripsToHDF5)
			{
				_hdf5Exporter.Flush();
			}
			_exporter.Dispose();
		}
	}
}
