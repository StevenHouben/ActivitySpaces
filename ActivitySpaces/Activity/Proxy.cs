using System.Runtime.Serialization;
using ABC.Windows.Desktop;
using ActivitySpaces.Xaml;

namespace ActivitySpaces
{
    public class Proxy
    {
        public VirtualDesktop Desktop { get; set; }
        public ABC.Model.Activity Activity{get;set;}    
        public ActivityButton Button { get; set; }

        public SavedProxy GetSaveableProxy()
        {
            return new SavedProxy()
                {
                    Activity = Activity,
                    Button = Button.GetSaveableButton(),
                    Sessions = Desktop.Store(),
                    Folder = Desktop.Folder
                };

        }
    }
    [DataContract]	
    public class SavedProxy
    {
        [DataMember]	
        public ABC.Model.Activity Activity { get; set; }

        [DataMember]
        public SavedButton Button { get; set; }

        [DataMember]
        public StoredSession Sessions { get; set; }

        [DataMember]
        public string Folder { get; set; }
    }
}
