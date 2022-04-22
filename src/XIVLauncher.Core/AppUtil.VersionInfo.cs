namespace XIVLauncher.Core;

public static partial class AppUtil
{
    /// <summary>
    ///    Class holding XIVLauncher version information.
    ///    Partial class to allow for injection of the assembly version using a source generator.
    ///    The constructor is generated by the build process.
    /// </summary>
    public partial class VersionInfo
    {
        private static VersionInfo? _version;

        /// <summary>
        ///    Gets the version information for the assembly.
        /// </summary>
        public (string FileName, Version FileVersion) Version { get; }

        /// <summary>
        ///    Singleton initializer for the <see cref="VersionInfo"/> class.
        /// </summary>
        public static VersionInfo Instance()
        {
            if (_version == null)
                _version = new VersionInfo();

            return _version;
        }
    }
}