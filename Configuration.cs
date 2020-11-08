using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using WindowsInput.Native;

namespace virtual_cube
{
    [Serializable]
    public class Configuration
    {
        [DataMember]
        public Boolean MappingEnabled { get; set; }
        [DataMember]
        public Dictionary<String, VirtualKeyCode> Mapping { get; set; }
        [DataMember]
        public int? TimePerKeyPress { get; set; }
        public Configuration()
        {
        }

        public Configuration(
            Boolean mappingEnabled, 
            Dictionary<String, VirtualKeyCode> mapping,
            int? timePerKeyPress)
        {
            this.MappingEnabled = mappingEnabled;
            this.Mapping = mapping;
            this.TimePerKeyPress = timePerKeyPress;
        }
    }
}
