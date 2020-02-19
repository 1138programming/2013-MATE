using System;
using System.Threading;

namespace Linda
{
    public class Greeter : MarshalByRefObject
    {
        // Used by GUI to listen for SayHello calls
        public delegate void HelloEventHandler(object sender, HelloEventArgs e);
        public event HelloEventHandler HelloEvent;

        // Time in msec to wait until we respond to the Client
        private int mRespondTime;
        public int RespondTime
        {
            get { return mRespondTime; }
            set { mRespondTime = Math.Max(0, value); }
        }

        public Greeter()
        {
            // Default no argument constructor
        }

        public override Object InitializeLifetimeService()
        {
            // Allow this object to live "forever"
            return null;
        }

        public String SayHello(String name)
        {
            // Inform the GUI that SayHello was called
            HelloEvent(this, new HelloEventArgs(name));

            // Pretend we're computing something that takes a while
            Thread.Sleep(mRespondTime);

            return ("2012 MATE||Code: " + name);
        }
    }
}
