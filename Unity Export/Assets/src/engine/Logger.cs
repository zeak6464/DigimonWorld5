using System;
using System.Runtime.InteropServices;

namespace Yukar.Engine
{
    public class Logger : SharpKmyBase.Logger
	{
		static Logger logger = null;
		static System.IO.Stream logfile = null;
		static System.IO.TextWriter tw = null;

        // ネイティブから呼ばれる用
		public override void output(uint type, string msg)
		{
#if DEBUG
            System.Diagnostics.Trace.Write(DateTime.Now.ToString("[HH:mm:ss:fff]") + msg);
#endif
			if (tw != null)
			{
                var now = DateTime.Now;
                tw.WriteLine(now.ToLongTimeString() + "." + now.Millisecond.ToString("000") + " : " + msg);
				tw.Flush();
			}
		}

        // C#から呼ぶ用
        public static void Put(string msg)
        {
#if DEBUG
            System.Diagnostics.Trace.WriteLine(DateTime.Now.ToString("[HH:mm:ss:fff]") + msg);
#endif
            if (tw != null)
            {
                var now = DateTime.Now;
                tw.WriteLine(now.ToLongTimeString() + "." + now.Millisecond.ToString("000") + " : " + msg);
                tw.Flush();
            }
        }

		public static void Initialize(bool isEngine, string dir = null)
		{
			logger = new Logger();
			try
			{
                logfile = System.IO.File.Open(
                    (dir != null ? dir : "") + "sgb" + (isEngine ? "p" : "t") + "log.txt",
                    System.IO.FileMode.Create, System.IO.FileAccess.Write);
				tw = new System.IO.StreamWriter(logfile);
			}
			catch( Exception )
			{
				logfile = null;
				tw = null;
				return;
			}
		}

		public static void finalize()
		{
			logger.Release();
			if(tw != null)tw.Close();
			if(logfile != null)logfile.Close();
		}

        public static void PutExceptionInfo(Exception exp)
        {
            Put(exp.Message + "\n" + exp.StackTrace);
        }
    }
}
