using System;
using System.Collections.Generic;
using System.Text;

namespace PdfPostprocess
{
    public class CorrectionData
    {
        public string LineText { get; set; }
        public bool GlueWithPrevious { get; set; }
    }
}
