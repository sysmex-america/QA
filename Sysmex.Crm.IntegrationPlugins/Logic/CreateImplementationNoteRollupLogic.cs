using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System.Collections.Generic;
using System;
using System.Text;
using Sysmex.Crm.Model;

namespace Sysmex.Crm.IntegrationPlugins.Logic
{
	public class CreateImplementationNoteRollupLogic
    {
        private const int MAX_NOTE_LENGTH = 1048575;

        private IOrganizationService orgService;
		private ITracingService trace;
        
		public CreateImplementationNoteRollupLogic(IOrganizationService orgService, ITracingService trace)
		{
			this.orgService = orgService;
			this.trace = trace;
        }


        /// <summary>
        /// Given an annotation, retrieves all of the annotations associated with the given annotation's implementation, 
        /// combines the notes in one single string, and saves it as a field (smx_ImplementationNote) in the implemenation
        /// </summary>
        /// <param name="annotation"></param>
        public void CreateImplementationNoteRollup(Entity annotation)
        {
            this.trace.Trace($"***** BEGIN CreateImplementationNoteRollup  *****");

            //retrieve all notes associated with this implementation
            var implementationId = annotation.GetAttributeValue<EntityReference>("objectid").Id;

            this.trace.Trace("Retrieve notes...");
            var implementationNotes = RetrieveImplementationNotes(implementationId);

            StringBuilder noteRollupBuilder = new StringBuilder();

            this.trace.Trace("Combine notes...");
            foreach (var implementationNote in implementationNotes.Entities)
            {
                noteRollupBuilder.Append($"---{GetFormatedDateTimeString(implementationNote.GetAttributeValue<DateTime?>("createdon"))} by {implementationNote.GetAttributeValue<EntityReference>("createdby").Name}--------\n");
                if (implementationNote.GetAttributeValue<string>("subject") != null)
                    noteRollupBuilder.Append($"* {implementationNote.GetAttributeValue<string>("subject")} *\n");
                noteRollupBuilder.Append(implementationNote.GetAttributeValue<string>("notetext") + "\n");
            }

            var noteRollup = noteRollupBuilder.ToString();

            if (noteRollup.Length > MAX_NOTE_LENGTH)
                noteRollup = noteRollup.Substring(0, MAX_NOTE_LENGTH);

            //update implementation record
            this.trace.Trace("Start update of implementation notes");          
            var implementation = new smx_implementation
            {
                Id = implementationId,
                smx_ImplementationNotes = noteRollup
            };

            orgService.Update(implementation.ToEntity<Entity>());

            trace.Trace("***** END CreateImplementationNoteRollup *****");
        }

		public string GetFormatedDateTimeString(DateTime? input)
		{
			var tempDate = input;
			var timeZone = "Central Time";

			TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
			DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(input.Value, cstZone);

			return $"{cstTime.ToString("MM/dd/yyyy hh:mm")} {timeZone}";
		}

        private EntityCollection RetrieveImplementationNotes(Guid implementationGuid)
        {
            this.trace.Trace("* BEGIN RetrieveImplementationNotes:"  + implementationGuid.ToString() + "*");

            QueryExpression query = new QueryExpression
            {
                EntityName = "annotation",
                ColumnSet = new ColumnSet("notetext","createdon", "subject", "createdby"),
                Criteria = new FilterExpression(LogicalOperator.Or)
            };
            query.Criteria.Conditions.Add(new ConditionExpression("objectid", ConditionOperator.Equal,implementationGuid));
            query.Orders.Add(new OrderExpression("createdon", OrderType.Descending));

            this.trace.Trace("* END/RETURN RetrieveImplementationNotes *");
            return this.orgService.RetrieveMultiple(query);
        }
    }
}

