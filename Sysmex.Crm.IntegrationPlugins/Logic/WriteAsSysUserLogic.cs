using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	public class WriteAsSysUserLogic
	{
		private IOrganizationService orgService;
		private ITracingService traceService;

		public WriteAsSysUserLogic(IOrganizationService orgService, ITracingService traceService)
		{
			this.orgService = orgService;
			this.traceService = traceService;
		}

		public void WriteAsSysUser(Entity entity, List<string> fields)
		{
			traceService.Trace($"Start {nameof(WriteAsSysUser)}");
			var updateEntity = new Entity(entity.LogicalName);
			updateEntity.Id = entity.Id;
			foreach (var field in fields)
			{
				if (entity.Contains(field))
				{
					traceService.Trace($"Update: {field}");
					updateEntity[field] = entity[field];
					entity.Attributes.Remove(field);
				}
			}
			orgService.Update(updateEntity);
			traceService.Trace($"End {nameof(WriteAsSysUser)}");
		}
	}
}