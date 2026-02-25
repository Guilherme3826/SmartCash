using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartCash.Android
{
    public static class Platform
    {
        public static global::Android.App.Activity? CurrentActivity { get; set; }
    }
}
