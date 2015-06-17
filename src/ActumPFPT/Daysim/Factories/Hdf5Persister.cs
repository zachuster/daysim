using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daysim.Attributes;
using Daysim.DomainModels;
using Daysim.Framework.Persistence;

namespace Daysim.Factories {
	public class Hdf5Persister<TModel>
	{
		private readonly Hdf5Exporter<TModel> _exporter; 

		private readonly List<TModel> _buffer = new List<TModel>();

		public Hdf5Persister()
		{
			_exporter = new Hdf5Exporter<TModel>();
		}

		public void Export(TModel model)
		{
			_buffer.Add(model);

			var props = model.GetType().GetProperties();
		}

		public void Flush()
		{
			Write(_buffer);
			_buffer.Clear();
		}

		private void Write(List<TModel> list)
		{
			int count = list.Count;
			Dictionary<string, int[]> intArrays = new Dictionary<string, int[]>();
			Dictionary<string, double[]> doubleArrays = new Dictionary<string, double[]>();

			var props = list[0].GetType().GetProperties();
			foreach (var propertyInfo in props)
			{
				if (propertyInfo.PropertyType == typeof (int))
				{
					intArrays.Add(propertyInfo.Name, new int[count]);
				}
				else if (propertyInfo.PropertyType == typeof (double))
				{
					doubleArrays.Add(propertyInfo.Name, new double[count]);
				}
			}

			int i = 0;
			foreach (var model in list)
			{
				foreach (var propertyInfo in props)
				{
					if (propertyInfo.PropertyType == typeof (int))
					{
						int value = (int) propertyInfo.GetValue(model, null);
						intArrays[propertyInfo.Name][i] = value;
					}
					else if (propertyInfo.PropertyType == typeof (double))
					{
						double value = (double) propertyInfo.GetValue(model, null);
						doubleArrays[propertyInfo.Name][i] = value;
					}
				}
				i++;
			}

			string className = typeof (TModel).Name + "/";
			
			foreach (var propertyInfo in props)
			{
				string attribute = ((ColumnNameAttribute) propertyInfo.GetCustomAttributes(typeof (ColumnNameAttribute), true)[0]).ColumnName;
				if (propertyInfo.PropertyType == typeof (int))
					{
						_exporter.WriteHdf5Data(intArrays[propertyInfo.Name], className, attribute);
					}
					else if (propertyInfo.PropertyType == typeof (double))
					{
						_exporter.WriteHdf5Data(doubleArrays[propertyInfo.Name], className, attribute);
					}
				
			}
		}
	}
}
