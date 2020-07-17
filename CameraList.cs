using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myCamViewer
{
    class CameraList : List<Camera>
    {
        public Camera GetByName (string name)
        {
            var result = (from cam in this
                          where cam.Name == name
                          select cam).ToList();

            if (result.Count > 0) return result[0];

            return null;
        }
    }
}
