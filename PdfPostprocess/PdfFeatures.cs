namespace PdfPostprocess
{
    public class PdfFeatures
    {
        public float ThisLen { get; set; }
        public float MeanLen { get; set; }
        public float PrevLen { get; set; }
        public string FirstChars { get; set; }
        public bool PrevLastIsAlpha { get; set; }
        public bool PrevLastIsDigit { get; set; }
        public bool PrevLastIsLower { get; set; }
        public bool PrevLastIsPunct { get; set; }
    }
}
