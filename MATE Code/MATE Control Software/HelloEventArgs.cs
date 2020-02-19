using System;

namespace Linda
{
    public class HelloEventArgs : EventArgs
    {
        private string mName;
        public string Name
        {
            get { return mName; }
        }

        public HelloEventArgs(string name)
        {
            mName = name;
        }
    }
}
