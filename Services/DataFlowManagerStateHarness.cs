using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fathym;
using Fathym.API;
using LCU.Graphs.Registry.Enterprises.DataFlows;
using LCU.Personas.Client.Applications;
using LCU.Personas.Client.DevOps;
using LCU.Personas.Client.Enterprises;
using LCU.Personas.Client.Identity;
using LCU.Personas.Enterprises;
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

        public virtual async Task<DataFlowManagerState> DeployDataFlow(string dataFlowLookup)
        {
            logger.LogInformation($"Deploying data flow: '{dataFlowLookup}'");

            var resp = await appDev.DeployDataFlow(new Personas.Applications.DeployDataFlowRequest()
            {
                DataFlowLookup = dataFlowLookup
            }, details.EnterpriseAPIKey, state.EnvironmentLookup);

            state.IsCreating = !resp.Status;

            return await LoadDataFlows();
        }

        public virtual async Task<DataFlowManagerState> LoadDataFlows()
        {
            logger.LogInformation("Loading Data Flows");

            var resp = await appMgr.ListDataFlows(details.EnterpriseAPIKey, state.EnvironmentLookup);

            state.DataFlows = resp.Model;

            return await SetActiveDataFlow(state?.ActiveDataFlow?.Lookup); ;
        }

        public virtual async Task<DataFlowManagerState> LoadEnvironment()
        {
            logger.LogInformation("Loading Data Flows");

            var resp = await entMgr.ListEnvironments(details.EnterpriseAPIKey);

            state.EnvironmentLookup = resp.Model?.FirstOrDefault()?.Lookup;

            return state;
        }

        public virtual async Task<DataFlowManagerState> LoadModulePackSetup()
        {
            logger.LogInformation("Loading Data Flows");

            var mpsResp = await appMgr.ListModulePackSetups(details.EnterpriseAPIKey);

            state.ModulePacks = new List<ModulePack>();

            state.ModuleDisplays = new List<ModuleDisplay>();

            state.ModuleOptions = new List<ModuleOption>();

            var moduleOptions = new List<ModuleOption>();

            if (mpsResp.Status)
            {
                mpsResp.Model.Each(mps =>
                {
                    state.ModulePacks = state.ModulePacks.Where(mp => mp.Lookup != mps.Pack.Lookup).ToList();

                    if (mps.Pack != null)
                        state.ModulePacks.Add(mps.Pack);

                    state.ModuleDisplays = state.ModuleDisplays.Where(mp => !mps.Displays.Any(disp => disp.ModuleType == mp.ModuleType)).ToList();

                    if (!mps.Displays.IsNullOrEmpty())
                        state.ModuleDisplays.AddRange(mps.Displays);

                    moduleOptions = moduleOptions.Where(mo => !mps.Options.Any(opt => opt.ModuleType == mo.ModuleType)).ToList();

                    if (!mps.Options.IsNullOrEmpty())
                        moduleOptions.AddRange(mps.Options);
                });

                await moduleOptions.Each(async mo =>
                {
                    var moInfraResp = await entMgr.LoadInfrastructureDetails(details.EnterpriseAPIKey, state.EnvironmentLookup, mo.ModuleType);

                    // var moInfraResp = await entMgr.Get<BaseResponse<List<InfrastructureDetails>>>($"environments/{details.EnterpriseAPIKey}/infra/{state.EnvironmentLookup}/details?type={mo.ModuleType}");

                    var moDisp = state.ModuleDisplays.FirstOrDefault(md => md.ModuleType == mo.ModuleType);

                    if (moInfraResp.Status && !moInfraResp.Model.IsNullOrEmpty())
                    {
                        moInfraResp.Model.Each(infraDets =>
                        {
                            var newMO = mo.JSONConvert<ModuleOption>();

                            newMO.ID = Guid.Empty;

                            newMO.Name = $"{mo.Name} - {infraDets.DisplayName}";

                            newMO.ModuleType = infraDets.Lookup;

                            state.ModuleOptions.Add(newMO);

                            var newMODisp = moDisp.JSONConvert<ModuleDisplay>();

                            newMODisp.ModuleType = newMO.ModuleType;

                            state.ModuleDisplays.Add(newMODisp);

                            if (state.AllowCreationModules)
                            {
                                state.ModuleOptions.Add(mo);

                                state.ModuleDisplays.Add(moDisp);
                            }
                        });
                    }
                    else
                    {
                        state.ModuleOptions.Add(mo);

                        state.ModuleDisplays.Add(moDisp);
                    }
                });
            }

            return state;
        }

        public virtual async Task<DataFlowManagerState> Refresh()
        {
            logger.LogInformation("Refreshing Data Flow Manager state");

            await LoadEnvironment();

            await WhenAll(
                LoadDataFlows(),
                LoadModulePackSetup()
            );

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

            // if (state.ActiveDataFlow != null)
            // {
            //     if (state.ActiveDataFlow.Output == null || state.ActiveDataFlow.Output.Modules.IsNullOrEmpty())
            //     {
            //         state.ActiveDataFlow.Output = new DataFlowOutput()
            //         {
            //             Modules = new List<Module>()
            //         {
            //             new Module()
            //             {
            //                 Display = state.ModuleDisplays.FirstOrDefault(md => md.ModuleType == "data-stream"),
            //                 Status = Status.Success,
            //                 Text = "Data Ingest",
            //                 ID = Guid.NewGuid()
            //             },
            //             new Module()
            //             {
            //                 Display = state.ModuleDisplays.FirstOrDefault(md => md.ModuleType == "data-map"),
            //                 Status = Status.Success,
            //                 Text = "Data Map",
            //                 ID = Guid.NewGuid()
            //             },
            //             new Module()
            //             {
            //                 Display = state.ModuleDisplays.FirstOrDefault(md => md.ModuleType == "cold-storage"),
            //                 Status = Status.Success,
            //                 Text = "Cold Storage",
            //                 ID = Guid.NewGuid()
            //             },
            //             new Module()
            //             {
            //                 Display = state.ModuleDisplays.FirstOrDefault(md => md.ModuleType == "warm-storage"),
            //                 Status = Status.Success,
            //                 Text = "Warm Storage",
            //                 ID = Guid.NewGuid()
            //             },
            //             new Module()
            //             {
            //                 Display = state.ModuleDisplays.FirstOrDefault(md => md.ModuleType == "hot-storage"),
            //                 Status = Status.Success,
            //                 Text = "Hot Storage",
            //                 ID = Guid.NewGuid()
            //             }
            //         }
            //         };

            //         state.ActiveDataFlow.Output.Streams = new List<ModuleStream>()
            //     {
            //         new ModuleStream()
            //         {
            //             InputModuleID = state.ActiveDataFlow.Output.Modules.ElementAt(0).ID,
            //             OutputModuleID = state.ActiveDataFlow.Output.Modules.ElementAt(1).ID
            //         },
            //         new ModuleStream()
            //         {
            //             InputModuleID = state.ActiveDataFlow.Output.Modules.ElementAt(1).ID,
            //             OutputModuleID = state.ActiveDataFlow.Output.Modules.ElementAt(2).ID
            //         },
            //         new ModuleStream()
            //         {
            //             InputModuleID = state.ActiveDataFlow.Output.Modules.ElementAt(1).ID,
            //             OutputModuleID = state.ActiveDataFlow.Output.Modules.ElementAt(3).ID
            //         },
            //         new ModuleStream()
            //         {
            //             InputModuleID = state.ActiveDataFlow.Output.Modules.ElementAt(1).ID,
            //             OutputModuleID = state.ActiveDataFlow.Output.Modules.ElementAt(4).ID
            //         }
            //     };
            //     }
            // }

            return state;
        }

        public virtual async Task<DataFlowManagerState> ToggleCreationModules()
        {
            state.AllowCreationModules = !state.AllowCreationModules;

            logger.LogInformation($"Toggling Creation Modules to: '{state.AllowCreationModules}'");

            return await LoadModulePackSetup();
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