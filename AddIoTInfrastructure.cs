using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using LCU.State.API.NapkinIDE.DataFlowManager.Models;
using LCU.State.API.NapkinIDE.DataFlowManager.Services;
using System.Runtime.Serialization;

namespace LCU.State.API.NapkinIDE.DataFlowManager
{
    [Serializable]
    [DataContract]
    public class AddIoTInfrastructureRequest
    {
    }

    public static class AddIoTInfrastructure
    {
        [FunctionName("AddIoTInfrastructure")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            return await req.Manage<AddIoTInfrastructureRequest, DataFlowManagerState, DataFlowManagerStateHarness>(log, async (mgr, reqData) =>
            {
                log.LogInformation($"Adding IoT Infrastructure");
                
                return await mgr.AddIoTInfrastructure();
            });
        }
    }
}
