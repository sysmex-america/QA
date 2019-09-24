using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace Sysmex.Crm.Plugins.Logic
{
    public class LogicBase
    {
        protected readonly IOrganizationService _orgService;
        protected readonly ITracingService _tracer;

        private Stopwatch _timer;

        public LogicBase(IOrganizationService orgService, ITracingService tracer)
        {
            _orgService = orgService;
            _tracer = tracer;

            _timer = Stopwatch.StartNew();
        }

        public void TimedTrace(string message)
        {
            if (_tracer != null && _timer != null)
            {
                _tracer.Trace($"{message} [{_timer.ElapsedMilliseconds}ms Elapsed since last log]");
                _timer.Restart();
            }
        }
    }
}
