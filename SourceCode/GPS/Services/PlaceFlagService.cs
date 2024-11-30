using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgOpenGPS.Services
{
    public partial class PlaceFlagService
    {
        public void placeFlag(FormGPS formGps, List<CFlag> flagPts, CNMEA pn, double fixHeading, byte flagColor, double easting, double northing, bool showForm)
        {
            int nextflag = flagPts.Count + 1;
            CFlag flagPt = new CFlag(pn.latitude, pn.longitude, easting, northing,
                fixHeading, flagColor, nextflag, nextflag.ToString());
            flagPts.Add(flagPt);
            formGps.FileSaveFlags();

            Form fc = Application.OpenForms["FormFlags"];

            if (fc != null)
            {
                fc.Focus();
                return;
            }

            if (showForm)
            {
                if (flagPts.Count > 0)
                {
                    formGps.flagNumberPicked = nextflag;
                    Form form = new FormFlags(formGps);
                    form.Show(formGps);
                }
            }
        }
    }

    public partial class PlaceFlagService
    {
        private static readonly Lazy<PlaceFlagService> _lazyInstance = new Lazy<PlaceFlagService>(() => new PlaceFlagService());
        public static PlaceFlagService Instance => _lazyInstance.Value;
        
    }
}
