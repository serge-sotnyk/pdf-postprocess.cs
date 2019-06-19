using System;
using System.Collections.Generic;
using System.Text;

namespace PdfPostprocess
{
    public class CorrectionData: PdfFeatures
    {
        public bool GlueWithPrevious { get; set; }
    }
}
