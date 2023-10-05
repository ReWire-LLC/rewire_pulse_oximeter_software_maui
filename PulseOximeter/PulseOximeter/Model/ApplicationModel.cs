using PulseOximeter.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PulseOximeter.Model
{
    public class ApplicationModel : NotifyPropertyChangedObject
    {
        #region Private data members

        private BackgroundWorker _background_thread = new BackgroundWorker();

        #endregion

        #region Constructor

        public ApplicationModel() 
        {
            //Set up the background thread
            _background_thread.DoWork += _background_thread_DoWork;
            _background_thread.RunWorkerCompleted += _background_thread_RunWorkerCompleted;
            _background_thread.ProgressChanged += _background_thread_ProgressChanged;
            _background_thread.WorkerReportsProgress = true;
            _background_thread.WorkerSupportsCancellation = true;
        }

        #endregion

        #region Background thread methods

        private void _background_thread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        private void _background_thread_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        private void _background_thread_DoWork(object sender, DoWorkEventArgs e)
        {
            
        }

        #endregion
    }
}
