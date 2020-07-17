using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCamViewer
{
    class Camera
    {
        private readonly string defaultName = "Camera";

        private string name;
        public string Name
        {
            get => name;
            set => name = value; // change name for local session
        }

        public string Id;

        private static int unnamedCamCounter = 0;

        public Camera (string id)
        {
            Id = id;
            Name = defaultName + unnamedCamCounter; // Camera0, etc

            unnamedCamCounter++;
        }

        public Camera (string id, string name)
        {
            Name = name;
            Id = id;
        }

        public string Url { get; set; }


    }
}
