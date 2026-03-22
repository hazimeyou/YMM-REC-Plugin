using System.Threading.Tasks;
using YukkuriMovieMaker.Plugin.Voice;

namespace YMM_REC_Plugin.Voice
{
    public class NoVoiceLicense : IVoiceLicense
    {
        public VoiceLicenseDisplayLocation SummaryLocation => VoiceLicenseDisplayLocation.None;
        public string Summary => string.Empty;
        public bool IsTermsAgreed { get; set; } = true;
        public string Terms => string.Empty;
        public string TermsURL => string.Empty;

        public ValueTask<bool> ValidateLicenseAsync() => ValueTask.FromResult(true);
    }
}
