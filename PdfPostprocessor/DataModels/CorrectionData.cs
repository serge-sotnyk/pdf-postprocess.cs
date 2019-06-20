using System;
using System.Collections.Generic;
using System.Text;

namespace PdfPostprocessor
{
    public class CorrectionData: PdfFeatures
    {
        public bool GlueWithPrevious { get; set; }
    }
}
