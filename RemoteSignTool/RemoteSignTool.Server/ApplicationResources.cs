namespace RemoteSignTool.Server
{
    public sealed class ApplicationResources
    {
        private static readonly Properties.Resources ApplicationStrings = new Properties.Resources();

        /// <summary>
        /// Gets the <see cref="ApplicationStrings"/>.
        /// </summary>
        public Properties.Resources Strings
        {
            get { return ApplicationStrings; }
        }
    }
}
