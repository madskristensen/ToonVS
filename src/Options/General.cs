using System.ComponentModel;

namespace ToonVS
{
    //internal partial class OptionsProvider
    //{
    //    // Register the options with this attribute on your package class:
    //    // [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "ToonVS", "General", 0, 0, true, SupportsProfiles = true)]
    //    [ComVisible(true)]
    //    public class GeneralOptions : BaseOptionPage<General> { }
    //}

    public class General : BaseOptionModel<General>, IRatingConfig
    {
        [Browsable(false)]
        public int RatingRequests { get; set; }
    }
}
