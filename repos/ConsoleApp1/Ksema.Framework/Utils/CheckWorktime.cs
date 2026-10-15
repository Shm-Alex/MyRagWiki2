using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ksema.Framework.Utils
{
    public static class CheckWorktime
    {
        private static readonly string _path = $"c:\\logs\\trackLog_{DateTime.Now:MMdd}.txt";
        private static int _laskId = -1;
        private static object _lockObj = new object();
        private static List<TrackingItem> _trackinTime = new List<TrackingItem>();

        public static int BeginTracking()
        {
            return BeginTracking(-1, string.Empty);
        }

        /// <param name="groupId">для группировки времени выполнения</param>
        /// <param name="description">описание группы</param>
        /// <returns>ид процесса, который нужен для использования в EndTracking для фиксации времени выполнения</returns>
        public static int BeginTracking(int groupId, string description)
        {
            lock (_lockObj)
            {
                _laskId++;
                _trackinTime.Add(new TrackingItem { Id = _laskId, Begin = DateTime.Now, End = null, Group = groupId, Decription = description});
            }

            return _laskId;
        }

        public static void EndTracking(int trackingId)
        {
            lock (_lockObj)
            {
                if (_trackinTime == null)
                    _trackinTime = new List<TrackingItem>();

                var track = _trackinTime.FirstOrDefault(x => x.Id == trackingId);

                if (track != null)
                    track.End = DateTime.Now;
            }
        }

        public static void WriteTrackingLog()
        {
            lock (_lockObj)
            {
                var text = new StringBuilder();
                foreach (var tracks in _trackinTime.GroupBy(x => x.Group))
                {
                    var seconds = tracks.Where(x => x.End.HasValue).Select(x => (x.End.Value - x.Begin).TotalSeconds).Sum();
                    text.AppendLine($"по группе {tracks.Key} ({tracks.First()?.Decription}) было затрачено {seconds} секунд");
                }

                text.AppendLine(" ---------------- ");
                text.AppendLine();
                
                File.AppendAllText(_path, text.ToString());

                _trackinTime = new List<TrackingItem>();
            }
        }
        
        public class TrackingItem
        {
            public int Id { get; set; }
            public string Decription { get; set; }
            public DateTime Begin { get; set; }
            public DateTime? End { get; set; }
            public int Group { get; set; }
        }
    }
}
