namespace eegProject.Forms
{
    internal sealed class ExportSessionDescriptor
    {
        public int SessionId { get; set; }

        public int UserId { get; set; }

        public string DisplayName { get; set; }

        public string TimeLabel { get; set; }

        public string ExperimentType { get; set; }
    }
}
