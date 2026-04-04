using System;
using System.Threading;
using System.Threading.Tasks;
using AndrewDemo.NetConf2023.Core.Time;

namespace AndrewDemo.NetConf2023.Core
{
    public class WaitingRoomTicket
    {
        private static int _sn = 0;

        public int Id { get; private set; }
        private DateTime _created = DateTime.MinValue;
        private DateTime _released = DateTime.MinValue;
        private readonly TimeProvider _timeProvider;

        public WaitingRoomTicket(TimeProvider timeProvider)
        {
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
            this.Id = Interlocked.Increment(ref _sn);
            this._created = _timeProvider.GetLocalDateTime();

            Random random = new Random();
            this._released = this._created + TimeSpan.FromSeconds(random.Next(1, 3));

            //Console.WriteLine($"[waiting-room] issue ticket: {this.Id} @ {this._created} (estimate: {this._released})");
        }

        public async Task WaitUntilCanRunAsync()
        {
            var now = _timeProvider.GetLocalDateTime();
            if (now > this._released) return;
            await Task.Delay(this._released - now);
        }
    }
}
