using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Fathym;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LCU.State.API.NapkinIDE.DataFlowManager.Models
{
    [DataContract]
    public class DataFlowManagerState
    {
        [DataMember]
        public virtual bool Loading { get; set; }
    }
}