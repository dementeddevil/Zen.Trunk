using System;
using System.Threading.Tasks;

namespace Zen.Trunk.Storage.Data.Audio
{
    public class AudioDataPage : ObjectDataPage
    {
        #region Public Constructors
        public AudioDataPage()
        {
            IsManagedData = false;
        } 
        #endregion

        #region Protected Methods
        protected override Task OnInitAsync(EventArgs e)
        {
            PageType = PageType.Audio;
            return base.OnInitAsync(e);
        } 
        #endregion
    }
}
