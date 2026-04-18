using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace KundaliEngine.Astrology.Astronomy
{
    // Responsibility: Calls native DLL
    // Made public so other projects (e.g., Console) can initialize and use it.
    public static class SwissEphWrapper
    {
        private const string RelativeDllPath = "Native\\swedll64.dll";
        private const int SEFLG_SWIEPH = 2;
        private const int SEFLG_SIDEREAL = 65536;
        private const int SE_SIDM_LAHIRI = 1;

        // Ensures the runtime can find the native DLL and verifies it can be loaded.
        public static void Initialize(string ephePath)
        {
            Console.WriteLine("Initialize called - setting sidereal mode");
            if (string.IsNullOrWhiteSpace(ephePath))
                throw new ArgumentException("ephePath is required", nameof(ephePath));

            if (!Directory.Exists(ephePath))
                throw new DirectoryNotFoundException($"Ephemeris directory not found: {ephePath}");

            string appBaseDirectory = AppContext.BaseDirectory;
            string nativeDirectory = Path.Combine(appBaseDirectory, "Native");
            string dllFullPath = Path.Combine(appBaseDirectory, RelativeDllPath);

            if (!Directory.Exists(nativeDirectory))
                throw new DirectoryNotFoundException($"Native DLL directory not found: {nativeDirectory}");

            // Add the native DLL folder to the search path for the process.
            if (!SetDllDirectory(nativeDirectory))
            {
                var err = Marshal.GetLastWin32Error();
                throw new InvalidOperationException($"Failed to set DLL directory to '{nativeDirectory}'. Win32 error: {err}");
            }

            // Verify the native DLL is present and can be loaded.
            if (!File.Exists(dllFullPath))
                throw new DllNotFoundException($"Native DLL not found at '{dllFullPath}'. Ensure {RelativeDllPath} is copied to the application output.");

            IntPtr h = LoadLibrary(dllFullPath);
            if (h == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                // Common causes: wrong architecture (x86 vs x64) or missing Visual C++ runtime.
                throw new DllNotFoundException(
                    $"Unable to load '{dllFullPath}'. Win32 error: {err}. " +
                    "Ensure the DLL architecture matches the process (x64) and that the Visual C++ redistributable is installed."
                );
            }

            // We only needed a probe; free the handle.
            FreeLibrary(h);

            // Point Swiss Ephemeris at the folder containing the ephemeris data files.
            swe_set_ephe_path(ephePath);
            // Set Lahiri Ayanamsa (Vedic)
            swe_set_sid_mode(SE_SIDM_LAHIRI, 0, 0);

        }

        public static (double Longitude, double Latitude) CalculatePlanet(string planetName, double julianDay)
        {
            try
            {
                int planetId = PlanetMapper.GetPlanetId(planetName);

                double[] xx = new double[6];
                StringBuilder serr = new StringBuilder(256);
                int flags = SEFLG_SWIEPH | SEFLG_SIDEREAL;
                Console.WriteLine($"SwissEph flags: {flags}");
                swe_calc_ut(julianDay, planetId, flags, xx, serr);

                return (xx[0], xx[1]);
            }
            catch (DllNotFoundException ex)
            {
                // Re-throw with extra guidance.
                throw new DllNotFoundException(
                    $"{ex.Message} Ensure Initialize(ephePath) was called with a valid ephemeris folder and that {RelativeDllPath} exists under the application output, and verify native dependencies and process architecture.", ex);
            }
        }

        [DllImport("Native\\swedll64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void swe_set_ephe_path(string path);

        [DllImport("Native\\swedll64.dll", CallingConvention = CallingConvention.Cdecl)]
        
        private static extern int swe_calc_ut(
            double tjd_ut,
            int ipl,
            int iflag,
            double[] xx,
            StringBuilder serr
        );


        [DllImport("Native\\swedll64.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void swe_set_sid_mode(
    int sid_mode,
    double t0,
    double ayan_t0
        );

        // kernel32 helpers
        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool FreeLibrary(IntPtr hModule);
    }
}
