using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LCU.Graphs.Registry.Enterprises.DataFlows;
using LCU.Personas.Client.Applications;
using LCU.Personas.Client.DevOps;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.Identity;
using LCU.State.API.NapkinIDE.DataFlowManager.Models;
using LCU.StateAPI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace LCU.State.API.NapkinIDE.DataFlowManager.Services
{
    public class DataFlowManagerStateHarness : LCUStateHarness<DataFlowManagerState>
    {
        #region Fields
        protected readonly ApplicationManagerClient appMgr;

        protected readonly DevOpsArchitectClient devOpsArch;

        protected readonly EnterpriseArchitectClient entArch;

        protected readonly EnterpriseManagerClient entMgr;

        protected readonly IdentityManagerClient idMgr;
        #endregion

        #region Properties

        #endregion

        #region Constructors
        public DataFlowManagerStateHarness(HttpRequest req, ILogger logger, DataFlowManagerState state)
            : base(req, logger, state)
        {
            appMgr = req.ResolveClient<ApplicationManagerClient>(logger);

            devOpsArch = req.ResolveClient<DevOpsArchitectClient>(logger);

            entArch = req.ResolveClient<EnterpriseArchitectClient>(logger);

            entMgr = req.ResolveClient<EnterpriseManagerClient>(logger);

            idMgr = req.ResolveClient<IdentityManagerClient>(logger);
        }
        #endregion

        #region API Methods
        public virtual async Task<DataFlowManagerState> DeleteDataFlow(string dataFlowLookup)
        {
            logger.LogInformation($"Deleting data flow: '{dataFlowLookup}'");

            var resp = await appMgr.DeleteDataFlow(details.EnterpriseAPIKey, state.EnvironmentLookup, dataFlowLookup);

            return await LoadDataFlows();
        }

        public virtual async Task<DataFlowManagerState> LoadDataFlows()
        {
            logger.LogInformation("Loading Data Flows");

            var resp = await appMgr.ListDataFlows(details.EnterpriseAPIKey, state.EnvironmentLookup);
          
            state.DataFlows = resp.Model;

            return state;
        }

        public virtual async Task<DataFlowManagerState> LoadEnvironment()
        {
            logger.LogInformation("Loading Data Flows");

            var resp = await entMgr.ListEnvironments(details.EnterpriseAPIKey);
          
            state.EnvironmentLookup = resp.Model?.FirstOrDefault()?.Lookup;

            return state;
        }

        public virtual async Task<DataFlowManagerState> Refresh()
        {
            logger.LogInformation("Refreshing Data Flow Manager state");

            await LoadEnvironment();

            await LoadDataFlows();

            return state;
        }

        public virtual async Task<DataFlowManagerState> SaveDataFlow(DataFlow dataFlow)
        {
            logger.LogInformation($"Saving data flow: '{dataFlow?.Lookup}'");

            var resp = await appMgr.SaveDataFlow(dataFlow, details.EnterpriseAPIKey, state.EnvironmentLookup);

            state.IsCreating = !resp.Status;

            return await LoadDataFlows();
        }

        public virtual async Task<DataFlowManagerState> SetActiveDataFlow(string dfLookup)
        {
            logger.LogInformation($"Setting active data flow to: '{dfLookup}'");

            state.ActiveDataFlow = state.DataFlows.FirstOrDefault(df => df.Lookup == dfLookup);

            return state;
        }
        
        public virtual async Task<DataFlowManagerState> ToggleIsCreating()
        {
            state.IsCreating = !state.IsCreating;

            logger.LogInformation($"Toggling IsCreating to: '{state.IsCreating}'");

            return state;
        }
        #endregion

        #region Helpers
        #endregion
    }
}