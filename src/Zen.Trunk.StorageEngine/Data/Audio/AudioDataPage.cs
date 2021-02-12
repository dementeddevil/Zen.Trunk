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
        protected override Task OnInitAsync()
        {
            PageType = PageType.Audio;
            return base.OnInitAsync();
        } 
        #endregion
    }
}
