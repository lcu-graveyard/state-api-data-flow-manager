using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym;
using LCU.Graphs.Registry.Enterprises.DataFlows;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LCU.State.API.NapkinIDE.DataFlowManager.Models
{
    [DataContract]
    public class DataFlowManagerState
    {
        [DataMember]
        public virtual DataFlow ActiveDataFlow { get; set; }
        
        [DataMember]
        public bool AllowCreationModules { get; set; }
        
        [DataMember]
        public virtual List<DataFlow> DataFlows { get; set; }
        
        [DataMember]
        public virtual string EnvironmentLookup { get; set; }
        
        [DataMember]
        public virtual bool IsCreating { get; set; }
        
        [DataMember]
        public virtual List<ModulePack> ModulePacks { get; set; }
        
        [DataMember]
        public virtual List<ModuleOption> ModuleOptions { get; set; }
        
        [DataMember]
        public virtual List<ModuleDisplay> ModuleDisplays { get; set; }
        
        [DataMember]
        public virtual bool Loading { get; set; }
    }
}