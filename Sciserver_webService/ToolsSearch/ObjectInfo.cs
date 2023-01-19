﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sciserver_webService.ToolsSearch
{
    public class ObjectInfo
    {
        public long? id = null;
        public string apid;
        public decimal? specId = null;

        public string objId = null;
        public string specObjId = null;

        public double? ra = null;
        public double? dec = null;

        public long? mjd = null;
        public decimal? plate = null;
        public decimal? plateId = null;
        public long? fiberId = null;
        public string fieldId = null;

        public int? run = null;
        public short? rerun = null;
        public short? camcol = null;
        public short? field = null;

        public string name = null;
        public int? zoom = null;
        public string plateIdApogee = null;

        public string mangaId = null;

    }
}