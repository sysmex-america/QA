using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	public class SetTimeToNoonLogic
	{
		private ITracingService trace;

		public SetTimeToNoonLogic(ITracingService trace)
		{
			this.trace = trace;
		}

		public void SetToNoon(Entity target, List<string> fieldsToCheck)
		{
			trace.Trace($"Start {nameof(SetToNoon)}");
			foreach (var field in fieldsToCheck)
			{
				trace.Trace($"Field {field}");
				if (target[field] is DateTime?)
				{

					trace.Trace("Is a DateTime");
					var value = target.GetAttributeValue<DateTime?>(field);
					trace.Trace($"Old Value: {value}");
					if (field == "msdyn_todate")
					{
						value = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day-1, 14, 1, 0);
					}
					else
					{
						value = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day, 14, 1, 0);
					}
					target[field] = value;
					trace.Trace($"New Value {value}");
				}
			}
			trace.Trace($"End {nameof(SetToNoon)}");
		}
	}
}