namespace GoClient
{
    using System;
    using System.Timers;

    public class HeartBeat
    {
        private int interval;
        private int timeout;
        private LeafClient leafClient;
        private Timer timer;
        private DateTime lastTime;

        public HeartBeat(LeafClient lc, int interval)
        {
            leafClient = lc;
            this.interval = interval;
        }

        public void resetTimeout()
        {
            this.timeout = 0;
            lastTime = DateTime.Now;
        }

        public void sendHeartBeat(object source, ElapsedEventArgs e)
        {
            //-----------------------for Test-----------------------
            //var rand = new Random();
            //int x = rand.Next(0, 100);
            //UnityEngine.Debug.Log(x);
            //if(x>= 60)
            //{
            //    UnityEngine.Debug.Log("++++++++++++++++++++++++++++++++++++++++++");
            //    this.leafClient.Disconnect("random to stop");
            //    this.stop();
            //    return;
            //}
            //-----------------------for Test-----------------------

            //check timeout
            if (timeout > interval * 2)
            {
                this.leafClient.Disconnect("heart beat time out!");
                this.stop();
                return;
            }

            TimeSpan span = DateTime.Now - lastTime;
            timeout += (int)span.TotalMilliseconds;

            //Send heart beat
            this.leafClient.SendHeartBeat();
        }

        public void start()
        {
            if (interval < 1000) return;

            //start hearbeat
            this.timer = new Timer();
            timer.Interval = interval;
            timer.Elapsed += new ElapsedEventHandler(sendHeartBeat);
            timer.Enabled = true;

            //Set timeout
            timeout = 0;
            lastTime = DateTime.Now;
        }

        public void stop()
        {
            if (this.timer != null)
            {
                this.timer.Enabled = false;
                this.timer.Dispose();
            }
        }
    }
}
